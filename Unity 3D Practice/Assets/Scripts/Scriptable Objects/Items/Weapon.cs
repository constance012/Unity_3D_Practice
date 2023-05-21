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

	[Serializable]
	public struct HandGripReferences
	{
		public Vector3 localPosition;
		public Vector3 localEulerAngles;

		public Vector3 elbowLocalPosition;
	}

	[Header("Types")]
	[Space]
	public WeaponSlot weaponSlot;
	public WeaponType weaponType;
	public WieldType wieldType;
	public UseType useType;

	[Header("Orientation in Hand")]
	[Space]
	public HandGripReferences rightHandGrip;
	public HandGripReferences leftHandGrip;
	[Space]
	public Vector3 inHandOffset;
	public float inHandScale;
	[Space]
	public Vector3 particlesLocalPosisiton;

	[Header("Shared Properties")]
	[Space]
	public float baseDamage;
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
}
