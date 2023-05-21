using UnityEngine;
using CSTGames.CommonEnums;
using UnityEditor.iOS.Extensions.Common;

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
	private float turnSmoothVelocity;

	protected override void Awake()
	{
		base.Awake();
		cam = Camera.main.transform;
	}

	protected override void Update()
	{
		base.Update();

		HandleLinearHorizontalMovement();

		if (PlayerActions.isAiming)
		{
			HandleStrafeHorizontalMovement();
			OnAimingMode();
			return;
		}

		currentDir = new Vector3(moveInputX, 0f, moveInputZ).normalized;

		// Move the player.
		if (currentDir.magnitude > 0f || linearVelocity > 0f)
		{
			// Decelerate and then stopping.
			if (currentDir.magnitude == 0f)
			{
				controller.Move(transform.forward * linearVelocity * Time.deltaTime);
				return;
			}

			float facingAngle = Mathf.Atan2(currentDir.x, currentDir.z) * Mathf.Rad2Deg + cam.eulerAngles.y;  // Calculate the angle using Arctan.

			float smoothedAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, facingAngle, ref turnSmoothVelocity, turnSmoothTime);

			transform.rotation = Quaternion.Euler(0f, smoothedAngle, 0f);  // Set the angle to the rotation attribute.

			Vector3 moveDir = Quaternion.Euler(0f, facingAngle, 0f) * Vector3.forward;
			
			controller.Move(moveDir * linearVelocity * Time.deltaTime);
		}
	}

	private void OnAimingMode()
	{
		float camEulerY = cam.rotation.eulerAngles.y;
		Quaternion lookRotation = Quaternion.Euler(0f, camEulerY, 0f);

		transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 10f * Time.fixedDeltaTime);

		// Move the target relative to the camera transform.
		currentDir = cam.right * moveInputX + cam.forward * moveInputZ;

		if (currentDir.magnitude > 0f)
			previousDir = currentDir;
		
		controller.Move(previousDir * linearVelocity * Time.deltaTime);
	}
}
