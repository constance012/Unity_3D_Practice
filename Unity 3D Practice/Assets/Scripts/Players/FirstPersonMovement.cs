using UnityEngine;

public class FirstPersonMovement : PlayerMovement
{
	protected override void Update()
	{
		base.Update();

		float x = InputManager.instance.GetAxisRaw("Horizontal");
		float z = InputManager.instance.GetAxisRaw("Vertical");

		// Use transform.right instead of Vector3.right to move in the local axis.
		Vector3 move = transform.right * x + transform.forward * z;
		controller.Move(move * velocity * Time.deltaTime);
	}
}
