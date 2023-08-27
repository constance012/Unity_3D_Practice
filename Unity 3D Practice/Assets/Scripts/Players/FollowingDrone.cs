using UnityEngine;

public class FollowingDrone : MonoBehaviour
{
	[Header("Movement Properties")]
	[Space]
	[SerializeField] private Transform followPoint;
	
	[SerializeField, Range(0f, 10f), Tooltip("How fast would the drone catch up to the player?\n" +
	"Measures in second if used with Slerp, or in deg/s if used with RotateTowards")]
	private float followSpeed;

	[Space]
	[SerializeField, Range(0f, 10f)] private float idleSpeed;
	[SerializeField, Range(0f, 10f)] private float idleAmplitude;

	[Header("Attack Properties")]
	[Space]
	[SerializeField] private RangedWeapon mainTurret;
	[SerializeField] private RangedWeapon missileSystem;

	[Space]
	[SerializeField, Min(0f)] private float engageRadius;	
	[SerializeField, Range(0f, 10f), Tooltip("How hard would the drone lock on its target?")]
	private float lockOnRigidity;
	[SerializeField] private bool homingProjectile;
	[SerializeField] private bool homingMissile;

	[Space]
	[SerializeField] private ParticleSystem leftTurret;
	[SerializeField] private ParticleSystem rightTurret;
	[SerializeField] private Transform leftMissilePod;
	[SerializeField] private Transform rightMissilePod;

	// Private fields.
	private Animator _animator;
	private TrailRenderer[] _trails;
	private Collider[] _targets = new Collider[1];

	private float _animationInterval;
	private float _timeForNextProjectile;
	private float _timeForNextMissile;

	private bool _shootFromLeft;
	private bool _targetInSight;

	private void Awake()
	{
		followPoint = GameObjectExtensions.FindChildTransformWithTag("Player", "Model/-----RIG LAYERS-----/RigLayer_Drone IK/Drone Pose");

		leftTurret = transform.GetComponentInChildren<ParticleSystem>("Model/Weaponry/Left Turret");
		rightTurret = transform.GetComponentInChildren<ParticleSystem>("Model/Weaponry/Right Turret");

		leftMissilePod = transform.Find("Model/Weaponry/Left Missile Pod");
		rightMissilePod = transform.Find("Model/Weaponry/Right Missile Pod");

		_trails = transform.GetComponentsInChildren<TrailRenderer>("Model/Trails");
		_animator = transform.GetComponentInChildren<Animator>("Model");
	}

	private void Start()
	{
		_animationInterval = Random.Range(30f, 45f);
	}

	private void Update()
	{
		if (PlayerMovement.linearVelocity <= 1.5f)
		{
			SetTrailsEmitting(false);
			transform.position = transform.position.SineFluctuate(VectorAxis.Y, idleSpeed, idleAmplitude);
		}
		else
			SetTrailsEmitting(true);

		ManageAnimation();
		ManageWeapons();
	}

	private void LateUpdate()
	{
		if (followPoint == null)
		{
			Debug.LogWarning("No follow point was found, so the drone will not follow the player.");
			return;
		}

		Vector3 newPosition = Vector3.Lerp(transform.position, followPoint.position, Time.deltaTime * followSpeed);
		transform.position = newPosition;
		
		CheckForTargetInRadius();
	}

	#region Weaponry
	private void CheckForTargetInRadius()
	{
		int layerToCheck = 1 << 8;
		
		int collidersCaptured = Physics.OverlapSphereNonAlloc(transform.position, engageRadius, _targets, layerToCheck);

		Quaternion targetRotation;
		float rotateSpeed;

		// If there's a target in sight.
		if (collidersCaptured == 1)
		{
			_targetInSight = true;
			_animationInterval = Random.Range(30f, 45f);
			SetTrailsEmitting(false);
			
			targetRotation = Quaternion.LookRotation(_targets[0].transform.position - this.transform.position);
			rotateSpeed = lockOnRigidity;
		}

		// Otherwise.
		else
		{
			_targetInSight = false;
			_targets[0] = null;
			
			targetRotation = followPoint.rotation;
			rotateSpeed = followSpeed;
		}
		
		Quaternion newRotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotateSpeed);
		transform.rotation = newRotation;
	}

	private void ManageWeapons()
	{
		_timeForNextProjectile -= Time.deltaTime;
		_timeForNextMissile -= Time.deltaTime;

		if (_targetInSight)
		{
			if (_timeForNextProjectile <= 0f)
			{
				FireTurrets();
			}

			if (_timeForNextMissile <= 0f)
			{
				FireMissiles();
			}
		}
	}

	private void FireTurrets()
	{
		ParticleSystem chosenTurret = _shootFromLeft ? leftTurret : rightTurret;

		Vector3 rayOrigin = chosenTurret.transform.position;
		Vector3 rayDirection = transform.forward;

		Transform targetToTrack = homingProjectile ? _targets[0].transform : null;

		if (mainTurret.FireProjectile(new Ray(rayOrigin, rayDirection), targetToTrack))
		{
			chosenTurret.Emit(1);
		}
		
		_shootFromLeft = !_shootFromLeft;

		_timeForNextProjectile = mainTurret.useSpeed;
	}

	private void FireMissiles()
	{
		Transform chosenPod = _shootFromLeft ? leftMissilePod : rightMissilePod;

		Vector3 rayOrigin = chosenPod.position;
		Vector3 rayDirection = chosenPod.up;

		Transform targetToTrack = homingMissile ? _targets[0].transform : null;

		missileSystem.FireProjectile(new Ray(rayOrigin, rayDirection), targetToTrack);

		_timeForNextMissile = missileSystem.useSpeed;
	}
	#endregion

	#region Effect and Animation
	private void SetTrailsEmitting(bool state)
	{
		foreach (var trail in _trails)
			trail.emitting = state;
	}

	private void ManageAnimation()
	{
		_animationInterval -= Time.deltaTime;

		if (_animationInterval <= 0f)
		{
			_animator.Play("Idle");
			_animationInterval = Random.Range(30f, 45f);
		}
	}
	#endregion

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, engageRadius);
	}
}
