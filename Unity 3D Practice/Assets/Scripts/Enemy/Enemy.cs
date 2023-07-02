using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Enemy : MonoBehaviour
{
	[SerializeField] private Rigidbody rigidBody;

	public Rigidbody rb => rigidBody;

	private void Awake()
	{
		rigidBody = GetComponentInChildren<Rigidbody>(true);
	}
}