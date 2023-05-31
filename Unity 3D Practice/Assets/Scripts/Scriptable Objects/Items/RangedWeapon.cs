using System.Collections.Generic;
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

	private class Bullet
	{
		public bool readyToBeDestroyed;
		public float aliveTime;

		public Vector3 initialPosition;
		public Vector3 initialVelocity;
		
		public TrailRenderer tracer;

		public Bullet(Vector3 initialPosition, Vector3 initialVelocity, TrailRenderer tracer)
		{
			this.aliveTime = 0f;

			this.initialPosition = initialPosition;
			this.initialVelocity = initialVelocity;

			this.tracer = Instantiate(tracer, initialPosition, Quaternion.identity);
			this.tracer.AddPosition(initialPosition);
		}
	}

	[Header("Gun Properties")]
	[Space]
	
	[Header("Gun Types")]
	[Space]
	public GunType gunType;

	[Header("Effect")]
	[Space]
	public TrailRenderer bulletTracer;
	public ParticleSystem bulletImpactEffect;

	[Space]
	[Header("Ammunition")]
	[Space]
	public float bulletSpeed = 1000f;
	public float bulletDrop = 0f;
	public float bulletMaxLifeTime = 3f;

	private List<Bullet> bullets = new List<Bullet>();

	[Space]
	public int reserveAmmo;

	[field: SerializeField]
	public int MagazineCapacity { get; private set; }
	public int currentMagazineAmmo;
	
	[Space]
	public float reloadTime;
	[HideInInspector] public bool promptReload;
	[HideInInspector] public bool isReloading;

	[Header("Stats")]
	[Space]
	public float stability;
	public float range;
	public float impactForce;

	public override bool FireBullet(Vector3 rayOrigin, Vector3 rayDestination)
	{		
		if (currentMagazineAmmo == 0)
		{
			promptReload = true;
			return false;
		}

		if (isReloading)
			return false;

		Vector3 velocity = (rayDestination - rayOrigin).normalized * bulletSpeed;
		Bullet bullet = new Bullet(rayOrigin, velocity, bulletTracer);
		bullets.Add(bullet);

		currentMagazineAmmo--;

		return true;
	}

	public void UpdateBullets(float deltaTime)
	{
		bullets.ForEach(bullet =>
		{
			Vector3 posBefore = GetBulletPosition(bullet);
			bullet.aliveTime += deltaTime;
			Vector3 posAfter = GetBulletPosition(bullet);

			RaycastAtDeltaPosition(posBefore, posAfter, bullet);
		});

		bullets.RemoveAll(bullet => bullet.aliveTime >= bulletMaxLifeTime);
	}

	public void Reload()
	{
		int firedBullets = MagazineCapacity - currentMagazineAmmo;

		int reloadedAmmo;
		if (reserveAmmo < firedBullets)
		{
			reloadedAmmo = currentMagazineAmmo + reserveAmmo;
			reserveAmmo = 0;
		}
		else
		{
			reloadedAmmo = MagazineCapacity;
			reserveAmmo -= firedBullets;
		}

		currentMagazineAmmo = reloadedAmmo;

		isReloading = false;
	}

	/// <summary>
	/// Gets the current position of the specified bullet as it travels in the air.
	/// </summary>
	/// <param name="bullet"></param>
	/// <returns></returns>
	private Vector3 GetBulletPosition(Bullet bullet)
	{
		// Formula: InitPos + (vel * time) + (0.5 * gravity * time^2).
		Vector3 gravity = bulletDrop * Vector3.down;

		return bullet.initialPosition + (bullet.initialVelocity * bullet.aliveTime) + (.5f * gravity * Mathf.Pow(bullet.aliveTime, 2));
	}
	
	private void RaycastAtDeltaPosition(Vector3 start, Vector3 end, Bullet bullet)
	{
		RaycastHit hitInfo;
		
		Vector3 rayDirection = end - start;
		float distance = rayDirection.magnitude;

		Ray ray = new Ray(start, rayDirection);

		// Shift the bits of decimal number "1" to the left 6 times. We'll collide with only the layer at index 6: Player.
		// Combine that with the bitmask of the "IgnoreRaycast" layer with the OR | operator. So we'll collide with only those 2 layers.
		// Invert the mask with the ~ operator, we'll collide with everything except those 2 layers.
		int layerMask = ~(1 << 6 | Physics.IgnoreRaycastLayer);

		if (Physics.Raycast(ray, out hitInfo, distance, layerMask))
		{
			Debug.Log($"Hit {hitInfo.transform.name}");

			if (hitInfo.rigidbody != null)
				hitInfo.rigidbody.AddForce(-hitInfo.normal * impactForce);

			ParticleSystem impactObj = Instantiate(bulletImpactEffect);

			impactObj.transform.position = hitInfo.point;
			impactObj.transform.forward = hitInfo.normal;
			impactObj.Emit(1);

			bullet.tracer.transform.position = hitInfo.point;
			bullet.aliveTime = bulletMaxLifeTime;
		}
		else
			bullet.tracer.transform.position = end;
	}
}
