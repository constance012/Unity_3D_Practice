using System.Collections;
using UnityEngine;

public sealed class Missile : ProjectileBase
{
	private enum DeviateMovementAxis { Vertical, Horizontal, Both }

	[Header("Target Prediction")]
	[Space]
	[SerializeField] private Enemy enemy;
	[SerializeField] private float minDistance;
	[SerializeField] private float maxDistance;
	[SerializeField, Min(0f), Tooltip("How far into the future will this missile predict the movement of its target? Measures in second.")]
	private float maxTimeIntoFuture;

	[Header("Deviation in Flight Path")]
	[Space]
	[SerializeField, Tooltip("How fast would the flight path be deviated? Negative value will invert the deviation direction.")]
	private float deviationFrequency;
	
	[SerializeField, Min(0f), Tooltip("How large is the deviation amount?")]
	private float deviationAmplitude;
	
	[SerializeField] private DeviateMovementAxis deviationAxis;

	// Private fiels.
	private Vector3 _targetPathPrediction;
	private bool _targetIsDynamic;
	private bool _beginTracking;

	private IEnumerator Start()
	{
		yield return 1f;

		if (isHoming)
			_targetIsDynamic = targetToTrack.TryGetComponent<Enemy>(out enemy);
		else
			targetToTrack = this.transform;

		_beginTracking = true;
	}

	private void OnCollisionEnter(Collision other)
	{
		flySpeed = 0f;

		ProcessCollision(other);

		Destroy(gameObject);
	}

	protected override void FixedUpdate()
	{
		TravelForwards();
		
		if (!_beginTracking)
			return;
		
		float distanceToTarget = Vector3.Distance(transform.position, targetToTrack.position);
		float distanceRatio = Mathf.InverseLerp(minDistance, maxDistance, distanceToTarget);

		PredictTargetMovement(distanceRatio);

		DeviateFlightPath(deviationAxis, distanceRatio);

		TrackingTarget();
	}

	public override void ProcessCollision(Collision other)
	{
		ContactPoint contact = other.GetContact(0);

		if (other.rigidbody != null)
			other.rigidbody.AddExplosionForce(impactForce, contact.point, .5f);

		// TODO: To be changed to an explosive effect.
		ParticleSystem impactFx = Instantiate(impactEffect, contact.point, Quaternion.identity);

		impactFx.Emit(1);
	}

	protected override void TrackingTarget()
	{
		Vector3 trackDirection = _targetPathPrediction - transform.position;

		Quaternion trackRotation = Quaternion.LookRotation(trackDirection);
		Quaternion newRotation = Quaternion.RotateTowards(transform.rotation, trackRotation, Time.deltaTime * trackingRigidity);

		rb.MoveRotation(newRotation);
	}

	private void PredictTargetMovement(float distanceRatio)
	{
		if (isHoming)
		{
			if (_targetIsDynamic)
			{
				// The further away we are from the target, the further into the future we predict.
				float timeIntoFuture = Mathf.Lerp(0f, maxTimeIntoFuture, distanceRatio);
				_targetPathPrediction = enemy.rb.position + enemy.rb.velocity * timeIntoFuture;
			}
			else
				_targetPathPrediction = targetToTrack.position;
		}
	}

	private void DeviateFlightPath(DeviateMovementAxis axis, float distanceRatio)
	{
		Vector3 deviation = Vector3.zero;

		float x = Mathf.Cos(Time.time * deviationFrequency) * deviationAmplitude;
		float y = Mathf.Sin(Time.time * deviationFrequency) * deviationAmplitude;

		switch (axis)
		{
			case DeviateMovementAxis.Horizontal:
				deviation.Set(x, 0f, 0f);
				break;

			case DeviateMovementAxis.Vertical:
				deviation.Set(0f, y, 0f);
				break;

			case DeviateMovementAxis.Both:
				deviation.Set(x, y, 0f);
				break;
		}

		Vector3 predictionOffset = deviation * distanceRatio;
		_targetPathPrediction += predictionOffset;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawLine(transform.position, _targetPathPrediction);
	}
}
