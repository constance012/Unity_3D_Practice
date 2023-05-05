using UnityEngine;
using CSTGames.CommonEnums;
using TMPro.EditorUtilities;

public class Interactable : MonoBehaviour
{
	[Header("Interaction Radius")]
	[Space]
	public float radius = 5f;

	[Header("References")]
	[Space]
	[SerializeField] protected Transform player;

	protected bool hasInteracted;

	protected virtual void Awake()
	{
		if (player == null)
			player = GameObject.FindWithTag("Player").transform;
	}

	protected virtual void Update()
	{
		float distance = Vector3.Distance(transform.position, player.position);

		if (distance <= radius)
		{
			// Set some outline or display item's name on the screen.

			if (!hasInteracted && InputManager.instance.GetKeyDown(KeybindingActions.Interact))
			{
				Interact();
				hasInteracted = true;
			}
		}

		else
		{
			hasInteracted = false;
		}
	}

	public virtual void Interact()
	{
		Debug.Log($"Interacting with {transform.name}.");
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, radius);
	}
}
