using UnityEditor.iOS.Xcode;
using UnityEngine;

public class HideRifleState : StateMachineBehaviour
{
	private PlayerActions playerActions;

	public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		playerActions = animator.GetComponent<PlayerActions>();
	}

	public override void OnStateExit(Animator animator, AnimatorStateInfo animatorStateInfo, int layerIndex)
	{
		playerActions.weaponSocket.HideWeapon();
		animator.SetLayerWeight(1, 0f);
	}
}
