using System.Collections;
using UnityEngine;
using TMPro;
using static RangedWeapon;
using UnityEngine.Events;

public class WeaponReloading : MonoBehaviour
{
	[Header("Event"), Space]
	public UnityEvent onWeaponReloadingDone = new UnityEvent();

	private Animator _rigAnimator;
	private TextMeshProUGUI _ammoText;
	private IEnumerator _reloadCoroutine;

	private void Awake()
	{
		_ammoText = GameObjectExtensions.GetComponentWithTag<TextMeshProUGUI>("UICanvas", "Gun UI/Ammo Text");
		_rigAnimator = transform.parent.GetComponent<Animator>();
	}

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
			Destroy(WeaponAnimationEvents.MagazineInHand);

			_rigAnimator.Play("Stand By", 3);
			rangedWeapon.isReloading = false;
			WeaponAiming.ForcedAiming = false;

			_reloadCoroutine = null;
		}
	}

	public void ReloadWeapon(RangedWeapon weapon)
	{
		weapon.promptReload = false;
		weapon.isReloading = true;

		if (!weapon.CanReload)
		{
			weapon.isReloading = false;
			return;
		}

		WeaponAiming.ForcedAiming = true;

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
		_rigAnimator.Play($"Reloading {weapon.itemName}", 3, 0f);

		yield return new WaitForSeconds(weapon.reloadTime);

		if (!weapon.hasReloadAnimation)
			WeaponSocket.Instance.LoadNewMagazine(weapon);

		weapon.isReloading = false;
		_reloadCoroutine = null;

		WeaponAiming.ForcedAiming = false;
		onWeaponReloadingDone?.Invoke();
	}

	private IEnumerator SingleRoundReload(RangedWeapon weapon)
	{
		_rigAnimator.Play($"Start Reload {weapon.itemName}", 3);

		_rigAnimator.SetTrigger(AnimationHandler.startReloadingHash);

		yield return new WaitForSeconds(.5f);

		while (weapon.CanReload)
		{
			_rigAnimator.Play($"Reloading {weapon.itemName}", 3, 0f);

			yield return new WaitForSeconds(weapon.reloadTime);

			weapon.SingleRoundReload();
			_ammoText.text = $"{weapon.currentMagazineAmmo} / {weapon.reserveAmmo}";
		}

		_rigAnimator.SetTrigger(AnimationHandler.endReloadingHash);

		yield return new WaitForSeconds(.5f);

		weapon.isReloading = false;
		_reloadCoroutine = null;

		WeaponAiming.ForcedAiming = false;
		onWeaponReloadingDone?.Invoke();
	}
	#endregion
}
