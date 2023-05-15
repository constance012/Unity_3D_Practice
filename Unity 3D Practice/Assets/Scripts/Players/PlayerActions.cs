using Cinemachine;
using System.Linq;
using UnityEngine;

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
	private int aimingVertical, grabRifle, hideRifle, startAiming, stopAiming;

	// Properties.
	public static bool isAiming { get; private set; }
	public static bool allowAimingAgain { get; set; } = true;
	public static Vector3 fpsCamAimingPos { get; } = new Vector3(0.00469999993f, -0.0322999991f, 0.0869999975f);
	public static Vector3 fpsCamOriginalPos { get; } = new Vector3(0f, 0.065200001f, 0.194900006f);

	private void Awake()
	{
		rigAnimator = transform.Find("Model/-----RIG LAYERS-----").GetComponent<Animator>();
		weaponSocket = GameObject.FindWithTag("WeaponSocket").GetComponent<WeaponSocket>();

		crosshair = GameObject.FindWithTag("UICanvas").transform.Find("Gun UI/Crosshair").gameObject;

		fpsCamPos = GameObject.FindWithTag("FPSCamPos").transform;
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

		bool isWeaponHeld = currentWeapon != null;

		if (Input.GetKey(KeyCode.Alpha1) && isWeaponHeld)
		{
			EquipWeapon(true);
			currentWeapon = null;
		}

		if (Input.GetKey(KeyCode.Alpha2) && !isWeaponHeld)
		{
			currentWeapon = weapons[0];
			weaponSocket.GrabWeapon(currentWeapon);

			EquipWeapon();
		}
	}

	private void HandleAiming()
	{
		bool wasAiming = isAiming;

		isAiming = false;

		// Check aiming.
		if (Input.GetKey(KeyCode.Mouse1) && allowAimingAgain)
		{
			isAiming = true;
			fpsCamPos.rotation = Quaternion.LookRotation(weaponSocket.transform.right, fpsCamPos.up);
		}
		
		// What to change when starts of stops aiming.
		if (wasAiming != isAiming)
		{
			// Start aiming.
			if (isAiming)
			{
				if (CameraSwitcher.IsActive(CameraSwitcher.tpsCam))
					crosshair.SetActive(true);

				fpsCamPos.localPosition = fpsCamAimingPos;
			}
			
			// Stop aiming.
			else
			{
				crosshair.SetActive(false);

				fpsCamPos.localPosition = fpsCamOriginalPos;
				fpsCamPos.localRotation = Quaternion.identity;
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
					rigAnimator.SetTrigger("UnequipWeapon");
				break;

			case Weapon.WeaponType.Melee:
				break;
		}
	}

	/// <summary>
	/// Callback method for camera active event.
	/// </summary>
	public void OnCameraLive()
	{
		if (CameraSwitcher.IsActive(CameraSwitcher.tpsCam))
		{
			CameraSwitcher.tpsCam.m_XAxis.Value = transform.eulerAngles.y;
			crosshair.SetActive(isAiming);
		}

		else if (CameraSwitcher.IsActive(CameraSwitcher.fpsCam))
		{
			return;
		}
	}
}
