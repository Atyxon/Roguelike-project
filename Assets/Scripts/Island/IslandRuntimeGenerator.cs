using UnityEngine;

public class IslandRuntimeGenerator : MonoBehaviour
{
    [Header("References")]
    public ProceduralIsland island;
    public TreeSpawner treeSpawner;
    public RockSpawner rockSpawner;
    public RedCubePeakSpawner redCubeSpawner;

    [Header("Return Teleport")]
    public GameObject teleportPrefab;                 // prefab z komponentem Teleport + collider trigger
    public string returnSceneName = "Mother Island";  // nazwa sceny matki
    public Transform teleportContainer;               // gdzie ma siê pojawiæ teleport (opcjonalnie)

    [Header("Keys")]
    public string seedKey = "IslandSeed";

    [Header("Generation toggles")]
    public bool generateTrees = true;
    public bool generateRocks = true;
    public bool generateRedCubes = true;
    public bool generateReturnTeleport = true;

    [Header("Safety")]
    public bool clearTreesBeforeGenerate = true;
    public bool clearRocksBeforeGenerate = true;
    public bool clearCubesBeforeGenerate = true;
    public bool clearTeleportBeforeGenerate = true;

    [Header("Teleport placement")]
    [Range(0.2f, 1f)]
    public float teleportCenterRadius01 = 0.65f; // 0.65 = raczej bli¿ej œrodka (mniej problemów z brzegiem)
    public float teleportYOffset = 0.2f;         // ¿eby nie wchodzi³ w ziemiê
    public LayerMask groundMask = ~0;            // mo¿esz zostawiæ ~0

    private bool _ran;

    void Start()
    {
        if (_ran) return;
        _ran = true;

        if (island == null) island = FindObjectOfType<ProceduralIsland>();
        if (treeSpawner == null) treeSpawner = FindObjectOfType<TreeSpawner>();
        if (rockSpawner == null) rockSpawner = FindObjectOfType<RockSpawner>();
        if (redCubeSpawner == null) redCubeSpawner = FindObjectOfType<RedCubePeakSpawner>();

        if (island == null)
        {
            Debug.LogError("IslandRuntimeGenerator: Nie znaleziono ProceduralIsland w scenie.");
            return;
        }

        // 1) Seed z teleportu (z Mother -> procedural)
        if (PlayerPrefs.HasKey(seedKey))
            island.seed = PlayerPrefs.GetInt(seedKey);

        // 2) Generuj teren
        island.Generate();

        // 3) Œwie¿y collider wyspy
        var mc = island.GetComponent<MeshCollider>();
        if (mc == null)
        {
            Debug.LogError("IslandRuntimeGenerator: Wyspa nie ma MeshCollider. W ProceduralIsland 'Add Collider' musi byæ true.");
            return;
        }

        // 4) Drzewa
        if (generateTrees && treeSpawner != null)
        {
            treeSpawner.islandCollider = mc;
            treeSpawner.randomSeed = false;
            treeSpawner.seed = island.seed;

            if (clearTreesBeforeGenerate) treeSpawner.ClearTrees();
            treeSpawner.GenerateTrees();
        }

        // 5) Ska³y (po drzewach, bo omijaj¹ forestAreas)
        if (generateRocks && rockSpawner != null)
        {
            rockSpawner.islandCollider = mc;
            rockSpawner.treeSpawner = treeSpawner;

            rockSpawner.randomSeed = false;
            rockSpawner.seed = island.seed ^ 0x6C8E9CF5;

            if (clearRocksBeforeGenerate) rockSpawner.ClearRocks();
            rockSpawner.GenerateRocks();
        }

        // 6) Czerwone szeœciany (po wszystkim)
        if (generateRedCubes && redCubeSpawner != null)
        {
            redCubeSpawner.islandCollider = mc;

            redCubeSpawner.randomSeed = false;
            redCubeSpawner.seed = island.seed ^ 0x1B873593;

            if (clearCubesBeforeGenerate) redCubeSpawner.ClearCubes();
            redCubeSpawner.GeneratePeakCubes();
        }

        // 7) Teleport powrotny do Mother Island (na samym koñcu)
        if (generateReturnTeleport)
        {
            SpawnReturnTeleport(mc);
        }
    }

    void SpawnReturnTeleport(MeshCollider islandMeshCollider)
    {
        if (teleportPrefab == null)
        {
            Debug.LogWarning("IslandRuntimeGenerator: teleportPrefab nie jest ustawiony — pomijam teleport powrotny.");
            return;
        }

        if (teleportContainer == null) teleportContainer = transform;

        if (clearTeleportBeforeGenerate)
            ClearExistingTeleport();

        float islandRadius = GetIslandRadiusFallback();
        Vector3 origin = islandMeshCollider.bounds.center;

        // kilka prób, ¿eby zawsze znaleŸæ dobre miejsce
        for (int attempt = 0; attempt < 40; attempt++)
        {
            Vector2 p = Random.insideUnitCircle * islandRadius * teleportCenterRadius01;
            Vector3 rayStart = origin + new Vector3(p.x, 2000f, p.y);

            if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 5000f, groundMask))
                continue;

            if (hit.collider != islandMeshCollider)
                continue;

            Vector3 pos = hit.point + Vector3.up * teleportYOffset;

            GameObject tp = Instantiate(teleportPrefab, pos, Quaternion.identity, teleportContainer);

            // ustaw scenê docelow¹ teleportu
            var t = tp.GetComponent<Teleport>();
            if (t != null)
                t.SetLevelName(returnSceneName);

            Debug.Log($"IslandRuntimeGenerator: Teleport powrotny postawiony -> {returnSceneName}");
            return;
        }

        Debug.LogWarning("IslandRuntimeGenerator: Nie uda³o siê znaleŸæ miejsca na teleport (sprawdŸ groundMask / collider).");
    }

    void ClearExistingTeleport()
    {
        // usuñ stare teleporty tylko z containera (¿eby nie mieszaæ innych obiektów)
        for (int i = teleportContainer.childCount - 1; i >= 0; i--)
        {
            var child = teleportContainer.GetChild(i);
            if (child.GetComponent<Teleport>() == null) continue;

#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(child.gameObject);
            else Destroy(child.gameObject);
#else
            Destroy(child.gameObject);
#endif
        }
    }

    float GetIslandRadiusFallback()
    {
        // bierzemy radius ze spawnerów (bo tam masz to ustawione “prawdziwie”)
        if (treeSpawner != null) return treeSpawner.islandRadius;
        if (rockSpawner != null) return rockSpawner.islandRadius;
        if (redCubeSpawner != null) return redCubeSpawner.islandRadius;

        return 180f;
    }
}