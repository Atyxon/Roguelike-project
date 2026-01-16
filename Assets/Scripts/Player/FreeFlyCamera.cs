using Console;
using UnityEngine;

public class FreeFlyCamera : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float boostMultiplier = 2f;

    [Header("Look Settings")]
    public float lookSpeed = 2f;

    private float rotationX = 0f; // Yaw
    private float rotationY = 0f; // Pitch

    void OnEnable()
    {
        // Initialize rotation based on current transform
        Vector3 angles = transform.rotation.eulerAngles;
        rotationX = angles.y;
        rotationY = angles.x;

        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (DevConsole.Instance != null && DevConsole.Instance.IsConsoleActive())
            return;

        HandleMouseLook();
        HandleMovement();

        if (Input.GetKeyDown(KeyCode.Escape))
            Cursor.lockState = CursorLockMode.None;
    }

    private void HandleMouseLook()
    {
        rotationX += Input.GetAxis("Mouse X") * lookSpeed;
        rotationY -= Input.GetAxis("Mouse Y") * lookSpeed;
        rotationY = Mathf.Clamp(rotationY, -90f, 90f);

        transform.rotation = Quaternion.Euler(rotationY, rotationX, 0f);
    }

    private void HandleMovement()
    {
        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
            speed *= boostMultiplier;

        Vector3 direction = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
        Vector3 movement = transform.TransformDirection(direction) * speed * Time.deltaTime;

        if (Input.GetKey(KeyCode.Space)) movement += Vector3.up * speed * Time.deltaTime;
        if (Input.GetKey(KeyCode.C)) movement += Vector3.down * speed * Time.deltaTime;

        transform.position += movement;
    }
    public void InitializeRotation()
    {
        Vector3 angles = transform.rotation.eulerAngles;
        rotationX = angles.y;
        rotationY = angles.x;
    }
}
