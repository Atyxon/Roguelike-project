using UnityEngine;
// Importujemy UnityEngine, czyli podstawow¹ bibliotekê Unity (Vector3, Mesh, Mathf itd.)

[ExecuteAlways]
// Sprawia, ¿e skrypt wykonuje siê tak¿e w edytorze (nie tylko po klikniêciu Play)

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
// Unity dopilnuje, ¿eby obiekt mia³ MeshFilter i MeshRenderer
// (bo bez nich nie da siê wyœwietliæ siatki/mesha)

public class ProceduralPlanePerlin : MonoBehaviour
{
    [Header("Rozdzielczoœæ siatki")]
    // Tylko nag³ówek w Inspectorze, dla czytelnoœci

    [Min(1)] public int sizeX = 200;
    // Ile "kratek" ma byæ w osi X.
    // Wa¿ne: wierzcho³ków bêdzie sizeX+1 (bo kratka ma 2 krawêdzie)

    [Min(1)] public int sizeZ = 200;
    // Ile "kratek" ma byæ w osi Z.

    [Min(0.01f)] public float cellSize = 0.2f;
    // Rozmiar jednej kratki w œwiecie Unity.
    // 0.2 oznacza gêstsz¹ siatkê na mniejszym obszarze.

    [Header("Perlin Noise")]
    // Kolejny nag³ówek w Inspectorze

    public float noiseScale = 0.08f;
    // Skala szumu: im mniejsza, tym wiêksze i ³agodniejsze fale (du¿e góry)
    // im wiêksza, tym wiêcej "drobnych szczegó³ów" (poszarpane)

    public float heightMultiplier = 8f;
    // Mno¿nik wysokoœci: jak wysoko maj¹ iœæ górki w osi Y

    public float offsetX = 0f;
    public float offsetZ = 0f;
    // Przesuniêcie noise po X i Z — pozwala "przewijaæ" wzór
    // (jakbyœ przesuwa³ mapê pod kamer¹)

    MeshFilter mf;
    // Zmienna na MeshFilter — tu bêdziemy podmieniaæ mesh

    MeshCollider mc;
    // Zmienna na MeshCollider — ¿eby collider pasowa³ do nowej siatki

    void OnEnable()
    // Unity odpala OnEnable gdy obiekt/skrypt siê aktywuje
    // (w edytorze i w Play)
    {
        mf = GetComponent<MeshFilter>();
        // Pobieramy komponent MeshFilter z tego samego GameObjectu

        mc = GetComponent<MeshCollider>();
        // Pobieramy MeshCollider (mo¿e istnieæ, ale nie musi)

        Generate();
        // Generujemy siatkê od razu
    }

    void OnValidate()
    // Unity odpala OnValidate zawsze, gdy zmienisz coœ w Inspectorze
    // (np. sizeX, noiseScale itd.)
    {
        if (!isActiveAndEnabled) return;
        // Jeœli obiekt/skrypt jest wy³¹czony, nie rób nic

        if (mf == null) mf = GetComponent<MeshFilter>();
        // Jeœli mf nie jest ustawiony (np. po re-kompilacji), pobierz go ponownie

        if (mc == null) mc = GetComponent<MeshCollider>();
        // To samo dla MeshCollider

        Generate();
        // Przelicz mesh po ka¿dej zmianie w Inspectorze
    }

    void Generate()
    // Nasza g³ówna metoda: tworzy siatkê (mesh) i ustawia j¹ na obiekcie
    {
        var mesh = new Mesh();
        // Tworzymy nowy obiekt Mesh (pusta siatka)

        mesh.name = "PerlinPlaneMesh";
        // Nadajemy nazwê (pomocne w debugowaniu w Inspectorze)

        Vector3[] vertices = new Vector3[(sizeX + 1) * (sizeZ + 1)];
        // Tworzymy tablicê wierzcho³ków (punktów 3D)
        // sizeX+1 i sizeZ+1 bo np. 1 kratka potrzebuje 4 wierzcho³ków

        int[] triangles = new int[sizeX * sizeZ * 6];
        // Trójk¹ty zapisujemy jako indeksy do tablicy vertices.
        // Ka¿da kratka = 2 trójk¹ty, a ka¿dy trójk¹t = 3 indeksy
        // wiêc 1 kratka = 6 liczb.

        Vector2[] uvs = new Vector2[vertices.Length];
        // UV to wspó³rzêdne tekstury (¿eby materia³ ³adnie siê mapowa³)

        int vi = 0;
        // vi = vertex index (indeks wierzcho³ka)

        for (int z = 0; z <= sizeZ; z++)
        // Lecimy po siatce w osi Z (rzêdy)
        {
            for (int x = 0; x <= sizeX; x++)
            // Lecimy po siatce w osi X (kolumny)
            {
                float nx = (x + offsetX) * noiseScale;
                // Przeliczamy X do przestrzeni szumu (noise)
                // offset pozwala przesun¹æ wzór

                float nz = (z + offsetZ) * noiseScale;
                // To samo dla Z

                float y = Mathf.PerlinNoise(nx, nz) * heightMultiplier;
                // PerlinNoise zwraca wartoœæ 0..1
                // mno¿ymy przez heightMultiplier, ¿eby to by³o np. 0..8

                vertices[vi] = new Vector3(x * cellSize, y, z * cellSize);
                // Ustawiamy wierzcho³ek:
                // - X: x * cellSize
                // - Y: wysokoœæ z Perlin Noise
                // - Z: z * cellSize

                uvs[vi] = new Vector2((float)x / sizeX, (float)z / sizeZ);
                // UV: normalizujemy do 0..1, ¿eby tekstura roz³o¿y³a siê na ca³oœci

                vi++;
                // Przechodzimy do kolejnego wierzcho³ka
            }
        }

        int ti = 0;
        // ti = triangle index (indeks w tablicy triangles)

        int v = 0;
        // v = aktualny "lewy-dolny" wierzcho³ek kratki

        for (int z = 0; z < sizeZ; z++)
        // Iterujemy po kratkach (ju¿ nie po wierzcho³kach)
        {
            for (int x = 0; x < sizeX; x++)
            {
                // Pierwszy trójk¹t kratki:
                triangles[ti + 0] = v;
                triangles[ti + 1] = v + sizeX + 1;
                triangles[ti + 2] = v + 1;

                // Drugi trójk¹t kratki:
                triangles[ti + 3] = v + 1;
                triangles[ti + 4] = v + sizeX + 1;
                triangles[ti + 5] = v + sizeX + 2;

                v++;
                // Przesuwamy siê o jedn¹ kratkê w prawo

                ti += 6;
                // Przechodzimy o 6 miejsc dalej (bo wpisaliœmy 6 indeksów)
            }

            v++;
            // Przeskok na pocz¹tek nastêpnego rzêdu
            // (bo wierzcho³ków w rzêdzie jest sizeX+1)
        }

        mesh.vertices = vertices;
        // Wrzucamy tablicê wierzcho³ków do mesha

        mesh.triangles = triangles;
        // Wrzucamy tablicê trójk¹tów

        mesh.uv = uvs;
        // Wrzucamy UV

        mesh.RecalculateNormals();
        // Unity liczy normalne (kierunki "na zewn¹trz"), potrzebne do œwiat³a/shadingu

        mesh.RecalculateBounds();
        // Unity liczy granice obiektu (wa¿ne dla culling i widocznoœci)

        mf.sharedMesh = mesh;
        // Podmieniamy mesh w MeshFilter — od teraz obiekt wyœwietla nasz¹ siatkê

        if (mc != null)
        // Jeœli obiekt ma MeshCollider
        {
            mc.sharedMesh = null;
            // Zerujemy, ¿eby Unity wymusi³o odœwie¿enie

            mc.sharedMesh = mesh;
            // Ustawiamy collider na nowy mesh, ¿eby kolizje pasowa³y do terenu
        }
    }
}