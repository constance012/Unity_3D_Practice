using UnityEngine;

public class SelfDestructor : MonoBehaviour
{
	[Header("Timer (in Seconds)")]
	public float timeBeforeDestruct;

	[Header("Destruct Configurations")]
	public bool includeParent;
	public bool includeRoot;

	private void Update()
	{
		if (timeBeforeDestruct < 0)
		{
			if (includeParent)
				Destroy(transform.parent.gameObject);

			if (includeRoot)
				Destroy(transform.root.gameObject);

			Destroy(gameObject);
		}

		timeBeforeDestruct -= Time.deltaTime;
	}
}
