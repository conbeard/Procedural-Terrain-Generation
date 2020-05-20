using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class InfiniteTerrain : MonoBehaviour {

    const float scale = 1f;

    const float moveForUpdateThreshold = 25f * 25f;
    
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
    static List<TerrainChunk> _chunksVisibleLastUpdate = new List<TerrainChunk>();

    void Start() {
        _mapGenerator = GetComponent<MapGenerator>();
        _mapGenerator.normalizeMode = Noise.NormalizeMode.Global;

        maxViewDistance = detailLevels[detailLevels.Length - 1].visibleDistance;
        chunkSize = MapGenerator.chunkSize - 1;
        chunksInViewDistance = Mathf.CeilToInt(maxViewDistance / chunkSize);
        
        UpdateVisibleChunks();
    }

    void Update() {
        Vector3 position = viewer.position / scale;
        viewerPos = new Vector2(position.x, position.z);
        if ((viewerPos - prevViewerPos).sqrMagnitude > moveForUpdateThreshold) {
            UpdateVisibleChunks();
            prevViewerPos = viewerPos;
        }
    }
 
    void UpdateVisibleChunks() {

        for (int i = 0; i < _chunksVisibleLastUpdate.Count; i++) {
            _chunksVisibleLastUpdate[i].SetVisible(false);
        }
        
        _chunksVisibleLastUpdate.Clear();
        
        int currentChunkX = Mathf.RoundToInt(viewerPos.x / chunkSize);
        int currentChunkY = Mathf.RoundToInt(viewerPos.y / chunkSize);

        for (int yOffset = -chunksInViewDistance; yOffset <= chunksInViewDistance; yOffset++) {
            for (int xOffset = -chunksInViewDistance; xOffset <= chunksInViewDistance; xOffset++) {
                Vector2 viewChunkCoord = new Vector2(currentChunkX + xOffset, currentChunkY + yOffset);

                if (!_chunks.ContainsKey(viewChunkCoord)) {
                    _chunks[viewChunkCoord] = new TerrainChunk(viewChunkCoord, chunkSize, detailLevels, transform, material);
                } else {
                    _chunks[viewChunkCoord].UpdateTerrainChunk();
                }
            }
        }
    }
    
    public class TerrainChunk {

        GameObject meshObject;
        Vector2 position;
        Bounds _bounds;
        
        LODInfo[] _detailLevels;
        LODMesh[] _detailMeshes;
        int _prevLODIndex = -1;

        MapData _mapData;
        bool _mapDataReceived;

        MeshRenderer _meshRenderer;
        MeshFilter _meshFilter;
        
        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material) {
            _detailLevels = detailLevels;
            position = coord * size;
            _bounds = new Bounds(position, Vector2.one * size);
            Vector3 position3 = new Vector3(position.x, 0, position.y);
            
            meshObject = new GameObject("Terrain Chunk");
            _meshRenderer = meshObject.AddComponent<MeshRenderer>();
            _meshFilter = meshObject.AddComponent<MeshFilter>();
            _meshRenderer.material = material;
            
            meshObject.transform.position = position3 * scale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * scale;
            SetVisible(false);
            
            _detailMeshes = new LODMesh[_detailLevels.Length];
            for (int i = 0; i < _detailMeshes.Length; i++) {
                _detailMeshes[i] = new LODMesh(_detailLevels[i].lod, UpdateTerrainChunk);
            }
            
            _mapGenerator.RequestMapData(position, OnMapDataReceived);
        }

        void OnMapDataReceived(MapData mapData) {
            _mapData = mapData;
            _mapDataReceived = true;

            Texture2D texture = TextureGenerator.TextureFromColorMap(
                mapData.ColorMap, MapGenerator.chunkSize, MapGenerator.chunkSize
            );

            _meshRenderer.material.mainTexture = texture;
            
            UpdateTerrainChunk();
        }

        public void UpdateTerrainChunk() {
            if (!_mapDataReceived) return;
            
            float distanceToPlayer = Mathf.Sqrt(_bounds.SqrDistance(viewerPos));
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
                
                _chunksVisibleLastUpdate.Add(this);
            }
            
            SetVisible(visible);
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
        Action updateCallback;

        public LODMesh(int levelOfDetail, Action updateCallback) {
            lod = levelOfDetail;
            this.updateCallback = updateCallback;
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
        public int lod;
        public float visibleDistance;
    }
}
