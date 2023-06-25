using UnityEngine;
using CSTGames.CommonEnums;

public abstract class Interactable : MonoBehaviour
{
	[Header("Interaction Radius")]
	[Space]
	[Tooltip("The interact interactRadius of this object.")] public float interactRadius = 5f;

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

		if (distance <= interactRadius)
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

	protected virtual void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, interactRadius);
	}
}
