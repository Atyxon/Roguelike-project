using UnityEngine;
using System.Collections;
using Console;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    public float walkSpeed = 2;
    public float runSpeed = 6;
    public float gravity = -12;
    public float jumpHeight = 1;
    [Range(0, 1)]
    public float airControlPercent;

    public float turnSmoothTime = 0.2f;
    float turnSmoothVelocity;

    public float speedSmoothTime = 0.1f;
    float speedSmoothVelocity;
    float currentSpeed;
    float velocityY;

    Animator animator;
    Transform cameraT;
    CharacterController controller;

    [Header("Check Sphere config")]
    public Transform groundCheck;
    public float groundDistance = 0.3f;
    public LayerMask groundMask;

    [Header("Free Cam")]
    public Transform freeCamObj;

    private bool _isFreeCamActive = false;

    [Header("Particles")]
    public ParticleSystem dustParticles;
    public ParticleSystem jumpParticles;

    [Header("Fly Mode")]
    public float flySpeed = 10f;
    private bool _fly;

    public bool isGrounded;
    private float _airTime;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        cameraT = Camera.main.transform;
        controller = GetComponent<CharacterController>();
        LoadDevConsole();
    }

    void Update()
    {
        var input = Vector2.zero;
        var running = false;
        var jumpPressed = false;

        var isConsoleActive = DevConsole.Instance != null && DevConsole.Instance.IsConsoleActive();
        var canReadInput = isConsoleActive == false && _isFreeCamActive == false;

        if (canReadInput)
        {
            input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            running = Input.GetKey(KeyCode.LeftShift);
            jumpPressed = Input.GetKeyDown(KeyCode.Space);
        }

        Vector2 inputDir = input.normalized;

        if (_fly)
        {
            FlyMove(inputDir, running, jumpPressed);
        }
        else
        {
            Move(inputDir, running);

            if (jumpPressed)
                Jump();
        }

        var dustParticlesEmission = dustParticles.emission;
        dustParticlesEmission.enabled = (currentSpeed > runSpeed * 0.7f) && controller.isGrounded;

        if (isGrounded)
        {
            var animationSpeedPercent = running ? currentSpeed / runSpeed : currentSpeed / walkSpeed * 0.5f;
            animator.SetFloat(AnimDefines.PARAMETER_SPEED_PERCENT, animationSpeedPercent, speedSmoothTime, Time.deltaTime);
        }
        _airTime = isGrounded ? 0 : _airTime + Time.deltaTime;
        if (_airTime > 0.4f)
        {
            animator.SetFloat(AnimDefines.PARAMETER_AIR_TIME, (_airTime/3)-0.4f);
        }
    }

    void Move(Vector2 inputDir, bool running)
    {
        if (inputDir != Vector2.zero)
        {
            var targetRotation = Mathf.Atan2(inputDir.x, inputDir.y) * Mathf.Rad2Deg + cameraT.eulerAngles.y;
            var smoothTime = GetModifiedSmoothTime(turnSmoothTime);

            transform.rotation = Quaternion.Euler(0f, Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref turnSmoothVelocity, smoothTime), 0f);
        }

        if (isGrounded)
        {
            var targetSpeed = (running ? runSpeed : walkSpeed) * inputDir.magnitude;
            currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, GetModifiedSmoothTime(speedSmoothTime), Mathf.Infinity, Time.unscaledDeltaTime);
        }

        if (controller.isGrounded)
        {
            if (velocityY < 0f)
                velocityY = -2f;
        }
        else
        {
            velocityY += gravity * Time.deltaTime;
        }

        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 velocity = forward * currentSpeed + Vector3.up * velocityY;

        controller.Move(velocity * Time.deltaTime);

        currentSpeed = new Vector2(controller.velocity.x, controller.velocity.z).magnitude;
        isGrounded = controller.isGrounded;

        animator.SetBool(AnimDefines.PARAMETER_IN_AIR, !isGrounded);
    }

    void FlyMove(Vector2 inputDir, bool running, bool ascend)
    {
        if (inputDir != Vector2.zero)
        {
            var targetRotation = Mathf.Atan2(inputDir.x, inputDir.y) * Mathf.Rad2Deg + cameraT.eulerAngles.y;
            transform.eulerAngles = Vector3.up * Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref turnSmoothVelocity, turnSmoothTime);
        }

        var forward = Vector3.Scale(cameraT.forward, new Vector3(1, 0, 1)).normalized;
        var right = cameraT.right;
        var move = forward * inputDir.y + right * inputDir.x;

        if (Input.GetKey(KeyCode.Space))
            move += Vector3.up;
        if (Input.GetKey(KeyCode.LeftControl))
            move += Vector3.down;

        var speed = running ? flySpeed * 1.5f : flySpeed;
        controller.Move(move.normalized * speed * Time.deltaTime);

        currentSpeed = new Vector2(controller.velocity.x, controller.velocity.z).magnitude;
        animator.SetBool(AnimDefines.PARAMETER_IN_AIR, true);
        animator.SetFloat(AnimDefines.PARAMETER_SPEED_PERCENT, 1);
    }
    
    void Jump()
    {
        if (controller.isGrounded)
        {
            animator.SetTrigger(AnimDefines.PARAMETER_JUMP);
            jumpParticles.Play();
            var jumpVelocity = Mathf.Sqrt(-2 * gravity * jumpHeight);
            velocityY = jumpVelocity;
        }
    }

    float GetModifiedSmoothTime(float smoothTime)
    {
        if (controller.isGrounded)
            return smoothTime;

        if (airControlPercent == 0)
            return float.MaxValue;

        return smoothTime / airControlPercent;
    }

    public void LoadDevConsole()
    {
        if (!SceneManager.GetSceneByName(Scenes.DEV_CONSOLE).isLoaded)
        {
            SceneManager.LoadSceneAsync(Scenes.DEV_CONSOLE, LoadSceneMode.Additive);
        }
    }

    public void ToggleFreeCam()
    {
        _isFreeCamActive = !_isFreeCamActive;
        freeCamObj.gameObject.SetActive(_isFreeCamActive);
        if (_isFreeCamActive)
        {
            freeCamObj.position = cameraT.position;
            freeCamObj.rotation = cameraT.rotation;
            freeCamObj.GetComponent<FreeFlyCamera>().InitializeRotation();
        }
        cameraT.gameObject.SetActive(!_isFreeCamActive);
    }

    public bool IsFreeCamActive() => _isFreeCamActive;
    public bool ToggleFlyMode() => _fly = !_fly;
}
