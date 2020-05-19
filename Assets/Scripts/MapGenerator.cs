using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour {
    
    public enum DrawMode {
        NoiseMap, ColorMap, Mesh
    }

    public DrawMode drawMode;

    public const int chunkSize = 241;
    [Range(0, 6)]
    public int levelOfDetail;
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

    public void GenerateMap() {
        float[,] noiseMap = Noise.GenerateNoiseMap(chunkSize, chunkSize, seed, noiseScale, octaves, persistence,
            lacunarity, offset);

        Color[] colorMap = new Color[chunkSize * chunkSize];
        for (int y = 0; y < chunkSize; y++) {
            for (int x = 0; x < chunkSize; x++) {
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < regions.Length; i++) {
                    if (heightCurve.Evaluate(currentHeight) <= regions[i].height) {
                        colorMap[x + y * chunkSize] = regions[i].color;
                        break;
                    }
                }
            }
        }

        MapDisplay mapDisplay = GetComponent<MapDisplay>();
        if (drawMode == DrawMode.NoiseMap)
            mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
        else if (drawMode == DrawMode.ColorMap)
            mapDisplay.DrawTexture(TextureGenerator.TextureFromColorMap(colorMap, chunkSize, chunkSize));
        else if (drawMode == DrawMode.Mesh)
            mapDisplay.DrawMesh(
                MeshGenerator.GenerateTerrainMesh(noiseMap, heightMultiplier, heightCurve, levelOfDetail),
                TextureGenerator.TextureFromColorMap(colorMap, chunkSize, chunkSize)
            );
    }

    void OnValidate() {
        if (lacunarity < 1) lacunarity = 1;
        if (octaves < 0) octaves = 0;
    }
}

[System.Serializable]
public struct TerrainType {
    public string name;
    public float height;
    public Color color;
}