using UnityEngine;

public enum VectorAxis { X, Y, Z }

public static class VectorExtensions
{
	/// <summary>
	/// Compares the absolute value of each component of this vector with other vector.
	/// </summary>
	/// <param name="original"></param>
	/// <param name="other"></param>
	/// <returns>1 if at least 1 component of this vector is greater than that of the other vector. 0 if all components are equal. And -1 otherwise.</returns>
	public static int CompareBounds(this Vector2 original, Vector2 other)
	{
		if (Mathf.Abs(original.x) > Mathf.Abs(other.x) || Mathf.Abs(original.y) > Mathf.Abs(other.y))
			return 1;
		else if (Mathf.Abs(original.x) == Mathf.Abs(other.x) && Mathf.Abs(original.y) == Mathf.Abs(other.y))
			return 0;

		return -1;
	}

	public static Vector3 SetComponentValue(this Vector3 original, VectorAxis axis, float newValue)
	{
		switch (axis)
		{
			case VectorAxis.X:
				return new Vector3(newValue, original.y, original.z);
			case VectorAxis.Y:
				return new Vector3(original.x, newValue, original.z);
			case VectorAxis.Z:
				return new Vector3(original.x, original.y, newValue);
			default:
				return original;
		}
	}

	public static Vector3 SineFluctuate(this Vector3 original, VectorAxis axis, float speed, float amplitude)
	{
		switch (axis)
		{
			case VectorAxis.X:
				original.x += (Mathf.Sin(Time.time * speed) * amplitude) * Time.deltaTime;
				break;
			case VectorAxis.Y:
				original.y += (Mathf.Sin(Time.time * speed) * amplitude) * Time.deltaTime;
				break;
			case VectorAxis.Z:
				original.z += (Mathf.Sin(Time.time * speed) * amplitude) * Time.deltaTime;
				break;
		}

		return original;
	}

	public static Vector3 CosineFluctuate(this Vector3 original, VectorAxis axis, float speed, float amplitude)
	{
		switch (axis)
		{
			case VectorAxis.X:
				original.x += (Mathf.Cos(Time.time * speed) * amplitude) * Time.deltaTime;
				break;
			case VectorAxis.Y:
				original.y += (Mathf.Cos(Time.time * speed) * amplitude) * Time.deltaTime;
				break;
			case VectorAxis.Z:
				original.z += (Mathf.Cos(Time.time * speed) * amplitude) * Time.deltaTime;
				break;
		}

		return original;
	}
}
