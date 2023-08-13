using UnityEngine;

public class HolsterGunState : StateMachineBehaviour
{
	public override void OnStateUpdate(Animator animator, AnimatorStateInfo animatorStateInfo, int layerIndex)
	{
		if (animatorStateInfo.normalizedTime > .95f && !PlayerActions.IsUnequipingDone)
			PlayerActions.IsUnequipingDone = true;
	}
}
