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
	public Vector2 bulletSpread;

	private List<Bullet> _bullets = new List<Bullet>();

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

	// Shift the bits of decimal number "1" to the left 6 times. We'll collide with only the layer at index 6: Player.
	// Combine that with the bitmask of other layers we want to collide with the OR | operator. So we'll collide with only those layers.
	// Invert the mask with the ~ operator, we'll collide with everything except those layers.
	public const int LAYER_TO_RAYCAST = ~(1 << 6 | 1 << 11 | Physics.IgnoreRaycastLayer);

	public void ClearBullets()
	{
		_bullets.ForEach(bullet => Destroy(bullet.tracer.gameObject));
		_bullets.Clear();
	}

	public override bool FireBullet(Vector3 rayOrigin, Vector3 rayDestination)
	{		
		if (currentMagazineAmmo == 0)
		{
			promptReload = true;
			return false;
		}

		if (isReloading)
			return false;
		
		// Calculate the direction and apply the spread.
		Vector3 initialDirection = (rayDestination - rayOrigin).normalized;

		float spreadX = Random.Range(-bulletSpread.x, bulletSpread.x);
		float spreadY = Random.Range(-bulletSpread.y, bulletSpread.y);

		Vector3 velocity = (initialDirection + new Vector3(spreadX, spreadY, 0f)) * bulletSpeed;

		Bullet bullet = new Bullet(rayOrigin, velocity, bulletTracer);
		_bullets.Add(bullet);

		currentMagazineAmmo--;

		return true;
	}

	public void UpdateBullets(float deltaTime)
	{
		foreach (Bullet bullet in _bullets)
		{
			if (bullet.aliveTime >= bulletMaxLifeTime)
			{
				Destroy(bullet.tracer.gameObject);
				bullet.readyToBeDestroyed = true;
				continue;
			}

			Vector3 posBefore = GetBulletPosition(bullet);
			bullet.aliveTime += deltaTime;
			Vector3 posAfter = GetBulletPosition(bullet);

			RaycastAtDeltaPosition(posBefore, posAfter, bullet);
		}

		_bullets.RemoveAll(bullet => bullet.readyToBeDestroyed);
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

		if (Physics.Raycast(ray, out hitInfo, distance, LAYER_TO_RAYCAST))
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
