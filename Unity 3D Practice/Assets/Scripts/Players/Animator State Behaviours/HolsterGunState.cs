using UnityEditor.iOS.Xcode;
using UnityEngine;

public class HolsterGunState : StateMachineBehaviour
{
	public override void OnStateUpdate(Animator animator, AnimatorStateInfo animatorStateInfo, int layerIndex)
	{
		if (animatorStateInfo.normalizedTime > .95f && !PlayerActions.isUnequipingDone)
			PlayerActions.isUnequipingDone = true;
	}
}
