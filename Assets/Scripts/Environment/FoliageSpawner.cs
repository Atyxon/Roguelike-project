using System.Collections.Generic;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
public class FoliageSpawner : MonoBehaviour
{
    // ---------------------------
    // SERIALIZED GRASS DATA
    // ---------------------------
    [System.Serializable]
    public struct GrassInstance
    {
        public Vector3 pos;
        public Quaternion rot;
        public float scale;
        public float randomSeed;
    }

    [SerializeField] private List<GrassInstance> savedGrass = new List<GrassInstance>();
    public int SavedGrassCount => savedGrass.Count;

    // ---------------------------
    // CHUNK DATA
    // ---------------------------
    [System.Serializable]
    public class GrassChunk
    {
        public Vector2Int coord;
        public Vector3 center;
        public List<Matrix4x4> matrices = new List<Matrix4x4>();
        public List<List<Matrix4x4>> batches = new List<List<Matrix4x4>>();
    }

    private Dictionary<Vector2Int, GrassChunk> chunks = new Dictionary<Vector2Int, GrassChunk>();

    // ---------------------------
    // RUNTIME
    // ---------------------------
    private const int batchSize = 1023;
    private bool hasBuiltRuntimeMatrices = false;
    private Transform cam;

    // ---------------------------
    // INSPECTOR SETTINGS
    // ---------------------------
    [Header("Grass Mesh + Material")]
    public Mesh grassMesh;
    public Material grassMaterial;

    [Header("Spawn Settings")]
    [Range(0f,1f)] public float density = 0.2f;
    public bool randomRotation = true;

    [Header("Grass Size & Height")]
    public float grassHeight = 1.0f;
    public float grassScale = 1.0f;

    [Header("Density Multiplier")]
    [Range(1,10)] public int bladesPerTriangle = 1;
    [Range(0f,0.5f)] public float clusterJitter = 0.1f;

    [Header("Slope Control")]
    [Range(0f,1f)] public float grassMinDot = 0.45f;
    [Range(0f,1f)] public float grassMaxDot = 1.0f;

    [Header("Ignore Objects")]
    public string ignoreTag = "grass_ignore";

    [Header("Distance Culling")]
    public float maxDistance = 80f;

    [Header("Chunking")]
    public float chunkSize = 30f;

    // ---------------------------
    private void OnEnable()
    {
        RebuildMatrices();
    }

    // ---------------------------
    // BUILD CHUNKS
    // ---------------------------
    private void RebuildMatrices()
    {
        chunks.Clear();

        foreach (var g in savedGrass)
        {
            Vector3 pos = g.pos;

            int cx = Mathf.FloorToInt(pos.x / chunkSize);
            int cz = Mathf.FloorToInt(pos.z / chunkSize);
            Vector2Int key = new Vector2Int(cx, cz);

            if (!chunks.TryGetValue(key, out GrassChunk chunk))
            {
                chunk = new GrassChunk();
                chunk.coord = key;

                // FIX: correct Y so distance culling works
                chunk.center = new Vector3(
                    (cx + 0.5f) * chunkSize,
                    pos.y,
                    (cz + 0.5f) * chunkSize
                );

                chunks[key] = chunk;
            }

            chunk.matrices.Add(Matrix4x4.TRS(g.pos, g.rot, Vector3.one * g.scale));
        }

        // Pre-split into 1023 batches
        foreach (var chunk in chunks.Values)
        {
            chunk.batches.Clear();
            for (int i = 0; i < chunk.matrices.Count; i += batchSize)
            {
                int size = Mathf.Min(batchSize, chunk.matrices.Count - i);
                chunk.batches.Add(chunk.matrices.GetRange(i, size));
            }
        }

        hasBuiltRuntimeMatrices = true;

        if (cam == null)
        {
            cam = Camera.main != null ? Camera.main.transform : FindObjectOfType<Camera>()?.transform;
        }
    }

    // ---------------------------
    // DRAW CULLED CHUNKS
    // ---------------------------
    private void LateUpdate()
    {
        if (!hasBuiltRuntimeMatrices || grassMesh == null || grassMaterial == null)
            return;

        if (cam == null)
        {
            cam = Camera.main != null ? Camera.main.transform : FindObjectOfType<Camera>()?.transform;
            if (cam == null) return;
        }

        Vector3 camPos = cam.position;

        foreach (var chunk in chunks.Values)
        {
            float dist = Vector3.Distance(camPos, chunk.center);
            if (dist > maxDistance) continue;

            foreach (var batch in chunk.batches)
            {
                Graphics.DrawMeshInstanced(grassMesh, 0, grassMaterial, batch);
            }
        }
    }

    // ---------------------------
    // SPAWN GRASS
    // ---------------------------
    public void SpawnGrass()
    {
        Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
        Vector3[] verts = mesh.vertices;
        Vector3[] normals = mesh.normals;
        int[] tris = mesh.triangles;

        savedGrass.Clear();
        chunks.Clear();

        int spawned = 0;

        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 v0 = transform.TransformPoint(verts[tris[i]]);
            Vector3 v1 = transform.TransformPoint(verts[tris[i + 1]]);
            Vector3 v2 = transform.TransformPoint(verts[tris[i + 2]]);

            Vector3 n0 = transform.TransformDirection(normals[tris[i]]);
            Vector3 n1 = transform.TransformDirection(normals[tris[i + 1]]);
            Vector3 n2 = transform.TransformDirection(normals[tris[i + 2]]);
            Vector3 avgNormal = (n0 + n1 + n2).normalized;

            float dot = Vector3.Dot(avgNormal, Vector3.up);
            if (dot < grassMinDot || dot > grassMaxDot) continue;

            for (int b = 0; b < bladesPerTriangle; b++)
            {
                if (Random.value >= density) continue;

                Vector3 spawnPos = GetRandomPointInTriangle(v0, v1, v2);
                spawnPos += avgNormal * grassHeight;
                spawnPos += new Vector3(
                    Random.Range(-clusterJitter, clusterJitter),
                    0f,
                    Random.Range(-clusterJitter, clusterJitter)
                );

                Collider[] hits = Physics.OverlapSphere(spawnPos, 0.5f);
                if (hits.Any(h => h.CompareTag(ignoreTag))) continue;

                Vector3 rayStart = spawnPos + Vector3.up * 3f;
                if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hitInfo, 100f))
                {
                    if (hitInfo.collider.CompareTag(ignoreTag)) continue;
                }

                Quaternion rot = randomRotation
                    ? Quaternion.Euler(0, Random.Range(0f, 360f), 0)
                    : Quaternion.identity;

                float scale = grassScale * Random.Range(0.8f, 1.2f);

                savedGrass.Add(new GrassInstance
                {
                    pos = spawnPos,
                    rot = rot,
                    scale = scale,
                    randomSeed = Random.value
                });

                spawned++;
            }
        }

        RebuildMatrices();
        Debug.Log($"[FoliageSpawner] Spawned {spawned} grass instances.");
    }

    // -------------------------------------------------
    public void ClearGrass()
    {
        savedGrass.Clear();
        chunks.Clear();
        hasBuiltRuntimeMatrices = false;
        Debug.Log("[FoliageSpawner] Cleared all grass.");
    }

    private Vector3 GetRandomPointInTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        float r1 = Mathf.Sqrt(Random.value);
        float r2 = Random.value;
        return (1 - r1) * a + (r1 * (1 - r2)) * b + (r1 * r2) * c;
    }
}
