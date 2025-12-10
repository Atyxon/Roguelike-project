using UnityEngine;

public class FreeFlyCamera : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float lookSpeed = 2f;
    public float boostMultiplier = 2f;
    
    private float rotationX = 0f;
    private float rotationY = 0f;
    
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }
    void Update()
    {
        rotationX += Input.GetAxis("Mouse X") * lookSpeed;
        rotationY -= Input.GetAxis("Mouse Y") * lookSpeed;
        rotationY = Mathf.Clamp(rotationY, -90f, 90f);
        transform.localRotation = Quaternion.Euler(rotationY, rotationX, 0f);
        
        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
            speed *= boostMultiplier;
        
        Vector3 direction = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        Vector3 movement = transform.TransformDirection(direction) * speed * Time.deltaTime;
        
        if (Input.GetKey(KeyCode.E)) movement += Vector3.up * speed * Time.deltaTime;
        if (Input.GetKey(KeyCode.Q)) movement += Vector3.down * speed * Time.deltaTime;
        
        transform.position += movement;
        
        if (Input.GetKeyDown(KeyCode.Escape))
            Cursor.lockState = CursorLockMode.None;
    }
}