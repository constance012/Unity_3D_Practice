using CSTGames.CommonEnums;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public abstract class PlayerMovement : MonoBehaviour
{
	[Header("Debugging")]
	[Space]
	public AnimationCurve fallSpeedCurve = new AnimationCurve();
	
	[Header("References")]
	[Space]
	[SerializeField] protected CharacterController controller;
	[SerializeField] protected Animator animator;
	[SerializeField] protected Transform groundCheck;
	[SerializeField] protected LayerMask whatIsGround;

	[Space]
	[Header("Movement")]
	[Space]
	public const float MAX_WALKING_SPEED = 1.5f;
	public const float MAX_RUNNING_SPEED = 7f;

	[Min(0f)] public float acceleration;
	[Min(0f)] public float deceleration;
	[Min(0f), Tooltip("How quick would we slow down before performing an 180 turn?")] public float turn180Deceleration;

	[Space]
	[Header("Jump, Gravity")]
	[Space]
	public float jumpHeight = 3f;
	public float gravity = 9.8f;
	public float footRadius = 0.4f;

	[Header("Pushing Power")]
	[Space]
	public float pushPower;

	[Space]
	[Header("Events")]
	public UnityEvent onJumpingEvent = new UnityEvent();
	public UnityEvent<bool> onStrafeSwitchingEvent = new UnityEvent<bool>();

	// Public static fields.
	public static float linearVelocity { get; protected set; }
	public static float velocityX { get; protected set; }
	public static float velocityZ { get; protected set; }
	public static bool canJumpAgain { get; set; } = true;	

	// Protected fields.
	protected static Vector3 fallMomentum;
	protected static bool isJumping;
	protected static bool isRunning;
	private static bool _isGrounded;
	
	protected float moveInputX, moveInputZ;
	protected Vector3 currentDir;
	protected Vector3 previousDir;

	// Private fields.
	private static bool _useStrafeMovement;
	private bool _turned180;

	protected virtual void Awake()
	{
		controller = GetComponent<CharacterController>();
		animator = GetComponent<Animator>();
		groundCheck = transform.Find("Ground Check");
	}

	protected virtual void Update()
	{
		_isGrounded = Physics.CheckSphere(groundCheck.position, footRadius, whatIsGround);

		HandleVerticalMovement();
	}

	protected void LateUpdate()
	{
		controller.Move(fallMomentum * Time.deltaTime);
	}

	private void OnControllerColliderHit(ControllerColliderHit hit)
	{
		Rigidbody body = hit.collider.attachedRigidbody;

		// no rigidbody
		if (body == null || body.isKinematic)
			return;

		// We dont want to push objects below us
		if (hit.moveDirection.y < -0.3f)
			return;

		// Calculate push direction from move direction,
		// we only push objects to the sides never up and down
		Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);

		// Apply the push
		body.velocity = pushDir * pushPower;
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
			float decelerateValue = _turned180 ? turn180Deceleration : deceleration;

			if (linearVelocity > 0f)
				linearVelocity -= decelerateValue * Time.deltaTime;

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

		if (z < 0f && velocityZ > -currentMaxVelocity)
			velocityZ -= acceleration * Time.deltaTime;

		if (x > 0f && velocityX < currentMaxVelocity)
			velocityX += acceleration * Time.deltaTime;

		if (x < 0f && velocityX > -currentMaxVelocity)
			velocityX -= acceleration * Time.deltaTime;

		// Decrease the forward velocity.
		if (z == 0f)
		{
			if (velocityZ > 0f)
				velocityZ -= deceleration * Time.deltaTime;

			if (velocityZ < 0f)
				velocityZ += deceleration * Time.deltaTime;

			if (velocityZ != 0f && velocityZ > -.05f && velocityZ < .05f)
				velocityZ = 0f;
		}

		// Decrease the sideways velocity.
		if (x == 0f)
		{
			if (velocityX > 0f)
				velocityX -= deceleration * Time.deltaTime;

			if (velocityX < 0f)
				velocityX += deceleration * Time.deltaTime;

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

		// Lock backward velocity.
		if (z < 0f)
		{
			if (isRunning && velocityZ < -currentMaxVelocity)
				velocityZ = -currentMaxVelocity;

			// Decelerate from running.
			else if (velocityZ < -currentMaxVelocity)
			{
				velocityZ += deceleration * Time.deltaTime;

				// Lock if decelerates from running.
				if (velocityZ < -currentMaxVelocity && velocityZ > (-currentMaxVelocity - .05f))
					velocityZ = -currentMaxVelocity;
			}

			// Lock if accelerates from standing still.
			else if (velocityZ > -currentMaxVelocity && velocityZ < (-currentMaxVelocity + .05f))
				velocityZ = -currentMaxVelocity;
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

	protected void HandleLinearHorizontalMovement()
	{
		if (_useStrafeMovement)
		{
			_useStrafeMovement = false;
			onStrafeSwitchingEvent?.Invoke(_useStrafeMovement);
		}

		GetInputSignals(out Vector3 moveVector, out float currentMaxVelocity);

		// Calculate the linear speed.
		ChangeVelocity(moveVector, currentMaxVelocity);
		LockOrResetVelocity(moveVector, currentMaxVelocity);
	}

	protected void HandleStrafeHorizontalMovement()
	{
		if (!_useStrafeMovement)
		{
			_useStrafeMovement = true;
			onStrafeSwitchingEvent?.Invoke(_useStrafeMovement);
		}

		GetInputSignals(out Vector3 moveVector, out float currentMaxVelocity);

		// Calculate the linear speed.
		ChangeVelocity(moveVector, currentMaxVelocity);
		LockOrResetVelocity(moveVector, currentMaxVelocity);

		// Calculate each axis velocity for the animator.
		ChangeVelocity(moveInputX, moveInputZ, currentMaxVelocity);
		LockOrResetVelocity(moveInputX, moveInputZ, currentMaxVelocity);
	}

	private void HandleVerticalMovement()
	{	
		if (_isGrounded)
		{
			fallMomentum.y = -controller.stepOffset;
			isJumping = false;
		}

		if (!_isGrounded)
		{
			// Calculate the free fall distance: s = v * t = g * t^2.
			fallMomentum.y -= (gravity * Time.deltaTime);

			// Limit the fall speed.
			fallMomentum.y = Mathf.Max(fallMomentum.y, -10f);
		}

		if (InputManager.Instance.GetKeyDown(KeybindingActions.Jump) && canJumpAgain)
		{
			Jump();
			onJumpingEvent?.Invoke();
		}

		fallSpeedCurve.AddKey(Time.realtimeSinceStartup, fallMomentum.y);

		controller.Move(fallMomentum * Time.deltaTime);
	}

	private void Jump()
	{
		animator.SetBool(AnimationHandler.isJumpingHash, true);
		isJumping = true;

		// Calculate the jump velocity: v = sqrt(2 * h * g).
		fallMomentum.y = Mathf.Sqrt(jumpHeight * 2f * gravity);
		controller.Move(fallMomentum * Time.deltaTime);
	}

	private void GetInputSignals(out Vector3 moveVector, out float currentMaxVelocity)
	{
		moveInputX = InputManager.Instance.GetAxisRaw("Horizontal");
		moveInputZ = InputManager.Instance.GetAxisRaw("Vertical");

		moveVector = new Vector3(moveInputX, 0f, moveInputZ).normalized;
		isRunning = InputManager.Instance.GetKey(KeybindingActions.Run) && !PlayerActions.IsAiming;

		currentMaxVelocity = isRunning ? MAX_RUNNING_SPEED : MAX_WALKING_SPEED;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.white;
		Gizmos.DrawWireSphere(groundCheck.position, footRadius);
	}
}
