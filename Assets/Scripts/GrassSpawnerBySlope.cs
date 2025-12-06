using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class GrassSpawnerBySlopeMesh : MonoBehaviour
{
    [Header("Prefab trawy")]
    public GameObject grassPrefab;

    [Header("Parametry spawnowania")]
    [Range(0, 1)] public float minDotForGrass = 0.8f; // dot(normal, up)
    [Range(0, 1)] public float density = 0.2f; // 0–1, ile trawy z danego trójkąta

    [Header("Rozmieszczenie")]
    public float offsetAboveSurface = 0.1f;
    public bool randomRotation = true;
    public bool generateOnStart = true;

    private Mesh mesh;

    void Start()
    {
        if (generateOnStart)
            SpawnGrass();
    }

    public void SpawnGrass()
    {
        mesh = GetComponent<MeshFilter>().sharedMesh;
        Vector3[] verts = mesh.vertices;
        Vector3[] normals = mesh.normals;
        int[] tris = mesh.triangles;

        int spawned = 0;

        for (int i = 0; i < tris.Length; i += 3)
        {
            // Dla każdego trójkąta
            Vector3 v0 = transform.TransformPoint(verts[tris[i]]);
            Vector3 v1 = transform.TransformPoint(verts[tris[i + 1]]);
            Vector3 v2 = transform.TransformPoint(verts[tris[i + 2]]);

            // Średnia normalna
            Vector3 n0 = transform.TransformDirection(normals[tris[i]]);
            Vector3 n1 = transform.TransformDirection(normals[tris[i + 1]]);
            Vector3 n2 = transform.TransformDirection(normals[tris[i + 2]]);
            Vector3 avgNormal = (n0 + n1 + n2).normalized;

            float dot = Vector3.Dot(avgNormal, Vector3.up);

            // Tak jak w Shader Graph: SmoothStep(InverseLerp(0.3, 0.4, dot))
            float t = Mathf.InverseLerp(0.3f, 0.4f, dot);
            float smooth = Mathf.SmoothStep(0, 1, t);

            // Jeśli trawa (czyli powierzchnia płaska)
            if (smooth > 0.5f && Random.value < density)
            {
                // Losowe miejsce w obrębie trójkąta
                Vector3 spawnPos = GetRandomPointInTriangle(v0, v1, v2);
                spawnPos += avgNormal * offsetAboveSurface;

                Quaternion rot = Quaternion.identity;
                if (randomRotation)
                    rot = Quaternion.Euler(0, Random.Range(0, 360f), 0);

                Instantiate(grassPrefab, spawnPos, rot, transform);
                spawned++;
            }
        }

        Debug.Log($"[GrassSpawner] Wygenerowano {spawned} kępek trawy.");
    }

    private Vector3 GetRandomPointInTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        float r1 = Mathf.Sqrt(Random.value);
        float r2 = Random.value;
        return (1 - r1) * a + (r1 * (1 - r2)) * b + (r1 * r2) * c;
    }
}
