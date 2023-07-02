using UnityEngine;

public enum TransformAxis { X, Y, Z }

public static class Vector3Extensions
{
	public static Vector3 SetComponentValue(this Vector3 original, TransformAxis axis, float newValue)
	{
		switch (axis)
		{
			case TransformAxis.X:
				return new Vector3(newValue, original.y, original.z);
			case TransformAxis.Y:
				return new Vector3(original.x, newValue, original.z);
			case TransformAxis.Z:
				return new Vector3(original.x, original.y, newValue);
			default:
				return original;
		}
	}

	public static Vector3 SineFluctuate(this Vector3 original, TransformAxis axis, float speed, float amplitude)
	{
		switch (axis)
		{
			case TransformAxis.X:
				original.x += (Mathf.Sin(Time.time * speed) * amplitude) * Time.deltaTime;
				break;
			case TransformAxis.Y:
				original.y += (Mathf.Sin(Time.time * speed) * amplitude) * Time.deltaTime;
				break;
			case TransformAxis.Z:
				original.z += (Mathf.Sin(Time.time * speed) * amplitude) * Time.deltaTime;
				break;
		}

		return original;
	}

	public static Vector3 CosineFluctuate(this Vector3 original, TransformAxis axis, float speed, float amplitude)
	{
		switch (axis)
		{
			case TransformAxis.X:
				original.x += (Mathf.Cos(Time.time * speed) * amplitude) * Time.deltaTime;
				break;
			case TransformAxis.Y:
				original.y += (Mathf.Cos(Time.time * speed) * amplitude) * Time.deltaTime;
				break;
			case TransformAxis.Z:
				original.z += (Mathf.Cos(Time.time * speed) * amplitude) * Time.deltaTime;
				break;
		}

		return original;
	}
}
