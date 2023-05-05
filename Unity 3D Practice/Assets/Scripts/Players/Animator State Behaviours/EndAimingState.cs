using UnityEngine;

public class EndAimingState : StateMachineBehaviour
{
	public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		PlayerActions.allowAimingAgain = false;
	}

	public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		animator.SetLayerWeight(layerIndex, 0f);
		PlayerActions.allowAimingAgain = true;
	}
}
