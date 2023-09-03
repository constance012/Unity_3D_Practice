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
	private Weapon _currentWeapon;

	// Properties.
	public static bool NeedToRebindAnimator { get; set; }
	public static bool IsAiming { get; private set; }
	public static bool IsUnequipingDone { get; set; }

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

		// Stop aiming when unarmed or is switching weapons.
		if (_currentWeapon == null || _switchingWeapon)
		{
			IsAiming = false;
			SetAimingBehaviour(false, false);

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
			rigAnimator.Play($"Start Aiming {_currentWeapon.itemName}", 1);

			StartCoroutine(AnimationHandler.ChangeRigLayerWeight("weapon aiming ik", RigLayerWeightControl.Increase, 1f, .5f));
		}
		else
		{
			if (setTrigger)
				rigAnimator.SetTrigger(AnimationHandler.endAimingHash);

			StartCoroutine(AnimationHandler.ChangeRigLayerWeight("weapon aiming ik", RigLayerWeightControl.Decrease, 0f, .5f));
		}
		
		crosshair.SetActive(state);
		IsAiming = state;
	}

	private void CheckForAimingInput()
	{
		if (_switchingWeapon)
			return;

		if (WeaponSocket.ForcedAiming)
		{
			IsAiming = true;
			return;
		}

		if (holdToAim)
		{
			IsAiming = false;

			// Check aiming.
			if (Input.GetKey(KeyCode.Mouse1))
				IsAiming = true;
		}
		else if (Input.GetKeyDown(KeyCode.Mouse1))
			IsAiming = !IsAiming;
	}

	private void HandleAiming()
	{
		bool wasAiming = IsAiming;

		CheckForAimingInput();
		
		// What to change when starts of stops aiming.
		if (wasAiming != IsAiming)
		{
			StopAllCoroutines();
			
			SetAimingBehaviour(IsAiming);
		}
	}
	#endregion

	private void EquipWeapon(bool unequip = false)
	{
		switch (_currentWeapon.weaponType)
		{
			case WeaponType.Ranged:
				RangedWeapon rangedWeapon = _currentWeapon as RangedWeapon;

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
		SetAimingBehaviour(false, IsAiming);

		_switchingWeapon = true;
		IsUnequipingDone = true;

		// Unequip the current weapon.
		if (_currentWeapon != null)
		{
			IsUnequipingDone = false;

			EquipWeapon(true);
			weaponSocket.UnequipWeapon();
		}

		yield return new WaitUntil(() => IsUnequipingDone);

		// If the new weapon is the same as the current one, switch to unarmed state.
		if (_currentWeapon == weapons[newWeaponIndex] || weapons[newWeaponIndex] == null)
		{
			rigAnimator.Play("Unarmed");
			_currentWeapon = null;
		}
		// Else, equip the new weapon.
		else
		{
			_currentWeapon = weapons[newWeaponIndex];

			weaponSocket.GrabWeapon(_currentWeapon);

			EquipWeapon();
		}

		_switchingWeapon = false;
	}

	/// <summary>
	/// Callback method for camera active event.
	/// </summary>
	public void OnCameraLive()
	{
		if (CameraSwitcher.IsActive(CameraSwitcher.TpsCam))
		{
			CameraSwitcher.TpsCam.m_XAxis.Value = transform.eulerAngles.y;
		}

		else if (CameraSwitcher.IsActive(CameraSwitcher.FpsCam))
		{
			return;
		}
	}

	/// <summary>
	/// Callback method for the <c>onWeaponPickup</c> event of the <c>WeaponSocket</c> script.
	/// </summary>
	public void OnWeaponPickup(bool forcedRebind = false)
	{
		if (NeedToRebindAnimator || forcedRebind)
		{
			SetAimingBehaviour(false, IsAiming);

			// Switch back to the previous holding weapon after rebinding the animator.
			if (_currentWeapon != null)
			{
				int indexBeforeRebind = (int)_currentWeapon.weaponSlot;
				_currentWeapon = null;

				rigAnimator.Rebind();

				StartCoroutine(SwitchWeapon(indexBeforeRebind));
			}
			// Otherwise, simply rebind.
			else
				rigAnimator.Rebind();

			NeedToRebindAnimator = false;
		}
	}

	/// <summary>
	/// Callback method for the <c>onWeaponDrop</c> event of the <c>WeaponSocket</c> script.
	/// </summary>
	public void OnWeaponDrop()
	{
		if (_currentWeapon == null)
			return;

		SetAimingBehaviour(false, IsAiming);
		rigAnimator.Play("Unarmed");

		weaponSocket.ResetWeaponOffset(_currentWeapon.weaponSlot);
		
		StartCoroutine(AnimationHandler.ChangeRigLayerWeight("weapon aiming ik", RigLayerWeightControl.Decrease, 0f, 0f));

		transform.rotation = Quaternion.Euler(Camera.main.transform.eulerAngles.y * Vector3.up);
		
		_currentWeapon = null;
	}

	public void OnWeaponReloadingDone()
	{
		SetAimingBehaviour(false, IsAiming);
	}
}
