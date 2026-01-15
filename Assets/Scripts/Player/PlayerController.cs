using UnityEngine;
using System.Collections;
using Console;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour {

	public float walkSpeed = 2;
	public float runSpeed = 6;
	public float gravity = -12;
	public float jumpHeight = 1;
	[Range(0,1)]
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
	
	[Header("Particles")]
	public ParticleSystem dustParticles;
	public ParticleSystem jumpParticles;

	public bool isGrounded;
	void Start () 
	{
		animator = GetComponent<Animator> ();
		cameraT = Camera.main.transform;
		controller = GetComponent<CharacterController> ();
		LoadDevConsole();
	}

	void Update()
	{
		Vector2 input = Vector2.zero;
		bool running = false;
		bool jumpPressed = false;

		bool canReadInput = DevConsole.Instance == null || !DevConsole.Instance.IsConsoleActive();

		if (canReadInput)
		{
			input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
			running = Input.GetKey(KeyCode.LeftShift);
			jumpPressed = Input.GetKeyDown(KeyCode.Space);
		}

		Vector2 inputDir = input.normalized;

		Move(inputDir, running);

		if (jumpPressed)
			Jump();

		var dustParticlesEmission = dustParticles.emission;
		dustParticlesEmission.enabled = (currentSpeed > runSpeed * 0.7f) && controller.isGrounded;

		float animationSpeedPercent = running 
			? currentSpeed / runSpeed 
			: currentSpeed / walkSpeed * 0.5f;

		animator.SetFloat(
			AnimDefines.PARAMETER_SPEED_PERCENT,
			animationSpeedPercent,
			speedSmoothTime,
			Time.deltaTime
		);
	}

	void Move(Vector2 inputDir, bool running) 
	{
		if (inputDir != Vector2.zero) 
		{
			var targetRotation = Mathf.Atan2 (inputDir.x, inputDir.y) * Mathf.Rad2Deg + cameraT.eulerAngles.y;
			transform.eulerAngles = Vector3.up * Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref turnSmoothVelocity, GetModifiedSmoothTime(turnSmoothTime));
		}
			
		var targetSpeed = ((running) ? runSpeed : walkSpeed) * inputDir.magnitude;
		currentSpeed = Mathf.SmoothDamp (currentSpeed, targetSpeed, ref speedSmoothVelocity, GetModifiedSmoothTime(speedSmoothTime));

		velocityY += Time.deltaTime * gravity;
		var velocity = transform.forward * currentSpeed + Vector3.up * velocityY;

		controller.Move (velocity * Time.deltaTime);
		currentSpeed = new Vector2 (controller.velocity.x, controller.velocity.z).magnitude;
		
		isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

		if (controller.isGrounded)
		{
			velocityY = 0;
		}
		
		animator.SetBool(AnimDefines.PARAMETER_IN_AIR, isGrounded == false);
	}

	void Jump() 
	{
		if (controller.isGrounded) 
		{
			jumpParticles.Play();
			var jumpVelocity = Mathf.Sqrt (-2 * gravity * jumpHeight);
			velocityY = jumpVelocity;
		}
	}

	float GetModifiedSmoothTime(float smoothTime) {
		if (controller.isGrounded) {
			return smoothTime;
		}

		if (airControlPercent == 0) {
			return float.MaxValue;
		}
		return smoothTime / airControlPercent;
	}
	public void LoadDevConsole()
	{
		if (!SceneManager.GetSceneByName(Scenes.DEV_CONSOLE).isLoaded)
		{
			SceneManager.LoadSceneAsync(Scenes.DEV_CONSOLE, LoadSceneMode.Additive);
		}
	}
}