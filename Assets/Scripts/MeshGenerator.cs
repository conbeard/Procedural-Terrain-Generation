using System.Collections;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using UnityEngine;

public static class MeshGenerator {

    public const int numSupoortedLODs = 5;
    public const int numSupportedChunkSizes = 9;
    public const int numSupportedFlatShadedChunkSizes = 3;
    public static readonly int[] supportedChunkSizes = {48, 72, 96, 120, 144, 168, 192, 216, 240};
    public static readonly int[] supportedFlatShadedChunkSizes = {48, 72, 96};

    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve curve, int levelOfDetail, bool useFlatShading) {
        AnimationCurve heightCurve = new AnimationCurve(curve.keys);
        
        int simplificationIncrement = levelOfDetail == 0 ? 1 : levelOfDetail * 2;
        
        int borderedSize = heightMap.GetLength(0);
        int meshSize = borderedSize - 2 * simplificationIncrement;
        int meshSizeUnsimplified = borderedSize - 2;
        float topLeftX = (meshSizeUnsimplified - 1) / -2.0f;
        float topLeftY = (meshSizeUnsimplified - 1) / 2.0f;

        int verticiesPerLine = (meshSize - 1) / simplificationIncrement + 1;

        MeshData meshData = new MeshData(verticiesPerLine, useFlatShading);
       
        int[] vertexIndicesMap = new int[borderedSize * borderedSize];

        int meshIndex = 0;
        int borderIndex = -1;


        for (int y = 0; y < borderedSize; y += simplificationIncrement) {
            for (int x = 0; x < borderedSize; x += simplificationIncrement) {
                if (y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1) {
                    vertexIndicesMap[x + y * borderedSize] = borderIndex--;
                } else {
                    vertexIndicesMap[x + y * borderedSize] = meshIndex++;
                }
                
                
            }
        }

        for (int y = 0; y < borderedSize; y += simplificationIncrement) {
            for (int x = 0; x < borderedSize; x += simplificationIncrement) {
                int vertexIndex = vertexIndicesMap[x + y * borderedSize];
                Vector2 percent = new Vector2((x - simplificationIncrement) / (float) meshSize, (y - simplificationIncrement) / (float) meshSize);
                float height = heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier;
                Vector3 vertexPosition = new Vector3(topLeftX + percent.x * meshSizeUnsimplified, height, topLeftY - percent.y * meshSizeUnsimplified);

                meshData.AddVertex(vertexPosition, percent, vertexIndex);
                
                if (x < borderedSize - 1 && y < borderedSize - 1) {
                    int a = vertexIndicesMap[x + y * borderedSize];
                    int b = vertexIndicesMap[x + simplificationIncrement + y * borderedSize];
                    int c = vertexIndicesMap[x + (y + simplificationIncrement) * borderedSize];
                    int d = vertexIndicesMap[x + simplificationIncrement + (y + simplificationIncrement) * borderedSize];
                    
                    meshData.AddTriangle(a, d, c);
                    meshData.AddTriangle(d, a, b);
                }
            }
        }

        meshData.Finish();
        return meshData;
    }
}

public class MeshData {
    Vector3[] verticies;
    int[] triangles;
    Vector2[] uvs;
    Vector3[] bakedNormals;

    Vector3[] borderVerticies;
    int[] borderTriangles;

    int triangleIndex = 0;
    int borderTriangleIndex = 0;

    private bool _useFlatShading;
    
    public MeshData(int verticesPerLine, bool useFlatShading) {
        _useFlatShading = useFlatShading;
        
        verticies = new Vector3[verticesPerLine * verticesPerLine];
        uvs = new Vector2[verticesPerLine * verticesPerLine];
        triangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6];
        
        borderVerticies = new Vector3[4 + 4 * verticesPerLine];
        borderTriangles = new int[24 * verticesPerLine];
    }

    public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex) {
        if (vertexIndex < 0) {
            borderVerticies[-vertexIndex - 1] = vertexPosition;
        } else {
            verticies[vertexIndex] = vertexPosition;
            uvs[vertexIndex] = uv;
        }
    }

    public void AddTriangle(int a, int b, int c) {
        if (a < 0 || b < 0 || c < 0) {
            borderTriangles[borderTriangleIndex] = a;
            borderTriangles[borderTriangleIndex + 1] = b;
            borderTriangles[borderTriangleIndex + 2] = c;
            borderTriangleIndex += 3;
        } else {
            triangles[triangleIndex] = a;
            triangles[triangleIndex + 1] = b;
            triangles[triangleIndex + 2] = c;
            triangleIndex += 3;
        }
    }

    Vector3[] CalculateNormals() {
        Vector3[] vertexNormals = new Vector3[verticies.Length];
        int triangleCount = triangles.Length / 3;

        for (int i = 0; i < triangleCount; i++) {
            int normalTriangleIndex = i * 3;
            int vertexA = triangles[normalTriangleIndex];
            int vertexB = triangles[normalTriangleIndex + 1];
            int vertexC = triangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndicies(vertexA, vertexB, vertexC);
            vertexNormals[vertexA] += triangleNormal;
            vertexNormals[vertexB] += triangleNormal;
            vertexNormals[vertexC] += triangleNormal;
        }
        
        int borderTriangleCount = borderTriangles.Length / 3;

        for (int i = 0; i < borderTriangleCount; i++) {
            int normalTriangleIndex = i * 3;
            int vertexA = borderTriangles[normalTriangleIndex];
            int vertexB = borderTriangles[normalTriangleIndex + 1];
            int vertexC = borderTriangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndicies(vertexA, vertexB, vertexC);
            if (vertexA >= 0)
                vertexNormals[vertexA] += triangleNormal;
            if (vertexB >= 0)
                vertexNormals[vertexB] += triangleNormal;
            if (vertexC >= 0)
                vertexNormals[vertexC] += triangleNormal;
        }

        for (int i = 0; i < vertexNormals.Length; i++) {
            vertexNormals[i].Normalize();
        }

        return vertexNormals;
    }

    Vector3 SurfaceNormalFromIndicies(int indexA, int indexB, int indexC) {
        Vector3 pointA = indexA < 0 ? borderVerticies[-indexA - 1] : verticies[indexA];
        Vector3 pointB = indexB < 0 ? borderVerticies[-indexB - 1] : verticies[indexB];
        Vector3 pointC = indexC < 0 ? borderVerticies[-indexC - 1] : verticies[indexC];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;
        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    public void Finish() {
        if (_useFlatShading)
            FlatShading();
        else
            BakeNormals();
    }

    private void BakeNormals() {
        bakedNormals = CalculateNormals();
    }

    void FlatShading() {
        Vector3[] flatShadedVerticies = new Vector3[triangles.Length];
        Vector2[] flatShadedUvs = new Vector2[triangles.Length];

        for (int i = 0; i < triangles.Length; i++) {
            flatShadedVerticies[i] = verticies[triangles[i]];
            flatShadedUvs[i] = flatShadedUvs[triangles[i]];
            triangles[i] = i;
        }

        verticies = flatShadedVerticies;
        uvs = flatShadedUvs;
    }

    public Mesh CreateMesh() {
        Mesh mesh = new Mesh();
        mesh.vertices = verticies;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        if (_useFlatShading)
            mesh.RecalculateNormals();
        else
            mesh.normals = bakedNormals;
        return mesh;
    }

}
