using UnityEngine;

public class FirstPersonMovement : PlayerMovement
{
	protected override void Update()
	{
		base.Update();

		if (PlayerActions.isAiming)
			HandleLinearHorizontalMovement();
		else
			HandleStrafeHorizontalMovement();

		currentDir = (transform.right * moveInputX + transform.forward * moveInputZ).normalized;

		if (currentDir.magnitude > 0f)
			previousDir = currentDir;
		
		controller.Move(previousDir * linearVelocity * Time.deltaTime);
	}
}
