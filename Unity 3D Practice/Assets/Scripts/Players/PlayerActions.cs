using System.Collections;
using System.Linq;
using UnityEngine;
using CSTGames.CommonEnums;
using static Weapon;

public class PlayerActions : MonoBehaviour
{
	[Header("Debugging")]
	[Space]
	[SerializeField] private bool holdToAim;
	
	[Header("References")]
	[Space]
	[SerializeField] private Animator rigAnimator;
	[SerializeField] private GameObject crosshair;

	public static Weapon[] weapons = new Weapon[4];

	[HideInInspector] public WeaponSocket weaponSocket;
	private Weapon currentWeapon;

	// Properties.
	public static bool needToRebindAnimator { get; set; }
	public static bool isAiming { get; private set; }
	public static bool isUnequipingDone { get; set; }

	// Private fields.
	private bool _switchingWeapon;

	private void Awake()
	{
		rigAnimator = transform.GetComponentInChildren<Animator>("Model/-----RIG LAYERS-----");
		weaponSocket = GameObjectExtensions.GetComponentWithTag<WeaponSocket>("WeaponSocket");

		crosshair = GameObjectExtensions.FindChildTransformWithTag("UICanvas", "Gun UI/Crosshair").gameObject;
	}

	private void Update()
	{
		SelectWeapon();

		if (currentWeapon == null || _switchingWeapon)
		{
			// Stop aiming if was doing so.
			if (isAiming)
				HandleAiming();

			return;
		}

		HandleAiming();
	}

	private void SelectWeapon()
	{
		// If the player has no weapon, then returns.
		if (weapons.All(weapon => weapon == null))
			return;

		if (!_switchingWeapon)
		{
			if (Input.GetKeyDown(KeyCode.Alpha1))
				StartCoroutine(SwitchWeapon((int)WeaponSlot.Primary));

			if (Input.GetKeyDown(KeyCode.Alpha2))
				StartCoroutine(SwitchWeapon((int)WeaponSlot.Secondary));
		}
	}

	#region Aiming Weapon.
	private void SetAimingBehaviour(bool state, bool setTrigger = true)
	{
		if (state)
		{
			rigAnimator.Play($"Start Aiming {currentWeapon.itemName}", 1);

			StartCoroutine(AnimationHandler.ChangeRigLayerWeight("weapon aiming ik", RigLayerWeightControl.Increase, 1f, .5f));
		}
		else
		{
			if (setTrigger)
				rigAnimator.SetTrigger(AnimationHandler.endAimingHash);

			StartCoroutine(AnimationHandler.ChangeRigLayerWeight("weapon aiming ik", RigLayerWeightControl.Decrease, 0f, .5f));
		}
		
		crosshair.SetActive(state);
		isAiming = state;
	}

	private void CheckForAimingInput()
	{
		if (_switchingWeapon)
			return;

		if (weaponSocket.forcedAiming)
		{
			isAiming = true;
			return;
		}

		if (holdToAim)
		{
			isAiming = false;

			// Check aiming.
			if (Input.GetKey(KeyCode.Mouse1))
				isAiming = true;
		}
		else if (Input.GetKeyDown(KeyCode.Mouse1))
			isAiming = !isAiming;
	}

	private void HandleAiming()
	{
		bool wasAiming = isAiming;

		CheckForAimingInput();
		
		// What to change when starts of stops aiming.
		if (wasAiming != isAiming)
		{
			StopAllCoroutines();
			
			SetAimingBehaviour(isAiming);
		}
	}
	#endregion

	private void EquipWeapon(bool unequip = false)
	{
		switch (currentWeapon.weaponType)
		{
			case WeaponType.Ranged:
				RangedWeapon rangedWeapon = currentWeapon as RangedWeapon;

				if (!unequip)
					rigAnimator.Play($"Ranged-Equip {rangedWeapon.itemName}");
				else 
				{
					rigAnimator.SetTrigger(AnimationHandler.holsterWeaponHash);
				}
				break;

			case WeaponType.Melee:
				break;
		}
	}

	private IEnumerator SwitchWeapon(int newWeaponIndex)
	{
		SetAimingBehaviour(false, isAiming);

		_switchingWeapon = true;
		isUnequipingDone = true;

		// Unequip the current weapon.
		if (currentWeapon != null)
		{
			isUnequipingDone = false;

			EquipWeapon(true);
			weaponSocket.UnequipWeapon();
		}

		yield return new WaitUntil(() => isUnequipingDone);

		// If the new weapon is the same as the current one, switch to unarmed state.
		if (currentWeapon == weapons[newWeaponIndex] || weapons[newWeaponIndex] == null)
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

		_switchingWeapon = false;
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

	/// <summary>
	/// Callback method for the <c>onWeaponPickup</c> event of the <c>WeaponSocket</c> script.
	/// </summary>
	public void OnWeaponPickup(bool forcedRebind = false)
	{
		if (needToRebindAnimator || forcedRebind)
		{
			SetAimingBehaviour(false, isAiming);

			// Switch back to the previous holding weapon after rebinding the animator.
			if (currentWeapon != null)
			{
				int indexBeforeRebind = (int)currentWeapon.weaponSlot;
				currentWeapon = null;

				rigAnimator.Rebind();

				StartCoroutine(SwitchWeapon(indexBeforeRebind));
			}
			// Otherwise, simply rebind.
			else
				rigAnimator.Rebind();

			needToRebindAnimator = false;
		}
	}

	/// <summary>
	/// Callback method for the <c>onWeaponDrop</c> event of the <c>WeaponSocket</c> script.
	/// </summary>
	public void OnWeaponDrop()
	{
		if (currentWeapon == null)
			return;

		SetAimingBehaviour(false, isAiming);
		rigAnimator.Play("Unarmed");

		weaponSocket.ResetWeaponOffset(currentWeapon.weaponSlot);
		
		StartCoroutine(AnimationHandler.ChangeRigLayerWeight("weapon aiming ik", RigLayerWeightControl.Decrease, 0f, 0f));

		transform.rotation = Quaternion.Euler(Camera.main.transform.eulerAngles.y * Vector3.up);
		
		currentWeapon = null;
	}

	public void OnWeaponReloadingDone()
	{
		SetAimingBehaviour(false, isAiming);
	}
}
