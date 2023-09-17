using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class WeaponAnimationEvents : MonoBehaviour
{
	public class AnimationEventCallback : UnityEvent<string> { }

	[Header("Hand References")]
	[Space]
	[SerializeField] private Transform leftHand;
	[SerializeField] private Transform rightHand;

	public AnimationEventCallback weaponAnimationCallback = new AnimationEventCallback();
	public static GameObject MagazineInHand { get { return _magazineInHand; } }

	// Private fields.
	private static GameObject _magazineInHand;
	private FirearmMono _firearm;

	private void Start()
	{
		if (leftHand == null || rightHand == null)
			Debug.LogWarning("Hand references are null, please assign them through the inspector.");

		weaponAnimationCallback.AddListener(OnWeaponEventCallback);
	}
	
	public void OnWeaponAnimation(string action)
	{
		weaponAnimationCallback?.Invoke(action);
	}

	public void OnWeaponHolstered()
	{
		PlayerActions.IsUnequipingDone = true;
	}

	#region Weapon Event Handler.
	private void OnWeaponEventCallback(string action)
	{
		action = action.Trim().ToLower().Replace(' ', '_');

		RangedWeapon weapon = PlayerActions.CurrentActiveWeapon as RangedWeapon;
		_firearm = WeaponSocket.CurrentFirearm;

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
				WeaponSocket.Instance.LoadNewMagazine(weapon);
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
	#endregion
}