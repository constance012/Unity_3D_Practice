using CSTGames.CommonEnums;
using System.Collections;
using System.ComponentModel.Design;
using UnityEngine;
using UnityEngine.Events;

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

	[Space]
	[Header("Events")]
	public UnityEvent<bool> onJumpingEvent;

	public static float velocityX { get; protected set; }
	public static float velocityZ { get; protected set; }
	public static float linearVelocity { get; protected set; }
	public static bool isRunning { get; protected set; }
	public static bool isJumping { get; protected set; }

	// Protected fields.
	protected bool isGrounded;
	protected Vector3 fallMomentum;
	protected Vector3 currentDir;
	protected Vector3 previousDir;

	protected virtual void Awake()
	{
		controller = GetComponent<CharacterController>();
		groundCheck = transform.Find("Ground Check");
	}

	protected virtual void Update()
	{
		Cursor.lockState = Input.GetKey(KeyCode.LeftAlt) ? CursorLockMode.None : CursorLockMode.Locked;

		isGrounded = Physics.CheckSphere(groundCheck.position, footRadius, groundMask);

		HandleHorizontalMovement();
		HandleVerticalMovement();
	}

	/// <summary>
	/// This overload is designed for 1D blend tree.
	/// </summary>
	/// <param name="moveVector"></param>
	/// <param name="currentMaxVelocity"></param>
	private void ChangeVelocity(Vector3 moveVector, float currentMaxVelocity)
	{
		if (moveVector.magnitude > .1f && linearVelocity < currentMaxVelocity)
			linearVelocity += acceleration * Time.deltaTime;

		if (moveVector.magnitude == 0f)
		{
			if (linearVelocity > 0f)
				linearVelocity -= deceleration * Time.deltaTime;

			if (linearVelocity < 0f)
				linearVelocity = 0f;
		}
	}

	/// <summary>
	/// This overload is designed for 1D blend tree.
	/// </summary>
	/// <param name="moveVector"></param>
	/// <param name="currentMaxVelocity"></param>
	private void LockOrResetVelocity(Vector3 moveVector, float currentMaxVelocity)
	{
		// Lock forward velocity.
		if (moveVector.magnitude > .1f)
		{
			if (isRunning && linearVelocity > currentMaxVelocity)
				linearVelocity = currentMaxVelocity;

			// Decelerate from running.
			else if (linearVelocity > currentMaxVelocity)
			{
				linearVelocity -= deceleration * Time.deltaTime;

				// Lock if decelerates from running.
				if (linearVelocity > currentMaxVelocity && linearVelocity < (currentMaxVelocity + .05f))
					linearVelocity = currentMaxVelocity;
			}

			// Lock if accelerates from standing still.
			else if (linearVelocity < currentMaxVelocity && linearVelocity > (currentMaxVelocity - .05f))
				linearVelocity = currentMaxVelocity;
		}
	}

	/// <summary>
	/// This overload is designed for 2D Freeform Directional blend tree.
	/// </summary>
	/// <param name="x"></param>
	/// <param name="z"></param>
	/// <param name="currentMaxVelocity"></param>
	private void ChangeVelocity(float x, float z, float currentMaxVelocity)
	{
		// Increase walking velocity.
		if (z > 0f && velocityZ < currentMaxVelocity)
			velocityZ += acceleration * Time.deltaTime;

		if (x < 0f && velocityX > -currentMaxVelocity)
			velocityX -= acceleration * Time.deltaTime;

		if (x > 0f && velocityX < currentMaxVelocity)
			velocityX += acceleration * Time.deltaTime;

		// Decrease the forward velocity.
		if (z == 0f)
		{
			if (velocityZ > 0f)
				velocityZ -= deceleration * Time.deltaTime;

			if (velocityZ < 0f)
				velocityZ = 0f;
		}

		// Decrease the sideways velocity.
		if (x == 0f)
		{
			if (velocityX < 0f)
				velocityX += deceleration * Time.deltaTime;

			if (velocityX > 0f)
				velocityX -= deceleration * Time.deltaTime;

			if (velocityX != 0f && velocityX > -.05f && velocityX < .05f)
				velocityX = 0f;
		}
	}

	/// <summary>
	/// This overload is designed for 2D Freeform Directional blend tree.
	/// </summary>
	/// <param name="x"></param>
	/// <param name="z"></param>
	/// <param name="currentMaxVelocity"></param>
	private void LockOrResetVelocity(float x, float z, float currentMaxVelocity)
	{
		// Lock forward velocity.
		if (z > 0f)
		{
			if (isRunning && velocityZ > currentMaxVelocity)
				velocityZ = currentMaxVelocity;

			// Decelerate from running.
			else if (velocityZ > currentMaxVelocity)
			{
				velocityZ -= deceleration * Time.deltaTime;

				// Lock if decelerates from running.
				if (velocityZ > currentMaxVelocity && velocityZ < (currentMaxVelocity + .05f))
					velocityZ = currentMaxVelocity;
			}

			// Lock if accelerates from standing still.
			else if (velocityZ < currentMaxVelocity && velocityZ > (currentMaxVelocity - .05f))
				velocityZ = currentMaxVelocity;
		}

		// Lock right velocity.
		if (x > 0f)
		{
			if (isRunning && velocityX > currentMaxVelocity)
				velocityX = currentMaxVelocity;

			// Decelerate from running.
			else if (velocityX > currentMaxVelocity)
			{
				velocityX -= deceleration * Time.deltaTime;

				// Lock if decelerates from running.
				if (velocityX > currentMaxVelocity && velocityX < (currentMaxVelocity + .05f))
					velocityX = currentMaxVelocity;
			}

			// Lock if accelerates from standing still.
			else if (velocityX < currentMaxVelocity && velocityX > (currentMaxVelocity - .05f))
				velocityX = currentMaxVelocity;
		}

		// Lock left velocity.
		if (x < 0f)
		{
			if (isRunning && velocityX < -currentMaxVelocity)
				velocityX = -currentMaxVelocity;

			// Decelerate from running.
			else if (velocityX < -currentMaxVelocity)
			{
				velocityX += deceleration * Time.deltaTime;

				// Lock if decelerates from running.
				if (velocityX < -currentMaxVelocity && velocityX > (-currentMaxVelocity - .05f))
					velocityX = -currentMaxVelocity;
			}

			// Lock if accelerates from standing still.
			else if (velocityX > -currentMaxVelocity && velocityX < (-currentMaxVelocity + .05f))
				velocityX = -currentMaxVelocity;
		}
	}

	private void HandleHorizontalMovement()
	{
		//float x = InputManager.instance.GetAxisRaw("Horizontal");
		//float z = InputManager.instance.GetAxisRaw("Vertical");

		velocityX = InputManager.instance.GetAxisRaw("Horizontal");
		velocityZ = InputManager.instance.GetAxisRaw("Vertical");
		Vector3 moveVector = new Vector3(velocityX, 0f, velocityZ).normalized;
		isRunning = InputManager.instance.GetKey(KeybindingActions.Run);

		float currentMaxVelocity = isRunning ? maxRunningSpeed : maxWalkingSpeed;

		ChangeVelocity(moveVector, currentMaxVelocity);
		LockOrResetVelocity(moveVector, currentMaxVelocity);
	}

	private void HandleVerticalMovement()
	{
		// Force the player to stand on the ground, just for better tho.
		if (isGrounded && fallMomentum.y < 0)
		{
			fallMomentum.y = -2f;
			isJumping = false;

			StopCoroutine(Jump());
			onJumpingEvent?.Invoke(isJumping);
		}

		// Calculate the jump velocity: v = sqrt(h * (-2) * g).
		if (InputManager.instance.GetKeyDown(KeybindingActions.Jump) && isGrounded)
		{
			isJumping = true;
			onJumpingEvent?.Invoke(isJumping);
			StartCoroutine(Jump());
		}

		// Calculate the free fall distance: s = v * t = g * t^2.
		fallMomentum.y += (gravity * Time.deltaTime);

		// Apply jump to the player.
		controller.Move(fallMomentum * Time.deltaTime);
	}

	private IEnumerator Jump()
	{
		if (linearVelocity < .05f)
			yield return new WaitForSeconds(.75f);

		fallMomentum.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
	}
}
