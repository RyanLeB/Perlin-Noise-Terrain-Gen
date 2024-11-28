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
        ConfigureFog();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GenerateTerrain();
        }
    
        SimulateWeather();
    }

    void ConfigureFog()
    {
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.7f, 0.7f, 0.7f); 
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.1f; 
    }
    
    
    void GenerateTerrain()
    {
        float[,] heightMap = GenerateHeightMap();
        ApplyErosion(heightMap);
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

        meshFilter.mesh = GenerateMesh(heightMap);

        Material terrainMaterial = Resources.Load<Material>("Materials/TerrainMaterial");
        if (terrainMaterial != null)
        {
            meshRenderer.material = terrainMaterial;
        }
        else
        {
            Debug.LogError("Terrain material not found!");
        }
        
    }

    float[,] GenerateHeightMap()
    {
        float[,] heightMap = new float[width, height];
        float offsetX = Random.Range(0f, 9999f);
        float offsetY = Random.Range(0f, 9999f);

        int octaves = 4; 
        float persistence = 0.3f;
        float lacunarity = 2.0f;
        float scale = 10f; 

        float minNoiseHeight = float.MaxValue;
        float maxNoiseHeight = float.MinValue;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++)
                {
                    float xCoord = (float)x / width * scale * frequency + offsetX;
                    float yCoord = (float)y / height * scale * frequency + offsetY;
                    float perlinValue = Mathf.PerlinNoise(xCoord, yCoord) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxNoiseHeight)
                {
                    maxNoiseHeight = noiseHeight;
                }
                if (noiseHeight < minNoiseHeight)
                {
                    minNoiseHeight = noiseHeight;
                }

                heightMap[x, y] = noiseHeight;
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                heightMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, heightMap[x, y]);
            }
        }

        return SmoothHeightMap(heightMap);
    }

    float[,] SmoothHeightMap(float[,] heightMap)
    {
        float[,] smoothedHeightMap = new float[width, height];

        for (int x = 2; x < width - 2; x++)
        {
            for (int y = 2; y < height - 2; y++)
            {
                float totalHeight = 0f;
                int count = 0;

                for (int i = -2; i <= 2; i++)
                {
                    for (int j = -2; j <= 2; j++)
                    {
                        totalHeight += heightMap[x + i, y + j];
                        count++;
                    }
                }

                smoothedHeightMap[x, y] = totalHeight / count;
            }
        }

        return smoothedHeightMap;
    }


    Color GetColorForHeight(float heightValue)
    {
        if (heightValue > 0.8f)
        {
            return new Color(0.8f, 0.8f, 0.8f); // ---- Light gray for snow ----
        }
        else if (heightValue > 0.6f)
        {
            return new Color(0.5f, 0.5f, 0.5f); // ---- Gray for rock ----
        }
        else if (heightValue > 0.4f)
        {
            return new Color(0.2f, 0.6f, 0.2f); // ---- Dark green for grass ----
        }
        else if (heightValue > 0.2f)
        {
            return new Color(0.6f, 0.5f, 0.2f); // ---- Brown for sand ----
        }
        else
        {
            return new Color(0.2f, 0.4f, 0.6f); // ---- Dark blue for water ----
        }
    }

    Mesh GenerateMesh(float[,] heightMap)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[width * height];
        int[] triangles = new int[(width - 1) * (height - 1) * 6];
        Vector2[] uv = new Vector2[width * height];
        Color[] colors = new Color[width * height];

        int vertIndex = 0;
        int triIndex = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float heightValue = heightMap[x, y];
                vertices[vertIndex] = new Vector3(x, heightValue * 50, y);
                uv[vertIndex] = new Vector2((float)x / width, (float)y / height);

                colors[vertIndex] = GetColorForHeight(heightValue);

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
        mesh.colors = colors;
        mesh.RecalculateNormals();

        return mesh;
    }
    
    
    void SimulateWeather()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            StartCoroutine(RainEffect());
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            StartCoroutine(SnowEffect());
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            ApplyWindEffect();
        }
    }
    
    IEnumerator RainEffect()
    {
        ParticleSystem rain = Resources.Load<ParticleSystem>("Effects/Rain");
    
        if (rain == null)
        {
            Debug.LogError("Rain particle system not found in Resources/Effects");
            yield break;
        }
    
        float terrainHeight = GetTerrainHeight(width / 2, height / 2);
        Vector3 centerPosition = new Vector3(width / 2, terrainHeight + 50, -20);
        ParticleSystem rainInstance = Instantiate(rain, centerPosition, Quaternion.identity);
    
        
        var renderer = rainInstance.GetComponent<ParticleSystemRenderer>();
        Material rainMaterial = Resources.Load<Material>("Materials/RainMaterial");
        if (rainMaterial != null)
        {
            renderer.material = rainMaterial;
        }
        else
        {
            Debug.LogError("Rain material not found!");
        }
    
        
        var main = rainInstance.main;
        main.startColor = new Color(0.5f, 0.5f, 1f, 0.5f); 
    
        rainInstance.Play();
        yield return new WaitForSeconds(10);
        rainInstance.Stop();
        Destroy(rainInstance.gameObject);
    }
    
    IEnumerator SnowEffect()
    {
        ParticleSystem snow = Resources.Load<ParticleSystem>("Effects/Snow");
    
        if (snow == null)
        {
            Debug.LogError("Snow particle system not found in Resources/Effects");
            yield break;
        }
    
        float terrainHeight = GetTerrainHeight(width / 2, height / 2);
        Vector3 centerPosition = new Vector3(width / 2, terrainHeight + 50, -20);
        ParticleSystem snowInstance = Instantiate(snow, centerPosition, Quaternion.identity);
        snowInstance.Play();
        yield return new WaitForSeconds(10);
        snowInstance.Stop();
        Destroy(snowInstance.gameObject);
    }
    
    float GetTerrainHeight(int x, int z)
    {
        float[,] heightMap = GenerateHeightMap();
        return heightMap[x, z] * 50; 
    }
    
    void ApplyWindEffect()
    {
        Debug.Log("Wind effect applied");
    }
    
    
    void ApplyErosion(float[,] heightMap)
    {
        int erosionRadius = 3;
        float erosionStrength = 0.01f;

        for (int x = erosionRadius; x < width - erosionRadius; x++)
        {
            for (int y = erosionRadius; y < height - erosionRadius; y++)
            {
                float currentHeight = heightMap[x, y];
                float totalHeightDifference = 0f;
                int count = 0;

                for (int i = -erosionRadius; i <= erosionRadius; i++)
                {
                    for (int j = -erosionRadius; j <= erosionRadius; j++)
                    {
                        if (i == 0 && j == 0) continue;

                        float neighborHeight = heightMap[x + i, y + j];
                        float heightDifference = currentHeight - neighborHeight;

                        if (heightDifference > 0)
                        {
                            totalHeightDifference += heightDifference;
                            count++;
                        }
                    }
                }

                if (count > 0)
                {
                    float averageHeightDifference = totalHeightDifference / count;
                    heightMap[x, y] -= averageHeightDifference * erosionStrength;
                }
            }
        }
    }
    
    
    
    
}