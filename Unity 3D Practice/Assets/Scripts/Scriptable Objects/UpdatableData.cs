using UnityEngine;
using System;

public abstract class UpdatableData : ScriptableObject
{
	public event Action OnValuesUpdated;
	public bool autoUpdate;

	public void Notify()
	{
		OnValuesUpdated?.Invoke();
	}
}
