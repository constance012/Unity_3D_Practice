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
		cam = GameObject.FindWithTag("MainCamera").transform;
	}

	protected override void Update()
	{
		base.Update();

		Vector3 direction = new Vector3(velocityX, 0f, velocityZ).normalized;

		// Move the player.
		if (direction.magnitude > 0f || linearVelocity > 0f)
		{
			// Decelerate and then stopping.
			if (direction.magnitude == 0f)
			{
				controller.Move(transform.forward * linearVelocity * Time.deltaTime);
				return;
			}

			float facingAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;  // Calculate the angle using Arctan.

			float smoothedAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, facingAngle, ref turnSmoothVelocity, turnSmoothTime);

			transform.rotation = Quaternion.Euler(0f, smoothedAngle, 0f);  // Set the angle to the rotation attribute.

			Vector3 moveDir = Quaternion.Euler(0f, facingAngle, 0f) * Vector3.forward;
			
			controller.Move(moveDir * linearVelocity * Time.deltaTime);
		}
	}
}
