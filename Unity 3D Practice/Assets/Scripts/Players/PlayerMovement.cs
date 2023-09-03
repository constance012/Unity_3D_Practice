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
	public static float LinearVelocity { get; protected set; }
	public static float VelocityX { get; protected set; }
	public static float VelocityZ { get; protected set; }
	public static bool CanJumpAgain { get; set; } = true;

	// Protected fields.
	protected static Vector3 _fallMomentum;
	protected static bool _isJumping;
	protected static bool _isRunning;
	private static bool _isGrounded;
	
	protected float _moveInputX, _moveInputZ;
	protected Vector3 _currentDir;
	protected Vector3 _previousDir;

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
		controller.Move(_fallMomentum * Time.deltaTime);
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
		if (moveVector.magnitude > .1f && LinearVelocity < currentMaxVelocity)
			LinearVelocity += acceleration * Time.deltaTime;

		if (moveVector.magnitude == 0f)
		{
			float decelerateValue = _turned180 ? turn180Deceleration : deceleration;

			if (LinearVelocity > 0f)
				LinearVelocity -= decelerateValue * Time.deltaTime;

			if (LinearVelocity < 0f)
				LinearVelocity = 0f;
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
			if (_isRunning && LinearVelocity > currentMaxVelocity)
				LinearVelocity = currentMaxVelocity;

			// Decelerate from running.
			else if (LinearVelocity > currentMaxVelocity)
			{
				LinearVelocity -= deceleration * Time.deltaTime;

				// Lock if decelerates from running.
				if (LinearVelocity > currentMaxVelocity && LinearVelocity < (currentMaxVelocity + .05f))
					LinearVelocity = currentMaxVelocity;
			}

			// Lock if accelerates from standing still.
			else if (LinearVelocity < currentMaxVelocity && LinearVelocity > (currentMaxVelocity - .05f))
				LinearVelocity = currentMaxVelocity;
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
		if (z > 0f && VelocityZ < currentMaxVelocity)
			VelocityZ += acceleration * Time.deltaTime;

		if (z < 0f && VelocityZ > -currentMaxVelocity)
			VelocityZ -= acceleration * Time.deltaTime;

		if (x > 0f && VelocityX < currentMaxVelocity)
			VelocityX += acceleration * Time.deltaTime;

		if (x < 0f && VelocityX > -currentMaxVelocity)
			VelocityX -= acceleration * Time.deltaTime;

		// Decrease the forward velocity.
		if (z == 0f)
		{
			if (VelocityZ > 0f)
				VelocityZ -= deceleration * Time.deltaTime;

			if (VelocityZ < 0f)
				VelocityZ += deceleration * Time.deltaTime;

			if (VelocityZ != 0f && VelocityZ > -.05f && VelocityZ < .05f)
				VelocityZ = 0f;
		}

		// Decrease the sideways velocity.
		if (x == 0f)
		{
			if (VelocityX > 0f)
				VelocityX -= deceleration * Time.deltaTime;

			if (VelocityX < 0f)
				VelocityX += deceleration * Time.deltaTime;

			if (VelocityX != 0f && VelocityX > -.05f && VelocityX < .05f)
				VelocityX = 0f;
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
			if (_isRunning && VelocityZ > currentMaxVelocity)
				VelocityZ = currentMaxVelocity;

			// Decelerate from running.
			else if (VelocityZ > currentMaxVelocity)
			{
				VelocityZ -= deceleration * Time.deltaTime;

				// Lock if decelerates from running.
				if (VelocityZ > currentMaxVelocity && VelocityZ < (currentMaxVelocity + .05f))
					VelocityZ = currentMaxVelocity;
			}

			// Lock if accelerates from standing still.
			else if (VelocityZ < currentMaxVelocity && VelocityZ > (currentMaxVelocity - .05f))
				VelocityZ = currentMaxVelocity;
		}

		// Lock backward velocity.
		if (z < 0f)
		{
			if (_isRunning && VelocityZ < -currentMaxVelocity)
				VelocityZ = -currentMaxVelocity;

			// Decelerate from running.
			else if (VelocityZ < -currentMaxVelocity)
			{
				VelocityZ += deceleration * Time.deltaTime;

				// Lock if decelerates from running.
				if (VelocityZ < -currentMaxVelocity && VelocityZ > (-currentMaxVelocity - .05f))
					VelocityZ = -currentMaxVelocity;
			}

			// Lock if accelerates from standing still.
			else if (VelocityZ > -currentMaxVelocity && VelocityZ < (-currentMaxVelocity + .05f))
				VelocityZ = -currentMaxVelocity;
		}

		// Lock right velocity.
		if (x > 0f)
		{
			if (_isRunning && VelocityX > currentMaxVelocity)
				VelocityX = currentMaxVelocity;

			// Decelerate from running.
			else if (VelocityX > currentMaxVelocity)
			{
				VelocityX -= deceleration * Time.deltaTime;

				// Lock if decelerates from running.
				if (VelocityX > currentMaxVelocity && VelocityX < (currentMaxVelocity + .05f))
					VelocityX = currentMaxVelocity;
			}

			// Lock if accelerates from standing still.
			else if (VelocityX < currentMaxVelocity && VelocityX > (currentMaxVelocity - .05f))
				VelocityX = currentMaxVelocity;
		}

		// Lock left velocity.
		if (x < 0f)
		{
			if (_isRunning && VelocityX < -currentMaxVelocity)
				VelocityX = -currentMaxVelocity;

			// Decelerate from running.
			else if (VelocityX < -currentMaxVelocity)
			{
				VelocityX += deceleration * Time.deltaTime;

				// Lock if decelerates from running.
				if (VelocityX < -currentMaxVelocity && VelocityX > (-currentMaxVelocity - .05f))
					VelocityX = -currentMaxVelocity;
			}

			// Lock if accelerates from standing still.
			else if (VelocityX > -currentMaxVelocity && VelocityX < (-currentMaxVelocity + .05f))
				VelocityX = -currentMaxVelocity;
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
		ChangeVelocity(_moveInputX, _moveInputZ, currentMaxVelocity);
		LockOrResetVelocity(_moveInputX, _moveInputZ, currentMaxVelocity);
	}

	private void HandleVerticalMovement()
	{	
		if (_isGrounded)
		{
			_fallMomentum.y = -controller.stepOffset;
			_isJumping = false;
		}

		if (!_isGrounded)
		{
			// Calculate the free fall distance: s = v * t = g * t^2.
			_fallMomentum.y -= (gravity * Time.deltaTime);

			// Limit the fall speed.
			_fallMomentum.y = Mathf.Max(_fallMomentum.y, -10f);
		}

		if (InputManager.Instance.GetKeyDown(KeybindingActions.Jump) && CanJumpAgain)
		{
			Jump();
			onJumpingEvent?.Invoke();
		}

		fallSpeedCurve.AddKey(Time.realtimeSinceStartup, _fallMomentum.y);

		controller.Move(_fallMomentum * Time.deltaTime);
	}

	private void Jump()
	{
		animator.SetBool(AnimationHandler.isJumpingHash, true);
		_isJumping = true;

		// Calculate the jump velocity: v = sqrt(2 * h * g).
		_fallMomentum.y = Mathf.Sqrt(jumpHeight * 2f * gravity);
		controller.Move(_fallMomentum * Time.deltaTime);
	}

	private void GetInputSignals(out Vector3 moveVector, out float currentMaxVelocity)
	{
		_moveInputX = InputManager.Instance.GetAxisRaw("Horizontal");
		_moveInputZ = InputManager.Instance.GetAxisRaw("Vertical");

		moveVector = new Vector3(_moveInputX, 0f, _moveInputZ).normalized;
		_isRunning = InputManager.Instance.GetKey(KeybindingActions.Run) && !PlayerActions.IsAiming;

		currentMaxVelocity = _isRunning ? MAX_RUNNING_SPEED : MAX_WALKING_SPEED;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.white;
		Gizmos.DrawWireSphere(groundCheck.position, footRadius);
	}
}
