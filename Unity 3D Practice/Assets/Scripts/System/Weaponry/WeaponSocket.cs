using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Animations.Rigging;
using TMPro;
using CSTGames.CommonEnums;
using static Weapon;

public class WeaponSocket : Singleton<WeaponSocket>
{
	[SerializeField] private GameObject droppedItemPrefab;

	[Space]
	public UnityEvent onWeaponPickup = new UnityEvent();
	public UnityEvent onWeaponDrop = new UnityEvent();
	
	public static FirearmMono CurrentFirearm { get { return Instance._firearm; } }

	// Private fields.
	private TextMeshProUGUI _ammoText;

	private MultiPositionConstraint _holdingPose;
	private MultiPositionConstraint _aimingPose;

	private Transform _weaponParentPrimary;
	private Transform _weaponParentSecondary;

	private Transform _weaponPivot;
	private Transform _crosshairTarget;

	private ParticleSystem _muzzleFlash;

	private Weapon _inHandWeapon;
	private WeaponReloading _weaponReloading;
	private FirearmMono _firearm;
	
	private float _timeForNextUse;
	private bool _burstCompleted = true;

	protected override void Awake()
	{
		base.Awake();

		_ammoText = GameObjectExtensions.GetComponentWithTag<TextMeshProUGUI>("UICanvas", "Gun UI/Ammo Text");

		_holdingPose = transform.parent.GetComponentInChildren<MultiPositionConstraint>("RigLayer_Weapon Holding IK/Holding Pose");
		_aimingPose = transform.parent.GetComponentInChildren<MultiPositionConstraint>("RigLayer_Weapon Aiming IK/Aiming Pose");

		_weaponParentPrimary = transform.parent.Find("RigLayer_Weapon Slot Holder IK/Primary Weapon Holder");
		_weaponParentSecondary = transform.parent.Find("RigLayer_Weapon Slot Holder IK/Secondary Weapon Holder");

		_weaponPivot = transform.Find("Weapon Pivot");
		_crosshairTarget = Camera.main.transform.Find("Crosshair Target");

		_muzzleFlash = _weaponPivot.GetComponentInChildren<ParticleSystem>("Muzzle Flash");

		_weaponReloading = GetComponent<WeaponReloading>();
	}

	private void OnDisable()
	{
		if (_inHandWeapon != null && _inHandWeapon.weaponType == WeaponType.Ranged)
			(_inHandWeapon as RangedWeapon).ClearBullets();

		_inHandWeapon = null;
	}

	private void Start()
	{
		onWeaponDrop.AddListener(ClearOrientation);

		_ammoText.gameObject.SetActive(_inHandWeapon != null);
	}

	public void Update()
	{
		// Drop the weapon in hand.
		if (InputManager.Instance.GetKeyDown(KeybindingActions.DropItemInHand))
			DropWeapon(_inHandWeapon);

		if (_inHandWeapon == null)
			return;
		
		if (_inHandWeapon.weaponType == WeaponType.Ranged)
			UseRangedWeapon();
	}

	#region Weapons Management
	public void AddWeaponToHolder(Weapon weapon)
	{
		GameObject prefab = Instantiate(weapon.prefab);
		prefab.name = weapon.itemName;

		switch (weapon.weaponSlot) 
		{
			case WeaponSlot.Primary:
				DropWeapon(PlayerActions.weapons[0]);
				prefab.transform.parent = _weaponParentPrimary;
				break;

			case WeaponSlot.Secondary:
				DropWeapon(PlayerActions.weapons[1]);
				prefab.transform.parent = _weaponParentSecondary;
				break;

			case WeaponSlot.CloseRange:
				DropWeapon(PlayerActions.weapons[2]);
				break;
			case WeaponSlot.Throwable:
				DropWeapon(PlayerActions.weapons[3]);
				break;
		}

		prefab.transform.SetLocalPositionAndRotation(weapon.inHolsterOffset, Quaternion.identity);
		prefab.transform.localScale = weapon.inHandScale * Vector3.one;

		onWeaponPickup?.Invoke();
	}

	public void GrabWeapon(Weapon weapon)
	{
		_inHandWeapon = weapon;
		GetWeaponRecoil(weapon.weaponSlot);

		SetupOrientation();

		if (_inHandWeapon.weaponType == WeaponType.Ranged)
		{
			RangedWeapon rangedWeapon = _inHandWeapon as RangedWeapon;

			_ammoText.text = $"{rangedWeapon.currentMagazineAmmo} / {rangedWeapon.reserveAmmo}";
			_ammoText.gameObject.SetActive(true);
		}
	}

	public void UnequipWeapon()
	{
		_weaponReloading.ForceStopReloading(_inHandWeapon);
		ClearOrientation();

		_inHandWeapon = null;
		_firearm = null;
		
		_ammoText.gameObject.SetActive(false);
	}

	public void DropWeapon(Weapon weaponToDrop)
	{
		if (weaponToDrop == null)
			return;

		// Disable UI elements.
		_ammoText.gameObject.SetActive(false);
		_weaponReloading.ForceStopReloading(weaponToDrop);

		// Instantiate a dropped item prefab and throw it out.
		Transform mainCamera = Camera.main.transform;

		droppedItemPrefab.GetComponent<ItemPickup>().itemData = weaponToDrop;
		Vector3 dropPosition = transform.position + mainCamera.forward;
		Debug.Log("Drop position: " + dropPosition);

		GameObject droppedWeapon = Instantiate(droppedItemPrefab, dropPosition, Quaternion.identity);

		droppedWeapon.name = weaponToDrop.itemName;
		droppedWeapon.GetComponent<Rigidbody>().AddForce(mainCamera.forward * 2f, ForceMode.Impulse);

		// Remove the weapon from the weapons array.
		int weaponSlotIndex = (int)weaponToDrop.weaponSlot;
		PlayerActions.weapons[weaponSlotIndex] = null;

		// Destroy the weapon game object in the corresponding parent.
		switch (weaponToDrop.weaponSlot)
		{
			case WeaponSlot.Primary:
				Destroy(_weaponParentPrimary.transform.Find(weaponToDrop.itemName).gameObject);
				break;

			case WeaponSlot.Secondary:
				Destroy(_weaponParentSecondary.transform.Find(weaponToDrop.itemName).gameObject);
				break;
		}

		// Invoke the event.
		onWeaponDrop?.Invoke();

		_inHandWeapon = null;
	}

	private void GetWeaponRecoil(WeaponSlot slot)
	{
		switch (slot)
		{
			case WeaponSlot.Primary:
				_firearm = _weaponParentPrimary.GetComponentInChildren<FirearmMono>();
				break;

			case WeaponSlot.Secondary:
				_firearm = _weaponParentSecondary.GetComponentInChildren<FirearmMono>();
				break;

			case WeaponSlot.CloseRange:
				break;
			case WeaponSlot.Throwable:
				break;
		}

		if (_firearm.rigAnimator == null)
			_firearm.rigAnimator = transform.parent.GetComponent<Animator>();
	}
	#endregion

	#region Weapon Orientation in Hands
	public void ResetWeaponOffset(WeaponSlot slot)
	{
		if (_inHandWeapon == null)
			return;

		string weaponName = _inHandWeapon.itemName;
		Debug.Log($"Reset the offset of {weaponName} to default.");

		switch (slot)
		{
			case WeaponSlot.Primary:
				_weaponParentPrimary.transform.Find(weaponName).SetLocalPositionAndRotation(_inHandWeapon.inHolsterOffset, Quaternion.identity);
				break;

			case WeaponSlot.Secondary:
				_weaponParentSecondary.transform.Find(weaponName).SetLocalPositionAndRotation(_inHandWeapon.inHolsterOffset, Quaternion.identity);
				break;
		}
	}

	private void SetupOrientation()
	{
		// Set the holding pose offset and rotation.
		_holdingPose.data.offset = _inHandWeapon.holderPositionOffset;
		_holdingPose.transform.localRotation = Quaternion.Euler(_inHandWeapon.holderLocalEulerAngles);

		_aimingPose.data.offset = _inHandWeapon.aimingPositionOffset;

		// Set the muzzle flash position.
		_muzzleFlash.transform.localPosition = _inHandWeapon.muzzleFlashLocalPosisiton;
		_muzzleFlash.transform.rotation = Quaternion.LookRotation(_weaponPivot.right, _muzzleFlash.transform.up);
	}

	private void ClearOrientation()
	{
		_muzzleFlash.transform.ResetTransform(true);
	}
	#endregion

	#region Weapons using method.
	private void UseRangedWeapon()
	{
		_timeForNextUse -= Time.deltaTime;

		RangedWeapon weapon = _inHandWeapon as RangedWeapon;

		weapon.UpdateBullets(Time.deltaTime);

		if (!weapon.isReloading && (InputManager.Instance.GetKeyDown(KeybindingActions.Reload) || weapon.promptReload))
			_weaponReloading.ReloadWeapon(weapon);

		// Use the weapon in hand only if the player is aiming.
		if (!WeaponAiming.IsAiming)
			return;

		if (_timeForNextUse <= 0f)
		{
			ShootWeapon(weapon);

			if (weapon.currentMagazineAmmo == 0)
				_firearm.firearmAnimator.TryPlay("Empty Magazine");
		}
	}
	#endregion

	#region Weapon shooting.
	private void ShootWeapon(RangedWeapon weapon)
	{
		switch (weapon.useType)
		{
			case UseType.Automatic:
				if (InputManager.Instance.GetKey(KeybindingActions.PrimaryAttack))
				{
					SingleFire(weapon);
				}
				else
				{
					weapon.ResetSpreading();
				}
				break;

			case UseType.Burst:
				if (InputManager.Instance.GetKeyDown(KeybindingActions.PrimaryAttack) && _burstCompleted)
				{
					StartCoroutine(BurstFire(weapon));
				}
				break;

			case UseType.Single:
				if (InputManager.Instance.GetKeyDown(KeybindingActions.PrimaryAttack))
				{
					SingleFire(weapon);
				}
				break;
		}
	}

	private void SingleFire(RangedWeapon weapon)
	{
		if (weapon.NotAllowShootDuringReload)
			return;

		_weaponReloading.ForceStopReloading(weapon);

		Vector3 rayOrigin = _muzzleFlash.transform.position;
		Vector3 rayDestination = _crosshairTarget.position;

		if (weapon.FireBullet(rayOrigin, rayDestination))
		{
			_muzzleFlash.Emit(1);
			_firearm.GenerateRecoil(weapon.itemName);

			_ammoText.text = $"{weapon.currentMagazineAmmo} / {weapon.reserveAmmo}";
			_timeForNextUse = weapon.useSpeed;
		}
	}

	private IEnumerator BurstFire(RangedWeapon weapon)
	{
		if (weapon.NotAllowShootDuringReload)
			yield break;

		_weaponReloading.ForceStopReloading(weapon);

		_burstCompleted = false;
		int shotCount = Mathf.Min(weapon.currentMagazineAmmo, 3);

		for (int i = 0; i < shotCount; i++)
		{
			yield return new WaitForSeconds(.1f);

			SingleFire(weapon);
		}

		_burstCompleted = true;
		_timeForNextUse = weapon.useSpeed;
	}
	#endregion

	public void LoadNewMagazine(RangedWeapon weapon)
	{
		if (weapon.currentMagazineAmmo == 0)
			_firearm.firearmAnimator.TryPlay("Reloaded Magazine");

		weapon.StandardReload();
		_ammoText.text = $"{weapon.currentMagazineAmmo} / {weapon.reserveAmmo}";
	}
}
