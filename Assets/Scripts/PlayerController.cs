using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Ruch")]
    public float walkSpeed = 4.0f;
    public float sprintSpeed = 7.0f;
    public float rotationSmoothTime = 0.1f;
    public float airControlMultiplier = 0.6f;

    [Header("Skok i grawitacja")]
    public float jumpHeight = 1.2f;
    public float gravity = -9.81f;
    public float fallGravityMultiplier = 2.0f;
    public float jumpCutMultiplier = 0.5f;

    [Header("Ground Check")]
    public float groundedProbeRadiusMul = 0.9f;    // sfera trochę mniejsza niż radius CC
    public float groundedProbeDistance = 0.2f;     // jak głęboko „patrzymy” w dół
    public LayerMask groundMask;                   // USTAW w Inspectorze tylko warstwy podłoża!
    [Range(0, 89f)] public float maxGroundAngle = 60f;

    [Header("Coyote & Buffer")]
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.12f;

    [Header("Odniesienia")]
    public Transform cameraTransform;

    private CharacterController controller;
    private Vector3 velocity;
    private float rotationVelocity;
    private bool isGrounded;
    private bool wasGrounded;
    private float coyoteTimer = 0f;
    private float jumpBufferTimer = -1f;
    private bool hasJumpedSinceGrounded = false;   // NEW: blokada „drugiego skoku” w tym samym oknie

    // cache
    private float minGroundDot; // cos(maxGroundAngle w radianach)

    private bool isSprinting;  // efektywny sprint (blokujemy zmianę w powietrzu)

 
    private Animator animator;   // NEW


    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        minGroundDot = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
    }

    void Update()
    {
        wasGrounded = isGrounded;
        GroundCheck();

        // Sprint można zmieniać tylko, gdy stoimy na ziemi
        if (isGrounded)
        {
            isSprinting = Input.GetKey(KeyCode.LeftShift);
        }

        // Input skoku + buffer
        if (Input.GetButtonDown("Jump"))
            jumpBufferTimer = jumpBufferTime;
        else
            jumpBufferTimer -= Time.deltaTime;

        // Coyote timer
        if (isGrounded) coyoteTimer = coyoteTime;
        else coyoteTimer -= Time.deltaTime;

        HandleMovement();
        HandleJumpAndGravity();
    }


    void GroundCheck()
    {
        // Sfera startuje mniej więcej z „pasa” kapsuły i leci w dół
        float radius = controller.radius * groundedProbeRadiusMul;
        Vector3 origin = transform.position + Vector3.up * (controller.height * 0.5f - radius);
        RaycastHit hit;

        bool sphereHit = Physics.SphereCast(origin, radius, Vector3.down, out hit, groundedProbeDistance, groundMask, QueryTriggerInteraction.Ignore);

        // Uznaj za ziemię tylko powierzchnie z wystarczającą składową „w górę”
        bool slopeOk = sphereHit && Vector3.Dot(hit.normal, Vector3.up) >= minGroundDot;

        // Opcjonalne wsparcie: CC.isGrounded pomaga na schodkach, ale nie decyduje sam
        bool ccGrounded = controller.isGrounded;

        isGrounded = (slopeOk || ccGrounded);

        if (isGrounded)
        {
            // NIE ścinaj mocno prędkości przy pierwszym kontakcie z ziemią
            if (controller.isGrounded && velocity.y < 0f)
                velocity.y = -0.1f;   // delikatny docisk zamiast hamulca ręcznego

            hasJumpedSinceGrounded = false;
        }
    }

    void HandleMovement()
    {
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");
        Vector2 input = Vector2.ClampMagnitude(new Vector2(inputX, inputZ), 1f);
        bool isMoving = input.sqrMagnitude > 0.0001f;

        Vector3 camForward = cameraTransform ? Vector3.Scale(cameraTransform.forward, new Vector3(1, 0, 1)).normalized : Vector3.forward;
        Vector3 camRight = cameraTransform ? cameraTransform.right : Vector3.right;
        Vector3 moveDir = (camForward * input.y + camRight * input.x).normalized;

        if (isMoving)
        {
            float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref rotationVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }

        float targetSpeed = isSprinting ? sprintSpeed : walkSpeed; // używamy isSprinting z poprzedniej modyfikacji
        if (!isGrounded) targetSpeed *= airControlMultiplier;

        Vector3 horizontalVelocity = moveDir * targetSpeed;
        controller.Move(horizontalVelocity * Time.deltaTime);

        // ===== ANIMACJA =====
        if (animator)
        {
            // prędkość pozioma
            float speed = new Vector3(controller.velocity.x, 0f, controller.velocity.z).magnitude;
            animator.SetFloat("Speed", speed);
            animator.SetBool("IsSprinting", isSprinting);
        }
    }


    void HandleJumpAndGravity()
    {
        bool wantJump = jumpBufferTimer > 0f;
        bool canJump = (coyoteTimer > 0f) && !hasJumpedSinceGrounded; // NEW: bez drugiego skoku w tym samym oknie

        if (wantJump && canJump)
        {
            velocity.y = Mathf.Sqrt(-2f * gravity * jumpHeight);
            jumpBufferTimer = -1f;
            coyoteTimer = 0f;
            hasJumpedSinceGrounded = true; // NEW
        }

        // Jump cut
        if (Input.GetButtonUp("Jump") && velocity.y > 0f)
            velocity.y *= jumpCutMultiplier;

        // Grawitacja (szybszy opad)
        float g = gravity;
        if (velocity.y < 0f) g *= fallGravityMultiplier;

        velocity.y += g * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Wyczyść buffer po lądowaniu
        if (!wasGrounded && isGrounded)
            jumpBufferTimer = -1f;
    }

    void OnDrawGizmosSelected()
    {
        if (controller == null) return;
        float radius = controller.radius * groundedProbeRadiusMul;
        Vector3 origin = transform.position + Vector3.up * (controller.height * 0.5f - radius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin + Vector3.down * groundedProbeDistance, radius);
    }
}
