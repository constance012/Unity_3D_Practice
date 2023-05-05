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
	public bool promptReload;
	private bool isReloading;
	[Space]
	public float stability;
	public float range;
	public float impactForce;

	public override void Use()
	{
		if (currentMagazineAmmo == 0)
		{
			promptReload = true;
			return;
		}

		if (isReloading)
			return;

		base.Use();

		RaycastHit hitObject;
		Transform camPos = Camera.main.transform;

		if (Physics.Raycast(camPos.transform.position, camPos.transform.forward, out hitObject, range))
		{
			Debug.Log($"Hit {hitObject.transform.name}");

			if (hitObject.rigidbody != null)
				hitObject.rigidbody.AddForce(-hitObject.normal * impactForce);
		}

		currentMagazineAmmo--;
	}

	public IEnumerator Reload()
	{
		promptReload = false;
		isReloading = true;
		
		if (remainingAmmo == 0)
			yield return null;

		yield return new WaitForSeconds(reloadTime);

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
