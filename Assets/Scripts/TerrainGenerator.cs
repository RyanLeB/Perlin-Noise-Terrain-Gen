using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public int width = 256;
    public int height = 256;
    public float scale = 20f;

    void Start()
    {
        GenerateTerrain();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GenerateTerrain();
        }
    }

    void GenerateTerrain()
    {
        float[,] heightMap = GenerateHeightMap();
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = GenerateMesh(heightMap);
    }

    float[,] GenerateHeightMap()
    {
        float[,] heightMap = new float[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float xCoord = (float)x / width * scale;
                float yCoord = (float)y / height * scale;
                heightMap[x, y] = Mathf.PerlinNoise(xCoord, yCoord);
            }
        }

        return heightMap;
    }

    Mesh GenerateMesh(float[,] heightMap)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[width * height];
        int[] triangles = new int[(width - 1) * (height - 1) * 6];
        Vector2[] uv = new Vector2[width * height];

        int vertIndex = 0;
        int triIndex = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                vertices[vertIndex] = new Vector3(x, heightMap[x, y] * 50, y); // Increased height scale
                uv[vertIndex] = new Vector2((float)x / width, (float)y / height);

                if (x < width - 1 && y < height - 1)
                {
                    triangles[triIndex + 0] = vertIndex + 0;
                    triangles[triIndex + 1] = vertIndex + width + 1;
                    triangles[triIndex + 2] = vertIndex + width + 0;
                    triangles[triIndex + 3] = vertIndex + 0;
                    triangles[triIndex + 4] = vertIndex + 1;
                    triangles[triIndex + 5] = vertIndex + width + 1;
                    triIndex += 6;
                }

                vertIndex++;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();

        return mesh;
    }
}