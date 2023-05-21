using System.Collections;
using System.Linq;
using UnityEngine;
using CSTGames.CommonEnums;

public class PlayerActions : MonoBehaviour
{
	[Header("References")]
	[Space]
	[SerializeField] private Animator rigAnimator;
	[SerializeField] private GameObject crosshair;

	[SerializeField] private Transform fpsCamPos;

	public static Weapon[] weapons = new Weapon[4];

	[HideInInspector] public WeaponSocket weaponSocket;
	private Weapon currentWeapon;

	// Animator's hashes.
	private int holsterWeaponHash, isAimingHash;

	// Properties.
	public static bool isAiming { get; private set; }
	public static bool isUnequipingDone { get; set; }
	public static Vector3 fpsCamAimingPos { get; } = new Vector3(0f, -0.09f, 0.065f);
	public static Vector3 fpsCamOriginalPos { get; } = new Vector3(0f, 0.065200001f, 0.194900006f);

	// Private fields.
	private bool canSwitchWeapon = true;

	private void Awake()
	{
		rigAnimator = transform.Find("Model/-----RIG LAYERS-----").GetComponent<Animator>();
		weaponSocket = GameObject.FindWithTag("WeaponSocket").GetComponent<WeaponSocket>();

		crosshair = GameObject.FindWithTag("UICanvas").transform.Find("Gun UI/Crosshair").gameObject;

		fpsCamPos = GameObject.FindWithTag("FPSCamPos").transform;
	}

	private void Start()
	{
		holsterWeaponHash = Animator.StringToHash("HolsterWeapon");
		isAimingHash = Animator.StringToHash("IsAiming");
	}

	private void Update()
	{
		SelectWeapon();

		if (currentWeapon == null)
			return;

		HandleAiming();
	}

	private void SelectWeapon()
	{
		// If the player has no weapon, then returns.
		if (weapons.All(weapon => weapon == null))
			return;

		if (Input.GetKeyDown(KeyCode.Alpha1) && canSwitchWeapon)
		{
			StartCoroutine(SwitchWeapon((int)Weapon.WeaponSlot.Primary));
		}

		//if (Input.GetKeyDown(KeyCode.Alpha2))
		//{
		//	SwitchWeapon((int)Weapon.WeaponSlot.Secondary);
		//}
	}

	private void HandleAiming()
	{
		bool wasAiming = isAiming;

		isAiming = false;

		// Check aiming.
		if (Input.GetKey(KeyCode.Mouse1))
		{
			isAiming = true;
		}
		
		// What to change when starts of stops aiming.
		if (wasAiming != isAiming)
		{
			//rigAnimator.SetBool(isAimingHash, isAiming);
			StopAllCoroutines();

			// Start aiming.
			if (isAiming)
			{
				crosshair.SetActive(true);

				fpsCamPos.localPosition = fpsCamAimingPos;
				StartCoroutine(AnimRiggingController.ChangeRigLayerWeight("weapon aiming ik", RigLayerWeightControl.Increase, 1f, .5f));
			}
			
			// Stop aiming.
			else
			{
				crosshair.SetActive(false);

				fpsCamPos.localPosition = fpsCamOriginalPos;
				fpsCamPos.localRotation = Quaternion.identity;
				StartCoroutine(AnimRiggingController.ChangeRigLayerWeight("weapon aiming ik", RigLayerWeightControl.Decrease, 0f, .5f));
			}
		}
	}

	private void EquipWeapon(bool unequip = false)
	{
		switch (currentWeapon.weaponType)
		{
			case Weapon.WeaponType.Ranged:
				RangedWeapon rangedWeapon = currentWeapon as RangedWeapon;

				if (!unequip)
					rigAnimator.Play("Ranged-Equip " + rangedWeapon.gunType);
				else 
				{
					rigAnimator.SetTrigger(holsterWeaponHash);
				}
				break;

			case Weapon.WeaponType.Melee:
				break;
		}
	}

	private IEnumerator SwitchWeapon(int newWeaponIndex)
	{
		canSwitchWeapon = false;
		isUnequipingDone = true;

		// Unequip the current weapon.
		if (currentWeapon != null)
		{
			isUnequipingDone = false;

			EquipWeapon(true);
			weaponSocket.HideWeapon();
		}

		yield return new WaitUntil(() => isUnequipingDone);

		// If the new weapon is the same as the current one, switch to unarmed state.
		if (currentWeapon == weapons[newWeaponIndex])
		{
			rigAnimator.Play("Unarmed");
			currentWeapon = null;
		}
		// Else, equip the new weapon.
		else
		{
			currentWeapon = weapons[newWeaponIndex];
			weaponSocket.GrabWeapon(currentWeapon);

			EquipWeapon();
		}

		canSwitchWeapon = true;
	}

	/// <summary>
	/// Callback method for camera active event.
	/// </summary>
	public void OnCameraLive()
	{
		if (CameraSwitcher.IsActive(CameraSwitcher.tpsCam))
		{
			CameraSwitcher.tpsCam.m_XAxis.Value = transform.eulerAngles.y;
		}

		else if (CameraSwitcher.IsActive(CameraSwitcher.fpsCam))
		{
			return;
		}
	}

	public void OnWeaponDrop()
	{
		isAiming = false;
		crosshair.SetActive(false);
		
		StartCoroutine(AnimRiggingController.ChangeRigLayerWeight("weapon aiming ik", RigLayerWeightControl.Decrease, 0f, 0f));

		fpsCamPos.localPosition = fpsCamOriginalPos;
		fpsCamPos.localRotation = Quaternion.identity;

		transform.rotation = Quaternion.Euler(Camera.main.transform.eulerAngles.y * Vector3.up);
		
		currentWeapon = null;
	}
}