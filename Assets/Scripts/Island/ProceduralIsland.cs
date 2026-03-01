using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralIsland : MonoBehaviour
{
    [Header("Mesh resolution")]
    [Min(2)] public int sizeX = 200;          // ilość quadów w osi X
    [Min(2)] public int sizeZ = 200;          // ilość quadów w osi Z

    [Header("Island shape (ellipse/circle)")]
    public float radiusX = 15f;               // promień w osi X
    public float radiusZ = 10f;               // promień w osi Z
    [Range(0.1f, 10f)] public float edgeFalloffPower = 3f; // jak ostro spada brzeg
    [Range(0f, 1f)] public float edgeInner = 0.75f;        // gdzie zaczyna się spadek (0..1)

    [Header("Height / noise")]
    public float baseHeight = 0f;             // poziom bazowy
    public float heightAmplitude = 4f;        // maks wysokość "gór"
    public float noiseScale = 0.08f;          // skala szumu
    public int octaves = 4;
    [Range(0f, 1f)] public float persistence = 0.5f;
    public float lacunarity = 2f;

    [Header("Seed")]
    public int seed = 12345;
    public Vector2 noiseOffset = Vector2.zero;

    [Header("Seed from Teleport (optional)")]
    public bool useSeedFromPrefs = true;
    public string seedKey = "IslandSeed";

    [Header("Options")]
    public bool addCollider = true;

    Mesh _mesh;
    Vector3[] _vertices;
    Vector2[] _uv;

    void OnEnable()
    {

        if (useSeedFromPrefs && PlayerPrefs.HasKey(seedKey))
            seed = PlayerPrefs.GetInt(seedKey);

        Generate();
        
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!UnityEditor.EditorApplication.isPlaying)
            Generate();
    }
#endif

    [ContextMenu("Generate")]
    public void Generate()
    {
        var mf = GetComponent<MeshFilter>();

        if (_mesh == null)
        {
            _mesh = new Mesh();
            _mesh.name = "Procedural Island";
        }
        else
        {
            _mesh.Clear();
        }

        int vertCountX = sizeX + 1;
        int vertCountZ = sizeZ + 1;

        _vertices = new Vector3[vertCountX * vertCountZ];
        _uv = new Vector2[_vertices.Length];

        var prng = new System.Random(seed);
        float offX = (float)prng.NextDouble() * 10000f + noiseOffset.x;
        float offZ = (float)prng.NextDouble() * 10000f + noiseOffset.y;

        // Vertices + UV
        int i = 0;
        for (int z = 0; z < vertCountZ; z++)
        {
            for (int x = 0; x < vertCountX; x++)
            {
                // 1. Normalizujemy x i z do zakresu [-1, 1] (kwadrat o boku 2)
                float u = ((float)x / sizeX) * 2f - 1f;
                float v = ((float)z / sizeZ) * 2f - 1f;

                // 2. Magia matematyki: Mapowanie kwadratu na koło (Shirley-Chiu)
                float mappedX = u * Mathf.Sqrt(1f - (v * v) / 2f);
                float mappedZ = v * Mathf.Sqrt(1f - (u * u) / 2f);

                // 3. Skalowanie do ostatecznych promieni elipsy
                float worldX = mappedX * radiusX;
                float worldZ = mappedZ * radiusZ;

                // Obliczamy dystans od środka znormalizowanego koła (od 0 do 1)
                float distanceToCenter = (mappedX * mappedX) + (mappedZ * mappedZ);

                // Szum
                float n = FractalPerlin((worldX + offX) * noiseScale, (worldZ + offZ) * noiseScale);

                // Falloff brzegu
                float edgeT = Mathf.InverseLerp(edgeInner, 1f, distanceToCenter);
                float falloff = 1f - Mathf.Pow(edgeT, edgeFalloffPower);
                falloff = Mathf.Clamp01(falloff);

                float height = baseHeight + (n * heightAmplitude * falloff);


                _vertices[i] = new Vector3(worldX, height, worldZ);
                _uv[i] = new Vector2((float)x / sizeX, (float)z / sizeZ);
                i++;
            }

        }

        // Triangles (Tworzymy standardową siatkę - nie musimy już niczego wycinać!)
        int[] triangles = new int[sizeX * sizeZ * 6];
        int ti = 0;
        int vi = 0;

        for (int z = 0; z < sizeZ; z++)
        {
            for (int x = 0; x < sizeX; x++)
            {
                triangles[ti + 0] = vi;
                triangles[ti + 1] = vi + vertCountX;
                triangles[ti + 2] = vi + 1;

                triangles[ti + 3] = vi + 1;
                triangles[ti + 4] = vi + vertCountX;
                triangles[ti + 5] = vi + vertCountX + 1;

                vi++;
                ti += 6;
            }
            vi++;
        }

        _mesh.vertices = _vertices;
        _mesh.triangles = triangles;
        _mesh.uv = _uv;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();

        mf.sharedMesh = _mesh;

        if (addCollider)
        {
            var mc = GetComponent<MeshCollider>();
            if (mc == null) mc = gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = null;
            mc.sharedMesh = _mesh;
        }
        else
        {
            var mc = GetComponent<MeshCollider>();
            if (mc != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(mc);
                else Destroy(mc);
#else
                Destroy(mc);
#endif
            }
        }
    }

    float FractalPerlin(float x, float z)
    {
        float total = 0f;
        float amplitude = 1f;
        float frequency = 1f;
        float maxValue = 0f;

        for (int o = 0; o < Mathf.Max(1, octaves); o++)
        {
            float p = Mathf.PerlinNoise(x * frequency, z * frequency);
            total += p * amplitude;
            maxValue += amplitude;

            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return (maxValue > 0f) ? total / maxValue : 0f;
    }

    public void Clear()
    {
        var mf = GetComponent<MeshFilter>();
        if (mf.sharedMesh != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(mf.sharedMesh);
#else
            Destroy(mf.sharedMesh);
#endif
            mf.sharedMesh = null;
        }

        var mc = GetComponent<MeshCollider>();
        if (mc != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(mc);
#else
            Destroy(mc);
#endif
        }
    }

    public void RandomizeSeed()
    {
        seed = Random.Range(int.MinValue, int.MaxValue);
    }
}