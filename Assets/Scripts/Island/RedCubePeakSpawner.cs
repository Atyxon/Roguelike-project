using System;
using System.Collections.Generic;
using UnityEngine;

public class RedCubePeakSpawner : MonoBehaviour
{
    [Header("References")]
    public Collider islandCollider;
    public GameObject cubePrefab;
    public Transform container;

    [Header("Island sampling")]
    [Tooltip("Promieñ wyspy w XZ (taki jak w TreeSpawner).")]
    public float islandRadius = 180f;

    [Tooltip("Jak bardzo trzymamy siê œrodka wyspy. 0.6 = tylko œrodek, 0.85 = prawie ca³a wyspa bez brzegu.")]
    [Range(0.2f, 1f)]
    public float centerRadius01 = 0.7f;

    [Tooltip("Ile punktów zeskanowaæ (losowo). Im wiêcej, tym pewniejsze znalezienie najwy¿szych gór.")]
    [Range(200, 20000)]
    public int samplePoints = 6000;

    [Tooltip("Warstwa terenu (opcjonalnie). Jak nie ustawisz, bêdzie wszystko.")]
    public LayerMask groundMask = ~0;

    [Header("Peaks selection")]
    [Tooltip("Ile szeœcianów postawiæ (u Ciebie 3).")]
    [Range(1, 10)]
    public int cubesCount = 3;

    [Tooltip("Minimalna odleg³oœæ miêdzy WYBRANYMI szeœcianami (XZ) — ¿eby by³y na ró¿nych górach.")]
    public float minPeakSeparation = 100f;

    [Tooltip("Promieñ uznania, ¿e punkt jest na tej SAMEJ górze (XZ). Punkty w tym promieniu grupujemy jako jedno wzniesienie.")]
    public float sameMountainRadius = 90f;

    [Tooltip("Ile najlepszych próbek bierzemy do analizy (dla wydajnoœci).")]
    [Range(100, 20000)]
    public int topSamplesToConsider = 4000;

    [Header("Cube placement")]
    public float cubeYUpOffset = 0.5f; // ¿eby nie wchodzi³ w ziemiê
    public Vector3 cubeScale = new Vector3(20, 20, 20);

    [Header("Random")]
    public bool randomSeed = true;
    public int seed = 12345;

    private System.Random _rng;

    [Serializable]
    private struct SampleHit
    {
        public Vector3 position;
        public float height;
    }

    void Reset()
    {
        container = transform;
    }

    [ContextMenu("Clear Cubes")]
    public void ClearCubes()
    {
        if (container == null) container = transform;

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            var ch = container.GetChild(i);
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(ch.gameObject);
            else Destroy(ch.gameObject);
#else
            Destroy(ch.gameObject);
#endif
        }
    }

    [ContextMenu("Generate Peak Cubes")]
    public void GeneratePeakCubes()
    {
        if (islandCollider == null)
        {
            Debug.LogError("RedCubePeakSpawner: Brak islandCollider (przypisz MeshCollider wyspy).");
            return;
        }

        if (cubePrefab == null)
        {
            Debug.LogError("RedCubePeakSpawner: Brak cubePrefab.");
            return;
        }

        if (container == null) container = transform;

        if (randomSeed) seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        _rng = new System.Random(seed);

        ClearCubes();

        Vector3 origin = islandCollider.bounds.center;

        // 1) Zbieramy próbki wysokoœci TYLKO w centrum wyspy
        float centerRadius = islandRadius * centerRadius01;
        List<SampleHit> samples = new List<SampleHit>(samplePoints);

        for (int i = 0; i < samplePoints; i++)
        {
            Vector2 p = RandomPointInCircle(centerRadius);

            Vector3 rayStart = origin + new Vector3(p.x, 2000f, p.y);
            if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 5000f, groundMask))
                continue;

            // tylko wyspa
            if (hit.collider != islandCollider)
                continue;

            samples.Add(new SampleHit
            {
                position = hit.point,
                height = hit.point.y
            });
        }

        if (samples.Count == 0)
        {
            Debug.LogError("RedCubePeakSpawner: Nie znaleziono hitów na islandCollider (sprawdŸ groundMask / collider).");
            return;
        }

        // 2) Sortujemy od najwy¿szego
        samples.Sort((a, b) => b.height.CompareTo(a.height));

        // 3) Budujemy listê "szczytów ró¿nych gór" (non-maximum suppression)
        int limit = Mathf.Min(topSamplesToConsider, samples.Count);
        List<Vector3> mountainPeaks = new List<Vector3>();

        float sameMountainSqr = sameMountainRadius * sameMountainRadius;

        for (int i = 0; i < limit; i++)
        {
            Vector3 cand = samples[i].position;

            bool isSameMountain = false;
            Vector2 candXZ = new Vector2(cand.x, cand.z);

            for (int m = 0; m < mountainPeaks.Count; m++)
            {
                Vector3 p = mountainPeaks[m];
                Vector2 pXZ = new Vector2(p.x, p.z);

                if ((candXZ - pXZ).sqrMagnitude < sameMountainSqr)
                {
                    isSameMountain = true;
                    break;
                }
            }

            if (!isSameMountain)
                mountainPeaks.Add(cand);

            // wystarczy mieæ pulê kilkudziesiêciu gór
            if (mountainPeaks.Count >= 40)
                break;
        }

        if (mountainPeaks.Count == 0)
        {
            Debug.LogError("RedCubePeakSpawner: Nie uda³o siê wy³apaæ ¿adnych 'gór'.");
            return;
        }

        // 4) Wybieramy 3 góry z dodatkowym dystansem miêdzy nimi
        List<Vector3> chosen = new List<Vector3>(cubesCount);
        float minSepSqr = minPeakSeparation * minPeakSeparation;

        for (int i = 0; i < mountainPeaks.Count && chosen.Count < cubesCount; i++)
        {
            Vector3 cand = mountainPeaks[i];
            Vector2 candXZ = new Vector2(cand.x, cand.z);

            bool farEnough = true;
            for (int c = 0; c < chosen.Count; c++)
            {
                Vector3 ex = chosen[c];
                Vector2 exXZ = new Vector2(ex.x, ex.z);

                if ((candXZ - exXZ).sqrMagnitude < minSepSqr)
                {
                    farEnough = false;
                    break;
                }
            }

            if (farEnough)
                chosen.Add(cand);
        }

        // Awaryjnie: jeœli teren jest ma³o zró¿nicowany, bierzemy kolejne góry bez minSep
        for (int i = 0; i < mountainPeaks.Count && chosen.Count < cubesCount; i++)
        {
            Vector3 cand = mountainPeaks[i];
            Vector2 candXZ = new Vector2(cand.x, cand.z);

            bool tooCloseSameMountain = false;
            for (int c = 0; c < chosen.Count; c++)
            {
                Vector3 ex = chosen[c];
                Vector2 exXZ = new Vector2(ex.x, ex.z);

                if ((candXZ - exXZ).sqrMagnitude < sameMountainSqr)
                {
                    tooCloseSameMountain = true;
                    break;
                }
            }

            if (!tooCloseSameMountain)
                chosen.Add(cand);
        }

        // 5) Spawn
        for (int i = 0; i < chosen.Count; i++)
        {
            Vector3 pos = chosen[i] + Vector3.up * cubeYUpOffset;
            var go = Instantiate(cubePrefab, pos, Quaternion.identity, container);
            go.transform.localScale = cubeScale;
        }

        Debug.Log($"RedCubePeakSpawner: Postawiono {chosen.Count}/{cubesCount} szeœcianów (seed={seed}, sampleHits={samples.Count}, mountains={mountainPeaks.Count}).");
    }

    Vector2 RandomPointInCircle(float radius)
    {
        double a = _rng.NextDouble() * Math.PI * 2.0;
        double r = Math.Sqrt(_rng.NextDouble()) * radius;

        return new Vector2(
            (float)(r * Math.Cos(a)),
            (float)(r * Math.Sin(a))
        );
    }
}