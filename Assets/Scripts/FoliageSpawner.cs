using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
public class FoliageSpawner : MonoBehaviour
{
    [Header("Prefab to Spawn")]
    public GameObject grassPrefab;

    [Header("Slope Control (dot(normal, up))")]
    [Tooltip("Minimum dot(normal, up) value for grass (flat = 1, vertical = 0)")]
    [Range(0f, 1f)] public float grassMinDot = 0.45f; // was 0.4
    [Tooltip("Maximum dot(normal, up) value for grass (use to fade out near cliffs)")]
    [Range(0f, 1f)] public float grassMaxDot = 1.0f;

    [Header("Spawn Settings")]
    [Range(0f, 1f)] public float density = 0.2f;
    public float offsetAboveSurface = 0.1f;
    public bool randomRotation = true;

    [HideInInspector] public bool hasGenerated = false;

    public void SpawnGrass()
    {
        if (grassPrefab == null)
        {
            Debug.LogWarning("[FoliageSpawner] No prefab assigned!");
            return;
        }

        Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
        if (mesh == null)
        {
            Debug.LogWarning("[FoliageSpawner] Missing mesh on MeshFilter!");
            return;
        }

        Vector3[] verts = mesh.vertices;
        Vector3[] normals = mesh.normals;
        int[] tris = mesh.triangles;
        Transform parent = this.transform;
        int spawned = 0;

        // --- Remove old grass first ---
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(parent.GetChild(i).gameObject);
            else
                Destroy(parent.GetChild(i).gameObject);
#else
            Destroy(parent.GetChild(i).gameObject);
#endif
        }

        // --- Generate new grass instances ---
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

            // ✅ Only flat-enough areas (where shader shows grass)
            if (dot >= grassMinDot && dot <= grassMaxDot && Random.value < density)
            {
                Vector3 spawnPos = GetRandomPointInTriangle(v0, v1, v2);
                spawnPos += avgNormal * offsetAboveSurface;

                Quaternion rot = randomRotation
                    ? Quaternion.Euler(0, Random.Range(0, 360f), 0)
                    : Quaternion.identity;

#if UNITY_EDITOR
                GameObject instance = null;
                if (!Application.isPlaying)
                {
                    instance = (GameObject)PrefabUtility.InstantiatePrefab(grassPrefab, parent);
                    instance.transform.SetPositionAndRotation(spawnPos, rot);
                }
                else
                {
                    instance = Instantiate(grassPrefab, spawnPos, rot, parent);
                }
#else
                GameObject instance = Instantiate(grassPrefab, spawnPos, rot, parent);
#endif
                spawned++;
            }
        }

        hasGenerated = true;
        Debug.Log($"[FoliageSpawner] Spawned {spawned} grass instances on '{name}'.");
    }

    public void ClearGrass()
    {
        Transform parent = this.transform;
        int count = parent.childCount;

        for (int i = count - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(parent.GetChild(i).gameObject);
            else
                Destroy(parent.GetChild(i).gameObject);
#else
            Destroy(parent.GetChild(i).gameObject);
#endif
        }

        hasGenerated = false;
        Debug.Log($"[FoliageSpawner] Cleared {count} grass objects from '{name}'.");
    }

    private Vector3 GetRandomPointInTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        float r1 = Mathf.Sqrt(Random.value);
        float r2 = Random.value;
        return (1 - r1) * a + (r1 * (1 - r2)) * b + (r1 * r2) * c;
    }
}
