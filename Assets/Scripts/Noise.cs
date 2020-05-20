using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise  {
    
    public enum NormalizeMode {
        Local, Global
    }

    public static float[,] GenerateNoiseMap(int width, int height, int seed, float scale, int octaves, float persistence, float lacunarity, Vector2 offset, NormalizeMode normalizeMode) {
        float[,] noiseMap = new float[width, height];
        
        System.Random prgn = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        float maxHeight = 0;
        float amplitude = 1;
        float frequency = 1;

        for (int i = 0; i < octaves; i++) {
            float offsetX = prgn.Next(-100000, 100000) + offset.x;
            float offsetY = prgn.Next(-100000, 100000) - offset.y;
            
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxHeight += amplitude;
            amplitude *= persistence;
        }

        if (scale <= 0) {
            scale = 0.0001f;
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        float halfWidth = width / 2.0f;
        float halfHeight = height / 2.0f;

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;
                
                for (int i = 0; i < octaves; i++) {
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxNoiseHeight)
                    maxNoiseHeight = noiseHeight;
                if (noiseHeight < minNoiseHeight)
                    minNoiseHeight = noiseHeight;

                noiseMap[x, y] = noiseHeight;
            }
        }

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                if (normalizeMode == NormalizeMode.Local) {
                    noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
                } else if (normalizeMode == NormalizeMode.Global) {
                    float normalizedHeight = (noiseMap[x, y] + 1) / (2f * maxHeight / 1.75f);
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, float.MaxValue);
                }
            }
        }

        return noiseMap;
    }
    
}
