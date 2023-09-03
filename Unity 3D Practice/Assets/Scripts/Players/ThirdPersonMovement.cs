using UnityEngine;

public class ThirdPersonMovement : PlayerMovement
{
	[Header("References")]
	[Space]
	[SerializeField] private Transform cam;

	[Space]
	[Header("Public Fields.")]
	[Space]
	public float turnSmoothTime = 0.1f;

	// Private fields.
	private float _turnSmoothVelocity;

	protected override void Awake()
	{
		base.Awake();
		cam = Camera.main.transform;
	}

	protected override void Update()
	{
		base.Update();

		HandleLinearHorizontalMovement();

		if (PlayerActions.IsAiming)
		{
			HandleStrafeHorizontalMovement();
			OnAimingMode();
			return;
		}

		_currentDir = new Vector3(_moveInputX, 0f, _moveInputZ).normalized;

		// Move the player.
		if (_currentDir.magnitude > 0f || LinearVelocity > 0f)
		{
			// Decelerate and then stopping.
			if (_currentDir.magnitude == 0f)
			{
				controller.Move(transform.forward * LinearVelocity * Time.deltaTime);
				return;
			}

			float facingAngle = Mathf.Atan2(_currentDir.x, _currentDir.z) * Mathf.Rad2Deg + cam.eulerAngles.y;  // Calculate the angle using Arctan.

			float smoothedAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, facingAngle, ref _turnSmoothVelocity, turnSmoothTime);

			transform.rotation = Quaternion.Euler(0f, smoothedAngle, 0f);  // Set the angle to the rotation attribute.

			Vector3 moveDir = Quaternion.Euler(0f, facingAngle, 0f) * Vector3.forward;
			
			controller.Move(moveDir * LinearVelocity * Time.deltaTime);
		}
	}

	private void OnAimingMode()
	{
		float camEulerY = cam.rotation.eulerAngles.y;
		Quaternion lookRotation = Quaternion.Euler(0f, camEulerY, 0f);

		transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 10f * Time.fixedDeltaTime);

		// Move the target relative to the camera transform.
		_currentDir = cam.right * _moveInputX + cam.forward * _moveInputZ;

		if (_currentDir.magnitude > 0f)
			_previousDir = _currentDir;
		
		controller.Move(_previousDir * LinearVelocity * Time.deltaTime);
	}
}
