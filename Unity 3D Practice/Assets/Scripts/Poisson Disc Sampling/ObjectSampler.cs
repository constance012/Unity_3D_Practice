using System.Collections.Generic;
using UnityEngine;

public class ObjectSampler : MonoBehaviour
{
	[Header("Object Generation Settings"), Space]
	[SerializeField] private float cellRadius = 1f;
	[SerializeField] private float previewRadius = 1f;
	[SerializeField] private Vector2 regionSize = Vector2.one;
	[SerializeField] private int maxSamplingSteps = 30;

	// Private fields.
	private List<Vector2> _points;

	private void OnValidate()
	{
		_points = PoissonDiscSampling.GeneratePoints(cellRadius, regionSize, maxSamplingSteps);
	}

	private void OnDrawGizmos()
	{
		if (_points != null)
		{
			Gizmos.color = Color.white;
			Vector3 regionXZ = new Vector3(regionSize.x, 0f, regionSize.y);
			Gizmos.DrawWireCube(regionXZ / 2f, regionXZ);

			Gizmos.color = Color.yellow;
			foreach (Vector2 point in _points)
			{
				Gizmos.DrawSphere(new Vector3(point.x, 0f, point.y), previewRadius);
			}
		}
	}
}