using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class MapGenerator : MonoBehaviour {
    
    public enum DrawMode {
        NoiseMap, ColorMap, Mesh
    }

    public DrawMode drawMode;
    public Noise.NormalizeMode normalizeMode;

    public const int chunkSize = 241;
    [Range(0, 6)]
    public int editorLOD;
    public float noiseScale;

    public int octaves;
    [Range(0, 1)]
    public float persistence;
    public float lacunarity;
    public float heightMultiplier;
    public AnimationCurve heightCurve;

    public int seed;
    public Vector2 offset;

    public bool autoUpdate = true;

    public TerrainType[] regions;
    
    Queue<MapThreadInfo<MapData>> _mapDataQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> _meshDataQueue = new Queue<MapThreadInfo<MeshData>>();

    public void DrawMapInEditor() {
        MapData mapData = GenerateMapData(Vector2.zero);
        MapDisplay mapDisplay = GetComponent<MapDisplay>();
        if (drawMode == DrawMode.NoiseMap)
            mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.HeightMap));
        else if (drawMode == DrawMode.ColorMap)
            mapDisplay.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.ColorMap, chunkSize, chunkSize));
        else if (drawMode == DrawMode.Mesh)
            mapDisplay.DrawMesh(
                MeshGenerator.GenerateTerrainMesh(mapData.HeightMap, heightMultiplier, heightCurve, editorLOD),
                TextureGenerator.TextureFromColorMap(mapData.ColorMap, chunkSize, chunkSize)
            );
    }

    public void RequestMapData(Vector2 center, Action<MapData> callback) {
        ThreadStart threadStart = delegate {
            MapDataThread(center, callback);
        };
        
        new Thread(threadStart).Start();
    }

    void MapDataThread(Vector2 center, Action<MapData> callback) {
        MapData mapData = GenerateMapData(center);
        lock (_mapDataQueue) {
            _mapDataQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback) {
        ThreadStart threadStart = delegate {
            MeshDataThread(mapData, lod, callback);
        };
        
        new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback) {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.HeightMap, heightMultiplier, heightCurve, lod);
        lock (_meshDataQueue) {
            _meshDataQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    void Update() {
        lock (_mapDataQueue) {
            if (_mapDataQueue.Count > 0) {
                for (int i = 0; i < _mapDataQueue.Count; i++) {
                    MapThreadInfo<MapData> threadInfo = _mapDataQueue.Dequeue();
                    threadInfo.callback(threadInfo.parameter);
                }
            }
        }

        lock (_meshDataQueue) {
            if (_meshDataQueue.Count > 0) {
                for (int i = 0; i < _meshDataQueue.Count; i++) {
                    MapThreadInfo<MeshData> threadInfo = _meshDataQueue.Dequeue();
                    threadInfo.callback(threadInfo.parameter);
                }
            }
        }
    }

    MapData GenerateMapData(Vector2 center) {
        float[,] noiseMap = Noise.GenerateNoiseMap(chunkSize, chunkSize, seed, noiseScale, octaves, persistence,
            lacunarity, center + offset, normalizeMode);

        Color[] colorMap = new Color[chunkSize * chunkSize];
        for (int y = 0; y < chunkSize; y++) {
            for (int x = 0; x < chunkSize; x++) {
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < regions.Length; i++) {
                    lock (heightCurve) {
                        if (heightCurve.Evaluate(currentHeight) >= regions[i].height) {
                            colorMap[x + y * chunkSize] = regions[i].color;
                        } else {
                            break;
                        }
                    }
                }
            }
        }
        
        return new MapData(noiseMap, colorMap);
    }

    void OnValidate() {
        if (lacunarity < 1) lacunarity = 1;
        if (octaves < 0) octaves = 0;
    }

    struct MapThreadInfo<T> {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter) {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}

[System.Serializable]
public struct TerrainType {
    public string name;
    public float height;
    public Color color;
}

public struct MapData {
    public readonly float[,] HeightMap;
    public readonly Color[] ColorMap;

    public MapData(float[,] heightMap, Color[] colorMap) {
        HeightMap = heightMap;
        ColorMap = colorMap;
    }
}
