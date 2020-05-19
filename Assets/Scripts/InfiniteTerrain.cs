using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class InfiniteTerrain : MonoBehaviour {

    public const float maxViewDistance = 500;
    public Transform viewer;

    public static Vector2 viewerPos;
    int chunkSize;
    int chunksInViewDistance;

    Dictionary<Vector2, TerrainChunk> _chunks = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> _chunksVisibleLastUpdate = new List<TerrainChunk>();

    void Start() {
        chunkSize = MapGenerator.chunkSize - 1;
        chunksInViewDistance = Mathf.CeilToInt(maxViewDistance / chunkSize);
    }

    void Update() {
        Vector3 position = viewer.position;
        viewerPos = new Vector2(position.x, position.z);
        UpdateVisibleChunks();
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
                    _chunks[viewChunkCoord] = new TerrainChunk(viewChunkCoord, chunkSize, transform);
                } else {
                    _chunks[viewChunkCoord].UpdateTerrainChunk();
                }
                
                if (_chunks[viewChunkCoord].IsVisible()) {
                    _chunksVisibleLastUpdate.Add(_chunks[viewChunkCoord]);
                }
            }
        }
    }
    
    public class TerrainChunk {

        GameObject meshObject;
        Vector2 position;
        Bounds _bounds;
        
        public TerrainChunk(Vector2 coord, int size, Transform parent) {
            position = coord * size;
            _bounds = new Bounds(position, Vector2.one * size);
            Vector3 position3 = new Vector3(position.x, 0, position.y);
            
            meshObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            meshObject.transform.position = position3;
            meshObject.transform.localScale = Vector3.one * size / 10.0f;
            meshObject.transform.parent = parent;
            SetVisible(false);
        }

        public void UpdateTerrainChunk() {
            float distanceToPlayer = Mathf.Sqrt(_bounds.SqrDistance(viewerPos));
            bool visible = distanceToPlayer <= maxViewDistance;
            SetVisible(visible);
        }

        public void SetVisible(bool visible) {
            meshObject.SetActive(visible);
        }

        public bool IsVisible() {
            return meshObject.activeSelf;
        }
        
    }
}
