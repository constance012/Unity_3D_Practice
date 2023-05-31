using UnityEngine;
using CSTGames.CommonEnums;

public class ItemPickup : Interactable
{
	[Header("Item")]
	[Space]
	public Item itemPrefab;
	private Item currentItem;
	private WeaponSocket weaponSocket;

	// This dropped item's components.
	private MeshCollider meshCollider;
	private MeshRenderer meshRenderer;
	private MeshFilter meshFilter;
	private Rigidbody rb;

	protected override void Awake()
	{
		base.Awake();

		weaponSocket = GameObject.FindWithTag("WeaponSocket").GetComponent<WeaponSocket>();
		
		meshCollider = GetComponent<MeshCollider>();
		meshRenderer = GetComponent<MeshRenderer>();
		meshFilter = GetComponent<MeshFilter>();
		rb = GetComponent<Rigidbody>();
	}

	private void Start()
	{
		currentItem = Instantiate(itemPrefab);
		currentItem.name = itemPrefab.itemName;
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

		rb.mass = currentItem.weight;
	}

	private void Pickup()
	{
		Debug.Log($"Picking up {currentItem.name}.");

		if (currentItem.category == ItemCategory.Weapon)
		{
			Weapon weapon = currentItem as Weapon;
			int slotIndex = (int)weapon.weaponSlot;

			PlayerActions.weapons[slotIndex] = weapon;
			PlayerActions.needToRebindAnimator = weapon.rebindAnimator;

			weaponSocket.AddWeaponToHolder(weapon);
		}

		Destroy(this.gameObject);
	}
}
