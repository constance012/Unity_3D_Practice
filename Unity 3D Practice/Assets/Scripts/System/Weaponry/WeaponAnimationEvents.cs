using UnityEngine;
using UnityEngine.Events;

public class WeaponAnimationEvents : MonoBehaviour
{
	public class AnimationEventCallback : UnityEvent<string> { }

	public AnimationEventCallback weaponAnimationCallback = new AnimationEventCallback();

	public void OnWeaponAnimation(string action)
	{
		weaponAnimationCallback?.Invoke(action);
	}
}