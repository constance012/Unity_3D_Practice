using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using static TransformExtensions;
using System.Linq;

[CreateAssetMenu(fileName = "New Generable", menuName = "Procedural Generation/Generable Object")]
public class GenerableObject : ScriptableObject
{
	public enum GenerableType { Tree, Grass, Bush, Stone, Object, Other}
	public enum SampleMode { Random, FullCoverage }

	[Header("Prefab to generate"), Space]
	public GameObject prefab;
	public GenerableType prefabType;
	public SampleMode sampleMode;

	[Header("Raycast Settings"), Space]
	[Tooltip("The density of sample points, only accounts if the sample mode is Random.")]
	public int density;

	[Range(0f, 1f)] public float minHeight;
	[Range(0f, 1f)] public float maxHeight;

	[ReadOnly] public Vector2 sampleXRange;
	[ReadOnly] public Vector2 sampleZRange;

	[Header("Prefab Rotation Settings"), Space]
	public TransformAxis rotateAxis;

	[Range(0f, 1f), Tooltip("An interpolation ratio. Determines how much this object rotates so that its up vector matches the normal vector of the hit point.")]
	public float rotateTowardsNormalTendency;

	[Tooltip("The range in which this object can rotate, in degrees.")]
	public Vector2 rotationRange;

	[Header("Prefab Scale Settings"), Space]
	[Tooltip("The scale multiplier range of this object, on all axes.")]
	public Vector2 scaleRange;

	public const int ENVIRONMENT_LAYER_MASK = 1 << 7;

	public void Generate(Transform rootParent, GenerableData data, Action<bool> returnCallback = null)
	{
		if (prefab == null)
		{
			Debug.LogWarning("No prefab found to generate, please asign it first.");
			return;
		}
		
		switch (prefabType)
		{
			case GenerableType.Grass:
				GenerateGrass(rootParent, data, returnCallback);
				break;

			default:
				GeneratePrefab(rootParent, data, returnCallback);
				break;
		}
	}

	private void GenerateGrass(Transform rootParent, GenerableData data, Action<bool> returnBack)
	{
		float chunkScale = rootParent.parent.localScale.x;

		GameObject prefabInstance = Instantiate(prefab, rootParent);
		
		prefabInstance.transform.ResetTransform(true);
		prefabInstance.transform.localScale = Vector3.one / chunkScale; // Keep the world scale at 1.
		prefabInstance.name = prefab.name;

		GrassPainter painter = prefabInstance.GetComponent<GrassPainter>();

		for (int i = 0; i < data.samplePoints.Length; i++)
		{
			Ray ray = new Ray(data.GetRayOrigin(i), Vector3.down);

			painter.GenerateAtRuntime(ray, data.RayMaxDistance);
		}

		prefabInstance.GetComponent<GrassComputeHandler>().UpdateShaderData();

		bool success = painter.mesh.vertices.Length > 0;
		returnBack?.Invoke(success);
	}

	private void GeneratePrefab(Transform rootParent, GenerableData data, Action<bool> returnCallback)
	{
		string parentName = prefabType.ToString();

		if (!rootParent.Find(parentName, out Transform parent))
			parent = rootParent.CreateEmptyChild(parentName).transform;

		Vector3 rotateAxis = GetRotateAxis();

		for (int i = 0; i < data.samplePoints.Length; i++)
		{
			Ray ray = new Ray(data.GetRayOrigin(i), Vector3.down);

			if (!Physics.Raycast(ray, out RaycastHit hit, data.RayMaxDistance, ENVIRONMENT_LAYER_MASK))
				continue;

			GameObject prefabInstance = null;

			#if UNITY_EDITOR
			if (!Application.isPlaying)
				prefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
			#endif

			if (Application.isPlaying)
			{
				prefabInstance = Instantiate(prefab, parent);
				prefabInstance.name = prefab.name;
			}

			prefabInstance.transform.position = hit.point;

			// Apply random rotation.
			float rotateDegree = UnityEngine.Random.Range(rotationRange.x, rotationRange.y);
			prefabInstance.transform.Rotate(rotateAxis, rotateDegree, Space.Self);

			// Align with the hit point normal vector.
			Quaternion matchNormalRotation = prefabInstance.transform.rotation * Quaternion.FromToRotation(rotateAxis, hit.normal);
			prefabInstance.transform.rotation = Quaternion.Lerp(prefabInstance.transform.rotation, matchNormalRotation, rotateTowardsNormalTendency);

			// Apply random scale.
			prefabInstance.transform.localScale = Vector3.one * UnityEngine.Random.Range(scaleRange.x, scaleRange.y);
		}

		returnCallback?.Invoke(parent.childCount != 0);
	}

	public void SetSampleRange(Bounds rendererBound)
	{
		Vector2 xRange = new Vector2(rendererBound.center.x - rendererBound.extents.x, rendererBound.center.x + rendererBound.extents.x);
		Vector2 zRange = new Vector2(rendererBound.center.z - rendererBound.extents.z, rendererBound.center.z + rendererBound.extents.z);

		sampleXRange = xRange;
		sampleZRange = zRange;
	}

	public Vector2 CalculateHeightRange(AnimationCurve heightCurve, float heightMultiplier)
	{
		float min = heightCurve.Evaluate(minHeight) * heightMultiplier;
		float max = heightCurve.Evaluate(maxHeight) * heightMultiplier;

		return new Vector2(min, max);
	}

	public Vector3 GetRotateAxis()
	{
		switch (rotateAxis)
		{
			case TransformAxis.LocalX:
				return prefab.transform.right;
				
			case TransformAxis.LocalY:
				return prefab.transform.up;

			case TransformAxis.LocalZ:
				return prefab.transform.forward;

			case TransformAxis.WorldX:
				return Vector3.right;
				
			case TransformAxis.WorldY:
				return Vector3.up;
				
			case TransformAxis.WorldZ:
				return Vector3.forward;

			default:
				return Vector3.zero;
		}
	}
}

public struct GenerableData
{
	public int index;

	public Vector2[] samplePoints;

	public Vector2 heightRange;

	public float RayMaxDistance => heightRange.y - heightRange.x;

	public GenerableData(int index, IEnumerable<Vector2> samplePoints, Vector2 heightRange)
	{
		this.index = index;
		this.samplePoints = samplePoints.ToArray();
		this.heightRange = heightRange;
	}

	public Vector3 GetRayOrigin(int index)
	{
		return new Vector3(samplePoints[index].x, heightRange.y, samplePoints[index].y);
	}
}