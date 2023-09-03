using UnityEngine;

public class FirstPersonMovement : PlayerMovement
{
	protected override void Update()
	{
		base.Update();

		HandleStrafeHorizontalMovement();

		_currentDir = (transform.right * _moveInputX + transform.forward * _moveInputZ).normalized;

		if (_currentDir.magnitude > 0f)
			_previousDir = _currentDir;
		
		controller.Move(_previousDir * LinearVelocity * Time.deltaTime);
	}
}
