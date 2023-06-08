using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Animations.Rigging;
using TMPro;
using CSTGames.CommonEnums;
using Unity.VisualScripting;

public class WeaponSocket : MonoBehaviour
{
	[SerializeField] private GameObject droppedItemPrefab;

	[Header("Events")]
	[Space]
	public UnityEvent onWeaponPickup = new UnityEvent();
	public UnityEvent onWeaponDrop = new UnityEvent();

	private Animator reloadTextAnim;
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

	private float timeForNextUse;
	private bool burstCompleted = true;

	private void Awake()
	{
		reloadTextAnim = GameObject.FindWithTag("UICanvas").transform.Find("Gun UI/Reload Text").GetComponent<Animator>();
		ammoText = GameObject.FindWithTag("UICanvas").transform.Find("Gun UI/Ammo Text").GetComponent<TextMeshProUGUI>();

		holdingPose = transform.parent.Find("RigLayer_Weapon Holding IK/Holding Pose").GetComponent<MultiPositionConstraint>();
		aimingPose = transform.parent.Find("RigLayer_Weapon Aiming IK/Aiming Pose").GetComponent<MultiPositionConstraint>();

		weaponParentPrimary = transform.parent.Find("RigLayer_Weapon Slot Holder IK/Primary Weapon Holder");
		weaponParentSecondary = transform.parent.Find("RigLayer_Weapon Slot Holder IK/Secondary Weapon Holder");

		weaponPivot = transform.Find("Weapon Pivot");
		crosshairTarget = Camera.main.transform.Find("Crosshair Target");

		rightHandGrip = weaponPivot.Find("Right Hand Grip");
		rightElbowHint = transform.parent.Find("RigLayer_Hands IK/RightHandIK/Right Elbow Hint");

		leftHandGrip = weaponPivot.Find("Left Hand Grip");
		leftElbowHint = transform.parent.Find("RigLayer_Hands IK/LeftHandIK/Left Elbow Hint");

		muzzleFlash = weaponPivot.Find("Muzzle Flash").GetComponent<ParticleSystem>();
	}

	private void Start()
	{
		onWeaponDrop.AddListener(ClearOrientation);

		reloadTextAnim.GetComponent<TextMeshProUGUI>().text = $"Press {InputManager.instance.GetKeyForAction(KeybindingActions.Reload)} to reload.";

		ammoText.gameObject.SetActive(inHandWeapon != null);
	}

	public void Update()
	{
		// Drop the weapon in hand.
		if (InputManager.instance.GetKeyDown(KeybindingActions.DropItemInHand))
			DropWeapon();

		if (inHandWeapon == null)
			return;
		
		if (inHandWeapon.weaponType == Weapon.WeaponType.Ranged)
			UseRangedWeapon();
	}

	public void AddWeaponToHolder(Weapon weapon)
	{
		GameObject prefab = Instantiate(weapon.prefab);
		prefab.name = weapon.itemName;

		if (weapon.weaponSlot == Weapon.WeaponSlot.Primary)
			prefab.transform.parent = weaponParentPrimary;
		else
			prefab.transform.parent = weaponParentSecondary;

		prefab.transform.localPosition = weapon.inHandOffset;
		prefab.transform.localRotation = Quaternion.identity;
		prefab.transform.localScale = weapon.inHandScale * Vector3.one;

		onWeaponPickup?.Invoke();
	}

	public void GrabWeapon(Weapon weapon)
	{
		inHandWeapon = weapon;
		GetWeaponRecoil(weapon.weaponSlot);

		SetupOrientation();

		if (inHandWeapon.weaponType == Weapon.WeaponType.Ranged)
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

	public void DropWeapon()
	{
		if (inHandWeapon == null)
			return;

		// Disable UI elements.
		ammoText.gameObject.SetActive(false);
		
		if (reloadTextAnim.GetBool("Slide In"))
			reloadTextAnim.SetBool("Slide In", false);

		// Instantiate a dropped item prefab and throw it out.
		Transform mainCamera = Camera.main.transform;

		droppedItemPrefab.GetComponent<ItemPickup>().itemPrefab = inHandWeapon;
		Vector3 dropPosition = transform.position + mainCamera.forward;
		Debug.Log("Drop position: " + dropPosition);

		GameObject droppedWeapon = Instantiate(droppedItemPrefab, dropPosition, Quaternion.identity);
		droppedWeapon.GetComponent<Rigidbody>().AddForce(mainCamera.forward * 2f, ForceMode.Impulse);

		// Remove the weapon from the weapons array.
		int weaponSlotIndex = (int)inHandWeapon.weaponSlot;
		PlayerActions.weapons[weaponSlotIndex] = null;

		// Destroy the weapon game object in the corresponding parent.
		switch (inHandWeapon.weaponSlot)
		{
			case Weapon.WeaponSlot.Primary:
				Destroy(weaponParentPrimary.transform.Find(inHandWeapon.itemName).gameObject);
				break;

			case Weapon.WeaponSlot.Secondary:
				Destroy(weaponParentSecondary.transform.Find(inHandWeapon.itemName).gameObject);
				break;
		}

		// Invoke the event.
		onWeaponDrop?.Invoke();

		// Clear the weapon in hands.
		inHandWeapon = null;
	}

	private void GetWeaponRecoil(Weapon.WeaponSlot slot)
	{
		switch (slot)
		{
			case Weapon.WeaponSlot.Primary:
				recoil = weaponParentPrimary.GetComponentInChildren<WeaponRecoil>();
				break;

			case Weapon.WeaponSlot.Secondary:
				recoil = weaponParentSecondary.GetComponentInChildren<WeaponRecoil>();
				break;

			case Weapon.WeaponSlot.CloseRange:
				break;
			case Weapon.WeaponSlot.Throwable:
				break;
		}

		if (recoil.rigAnimator == null)
			recoil.rigAnimator = transform.parent.GetComponent<Animator>();
	}

	private void UseRangedWeapon()
	{
		timeForNextUse -= Time.deltaTime;

		RangedWeapon weapon = inHandWeapon as RangedWeapon;

		weapon.UpdateBullets(Time.deltaTime);

		if (weapon.promptReload && !reloadTextAnim.GetBool("Slide In"))
			reloadTextAnim.SetBool("Slide In", true);
		else if (!weapon.promptReload && reloadTextAnim.GetBool("Slide In"))
			reloadTextAnim.SetBool("Slide In", false);

		if (InputManager.instance.GetKeyDown(KeybindingActions.Reload) && !weapon.isReloading)
			StartCoroutine(Reload(weapon));

		// Use the weapon in hand only if the player is aiming.
		if (!PlayerActions.isAiming)
			return;
		
		if (timeForNextUse <= 0f)
			switch (weapon.useType)
			{
				case Weapon.UseType.Automatic:
					if (InputManager.instance.GetKey(KeybindingActions.PrimaryAttack))
					{
						Shoot(weapon);
						timeForNextUse = weapon.useSpeed;  // Interval before the next shot.
					}
					break;
			
				case Weapon.UseType.Burst:
					if (InputManager.instance.GetKeyDown(KeybindingActions.PrimaryAttack) && burstCompleted)
						StartCoroutine(BurstFire(weapon));
					break;
			
				case Weapon.UseType.Single:
					if (InputManager.instance.GetKeyDown(KeybindingActions.PrimaryAttack))
					{
						Shoot(weapon);
						timeForNextUse = weapon.useSpeed;
					}
					break;
			}
	}

	private void SetupOrientation()
	{
		// Set the holding pose offset and rotation.
		holdingPose.data.offset = inHandWeapon.holderPositionOffset;
		holdingPose.transform.localRotation = Quaternion.Euler(inHandWeapon.holderLocalEulerAngles);

		aimingPose.data.offset = inHandWeapon.aimingPositionOffset;

		// Set the hands' grip references.
		rightHandGrip.localPosition = inHandWeapon.rightHandGrip.localPosition;
		rightHandGrip.localRotation = Quaternion.Euler(inHandWeapon.rightHandGrip.localEulerAngles);
		rightElbowHint.localPosition = inHandWeapon.rightHandGrip.elbowLocalPosition;

		leftHandGrip.localPosition = inHandWeapon.leftHandGrip.localPosition;
		leftHandGrip.localRotation = Quaternion.Euler(inHandWeapon.leftHandGrip.localEulerAngles);
		leftElbowHint.localPosition = inHandWeapon.leftHandGrip.elbowLocalPosition;

		// Set the muzzle flash position.
		muzzleFlash.transform.localPosition = inHandWeapon.muzzleFlashLocalPosisiton;
		muzzleFlash.transform.rotation = Quaternion.LookRotation(weaponPivot.right, muzzleFlash.transform.up);
	}

	private void ClearOrientation()
	{
		rightHandGrip.localPosition = Vector3.zero;
		rightHandGrip.localRotation = Quaternion.identity;
		rightElbowHint.localPosition = Vector3.zero;

		leftHandGrip.localPosition = Vector3.zero;
		leftHandGrip.localRotation = Quaternion.identity;
		leftElbowHint.localPosition = Vector3.zero;

		muzzleFlash.transform.localPosition = Vector3.zero;
		muzzleFlash.transform.rotation = Quaternion.identity;
	}

	private void Shoot(RangedWeapon weapon)
	{
		Vector3 rayOrigin = muzzleFlash.transform.position;
		Vector3 rayDestination = crosshairTarget.position;

		if (weapon.FireBullet(rayOrigin, rayDestination))
		{
			muzzleFlash.Emit(1);
			recoil.GenerateRecoil();

			ammoText.text = $"{weapon.currentMagazineAmmo} / {weapon.reserveAmmo}";
		}
	}

	private IEnumerator BurstFire(RangedWeapon weapon)
	{
		burstCompleted = false;

		for (int i = 0; i < 3; i++)
		{
			yield return new WaitForSeconds(.1f);

			Shoot(weapon);
		}

		burstCompleted = true;
	}

	private IEnumerator Reload(RangedWeapon weapon)
	{
		weapon.promptReload = false;
		weapon.isReloading = true;

		if (weapon.reserveAmmo == 0)
		{
			weapon.isReloading = false;
			yield break;
		}

		yield return new WaitForSeconds(weapon.reloadTime);

		weapon.Reload();
		ammoText.text = $"{weapon.currentMagazineAmmo} / {weapon.reserveAmmo}";
	}
}
