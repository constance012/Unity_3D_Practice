using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
	public static T Instance { get; private set; }

	protected virtual void Awake()
	{
		if (Instance == null)
			Instance = this as T;
		else
		{
			string typeName = typeof(T).Name;
			Debug.LogWarning($"More than one Instance of {typeName} found!! Destroy the newest one.");
			
			Destroy(this.gameObject);
			
			return;
		}
	}
}
