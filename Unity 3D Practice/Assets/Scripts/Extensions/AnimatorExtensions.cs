using UnityEngine;

public static class AnimatorExtensions
{
	public static bool TryPlay(this Animator animator, string stateName)
	{
		if (animator == null)
			return false;

		animator.Play(stateName);
		return true;
	}

	public static bool TryPlay(this Animator animator, string stateName, int layer)
	{
		if (animator == null)
			return false;

		animator.Play(stateName, layer);
		return true;
	}

	public static bool TryPlay(this Animator animator, string stateName, int layer, float normalizedTime)
	{
		if (animator == null)
			return false;

		animator.Play(stateName, layer, normalizedTime);
		return true;
	}

	public static bool TryPlay(this Animator animator, int stateNameHash)
	{
		if (animator == null)
			return false;

		animator.Play(stateNameHash);
		return true;
	}

	public static bool TryPlay(this Animator animator, int stateNameHash, int layer)
	{
		if (animator == null)
			return false;

		animator.Play(stateNameHash, layer);
		return true;
	}

	public static bool TryPlay(this Animator animator, int stateNameHash, int layer, float normalizedTime)
	{
		if (animator == null)
			return false;

		animator.Play(stateNameHash, layer, normalizedTime);
		return true;
	}
}
