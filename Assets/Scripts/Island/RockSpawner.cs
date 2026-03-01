using System;
using System.Collections.Generic;
using UnityEngine;

public class RockSpawner : MonoBehaviour
{
    [Header("References")]
    public Collider islandCollider;

    [Tooltip("TreeSpawner, ¿eby ska³y wiedzia³y gdzie s¹ lasy i ich unika³y.")]
    public TreeSpawner treeSpawner;

    [Tooltip("Prefab'y ska³ (3 sztuki i wiêcej).")]
    public List<GameObject> rockPrefabs = new List<GameObject>();

    [Header("Global island settings")]
    public float islandRadius = 180f;
    public LayerMask groundMask = ~0;

    [Header("Rock clusters (auto each GenerateRocks)")]
    public bool autoGenerateAreasEachRun = true;

    [Min(1)] public int areasMin = 3;
    [Min(1)] public int areasMax = 6;

    [Tooltip("Losowany radius obszaru skalnego.")]
    public Vector2 areaRadiusRange = new Vector2(20f, 120f);

    [Tooltip("Iloœæ ska³ w obszarze (bazowo). Finalnie zale¿y od wielkoœci + losowoœci.")]
    public Vector2Int areaRocksRange = new Vector2Int(30, 220);

    [Range(0.2f, 1f)]
    public float areaCenterMaxRadius01 = 0.95f;

    [Tooltip("Czy pokazywaæ wylosowane obszary w inspectorze (do debug/podgl¹du).")]
    public bool showGeneratedAreasInInspector = true;

    public List<SpawnArea> areas = new List<SpawnArea>();

    [Header("Extra scattered rocks (outside clusters)")]
    [Min(0)] public int extraScatteredRocks = 150;

    [Tooltip("Rozsypka ska³ omija lasy (zalecane true).")]
    public bool scatteredAvoidForests = true;

    [Header("Avoid forests (main rule)")]
    [Tooltip("G³ówna zasada: klastry ska³ maj¹ omijaæ obszary lasów.")]
    public bool clustersAvoidForests = true;

    [Tooltip("Bufor dooko³a lasu (metry). 0 = tylko nie w œrodku, >0 = dodatkowy dystans.")]
    public float forestAvoidPadding = 8f;

    [Header("Spawn settings")]
    [Tooltip("Min. odleg³oœæ miêdzy ska³ami (metry).")]
    public float minSpacing = 2.0f;

    [Range(1, 400)]
    public int attemptsPerRock = 80;

    [Header("Placement rules")]
    public bool alignToGroundNormal = true;

    [Range(0f, 89f)]
    public float maxSlopeAngle = 45f;

    [Header("Ground embedding (sink)")]
    public float sinkOnFlat = 0.05f;
    public float extraSinkOnSlope = 0.25f;
    public float extraSinkNearEdge = 0.1f;

    [Range(0f, 1f)]
    public float edgeSinkStart = 0.9f;

    public bool scaleAffectsSink = true;

    [Header("Random")]
    public bool randomSeed = true;
    public int seed = 12345;

    [Header("Random scale/rotation")]
    public Vector2 scaleRange = new Vector2(0.8f, 1.3f);
    public Vector2 yRotationRange = new Vector2(0f, 360f);

    [Header("Parenting")]
    public Transform container;

    private readonly List<Vector3> _placedPositions = new List<Vector3>();

    [Serializable]
    public class SpawnArea
    {
        public Vector2 centerXZ = Vector2.zero;
        public float radius = 40f;

        public int rocksCount = 80;

        [Tooltip("Mno¿nik globalnego minSpacing tylko dla tego obszaru.")]
        public float spacingMultiplier = 1f;

        public bool uniformInCircle = true;
    }

    void Reset()
    {
        container = transform;
    }

    public void ClearRocks()
    {
        if (container == null) container = transform;

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            var child = container.GetChild(i);
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(child.gameObject);
            else Destroy(child.gameObject);
#else
            Destroy(child.gameObject);
#endif
        }

        _placedPositions.Clear();
    }

    public void GenerateRocks()
    {
        if (islandCollider == null)
        {
            Debug.LogError("RockSpawner: Brak islandCollider.");
            return;
        }

        if (rockPrefabs == null || rockPrefabs.Count == 0)
        {
            Debug.LogError("RockSpawner: Lista rockPrefabs jest pusta.");
            return;
        }

        if (container == null) container = transform;

        if (randomSeed) seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        var rng = new System.Random(seed);

        _placedPositions.Clear();

        Vector3 origin = islandCollider.bounds.center;

        // pobierz obszary lasów z TreeSpawner
        List<TreeSpawner.SpawnArea> forestAreas = (treeSpawner != null) ? treeSpawner.areas : null;

        // 1) Losuj obszary ska³ za ka¿dym razem (opcjonalnie)
        List<SpawnArea> spawnAreas;
        if (autoGenerateAreasEachRun)
        {
            spawnAreas = GenerateRandomAreas(rng, forestAreas);

            if (showGeneratedAreasInInspector)
                areas = spawnAreas;
            else
                areas.Clear();
        }
        else
        {
            spawnAreas = areas;
        }

        if (spawnAreas == null) spawnAreas = new List<SpawnArea>();

        int spawned = 0;

        // 2) Skupiska ska³
        for (int ai = 0; ai < spawnAreas.Count; ai++)
        {
            var area = spawnAreas[ai];
            float areaSpacing = Mathf.Max(0.05f, minSpacing * Mathf.Max(0.1f, area.spacingMultiplier));
            int target = Mathf.Max(0, area.rocksCount);

            for (int r = 0; r < target; r++)
            {
                if (TryPlaceOneRockInArea(rng, origin, area, areaSpacing, forestAreas, out _))
                    spawned++;
            }
        }

        // 3) Rozsypka ska³ po ca³ej wyspie (g³ównie poza lasami)
        for (int i = 0; i < extraScatteredRocks; i++)
        {
            if (TryPlaceOneRockScattered(rng, origin, minSpacing, forestAreas, out _))
                spawned++;
        }

        Debug.Log($"RockSpawner: Wygenerowano {spawned} ska³ (obszary={spawnAreas.Count}, rozsypka={extraScatteredRocks}, seed={seed}).");
    }

    List<SpawnArea> GenerateRandomAreas(System.Random rng, List<TreeSpawner.SpawnArea> forestAreas)
    {
        int minC = Mathf.Min(areasMin, areasMax);
        int maxC = Mathf.Max(areasMin, areasMax);
        int count = rng.Next(minC, maxC + 1);

        var list = new List<SpawnArea>(count);

        float minR = Mathf.Min(areaRadiusRange.x, areaRadiusRange.y);
        float maxR = Mathf.Max(areaRadiusRange.x, areaRadiusRange.y);
        maxR = Mathf.Min(maxR, Mathf.Max(minR, islandRadius));

        for (int i = 0; i < count; i++)
        {
            var a = new SpawnArea();

            a.radius = RandomRange(rng, minR, maxR);

            // œrodek obszaru: losujemy tak, ¿eby (w miarê) omija³ lasy
            bool found = false;
            for (int tries = 0; tries < 80; tries++)
            {
                float maxCenterDist = islandRadius * areaCenterMaxRadius01;
                maxCenterDist = Mathf.Max(0f, maxCenterDist - a.radius * 0.25f);

                Vector2 c = RandomPointInCircle(rng, maxCenterDist);

                if (clustersAvoidForests && IsInsideAnyForest(c, forestAreas, forestAvoidPadding))
                    continue;

                a.centerXZ = c;
                found = true;
                break;
            }

            if (!found)
            {
                // jak nie znajdzie miejsca, i tak ustaw (zwykle tylko przy ekstremalnych ustawieniach)
                a.centerXZ = RandomPointInCircle(rng, islandRadius * areaCenterMaxRadius01);
            }

            // iloœæ ska³: losowe, ale zale¿ne od radius (wiêkszy obszar -> statystycznie wiêcej)
            float size01 = Mathf.InverseLerp(minR, maxR, a.radius);

            int minRocks = areaRocksRange.x;
            int maxRocks = areaRocksRange.y;

            int minBySize = Mathf.RoundToInt(Mathf.Lerp(minRocks, Mathf.Min(maxRocks, minRocks + 80), size01));
            int maxBySize = Mathf.RoundToInt(Mathf.Lerp(Mathf.Min(maxRocks, minRocks + 120), maxRocks, size01));

            if (maxBySize < minBySize) (maxBySize, minBySize) = (minBySize, maxBySize);
            a.rocksCount = rng.Next(minBySize, maxBySize + 1);

            a.spacingMultiplier = Mathf.Lerp(0.75f, 1.4f, (float)rng.NextDouble());
            a.uniformInCircle = true;

            list.Add(a);
        }

        return list;
    }

    bool TryPlaceOneRockInArea(System.Random rng, Vector3 origin, SpawnArea area, float spacing, List<TreeSpawner.SpawnArea> forestAreas, out Vector3 placedPos)
    {
        placedPos = default;

        for (int attempt = 0; attempt < attemptsPerRock; attempt++)
        {
            Vector2 offset = RandomPointInCircle(rng, area.radius, area.uniformInCircle);
            Vector2 p = area.centerXZ + offset;

            if (p.x * p.x + p.y * p.y > islandRadius * islandRadius)
                continue;

            // omijanie lasów
            if (clustersAvoidForests && IsInsideAnyForest(p, forestAreas, forestAvoidPadding))
                continue;

            if (TryRaycastAndPlace(rng, origin, p.x, p.y, spacing, out placedPos))
                return true;
        }

        return false;
    }

    bool TryPlaceOneRockScattered(System.Random rng, Vector3 origin, float spacing, List<TreeSpawner.SpawnArea> forestAreas, out Vector3 placedPos)
    {
        placedPos = default;

        for (int attempt = 0; attempt < attemptsPerRock; attempt++)
        {
            Vector2 p = RandomPointInCircle(rng, islandRadius);

            if (scatteredAvoidForests && IsInsideAnyForest(p, forestAreas, forestAvoidPadding))
                continue;

            if (TryRaycastAndPlace(rng, origin, p.x, p.y, spacing, out placedPos))
                return true;
        }

        return false;
    }

    bool TryRaycastAndPlace(System.Random rng, Vector3 origin, float x, float z, float spacing, out Vector3 placedPos)
    {
        placedPos = default;

        Vector3 rayStart = origin + new Vector3(x, 500f, z);

        if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 1000f, groundMask))
            return false;

        if (hit.collider != islandCollider)
            return false;

        if (alignToGroundNormal)
        {
            float slope = Vector3.Angle(hit.normal, Vector3.up);
            if (slope > maxSlopeAngle) return false;
        }

        GameObject prefab = rockPrefabs[rng.Next(0, rockPrefabs.Count)];
        float yRot = RandomRange(rng, yRotationRange.x, yRotationRange.y);
        float s = RandomRange(rng, scaleRange.x, scaleRange.y);

        Vector3 pos = hit.point;

        // sink
        float slopeDeg = Vector3.Angle(hit.normal, Vector3.up);
        float slope01 = Mathf.InverseLerp(0f, maxSlopeAngle, Mathf.Min(slopeDeg, maxSlopeAngle));

        float dist01 = new Vector2(x, z).magnitude / Mathf.Max(0.01f, islandRadius);
        float edge01 = Mathf.InverseLerp(edgeSinkStart, 1f, dist01);

        float sink = sinkOnFlat
                   + extraSinkOnSlope * slope01
                   + extraSinkNearEdge * edge01;

        if (scaleAffectsSink) sink *= s;

        pos += Vector3.down * sink;

        if (IsTooClose(pos, spacing))
            return false;

        Quaternion rot;
        if (alignToGroundNormal)
        {
            Quaternion align = Quaternion.FromToRotation(Vector3.up, hit.normal);
            Quaternion randomAroundNormal = Quaternion.AngleAxis(yRot, hit.normal);
            rot = randomAroundNormal * align;
        }
        else
        {
            rot = Quaternion.Euler(0f, yRot, 0f);
        }

        var go = Instantiate(prefab, pos, rot, container);
        go.transform.localScale = go.transform.localScale * s;

        _placedPositions.Add(pos);
        placedPos = pos;
        return true;
    }

    bool IsInsideAnyForest(Vector2 p, List<TreeSpawner.SpawnArea> forestAreas, float padding)
    {
        if (forestAreas == null || forestAreas.Count == 0) return false;

        for (int i = 0; i < forestAreas.Count; i++)
        {
            var f = forestAreas[i];
            float r = Mathf.Max(0f, f.radius + padding);
            Vector2 d = p - f.centerXZ;
            if (d.sqrMagnitude <= r * r)
                return true;
        }
        return false;
    }

    bool IsTooClose(Vector3 pos, float spacing)
    {
        float sqr = spacing * spacing;
        for (int i = 0; i < _placedPositions.Count; i++)
        {
            if ((pos - _placedPositions[i]).sqrMagnitude < sqr)
                return true;
        }
        return false;
    }

    static float RandomRange(System.Random rng, float min, float max)
    {
        return (float)(min + rng.NextDouble() * (max - min));
    }

    static Vector2 RandomPointInCircle(System.Random rng, float radius, bool uniform = true)
    {
        double a = rng.NextDouble() * Math.PI * 2.0;
        double r = uniform ? Math.Sqrt(rng.NextDouble()) : rng.NextDouble();
        float rr = (float)(r * radius);

        return new Vector2(
            rr * (float)Math.Cos(a),
            rr * (float)Math.Sin(a)
        );
    }
}