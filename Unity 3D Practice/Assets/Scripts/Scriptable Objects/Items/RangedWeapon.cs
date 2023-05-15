using System;
using System.Collections;
using TMPro;
using Unity.Profiling.LowLevel;
using UnityEngine;

[CreateAssetMenu(fileName = "New Ranged Weapon", menuName = "Items/Weapon/Ranged Weapon")]
public sealed class RangedWeapon : Weapon
{
	public enum GunType
	{
		Pistol,
		SMG,
		Rifle,
		Shotgun,
		Sniper,
		Heavy,
		Launcher
	}

	[Header("Types")]
	[Space]
	public GunType gunType;

	[Header("Gun Properties")]
	[Space]
	public int remainingAmmo;
	[field: SerializeField]
	public int MagazineCapacity { get; private set; }
	public int currentMagazineAmmo;
	[Space]
	public float reloadTime;
	[HideInInspector] public bool promptReload;
	[HideInInspector] public bool isReloading;
	[Space]
	public float stability;
	public float range;
	public float impactForce;

	public override bool Fire(out RaycastHit hitInfo)
	{
		hitInfo = new RaycastHit();
		
		if (currentMagazineAmmo == 0)
		{
			promptReload = true;
			return false;
		}

		if (isReloading)
			return false;

		Transform camPos = Camera.main.transform;
		Ray ray = new Ray(camPos.transform.position, camPos.transform.forward);

		if (Physics.Raycast(ray, out hitInfo, range))
		{
			Debug.Log($"Hit {hitInfo.transform.name}");

			if (hitInfo.rigidbody != null)
				hitInfo.rigidbody.AddForce(-hitInfo.normal * impactForce);
		}

		currentMagazineAmmo--;

		return true;
	}

	public void Reload()
	{
		int firedBullets = MagazineCapacity - currentMagazineAmmo;

		int reloadedAmmo;
		if (remainingAmmo < firedBullets)
		{
			reloadedAmmo = currentMagazineAmmo + remainingAmmo;
			remainingAmmo = 0;
		}
		else
		{
			reloadedAmmo = MagazineCapacity;
			remainingAmmo -= firedBullets;
		}

		currentMagazineAmmo = reloadedAmmo;

		isReloading = false;
	}
}
