using UnityEngine;

public class JumpStates : StateMachineBehaviour
{
	public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		PlayerMovement.canJumpAgain = true;
	}
}
