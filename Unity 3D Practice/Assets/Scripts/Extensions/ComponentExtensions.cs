using UnityEngine;

public static class ComponentExtensions
{
	/// <summary>
	/// Gets a component of type TComponent in a specified child of this Transform, includes inactive one.
	/// </summary>
	/// <typeparam name="TComponent"></typeparam>
	/// <param name="transform"></param>
	/// <param name="childName"></param>
	/// <returns></returns>
	public static TComponent GetComponentInChildren<TComponent>(this Component component, string childName) where TComponent : Component
	{
		Transform child = component.transform.Find(childName);
		return child.GetComponent<TComponent>();
	}

	/// <summary>
	/// Gets an array of components in a specified child of this Transform, includes inactive ones.
	/// </summary>
	/// <typeparam name="TComponent"></typeparam>
	/// <param name="transform"></param>
	/// <param name="childToSearch"></param>
	/// <returns></returns>
	public static TComponent[] GetComponentsInChildren<TComponent>(this Component component, string childToSearch) where TComponent : Component
	{
		Transform child = component.transform.Find(childToSearch);
		return child.GetComponentsInChildren<TComponent>();
	}

	/// <summary>
	/// Gets a component of type TComponent in a specified sibling of this Transform by name, includes inactive one. 
	/// </summary>
	/// <typeparam name="TComponent"></typeparam>
	/// <param name="transform"></param>
	/// <param name="siblingName"></param>
	/// <returns></returns>
	public static TComponent GetComponentInSibling<TComponent>(this Component component, string siblingName) where TComponent : Component
	{
		Transform sibling = component.transform.parent.Find(siblingName);
		return sibling.GetComponent<TComponent>();
	}
}
