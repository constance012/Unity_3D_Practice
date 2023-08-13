using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

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

	[Header("GUN PROPERTIES")]
	
	[Header("Gun Types"), Space]
	public GunType gunType;

	[Header("Effects (If use raycast shooting)"), Space]
	public TrailRenderer bulletTracer;
	public ParticleSystem bulletImpactEffect;
	public ParticleSystem bulletCasing;

	[Header("Projectiles (If use prefab shooting)."), Space]
	public ProjectileBase projectilePrefab;
	
	[Range(90f, 1000f), Tooltip("How sharp does the projectile turn to reach its target? Measures in deg/s.")]
	public float trackingRigidity;

	[Header("BULLETS"), Space]

	[Header("Properties"), Space]
	[Min(0f)] public float bulletSpeed = 1000f;
	public float bulletDrop = 0f;
	
	[Min(1), Tooltip("How many bullets per shot? Most guns are 1, except the shotgun of course.")] 
	public int bulletsPerShot = 1;
	[Min(3f)] public float bulletMaxLifeTime = 3f;

	[Header("Spreading"), Space]
	public Vector2 bulletSpread;
	[Tooltip("AUTOMATIC ONLY: How fast would this weapon reach its max spread?")] public float spreadGainRate;

	[Header("AMMUNATION"), Space]

	[Header("Ammo Info"), Space]
	[Min(0f)] public int reserveAmmo;

	[field: SerializeField, Min(0f)]
	public int MagazineCapacity { get; private set; }
	
	[Min(0f)] public int currentMagazineAmmo;
	
	public bool infiniteAmmo;

	[Header("Magazine Info"), Space]
	[Range(0f, 10f)] public float magazineDropForce;

	[Tooltip("Is the magazine game object of this gun active?")]
	public bool activeMagazine;

	[Tooltip("Is the magazine game object detached to the player's hand active?")]
	public bool activeMagazineInHand;

	[Header("Reloading Info"), Space]
	[Min(0f)] public float reloadTime;
	public bool canShootWhileReloading;
	public bool hasReloadAnimation;
	[HideInInspector] public bool promptReload;
	[HideInInspector] public bool isReloading;

	[Header("STATS"), Space]
	[Space]
	public float stability;
	[Min(0f)] public float range;
	[Min(0f), Tooltip("The total force applies to rigid bodies each shot.")] public float impactForce;

	// Shift the bits of decimal number "1" to the left 6 times. We'll collide with only the layer at index 6: Player.
	// Combine that with the bitmask of other layers we want to collide with the OR | operator. So we'll collide with only those layers.
	// Invert the mask with the ~ operator, we'll collide with everything except those layers.
	public const int LAYER_TO_RAYCAST = ~(1 << 6 | 1 << 11 | Physics.IgnoreRaycastLayer);
	
	// Properties.
	public bool CanReload => reserveAmmo > 0 && currentMagazineAmmo < MagazineCapacity;

	// Private fields.
	private List<Bullet> _bullets = new List<Bullet>();
	private Vector3 _bulletOrigin;
	private Vector2 _currentSpread;


	#region Bullets Management.
	public void ClearBullets()
	{
		_bullets.ForEach(bullet => Destroy(bullet.tracer.gameObject));
		_bullets.Clear();
	}

	public void ResetSpreading() => _currentSpread.Set(0f, 0f);

	public override bool FireBullet(Vector3 rayOrigin, Vector3 rayDestination)
	{
		if (!CheckForAmmo())
			return false;

		_bulletOrigin = rayOrigin;

		// Calculate the direction and apply the spread.
		Vector3 initialDirection = (rayDestination - _bulletOrigin).normalized;

		InitializeBulletsPerShot(initialDirection);

		if (!infiniteAmmo)
			currentMagazineAmmo--;

		return true;
	}

	public override bool FireBullet(Ray shootRay)
	{
		if (!CheckForAmmo())
			return false;

		_bulletOrigin = shootRay.origin;

		InitializeBulletsPerShot(shootRay.direction);

		if (!infiniteAmmo)
			currentMagazineAmmo--;

		return true;
	}

	public override bool FireProjectile(Ray shootRay, Transform target = null)
	{
		if (!CheckForAmmo())
			return false;

		_bulletOrigin = shootRay.origin;

		// Calculate the direction and apply the spread.
		float spreadX = Random.Range(-bulletSpread.x, bulletSpread.x);
		float spreadY = Random.Range(-bulletSpread.y, bulletSpread.y);

		Vector3 direction = shootRay.direction + new Vector3(spreadX, spreadY, 0f);
		Quaternion rotation = Quaternion.LookRotation(direction);

		ProjectileBase projectile = Instantiate(projectilePrefab, _bulletOrigin, rotation);

		projectile.Initialize(bulletImpactEffect, bulletSpeed, trackingRigidity, bulletMaxLifeTime, impactForce);
		projectile.SetTarget(target);

		if (!infiniteAmmo)
			currentMagazineAmmo--;

		return true;
	}

	/// <summary>
	/// Call this method every frame to update each bullet of this weapon.
	/// </summary>
	/// <param name="deltaTime"></param>
	public void UpdateBullets(float deltaTime)
	{
		_bullets.RemoveAll(DestroyBullet);

		foreach (Bullet bullet in _bullets)
		{
			if (bullet.aliveTime >= bulletMaxLifeTime)
			{
				bullet.readyToBeDestroyed = true;
				continue;
			}

			Vector3 posBefore = GetBulletPosition(bullet);
			bullet.aliveTime += deltaTime;
			Vector3 posAfter = GetBulletPosition(bullet);

			if (Vector3.Distance(_bulletOrigin, posAfter) > range)
			{
				bullet.readyToBeDestroyed = true;
				continue;
			}

			RaycastAtDeltaPosition(posBefore, posAfter, bullet);
		}
	}
	#endregion

	#region Bullet Reloading.
	public void StandardReload()
	{
		int firedBullets = MagazineCapacity - currentMagazineAmmo;

		if (reserveAmmo < firedBullets)
		{
			currentMagazineAmmo += reserveAmmo;
			reserveAmmo = 0;
		}
		else
		{
			currentMagazineAmmo += firedBullets;
			reserveAmmo -= firedBullets;
		}

		isReloading = false;
	}

	public void SingleRoundReload()
	{
		currentMagazineAmmo++;
		reserveAmmo--;
	}
	#endregion

	#region Private Methods.
	private bool DestroyBullet(Bullet bullet)
	{
		if (bullet.readyToBeDestroyed)
			Destroy(bullet.tracer.gameObject);

		return bullet.readyToBeDestroyed;
	}

	private bool CheckForAmmo()
	{
		if (currentMagazineAmmo == 0)
		{
			promptReload = true;
			return false;
		}

		if (isReloading)
			return false;

		return true;
	}

	private void InitializeBulletsPerShot(Vector3 direction)
	{
		for (int i = 0; i < bulletsPerShot; i++)
		{
			ApplySpreading(out float spreadX, out float spreadY);

			Vector3 velocity = (direction + new Vector3(spreadX, spreadY, 0f)) * bulletSpeed;

			Bullet bullet = new Bullet(_bulletOrigin, velocity, bulletTracer);
			_bullets.Add(bullet);
		}
	}

	private void ApplySpreading(out float spreadX, out float spreadY)
	{
		if (useType != UseType.Automatic)
		{
			spreadX = Random.Range(-bulletSpread.x, bulletSpread.x);
			spreadY = Random.Range(-bulletSpread.y, bulletSpread.y);
			return;
		}

		spreadX = Random.Range(-_currentSpread.x, _currentSpread.x);
		spreadY = Random.Range(-_currentSpread.y, _currentSpread.y);

		// The longer the user holds the fire button, the wider the spread.
		if (_currentSpread.magnitude < bulletSpread.magnitude)
		{
			_currentSpread.x += Time.deltaTime * spreadGainRate;
			_currentSpread.y += Time.deltaTime * spreadGainRate;

			_currentSpread.x = Mathf.Clamp(_currentSpread.x, 0f, bulletSpread.x);
			_currentSpread.y = Mathf.Clamp(_currentSpread.y, 0f, bulletSpread.y);
		}
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
		Vector3 rayDirection = end - start;
		float distance = rayDirection.magnitude;

		Ray ray = new Ray(start, rayDirection);

		if (Physics.Raycast(ray, out RaycastHit hitInfo, distance, LAYER_TO_RAYCAST))
		{
			Debug.Log($"Hit {hitInfo.transform.name}");

			ProcessContact(hitInfo);

			bullet.tracer.transform.position = hitInfo.point;
			bullet.readyToBeDestroyed = true;
		}
		else
			bullet.tracer.transform.position = end;
	}

	private void ProcessContact(RaycastHit hitInfo)
	{
		if (hitInfo.rigidbody != null)
			hitInfo.rigidbody.AddForce(-hitInfo.normal * (impactForce / bulletsPerShot));

		ParticleSystem impactObj = Instantiate(bulletImpactEffect);

		MainModule main = impactObj.GetMainModuleInChildren("Decal");
		main.customSimulationSpace = hitInfo.transform;

		impactObj.AlignWithNormal(hitInfo.point, hitInfo.normal);
		impactObj.Emit(1);
	}
	#endregion
}
