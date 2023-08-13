using System.Collections.Generic;
using UnityEngine;

public static class TransformExtensions
{
	/// <summary>
	/// Finds any child with the matching name within this Transform, traverses through the hierarchy using Breadth-first Search.
	/// </summary>
	/// <param name="transform"></param>
	/// <param name="childName"></param>
	/// <returns></returns>
	public static Transform FindAny(this Transform transform, string childName)
	{
		Queue<Transform> queue = new Queue<Transform>();
		queue.Enqueue(transform);

		while (queue.Count > 0)
		{
			Transform current = queue.Dequeue();

			if (current.name == childName)
				return current;

			foreach (Transform grandChild in current)
				queue.Enqueue(grandChild);
		}

		return null;
	}

	/// <summary>
	/// Resets the local or world transform of a game object.
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
