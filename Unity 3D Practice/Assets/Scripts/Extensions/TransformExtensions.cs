using UnityEngine;

public static class TransformExtensions
{
	/// <summary>
	/// Gets a component of type TComponent in any child of this Transform, includes inactive ones.
	/// </summary>
	/// <typeparam name="TComponent"></typeparam>
	/// <param name="transform"></param>
	/// <param name="childName"></param>
	/// <returns></returns>
	public static TComponent GetComponentInChildren<TComponent>(this Transform transform, string childName)
	{
		Transform child = transform.Find(childName);
		return child.GetComponent<TComponent>();
	}

	/// <summary>
	/// Gets an array of components in a specified child of this Transform, includes inactive ones.
	/// </summary>
	/// <typeparam name="TComponent"></typeparam>
	/// <param name="transform"></param>
	/// <param name="childToSearch"></param>
	/// <returns></returns>
	public static TComponent[] GetComponentsInChildren<TComponent>(this Transform transform, string childToSearch)
	{
		Transform child = transform.Find(childToSearch);
		return child.GetComponentsInChildren<TComponent>();
	}

	/// <summary>
	/// Reset the local or world transform of a game object.
	/// </summary>
	/// <param name="transform"></param>
	/// <param name="isLocal"></param>
	public static void ResetTransform(this Transform transform, bool isLocal = false)
	{
		if (isLocal)
		{
			transform.localPosition = Vector3.zero;
			transform.localRotation = Quaternion.identity;
		}
		else
		{
			transform.position = Vector3.zero;
			transform.rotation = Quaternion.identity;
		}

		transform.localScale = Vector3.one;
	}
}
