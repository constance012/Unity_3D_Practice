using System;
using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Items/Weapon/Base Weapon")]
public class Weapon : Item
{
	public enum WeaponSlot
	{
		Primary,
		Secondary,
		CloseRange,
		Throwable
	}
	public enum WeaponType
	{
		Ranged,
		Melee
	}

	public enum WieldType
	{
		OneHanded,
		TwoHanded,
	}

	public enum UseType
	{
		Automatic,
		Burst,
		Single
	}

	[Header("TYPES"), Space]
	public WeaponSlot weaponSlot;
	public WeaponType weaponType;
	public WieldType wieldType;
	public UseType useType;

	[Header("HOLDING AND AIMING POSES"), Space]
	[Tooltip("The offset of the Multi-position Constraint component of the holding pose.")]
	public Vector3 holderPositionOffset;
	public Vector3 holderLocalEulerAngles;

	[Tooltip("The offset of the Multi-position Constraint component of the aiming pose.")]
	public Vector3 aimingPositionOffset;

	[Header("ORIENTATION IN HANDS"), Space]
	public Vector3 inHolsterOffset;
	public float inHandScale = 1f;
	public bool rebindAnimator;
	[Space]
	public Vector3 muzzleFlashLocalPosisiton;

	[Header("SHARED PROPERTIES"), Space]
	[Min(0f)]
	public float baseDamage;
	public float headshotDamage;
	public float durability;
	public float knockBack;

	[Range(0f, 100f)] public float baseCriticalChance;
	
	[Tooltip("The interval time in second between each use")]
	public float useSpeed;

	public float sellPrice;

	public virtual bool FireBullet(Vector3 rayOrigin, Vector3 rayDestination)
	{
		return false;
	}

	public virtual bool FireBullet(Ray shootRay)
	{
		return false;
	}

	public virtual bool FireProjectile(Ray shootRay, Transform target = null)
	{
		return false;
	}
}
