using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Items/Weapon/Base Weapon")]
public class Weapon : Item
{
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

	[Header("Types")]
	[Space]
	public WeaponType weaponType;
	public WieldType wieldType;
	public UseType useType;

	[Header("Orientation")]
	[Space]
	public Vector3 localPositionInHand;
	public Vector3 eulerAnglesInHand;
	public float inHandScale;

	[Header("Shared Properties")]
	[Space]
	public float baseDamage;
	public float durability;
	public float knockBack;

	[Range(0f, 100f)] public float baseCriticalChance;
	public float useSpeed;

	public float sellPrice;
}
