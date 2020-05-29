using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class InfiniteTerrain : MonoBehaviour {


    const float moveForUpdateThreshold = 25f * 25f;
    private const float colliderGenerationDistanceThreshold = 5.0f;

    public int colliderLODIndex;
    public LODInfo[] detailLevels;
    public static float maxViewDistance;
    
    public Transform viewer;
    public Material material;

    public static Vector2 viewerPos;
    Vector2 prevViewerPos = Vector2.zero;
    static MapGenerator _mapGenerator;
    int chunkSize;
    int chunksInViewDistance;

    Dictionary<Vector2, TerrainChunk> _chunks = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> _visibleTerrainChunks = new List<TerrainChunk>();

    void Start() {
        _mapGenerator = GetComponent<MapGenerator>();
        _mapGenerator.noiseData.normalizeMode = Noise.NormalizeMode.Global;

        maxViewDistance = detailLevels[detailLevels.Length - 1].visibleDistance;
        chunkSize = _mapGenerator.chunkSize - 1;
        chunksInViewDistance = Mathf.CeilToInt(maxViewDistance / chunkSize);
        
        UpdateVisibleChunks();
    }

    void Update() {
        Vector3 position = viewer.position / _mapGenerator.terrainData.scale;
        viewerPos = new Vector2(position.x, position.z);

        if (viewerPos != prevViewerPos) {
            foreach (TerrainChunk chunk in _visibleTerrainChunks) {
                chunk.UpdateCollisionMesh();
            }
        }
        
        if ((viewerPos - prevViewerPos).sqrMagnitude > moveForUpdateThreshold) {
            UpdateVisibleChunks();
            prevViewerPos = viewerPos;
        }
    }
 
    void UpdateVisibleChunks() {
        HashSet<Vector2> updatedChunks = new HashSet<Vector2>();
        for (int i = _visibleTerrainChunks.Count - 1; i >= 0; i--) {
            _visibleTerrainChunks[i].UpdateTerrainChunk();
            updatedChunks.Add(_visibleTerrainChunks[i].coord);
        }
        
        int currentChunkX = Mathf.RoundToInt(viewerPos.x / chunkSize);
        int currentChunkY = Mathf.RoundToInt(viewerPos.y / chunkSize);

        for (int yOffset = -chunksInViewDistance; yOffset <= chunksInViewDistance; yOffset++) {
            for (int xOffset = -chunksInViewDistance; xOffset <= chunksInViewDistance; xOffset++) {
                Vector2 viewChunkCoord = new Vector2(currentChunkX + xOffset, currentChunkY + yOffset);

                if (!updatedChunks.Contains(viewChunkCoord)) {
                    if (!_chunks.ContainsKey(viewChunkCoord)) {
                        _chunks[viewChunkCoord] = new TerrainChunk(viewChunkCoord, chunkSize, detailLevels,
                            colliderLODIndex, transform, material);
                    } else {
                        _chunks[viewChunkCoord].UpdateTerrainChunk();
                    }
                }
            }
        }
    }
    
    public class TerrainChunk {
        public Vector2 coord;
        GameObject meshObject;
        Vector2 position;
        Bounds _bounds;
        
        LODInfo[] _detailLevels;
        LODMesh[] _detailMeshes;
        private int _colliderLODIndex;
        
        int _prevLODIndex = -1;

        MapData _mapData;
        bool _mapDataReceived;

        MeshRenderer _meshRenderer;
        MeshFilter _meshFilter;
        MeshCollider _meshCollider;

        private bool hasSetCollider;
        
        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Material material) {
            this.coord = coord;
            _detailLevels = detailLevels;
            _colliderLODIndex = colliderLODIndex;
            position = coord * size;
            _bounds = new Bounds(position, Vector2.one * size);
            
            Vector3 position3 = new Vector3(position.x, 0, position.y);
            
            meshObject = new GameObject("Terrain Chunk");
            _meshRenderer = meshObject.AddComponent<MeshRenderer>();
            _meshFilter = meshObject.AddComponent<MeshFilter>();
            _meshCollider = meshObject.AddComponent<MeshCollider>();
            _meshRenderer.material = material;
            
            meshObject.transform.position = position3 * _mapGenerator.terrainData.scale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * _mapGenerator.terrainData.scale;
            SetVisible(false);
            
            _detailMeshes = new LODMesh[_detailLevels.Length];
            for (int i = 0; i < _detailMeshes.Length; i++) {
                _detailMeshes[i] = new LODMesh(_detailLevels[i].lod);
                _detailMeshes[i].updateCallback += UpdateTerrainChunk;
                if (i == colliderLODIndex)
                    _detailMeshes[i].updateCallback += UpdateCollisionMesh;
            }
            
            _mapGenerator.RequestMapData(position, OnMapDataReceived);
        }

        void OnMapDataReceived(MapData mapData) {
            _mapData = mapData;
            _mapDataReceived = true;
            UpdateTerrainChunk();
        }

        public void UpdateTerrainChunk() {
            if (!_mapDataReceived) return;
            
            float distanceToPlayer = Mathf.Sqrt(_bounds.SqrDistance(viewerPos));

            bool wasVisible = IsVisible();
            bool visible = distanceToPlayer <= maxViewDistance;

            if (visible) {
                int lodIndex = 0;

                for (int i = 0; i < _detailLevels.Length - 1; i++) {
                    if (distanceToPlayer > _detailLevels[i].visibleDistance) 
                        lodIndex = i + 1;
                    else 
                        break;
                }

                if (lodIndex != _prevLODIndex) {
                    LODMesh lodMesh = _detailMeshes[lodIndex];
                    if (lodMesh.hasMesh) {
                        _meshFilter.mesh = lodMesh.mesh;
                        _prevLODIndex = lodIndex;
                    } else if (!lodMesh.hasRequestedMesh) {
                        lodMesh.RequestMesh(_mapData);
                    }
                }
            }

            if (wasVisible != visible) {
                if (visible) {
                    _visibleTerrainChunks.Add(this);
                } else {
                    _visibleTerrainChunks.Remove(this);
                }
                SetVisible(visible);
            }
        }

        public void UpdateCollisionMesh() {
            if (hasSetCollider) return;
            
            float sqrDistViewerToEdge = _bounds.SqrDistance(viewerPos);

            if (sqrDistViewerToEdge < _detailLevels[_colliderLODIndex].sqrVisibleDistance) {
                if (!_detailMeshes[_colliderLODIndex].hasRequestedMesh) {
                    _detailMeshes[_colliderLODIndex].RequestMesh(_mapData);
                }
            }
            
            if (sqrDistViewerToEdge <= colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold) {
                if (_detailMeshes[_colliderLODIndex].hasMesh) {
                    _meshCollider.sharedMesh = _detailMeshes[_colliderLODIndex].mesh;
                    hasSetCollider = true;
                }
            }
        }

        public void SetVisible(bool visible) {
            meshObject.SetActive(visible);
        }

        public bool IsVisible() {
            return meshObject.activeSelf;
        }
        
    }

    public class LODMesh {

        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;
        public event System.Action updateCallback;

        public LODMesh(int levelOfDetail) {
            lod = levelOfDetail;
        }

        public void RequestMesh(MapData mapData) {
            hasRequestedMesh = true;
            _mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
        }

        void OnMeshDataReceived(MeshData meshData) {
            mesh = meshData.CreateMesh();
            hasMesh = true;
            updateCallback();
        }

    }
    
    [System.Serializable]
    public struct LODInfo {
        [Range(0, MeshGenerator.numSupoortedLODs - 1)]
        public int lod;
        public float visibleDistance;

        public float sqrVisibleDistance {
            get => visibleDistance * visibleDistance;
        }
    }
}
