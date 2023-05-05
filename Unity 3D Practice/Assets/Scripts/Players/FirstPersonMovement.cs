using UnityEngine;

public class FirstPersonMovement : PlayerMovement
{
	protected override void Update()
	{
		base.Update();

		// Use transform.right instead of Vector3.right to move in the local axis.

		currentDir = (transform.right * velocityX + transform.forward * velocityZ).normalized;

		if (currentDir.magnitude > 0f)
			previousDir = currentDir;
		
		controller.Move(previousDir * linearVelocity * Time.deltaTime);
	}
}
