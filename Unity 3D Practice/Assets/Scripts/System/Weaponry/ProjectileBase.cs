using UnityEngine;

public abstract class ProjectileBase : MonoBehaviour
{
	[Header("References")]
	[Space]
	[SerializeField] protected Rigidbody rb;
	[SerializeField, Tooltip("The target to track if this projectile is homing.")]
	protected Transform targetToTrack;

	[Header("Effects")]
	[Space]
	[SerializeField] protected ParticleSystem impactEffect;

	[Header("General Properties")]
	[Space]
	[SerializeField] protected float maxLifeTime;
	[SerializeField, ReadOnly] protected float impactForce;
	[SerializeField] protected bool isHoming;
	
	[Header("Movement Properties")]
	[Space]
	[SerializeField] protected float flySpeed;
	[SerializeField, Tooltip("How sharp does the projectile turn to reach its target? Measures in deg/s.")]
	protected float trackingRigidity;

	protected float _aliveTime;

	protected virtual void Awake()
	{
		rb = GetComponent<Rigidbody>();
	}

	protected virtual void Update()
	{
		_aliveTime += Time.deltaTime;

		if (_aliveTime >= maxLifeTime)
		{
			Destroy(gameObject);
		}
	}

	protected virtual void FixedUpdate()
	{
		TravelForwards();

		TrackingTarget();
	}

	public void SetTarget(Transform target)
	{
		this.targetToTrack = target;
		this.isHoming = this.targetToTrack != null;
	}
	
	public virtual void Initialize(ParticleSystem impactEffect, float flySpeed, float trackingRigidity, float maxLifeTime, float impactForce)
	{
		this.impactEffect = impactEffect;
		
		this.flySpeed = flySpeed;
		this.trackingRigidity = trackingRigidity;

		this.maxLifeTime = maxLifeTime;
		this.impactForce = impactForce;
	}

	/// <summary>
	/// Determines what happens if this projectile collides with other objects.
	/// </summary>
	/// <param name="other"></param>
	public abstract void ProcessCollision(Collision other);

	protected void TravelForwards() => rb.velocity = transform.forward * flySpeed;

	protected virtual void TrackingTarget()
	{
		if (isHoming)
		{
			Vector3 trackDirection = targetToTrack.position - transform.position;

			Quaternion trackRotation = Quaternion.LookRotation(trackDirection);
			Quaternion newRotation = Quaternion.RotateTowards(transform.rotation, trackRotation, Time.deltaTime * trackingRigidity);

			rb.MoveRotation(newRotation);
		}
	}
}
