using CSTGames.CommonEnums;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
	[Header("References")]
	[Space]
	[SerializeField] protected CharacterController controller;
	[SerializeField] protected Transform groundCheck;
	[SerializeField] protected LayerMask groundMask;

	[Space]
	[Header("Movement")]
	[Space]
	public const float maxWalkingSpeed = 1.5f;
	public const float maxRunningSpeed = 7f;
	public float acceleration;
	public float deceleration;

	[Space]
	[Header("Jump, Gravity")]
	[Space]
	public float jumpHeight = 3f;
	public float gravity = -9.8f;
	public float footRadius = 0.4f;

	public float velocity;
	public bool isRunning, isWalking;

	// Protected fields.
	protected bool isGrounded;
	protected Vector3 fallMomentum;

	protected virtual void Awake()
	{
		controller = GetComponent<CharacterController>();
		groundCheck = transform.Find("Ground Check");
	}

	protected virtual void Update()
	{
		Cursor.lockState = Input.GetKey(KeyCode.LeftAlt) ? CursorLockMode.None : CursorLockMode.Locked;

		isGrounded = Physics.CheckSphere(groundCheck.position, footRadius, groundMask);
		float x = InputManager.instance.GetAxisRaw("Horizontal");
		float z = InputManager.instance.GetAxisRaw("Vertical");

		// Force the player stand on the ground, just for better tho.
		if (isGrounded && fallMomentum.y < 0)
			fallMomentum.y = -2f;

		// Calculate the speed when walking or running.
		if (isWalking = (Mathf.Abs(x) + Mathf.Abs(z)) > 0)
		{
			velocity += acceleration * Time.deltaTime;

			isRunning = InputManager.instance.GetKey(KeybindingActions.Run);
		}
		else if (!isWalking && velocity > 0f)
			velocity -= deceleration * Time.deltaTime;

		if (isRunning)
			velocity = Mathf.Clamp(velocity, 0f, maxRunningSpeed);
		else if (isWalking && velocity > maxWalkingSpeed)
			velocity = Mathf.Clamp(velocity, 0f, maxWalkingSpeed);

		// Calculate the jump velocity: v = sqrt(h * (-2) * g).
		if (InputManager.instance.GetKeyDown(KeybindingActions.Jump) && isGrounded)
			fallMomentum.y = Mathf.Sqrt(jumpHeight * (-2f) * gravity);

		// Calculate the free fall distance: s = v * t = g * t^2.
		fallMomentum.y += (gravity * Time.deltaTime);

		// Apply jump to the player.
		controller.Move(fallMomentum * Time.deltaTime);
	}
}
