using UnityEngine;

public class CameraOrbit : MonoBehaviour
{
    [Header("Target")]
    public Transform target;         // gracz
    public Vector3 targetOffset = new Vector3(0f, 1.6f, 0f); // punkt patrzenia (wysokość barków)

    [Header("Orbita")]
    public float distance = 4.5f;    // dystans od gracza
    public float minDistance = 1.2f;
    public float maxDistance = 6.0f;

    [Header("Mysz")]
    public float sensitivityX = 150f; // deg/s na Mouse X
    public float sensitivityY = 120f; // deg/s na Mouse Y
    public float minPitch = -25f;     // ograniczenia patrzenia w górę/dół
    public float maxPitch = 65f;
    public bool lockCursor = true;

    [Header("Wygładzenie")]
    public float moveSmoothTime = 0.08f;

    [Header("Kolizje kamery")]
    public LayerMask collisionMask = ~0; // wszystko
    public float collisionRadius = 0.2f;
    public float collisionBuffer = 0.1f;

    private float _yaw;   // rotacja wokół osi Y
    private float _pitch; // rotacja wokół osi X (góra/dół)
    private Vector3 _vel; // do SmoothDamp

    void Start()
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        // Zainicjuj yaw/pitch z aktualnej pozycji kamery
        Vector3 toCam = (transform.position - (target ? target.position + targetOffset : Vector3.zero)).normalized;
        if (toCam.sqrMagnitude > 0.0001f)
        {
            Quaternion q = Quaternion.LookRotation(toCam, Vector3.up);
            _pitch = q.eulerAngles.x;
            _yaw = q.eulerAngles.y;
            // Normalizacja pitcha do [-180,180]
            if (_pitch > 180f) _pitch -= 360f;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
        }
    }

    void LateUpdate()
    {
        if (!target) return;

        // 1) Wejście z MYSZKI (tylko Mouse X/Y sterują kamerą)
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        _yaw += mouseX * sensitivityX * Time.deltaTime;
        _pitch -= mouseY * sensitivityY * Time.deltaTime; // minus = ruch myszy w górę patrzy w górę
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

        // 2) Docelowa pozycja kamery (orbita wokół targetu)
        Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 focus = target.position + targetOffset;
        Vector3 desiredCamPos = focus - rot * Vector3.forward * distance;

        // 3) Kolizje: zbliż kamerę jeśli ściana między focus a desiredCamPos
        Vector3 dir = (desiredCamPos - focus).normalized;
        float dst = distance;
        if (Physics.SphereCast(focus, collisionRadius, dir, out RaycastHit hit, distance + collisionBuffer, collisionMask, QueryTriggerInteraction.Ignore))
        {
            dst = Mathf.Clamp(hit.distance - collisionBuffer, minDistance, distance);
            desiredCamPos = focus + dir * dst;
        }

        // 4) Płynne dosuwanie + patrz w target
        transform.position = Vector3.SmoothDamp(transform.position, desiredCamPos, ref _vel, moveSmoothTime);
        transform.rotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);

        // 5) Scroll – opcjonalny zoom myszką (usuń jeśli nie chcesz)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
            distance = Mathf.Clamp(distance - scroll * 4.0f, minDistance, maxDistance);

        // 6) Odblokowanie kursora (np. ESC)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
