using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public static class TransformExtensions
{
	public enum TransformAxis { LocalX, LocalY, LocalZ, WorldX, WorldY, WorldZ }

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

	public static bool Find(this Transform transform, string childName, out Transform result)
	{
		result = transform.Find(childName);

		return result != null;
	}

	public static GameObject CreateEmptyChild(this Transform parent, string childName)
	{
		GameObject child = new GameObject(childName);

		child.transform.parent = parent;
		child.transform.ResetTransform(true);

		return child;
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
