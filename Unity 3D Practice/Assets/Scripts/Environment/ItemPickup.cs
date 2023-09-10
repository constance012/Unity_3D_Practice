using UnityEngine;
using CSTGames.CommonEnums;

public class ItemPickup : Interactable
{
	[Header("Item Scriptable Object")]
	[Space]
	public Item itemData;

	private static Transform s_DroppedItemsHolder;

	// Private fields.
	private Item _currentItem;
	private WeaponSocket _weaponSocket;
	private Rigidbody _rb;

	protected override void Awake()
	{
		base.Awake();

		if (s_DroppedItemsHolder == null)
			s_DroppedItemsHolder = GameObject.FindWithTag("DroppedItemsHolder").transform;

		_weaponSocket = GameObjectExtensions.GetComponentWithTag<WeaponSocket>("WeaponSocket");
		_rb = GetComponent<Rigidbody>();
	}

	private void Start()
	{
		_currentItem = Instantiate(itemData);
		_currentItem.name = itemData.itemName;
		InstantiateItem();
	}

	public override void Interact()
	{
		base.Interact();

		Pickup();
	}

	private void InstantiateItem()
	{
		if (transform.parent != s_DroppedItemsHolder)
			transform.parent = s_DroppedItemsHolder;

		GameObject item = Instantiate(_currentItem.prefab, this.transform);
		item.name = _currentItem.name;

		_rb.mass = _currentItem.weight;
	}

	private void Pickup()
	{
		Debug.Log($"Picking up {_currentItem.name}.");

		if (_currentItem.category == ItemCategory.Weapon)
		{
			Weapon weapon = _currentItem as Weapon;
			int slotIndex = (int)weapon.weaponSlot;

			PlayerActions.NeedToRebindAnimator = weapon.rebindAnimator;
			_weaponSocket.AddWeaponToHolder(weapon);

			PlayerActions.weapons[slotIndex] = weapon;
		}

		Destroy(this.gameObject);
	}
}
