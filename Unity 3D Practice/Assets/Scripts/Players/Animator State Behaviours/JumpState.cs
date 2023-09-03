using UnityEngine;

public class JumpState : StateMachineBehaviour
{
	public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		PlayerMovement.CanJumpAgain = false;
	}

	public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		PlayerMovement.CanJumpAgain = true;
		animator.SetBool("IsJumping", false);
	}
}
