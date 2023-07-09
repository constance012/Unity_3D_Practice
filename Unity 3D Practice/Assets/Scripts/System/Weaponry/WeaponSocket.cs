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

	private Weapon inHandWeapon;
	private WeaponRecoil recoil;

	private MultiPositionConstraint holdingPose;
	private MultiPositionConstraint aimingPose;

	private Transform weaponParentPrimary;
	private Transform weaponParentSecondary;

	private Transform weaponPivot;
	private Transform crosshairTarget;

	private Transform rightHandGrip;
	private Transform rightElbowHint;

	private Transform leftHandGrip;
	private Transform leftElbowHint;

	private ParticleSystem muzzleFlash;

	[ReadOnly] public bool forcedAiming;

	// Private fields.
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

		rightHandGrip = weaponPivot.Find("Right Hand Grip");
		rightElbowHint = transform.parent.Find("RigLayer_Hands IK/RightHandIK/Right Elbow Hint");

		leftHandGrip = weaponPivot.Find("Left Hand Grip");
		leftElbowHint = transform.parent.Find("RigLayer_Hands IK/LeftHandIK/Left Elbow Hint");

		muzzleFlash = weaponPivot.GetComponentInChildren<ParticleSystem>("Muzzle Flash");
	}

	private void OnDisable()
	{
		if (inHandWeapon != null && inHandWeapon.weaponType == WeaponType.Ranged)
			(inHandWeapon as RangedWeapon).ClearBullets();

		inHandWeapon = null;
	}

	private void Start()
	{
		if (leftHand == null)
			Debug.LogWarning("Left hand reference is null, please assign it through the inspector.");

		weaponEvent.weaponAnimationCallback.AddListener(OnWeaponEventCallback);
		onWeaponDrop.AddListener(ClearOrientation);

		ammoText.gameObject.SetActive(inHandWeapon != null);
	}

	public void Update()
	{
		// Drop the weapon in hand.
		if (InputManager.instance.GetKeyDown(KeybindingActions.DropItemInHand))
			DropWeapon(inHandWeapon);

		if (inHandWeapon == null)
			return;
		
		if (inHandWeapon.weaponType == WeaponType.Ranged)
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
		inHandWeapon = weapon;
		GetWeaponRecoil(weapon.weaponSlot);

		SetupOrientation();

		if (inHandWeapon.weaponType == WeaponType.Ranged)
		{
			RangedWeapon rangedWeapon = inHandWeapon as RangedWeapon;

			ammoText.text = $"{rangedWeapon.currentMagazineAmmo} / {rangedWeapon.reserveAmmo}";
			ammoText.gameObject.SetActive(true);
		}
	}

	public void UnequipWeapon()
	{
		ClearOrientation();

		inHandWeapon = null;
		recoil = null;
		
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

		droppedItemPrefab.GetComponent<ItemPickup>().itemPrefab = weaponToDrop;
		Vector3 dropPosition = transform.position + mainCamera.forward;
		Debug.Log("Drop position: " + dropPosition);

		GameObject droppedWeapon = Instantiate(droppedItemPrefab, dropPosition, Quaternion.identity);
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

		inHandWeapon = null;
	}

	private void GetWeaponRecoil(WeaponSlot slot)
	{
		switch (slot)
		{
			case WeaponSlot.Primary:
				recoil = weaponParentPrimary.GetComponentInChildren<WeaponRecoil>();
				break;

			case WeaponSlot.Secondary:
				recoil = weaponParentSecondary.GetComponentInChildren<WeaponRecoil>();
				break;

			case WeaponSlot.CloseRange:
				break;
			case WeaponSlot.Throwable:
				break;
		}

		if (recoil.rigAnimator == null)
			recoil.rigAnimator = transform.parent.GetComponent<Animator>();
	}
	#endregion

	#region Weapon Orientation in Hands
	public void ResetWeaponOffset(WeaponSlot slot)
	{
		if (inHandWeapon == null)
			return;

		string weaponName = inHandWeapon.itemName;
		Debug.Log($"Reset the offset of {weaponName} to default.");

		switch (slot)
		{
			case WeaponSlot.Primary:
				weaponParentPrimary.transform.Find(weaponName).SetLocalPositionAndRotation(inHandWeapon.inHolsterOffset, Quaternion.identity);
				break;

			case WeaponSlot.Secondary:
				weaponParentSecondary.transform.Find(weaponName).SetLocalPositionAndRotation(inHandWeapon.inHolsterOffset, Quaternion.identity);
				break;
		}
	}

	private void SetupOrientation()
	{
		// Set the holding pose offset and rotation.
		holdingPose.data.offset = inHandWeapon.holderPositionOffset;
		holdingPose.transform.localRotation = Quaternion.Euler(inHandWeapon.holderLocalEulerAngles);

		aimingPose.data.offset = inHandWeapon.aimingPositionOffset;

		// Set the right hand's orientation.
		rightHandGrip.SetLocalPositionAndRotation(inHandWeapon.rightHandGrip.localPosition,
							Quaternion.Euler(inHandWeapon.rightHandGrip.localEulerAngles));

		rightElbowHint.localPosition = inHandWeapon.rightHandGrip.elbowLocalPosition;

		// Set the left hand's orientation.
		leftHandGrip.SetLocalPositionAndRotation(inHandWeapon.leftHandGrip.localPosition,
							Quaternion.Euler(inHandWeapon.leftHandGrip.localEulerAngles));

		leftElbowHint.localPosition = inHandWeapon.leftHandGrip.elbowLocalPosition;

		// Set the muzzle flash position.
		muzzleFlash.transform.localPosition = inHandWeapon.muzzleFlashLocalPosisiton;
		muzzleFlash.transform.rotation = Quaternion.LookRotation(weaponPivot.right, muzzleFlash.transform.up);
	}

	private void ClearOrientation()
	{
		rightHandGrip.ResetTransform(true);
		rightElbowHint.localPosition = Vector3.zero;

		leftHandGrip.ResetTransform(true);
		leftElbowHint.localPosition = Vector3.zero;

		muzzleFlash.transform.ResetTransform(true);
	}
	#endregion

	#region Weapons using method.
	private void UseRangedWeapon()
	{
		_timeForNextUse -= Time.deltaTime;

		RangedWeapon weapon = inHandWeapon as RangedWeapon;

		weapon.UpdateBullets(Time.deltaTime);

		if (!weapon.isReloading && (InputManager.instance.GetKeyDown(KeybindingActions.Reload) || weapon.promptReload))
			ReloadWeapon(weapon);

		// Use the weapon in hand only if the player is aiming.
		if (!PlayerActions.isAiming)
			return;

		if (_timeForNextUse <= 0f)
			ShootWeapon(weapon);
	}
	#endregion


	#region Weapon shooting.
	private void ShootWeapon(RangedWeapon weapon)
	{
		switch (weapon.useType)
		{
			case UseType.Automatic:
				if (InputManager.instance.GetKey(KeybindingActions.PrimaryAttack))
				{
					SingleFire(weapon);
				}
				break;

			case UseType.Burst:
				if (InputManager.instance.GetKeyDown(KeybindingActions.PrimaryAttack) && _burstCompleted)
				{
					StartCoroutine(BurstFire(weapon));
				}
				break;

			case UseType.Single:
				if (InputManager.instance.GetKeyDown(KeybindingActions.PrimaryAttack))
				{
					SingleFire(weapon);
				}
				break;
		}
	}

	private void SingleFire(RangedWeapon weapon)
	{
		if (weapon.isReloading && !weapon.canShootWhileReloading)
			return;

		ForceStopReloading(weapon);

		Vector3 rayOrigin = muzzleFlash.transform.position;
		Vector3 rayDestination = crosshairTarget.position;

		if (weapon.FireBullet(rayOrigin, rayDestination))
		{
			muzzleFlash.Emit(1);
			recoil.GenerateRecoil();

			ammoText.text = $"{weapon.currentMagazineAmmo} / {weapon.reserveAmmo}";
		}

		_timeForNextUse = weapon.useSpeed;
	}

	private IEnumerator BurstFire(RangedWeapon weapon)
	{
		if (weapon.isReloading && !weapon.canShootWhileReloading)
			yield break;

		ForceStopReloading(weapon);

		_burstCompleted = false;

		for (int i = 0; i < 3; i++)
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
	private void ForceStopReloading(RangedWeapon weapon)
	{
		if (_reloadCoroutine != null)
		{
			StopCoroutine(_reloadCoroutine);

			rigAnimator.Play("Stand By", 2);
			weapon.isReloading = false;
			_reloadCoroutine = null;
		}
	}

	private void ReloadWeapon(RangedWeapon weapon)
	{
		weapon.promptReload = false;
		weapon.isReloading = true;

		if (weapon.reserveAmmo == 0 || weapon.currentMagazineAmmo == weapon.MagazineCapacity)
		{
			weapon.isReloading = false;
			return;
		}

		forcedAiming = true;

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
		yield return new WaitForSeconds(weapon.reloadTime);

		weapon.StandardReload();
		ammoText.text = $"{weapon.currentMagazineAmmo} / {weapon.reserveAmmo}";

		weapon.isReloading = false;
		_reloadCoroutine = null;

		forcedAiming = false;
		onWeaponReloadingDone?.Invoke();
	}

	private IEnumerator SingleRoundReload(RangedWeapon weapon)
	{
		rigAnimator.Play($"Start Reload {weapon.itemName}", 2);

		yield return new WaitForSeconds(.5f);
		
		int firedBullets = weapon.MagazineCapacity - weapon.currentMagazineAmmo;

		for (int i = firedBullets; i > 0; i--)
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

		forcedAiming = false;
		onWeaponReloadingDone?.Invoke();
	}

	#endregion

	#region Weapon Events.
	private void OnWeaponEventCallback(string action)
	{
		action = action.Trim().ToLower().Replace(' ', '_');

		RangedWeapon weapon = inHandWeapon as RangedWeapon;

		switch (action)
		{
			case "eject_bullet_casing":
				EjectBulletCasing(weapon);
				break;

			case "drop_magazine_gun":
				break;

			case "drop_magazine_left_hand":
				break;

			case "drop_magazine_right_hand":
				break;

			case "grab_new_magazine":
				GrabNewMagazine();
				break;

			case "attach_magazine_to_gun":
				AttachMagazineToGun(weapon);
				break;

			case "detach_magazine_to_left_hand":
				DetachMagazineToLeftHand();
				break;

			case "detach_magazine_to_right_hand":
				DetachMagazineToRightHand();
				break;

			default:
				return;
		}
	}

	private void EjectBulletCasing(RangedWeapon weapon)
	{
		Instantiate(weapon.bulletCasing, recoil.caseEjector.position, recoil.caseEjector.rotation);
	}

	private void DetachMagazineToLeftHand()
	{
		_magazineInHand = Instantiate(recoil.magazine, leftHand, true);

		recoil.magazine.SetActive(false);
	}

	private void DetachMagazineToRightHand()
	{
		_magazineInHand = Instantiate(recoil.magazine, rightHand, true);

		recoil.magazine.SetActive(false);
	}

	private void GrabNewMagazine()
	{
		_magazineInHand.SetActive(true);
	}

	private void AttachMagazineToGun(RangedWeapon weapon)
	{
		if (!weapon.inactiveMagazine)
			recoil.magazine.SetActive(true);

		Destroy(_magazineInHand);
	}
	#endregion
}
