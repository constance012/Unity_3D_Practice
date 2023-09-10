using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Animations.Rigging;
using TMPro;
using CSTGames.CommonEnums;
using static Weapon;
using static RangedWeapon;

public class WeaponSocket : MonoBehaviour
{
	[SerializeField] private GameObject droppedItemPrefab;

	[Header("Hand References")]
	[Space]
	[SerializeField] private Transform leftHand;
	[SerializeField] private Transform rightHand;

	[Header("Events")]
	[Space]
	public WeaponAnimationEvents weaponEvent;

	[Space]
	public UnityEvent onWeaponPickup = new UnityEvent();
	public UnityEvent onWeaponDrop = new UnityEvent();
	public UnityEvent onWeaponReloadingDone = new UnityEvent();

	private Animator rigAnimator;
	private TextMeshProUGUI ammoText;

	private MultiPositionConstraint holdingPose;
	private MultiPositionConstraint aimingPose;

	private Transform weaponParentPrimary;
	private Transform weaponParentSecondary;

	private Transform weaponPivot;
	private Transform crosshairTarget;

	private ParticleSystem muzzleFlash;

	public static bool ForcedAiming { get; private set; }

	// Private fields.
	private Weapon _inHandWeapon;
	private FirearmMono _firearm;
	private GameObject _magazineInHand;
	private IEnumerator _reloadCoroutine;
	
	private float _timeForNextUse;
	private bool _burstCompleted = true;

	private void Awake()
	{
		weaponEvent = transform.parent.GetComponent<WeaponAnimationEvents>();

		rigAnimator = transform.parent.GetComponent<Animator>();
		ammoText = GameObjectExtensions.GetComponentWithTag<TextMeshProUGUI>("UICanvas", "Gun UI/Ammo Text");

		holdingPose = transform.parent.GetComponentInChildren<MultiPositionConstraint>("RigLayer_Weapon Holding IK/Holding Pose");
		aimingPose = transform.parent.GetComponentInChildren<MultiPositionConstraint>("RigLayer_Weapon Aiming IK/Aiming Pose");

		weaponParentPrimary = transform.parent.Find("RigLayer_Weapon Slot Holder IK/Primary Weapon Holder");
		weaponParentSecondary = transform.parent.Find("RigLayer_Weapon Slot Holder IK/Secondary Weapon Holder");

		weaponPivot = transform.Find("Weapon Pivot");
		crosshairTarget = Camera.main.transform.Find("Crosshair Target");

		muzzleFlash = weaponPivot.GetComponentInChildren<ParticleSystem>("Muzzle Flash");
	}

	private void OnDisable()
	{
		if (_inHandWeapon != null && _inHandWeapon.weaponType == WeaponType.Ranged)
			(_inHandWeapon as RangedWeapon).ClearBullets();

		_inHandWeapon = null;
	}

	private void Start()
	{
		if (leftHand == null)
			Debug.LogWarning("Left hand reference is null, please assign it through the inspector.");

		weaponEvent.weaponAnimationCallback.AddListener(OnWeaponEventCallback);
		onWeaponDrop.AddListener(ClearOrientation);

		ammoText.gameObject.SetActive(_inHandWeapon != null);
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
				prefab.transform.parent = weaponParentPrimary;
				break;

			case WeaponSlot.Secondary:
				DropWeapon(PlayerActions.weapons[1]);
				prefab.transform.parent = weaponParentSecondary;
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

			ammoText.text = $"{rangedWeapon.currentMagazineAmmo} / {rangedWeapon.reserveAmmo}";
			ammoText.gameObject.SetActive(true);
		}
	}

	public void UnequipWeapon()
	{
		ClearOrientation();

		_inHandWeapon = null;
		_firearm = null;
		
		ammoText.gameObject.SetActive(false);
	}

	public void DropWeapon(Weapon weaponToDrop)
	{
		if (weaponToDrop == null)
			return;

		// Disable UI elements.
		ammoText.gameObject.SetActive(false);

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
				Destroy(weaponParentPrimary.transform.Find(weaponToDrop.itemName).gameObject);
				break;

			case WeaponSlot.Secondary:
				Destroy(weaponParentSecondary.transform.Find(weaponToDrop.itemName).gameObject);
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
				_firearm = weaponParentPrimary.GetComponentInChildren<FirearmMono>();
				break;

			case WeaponSlot.Secondary:
				_firearm = weaponParentSecondary.GetComponentInChildren<FirearmMono>();
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
				weaponParentPrimary.transform.Find(weaponName).SetLocalPositionAndRotation(_inHandWeapon.inHolsterOffset, Quaternion.identity);
				break;

			case WeaponSlot.Secondary:
				weaponParentSecondary.transform.Find(weaponName).SetLocalPositionAndRotation(_inHandWeapon.inHolsterOffset, Quaternion.identity);
				break;
		}
	}

	private void SetupOrientation()
	{
		// Set the holding pose offset and rotation.
		holdingPose.data.offset = _inHandWeapon.holderPositionOffset;
		holdingPose.transform.localRotation = Quaternion.Euler(_inHandWeapon.holderLocalEulerAngles);

		aimingPose.data.offset = _inHandWeapon.aimingPositionOffset;

		// Set the muzzle flash position.
		muzzleFlash.transform.localPosition = _inHandWeapon.muzzleFlashLocalPosisiton;
		muzzleFlash.transform.rotation = Quaternion.LookRotation(weaponPivot.right, muzzleFlash.transform.up);
	}

	private void ClearOrientation()
	{
		muzzleFlash.transform.ResetTransform(true);
	}
	#endregion

	#region Weapons using method.
	private void UseRangedWeapon()
	{
		_timeForNextUse -= Time.deltaTime;

		RangedWeapon weapon = _inHandWeapon as RangedWeapon;

		weapon.UpdateBullets(Time.deltaTime);

		if (!weapon.isReloading && (InputManager.Instance.GetKeyDown(KeybindingActions.Reload) || weapon.promptReload))
			ReloadWeapon(weapon);

		// Use the weapon in hand only if the player is aiming.
		if (!PlayerActions.IsAiming)
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

		ForceStopReloading(weapon);

		Vector3 rayOrigin = muzzleFlash.transform.position;
		Vector3 rayDestination = crosshairTarget.position;

		if (weapon.FireBullet(rayOrigin, rayDestination))
		{
			muzzleFlash.Emit(1);
			_firearm.GenerateRecoil(weapon.itemName);

			ammoText.text = $"{weapon.currentMagazineAmmo} / {weapon.reserveAmmo}";
			_timeForNextUse = weapon.useSpeed;
		}
	}

	private IEnumerator BurstFire(RangedWeapon weapon)
	{
		if (weapon.NotAllowShootDuringReload)
			yield break;

		ForceStopReloading(weapon);

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

	#region Weapons reloading.
	/// <summary>
	/// Stop reloading if are doing so.
	/// </summary>
	/// <param name="weapon"></param>
	public void ForceStopReloading(Weapon weapon)
	{
		if (_reloadCoroutine != null)
		{
			RangedWeapon rangedWeapon = weapon as RangedWeapon;

			StopCoroutine(_reloadCoroutine);
			Destroy(_magazineInHand);

			rigAnimator.Play("Stand By", 2);
			rangedWeapon.isReloading = false;
			ForcedAiming = false;

			_reloadCoroutine = null;
		}
	}

	private void ReloadWeapon(RangedWeapon weapon)
	{
		weapon.promptReload = false;
		weapon.isReloading = true;

		if (!weapon.CanReload)
		{
			weapon.isReloading = false;
			return;
		}

		ForcedAiming = true;

		switch (weapon.gunType)
		{
			case GunType.Shotgun:
				_reloadCoroutine = SingleRoundReload(weapon);
				StartCoroutine(_reloadCoroutine);
				break;

			default:
				_reloadCoroutine = StandardReload(weapon);
				StartCoroutine(_reloadCoroutine);
				break;
		}
	}

	private IEnumerator StandardReload(RangedWeapon weapon)
	{
		rigAnimator.Play($"Reloading {weapon.itemName}", 2, 0f);

		yield return new WaitForSeconds(weapon.reloadTime);

		if (!weapon.hasReloadAnimation)
			LoadNewMagazine(weapon);

		weapon.isReloading = false;
		_reloadCoroutine = null;

		ForcedAiming = false;
		onWeaponReloadingDone?.Invoke();
	}

	private IEnumerator SingleRoundReload(RangedWeapon weapon)
	{
		rigAnimator.Play($"Start Reload {weapon.itemName}", 2);

		yield return new WaitForSeconds(.5f);
		
		while (weapon.CanReload)
		{
			rigAnimator.Play($"Reloading {weapon.itemName}", 2, 0f);

			yield return new WaitForSeconds(weapon.reloadTime);

			weapon.SingleRoundReload();
			ammoText.text = $"{weapon.currentMagazineAmmo} / {weapon.reserveAmmo}";
		}

		rigAnimator.SetTrigger(AnimationHandler.endReloadingHash);

		yield return new WaitForSeconds(.5f);

		weapon.isReloading = false;
		_reloadCoroutine = null;

		ForcedAiming = false;
		onWeaponReloadingDone?.Invoke();
	}

	#endregion

	#region Weapon Events.
	private void OnWeaponEventCallback(string action)
	{
		action = action.Trim().ToLower().Replace(' ', '_');

		RangedWeapon weapon = _inHandWeapon as RangedWeapon;

		switch (action)
		{
			case "eject_bullet_casing":
				EjectBulletCasing(weapon);
				break;

			case "drop_magazine_from_gun":
				DropMagazineFromGun(weapon);
				break;

			case "drop_magazine_from_hand":
				DropMagazineFromHand(weapon);
				break;

			case "grab_new_magazine":
				GrabNewMagazine();
				break;

			case "attach_magazine_to_gun":
				AttachMagazineToGun(weapon);
				break;

			case "detach_magazine_to_left_hand":
				DetachMagazineToLeftHand(weapon);
				break;

			case "detach_magazine_to_right_hand":
				DetachMagazineToRightHand(weapon);
				break;

			case "load_new_magazine":
				LoadNewMagazine(weapon);
				break;

			default:
				return;
		}
	}

	private void EjectBulletCasing(RangedWeapon weapon)
	{
		Instantiate(weapon.bulletCasing, _firearm.caseEjector.position, _firearm.caseEjector.rotation);
	}

	private void DropMagazineFromGun(RangedWeapon weapon)
	{
		GameObject droppedMagazine = Instantiate(_firearm.magazine, _firearm.magazine.WorldPosition(), _firearm.magazine.WorldRotation());

		droppedMagazine.AddComponent<BoxCollider>();
		droppedMagazine.AddComponent<Rigidbody>().AddForce(-_firearm.transform.up * weapon.magazineDropForce, ForceMode.Impulse);
		droppedMagazine.AddComponent<SelfDestructor>().timeBeforeDestruct = 5f;
		droppedMagazine.transform.localScale *= weapon.inHandScale;

		_firearm.magazine.SetActive(false);
	}

	private void DropMagazineFromHand(RangedWeapon weapon)
	{
		GameObject droppedMagazine = Instantiate(_magazineInHand, _magazineInHand.WorldPosition(), _magazineInHand.WorldRotation());

		droppedMagazine.AddComponent<BoxCollider>();
		droppedMagazine.AddComponent<Rigidbody>();
		droppedMagazine.AddComponent<SelfDestructor>().timeBeforeDestruct = 5f;
		droppedMagazine.transform.localScale *= weapon.inHandScale;

		_magazineInHand.SetActive(false);
	}

	private void GrabNewMagazine()
	{
		_magazineInHand.SetActive(true);
	}
	
	private void AttachMagazineToGun(RangedWeapon weapon)
	{
		_firearm.magazine.SetActive(weapon.activeMagazine);

		Destroy(_magazineInHand);
	}
	
	private void DetachMagazineToLeftHand(RangedWeapon weapon)
	{
		_magazineInHand = Instantiate(_firearm.magazine, leftHand, true);

		_magazineInHand.SetActive(weapon.activeMagazineInHand);
		_firearm.magazine.SetActive(weapon.activeMagazine);
	}

	private void DetachMagazineToRightHand(RangedWeapon weapon)
	{
		_magazineInHand = Instantiate(_firearm.magazine, rightHand, true);
		
		_magazineInHand.SetActive(weapon.activeMagazineInHand);
		_firearm.magazine.SetActive(weapon.activeMagazine);
	}

	private void LoadNewMagazine(RangedWeapon weapon)
	{
		if (weapon.currentMagazineAmmo == 0)
			_firearm.firearmAnimator.TryPlay("Reloaded Magazine");

		weapon.StandardReload();
		ammoText.text = $"{weapon.currentMagazineAmmo} / {weapon.reserveAmmo}";
	}
	#endregion
}
