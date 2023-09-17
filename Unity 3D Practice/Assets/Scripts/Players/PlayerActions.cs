using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using static Weapon;
using CSTGames.CommonEnums;

public class PlayerActions : MonoBehaviour
{
	[Header("References")]
	[Space]
	[SerializeField] private Animator rigAnimator;
	[SerializeField] private GrassComputeHandler grassHandler;
	[SerializeField] private GrassPainter grassPainter;

	[Header("Events"), Space]
	public UnityEvent onActiveWeaponSwitched = new UnityEvent();

	public static Weapon[] weapons = new Weapon[4];

	[HideInInspector] public WeaponSocket weaponSocket;

	// Properties.
	public static bool NeedToRebindAnimator { get; set; }
	public static bool IsUnequipingDone { get; set; }
	public static bool IsSwitchingWeapon { get; private set; }
	public static Weapon CurrentActiveWeapon { get { return _currentWeapon; } }

	// Private fields.
	private Vector3 _previousPosition;
	private static Weapon _currentWeapon;

	private void Awake()
	{
		rigAnimator = transform.GetComponentInChildren<Animator>("Model/-----RIG LAYERS-----");
		
		weaponSocket = GameObjectExtensions.GetComponentWithTag<WeaponSocket>("WeaponSocket");
	}

	private void Update()
	{
		//GenerateGrassOnTheRun();

		SelectWeapon();
	}

	private void GenerateGrassOnTheRun()
	{
		if (Vector3.Distance(_previousPosition, transform.position) > .1f)
		{
			Ray ray = new Ray(transform.position, Vector3.down);

			grassPainter.GenerateAtRuntime(ray, 10f);
			grassHandler.UpdateShaderData();

			_previousPosition = transform.position;
		}
	}

	private void SelectWeapon()
	{
		// If the player has no weapon, then returns.
		if (weapons.All(weapon => weapon == null))
			return;

		if (!IsSwitchingWeapon)
		{
			if (Input.GetKeyDown(KeyCode.Alpha1))
				StartCoroutine(SwitchWeapon((int)WeaponSlot.Primary));

			if (Input.GetKeyDown(KeyCode.Alpha2))
				StartCoroutine(SwitchWeapon((int)WeaponSlot.Secondary));
		}
	}

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
		onActiveWeaponSwitched?.Invoke(); // Force stop aiming.
		IsSwitchingWeapon = true;
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

		IsSwitchingWeapon = false;
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

		rigAnimator.Play("Unarmed");

		weaponSocket.ResetWeaponOffset(_currentWeapon.weaponSlot);
		
		StartCoroutine(AnimationHandler.ChangeRigLayerWeight("weapon aiming ik", 0f, 0f));

		transform.rotation = Quaternion.Euler(Camera.main.transform.eulerAngles.y * Vector3.up);
		
		_currentWeapon = null;
	}
}
