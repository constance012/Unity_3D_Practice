using UnityEngine;
using CSTGames.CommonEnums;

public class ItemPickup : Interactable
{
	[Header("Item")]
	[Space]
	public Item itemPrefab;
	private Item currentItem;

	// This dropped item's components.
	private MeshCollider meshCollider;
	private MeshRenderer meshRenderer;
	private MeshFilter meshFilter;

	protected override void Awake()
	{
		base.Awake();

		meshCollider = GetComponent<MeshCollider>();
		meshRenderer = GetComponent<MeshRenderer>();
		meshFilter = GetComponent<MeshFilter>();
	}

	private void Start()
	{
		currentItem = Instantiate(itemPrefab);
		AddItem();
	}

	public override void Interact()
	{
		base.Interact();

		Pickup();
	}

	public void AddItem()
	{
		meshFilter.mesh = currentItem.mesh;
		meshCollider.sharedMesh = currentItem.mesh;
		meshRenderer.materials = currentItem.materials;
	}

	private void Pickup()
	{
		Debug.Log($"Picking up {currentItem.name}.");

		if (currentItem.category == ItemCategory.Weapon)
			PlayerActions.weapons[0] = currentItem as Weapon;

		Destroy(this.gameObject);
	}
}
