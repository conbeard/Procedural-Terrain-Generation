using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator {

    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve curve, int levelOfDetail) {
        AnimationCurve heightCurve = new AnimationCurve(curve.keys);
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        float topLeftX = (width - 1) / -2.0f;
        float topLeftY = (height - 1) / 2.0f;

        int simplificationIncrement = levelOfDetail == 0 ? 1 : levelOfDetail * 2;
        int verticiesPerLine = (width - 1) / simplificationIncrement + 1;

        MeshData meshData = new MeshData(verticiesPerLine, verticiesPerLine);
        int vertexIndex = 0;

        for (int y = 0; y < height; y += simplificationIncrement) {
            for (int x = 0; x < width; x += simplificationIncrement) {
                meshData.verticies[vertexIndex] = new Vector3(topLeftX + x, heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier, topLeftY - y);
                meshData.uvs[vertexIndex] = new Vector2(x / (float) width, y / (float) height);

                if (x < width - 1 && y < height - 1) {
                    meshData.AddTriangle(vertexIndex, vertexIndex + verticiesPerLine + 1, vertexIndex + verticiesPerLine);
                    meshData.AddTriangle(vertexIndex + verticiesPerLine + 1, vertexIndex, vertexIndex + 1);
                }

                vertexIndex++;
            }
        }

        return meshData;
    }
}

public class MeshData {
    public Vector3[] verticies;
    public int[] indicies;
    public Vector2[] uvs;

    int triangleIndex = 0;
    
    public MeshData(int width, int height) {
        verticies = new Vector3[width * height];
        uvs = new Vector2[width * height];
        indicies = new int[(width - 1) * (height - 1) * 6];
    }

    public void AddTriangle(int a, int b, int c) {
        indicies[triangleIndex] = a;
        indicies[triangleIndex + 1] = b;
        indicies[triangleIndex + 2] = c;
        triangleIndex += 3;
    }

    public Mesh CreateMesh() {
        Mesh mesh = new Mesh();
        mesh.vertices = verticies;
        mesh.triangles = indicies;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        return mesh;
    }

}
