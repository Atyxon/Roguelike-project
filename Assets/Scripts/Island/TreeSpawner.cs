using System;
using System.Collections.Generic;
using UnityEngine;

public class TreeSpawner : MonoBehaviour
{
    [Header("References")]
    public Collider islandCollider;
    public List<GameObject> treePrefabs = new List<GameObject>();

    [Header("Global island settings")]
    [Tooltip("Promieñ wyspy / obszaru losowania (w XZ).")]
    public float islandRadius = 180f;

    [Tooltip("Warstwa terenu (opcjonalnie). Jeœli ustawisz, raycast trafi tylko w ni¹.")]
    public LayerMask groundMask = ~0;

    [Header("Clusters (auto each GenerateTrees)")]
    [Tooltip("Jeœli true, przy ka¿dym GenerateTrees losujemy obszary od nowa.")]
    public bool autoGenerateAreasEachRun = true;

    [Tooltip("Min liczba obszarów na wyspie.")]
    [Min(1)] public int areasMin = 4;

    [Tooltip("Max liczba obszarów na wyspie.")]
    [Min(1)] public int areasMax = 5;

    [Tooltip("Losowany radius obszaru.")]
    public Vector2 areaRadiusRange = new Vector2(30f, 200f);

    [Tooltip("Minimalna i maksymalna iloœæ drzew w obszarze (bazowo). Finalnie zale¿y od wielkoœci + losowoœci.")]
    public Vector2Int areaTreesRange = new Vector2Int(100, 600);

    [Tooltip("Jak blisko brzegu wyspy mog¹ byæ œrodki obszarów (0..1). 1 = prawie przy brzegu, 0.8 = bardziej w œrodku.")]
    [Range(0.2f, 1f)]
    public float areaCenterMaxRadius01 = 0.85f;

    [Tooltip("Jeœli true, zapisujemy wylosowane obszary do listy areas (do podgl¹du w inspectorze).")]
    public bool showGeneratedAreasInInspector = true;

    [Tooltip("Tu zobaczysz wylosowane obszary po GenerateTrees (tylko podgl¹d).")]
    public List<SpawnArea> areas = new List<SpawnArea>();

    [Header("Extra scattered trees (outside clusters)")]
    [Tooltip("Ile drzew losowo rozrzuconych po ca³ej wyspie poza skupiskami.")]
    [Min(0)] public int extraScatteredTrees = 80;

    [Tooltip("Jeœli true, rozproszone drzewa NIE bêd¹ mog³y wpaœæ do obszarów (bêd¹ tylko poza nimi).")]
    public bool scatteredAvoidAreas = true;

    [Header("Spawn settings")]
    [Tooltip("Min. odleg³oœæ miêdzy drzewami (w metrach).")]
    public float minSpacing = 3f;

    [Tooltip("Ile prób na znalezienie miejsca dla jednego drzewa.")]
    [Range(1, 400)]
    public int attemptsPerTree = 60;

    [Header("Placement rules")]
    public bool alignToGroundNormal = false;

    [Range(0f, 89f)]
    public float maxSlopeAngle = 35f;

    [Header("Ground embedding (sink)")]
    public float sinkOnFlat = 0.15f;
    public float extraSinkOnSlope = 0.6f;
    public float extraSinkNearEdge = 0.4f;

    [Range(0f, 1f)]
    public float edgeSinkStart = 0.85f;

    public bool scaleAffectsSink = true;

    [Header("Random")]
    public bool randomSeed = true;
    public int seed = 12345;

    [Header("Random scale/rotation")]
    public Vector2 scaleRange = new Vector2(0.9f, 1.2f);
    public Vector2 yRotationRange = new Vector2(0f, 360f);

    [Header("Parenting")]
    public Transform container;

    private readonly List<Vector3> _placedPositions = new List<Vector3>();

    [Serializable]
    public class SpawnArea
    {
        public Vector2 centerXZ = Vector2.zero;
        public float radius = 50f;

        [Tooltip("Ile drzew ma powstaæ w tym obszarze.")]
        public int treesCount = 100;

        [Tooltip("Mno¿nik globalnego minSpacing tylko dla tego obszaru.")]
        public float spacingMultiplier = 1f;

        public bool uniformInCircle = true;
    }

    void Reset()
    {
        container = transform;
    }

    public void ClearTrees()
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

    public void GenerateTrees()
    {
        if (islandCollider == null)
        {
            Debug.LogError("TreeSpawner: Brak islandCollider (przypisz MeshCollider z obiektu wyspy).");
            return;
        }

        if (treePrefabs == null || treePrefabs.Count == 0)
        {
            Debug.LogError("TreeSpawner: Lista treePrefabs jest pusta.");
            return;
        }

        if (container == null) container = transform;

        if (randomSeed) seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        var rng = new System.Random(seed);

        _placedPositions.Clear();

        Vector3 origin = islandCollider.bounds.center;

        // 1) Generuj obszary przy ka¿dym runie
        List<SpawnArea> spawnAreas;
        if (autoGenerateAreasEachRun)
        {
            spawnAreas = GenerateRandomAreas(rng);

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

        // 2) Skupiska
        for (int ai = 0; ai < spawnAreas.Count; ai++)
        {
            var area = spawnAreas[ai];

            float areaSpacing = Mathf.Max(0.05f, minSpacing * Mathf.Max(0.1f, area.spacingMultiplier));
            int target = Mathf.Max(0, area.treesCount);

            for (int t = 0; t < target; t++)
            {
                if (TryPlaceOneTreeInArea(rng, origin, area, areaSpacing, out _))
                    spawned++;
            }
        }

        // 3) Losowe drzewa po ca³ej wyspie (rozproszone)
        for (int i = 0; i < extraScatteredTrees; i++)
        {
            if (TryPlaceOneTreeScattered(rng, origin, minSpacing, spawnAreas, out _))
                spawned++;
        }

        Debug.Log($"TreeSpawner: Wygenerowano {spawned} drzew (obszary={spawnAreas.Count}, rozsypka={extraScatteredTrees}, seed={seed}).");
    }

    List<SpawnArea> GenerateRandomAreas(System.Random rng)
    {
        int minC = Mathf.Min(areasMin, areasMax);
        int maxC = Mathf.Max(areasMin, areasMax);
        int count = rng.Next(minC, maxC + 1);

        var list = new List<SpawnArea>(count);

        float minR = Mathf.Min(areaRadiusRange.x, areaRadiusRange.y);
        float maxR = Mathf.Max(areaRadiusRange.x, areaRadiusRange.y);

        // jeœli islandRadius mniejszy ni¿ maxR, przytnij
        maxR = Mathf.Min(maxR, Mathf.Max(minR, islandRadius));

        for (int i = 0; i < count; i++)
        {
            var a = new SpawnArea();

            // radius 30-200
            a.radius = RandomRange(rng, minR, maxR);

            // œrodek obszaru: w miarê w œrodku, z marginesem zale¿nym od radius
            float maxCenterDist = islandRadius * areaCenterMaxRadius01;
            maxCenterDist = Mathf.Max(0f, maxCenterDist - a.radius * 0.35f);

            a.centerXZ = RandomPointInCircle(rng, maxCenterDist);

            // treesCount: losowe, ale zale¿ne od wielkoœci
            float size01 = Mathf.InverseLerp(minR, maxR, a.radius);

            int minTrees = areaTreesRange.x;
            int maxTrees = areaTreesRange.y;

            int minBySize = Mathf.RoundToInt(Mathf.Lerp(minTrees, Mathf.Min(maxTrees, minTrees + 200), size01));
            int maxBySize = Mathf.RoundToInt(Mathf.Lerp(Mathf.Min(maxTrees, minTrees + 250), maxTrees, size01));

            if (maxBySize < minBySize) (maxBySize, minBySize) = (minBySize, maxBySize);

            a.treesCount = rng.Next(minBySize, maxBySize + 1);

            // lekka losowoœæ gêstoœci
            a.spacingMultiplier = Mathf.Lerp(0.85f, 1.35f, (float)rng.NextDouble());
            a.uniformInCircle = true;

            list.Add(a);
        }

        return list;
    }

    bool TryPlaceOneTreeInArea(System.Random rng, Vector3 origin, SpawnArea area, float spacing, out Vector3 placedPos)
    {
        placedPos = default;

        for (int attempt = 0; attempt < attemptsPerTree; attempt++)
        {
            Vector2 offset = RandomPointInCircle(rng, area.radius, area.uniformInCircle);
            float x = area.centerXZ.x + offset.x;
            float z = area.centerXZ.y + offset.y;

            // ograniczenie do wyspy
            if (x * x + z * z > islandRadius * islandRadius)
                continue;

            if (TryRaycastAndPlace(rng, origin, x, z, spacing, out placedPos))
                return true;
        }

        return false;
    }

    bool TryPlaceOneTreeScattered(System.Random rng, Vector3 origin, float spacing, List<SpawnArea> spawnAreas, out Vector3 placedPos)
    {
        placedPos = default;

        for (int attempt = 0; attempt < attemptsPerTree; attempt++)
        {
            Vector2 p = RandomPointInCircle(rng, islandRadius);

            // jeœli rozproszone maj¹ omijaæ obszary, sprawdzamy czy punkt nie wpada do ¿adnego
            if (scatteredAvoidAreas && spawnAreas != null && spawnAreas.Count > 0)
            {
                bool insideAny = false;
                for (int i = 0; i < spawnAreas.Count; i++)
                {
                    var a = spawnAreas[i];
                    Vector2 d = p - a.centerXZ;
                    if (d.sqrMagnitude <= a.radius * a.radius)
                    {
                        insideAny = true;
                        break;
                    }
                }
                if (insideAny) continue;
            }

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

        // filtr nachylenia
        if (alignToGroundNormal)
        {
            float slope = Vector3.Angle(hit.normal, Vector3.up);
            if (slope > maxSlopeAngle) return false;
        }

        // losowania wczeœniej
        GameObject prefab = treePrefabs[rng.Next(0, treePrefabs.Count)];
        float yRot = RandomRange(rng, yRotationRange.x, yRotationRange.y);
        float s = RandomRange(rng, scaleRange.x, scaleRange.y);

        Vector3 pos = hit.point;

        // --- SINK ---
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

        // rotacja
        Quaternion rot;
        if (alignToGroundNormal)
        {
            Quaternion align = Quaternion.FromToRotation(Vector3.up, hit.normal);
            Quaternion randomAroundNormal = Quaternion.AngleAxis(yRot, hit.normal);
            rot = randomAroundNormal * align;
        }
        else
        {
            Quaternion correction = Quaternion.Euler(-90f, 0f, 0f);
            rot = Quaternion.Euler(0f, yRot, 0f) * correction;
        }

        var go = Instantiate(prefab, pos, rot, container);
        go.transform.localScale = go.transform.localScale * s;

        _placedPositions.Add(pos);
        placedPos = pos;
        return true;
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