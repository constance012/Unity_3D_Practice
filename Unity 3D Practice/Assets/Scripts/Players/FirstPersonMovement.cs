using UnityEngine;

public class FirstPersonMovement : PlayerMovement
{
	protected override void Update()
	{
		base.Update();

		// Use transform.right instead of Vector3.right to move in the local axis.
		Vector3 moveDir = transform.right * velocityX + transform.forward * velocityZ;

		if (moveDir.magnitude > 0f)
			controller.Move(moveDir * linearVelocity * Time.deltaTime);
		else
			controller.Move(transform.forward * linearVelocity * Time.deltaTime);
	}
}
