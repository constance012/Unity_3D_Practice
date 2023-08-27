using UnityEditor.Animations.Rigging;
using UnityEngine;

public static class FalloffMapGenerator
{
	public static float[,] GenerateFalloffMapSquare(int size, int inverse, float smoothness, float intensity)
	{
		float[,] falloffMap = new float[size, size];

		for (int i = 0; i < size; i++)
		{
			for (int j = 0; j < size; j++)
			{
				float x = i / (float)size * 2 - 1;
				float y = j / (float)size * 2 - 1;

				// Find out which value is the closest to 1 (edges of the map).
				float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));

				falloffMap[i, j] = Evaluate(value, inverse, smoothness, intensity);
			}
		}

		return falloffMap;
	}

	public static float[,] GenerateFalloffMapCircle(int size, int inverse, float smoothness, float intensity)
	{
		float[,] falloffMap = new float[size, size];

		Vector2 center = new Vector2(size / 2f, size / 2f);

		for (int i = 0; i < size; i++)
		{
			for (int j = 0; j < size; j++)
			{
				float distanceFromCenter = Vector2.Distance(center, new Vector2(i, j));
				float currentAlpha;

				if ((1 - (distanceFromCenter / size)) >= 0)
				{
					currentAlpha = 1 - (distanceFromCenter / size);
				}
				else
				{
					currentAlpha = 0;
				}

				falloffMap[i, j] = Evaluate(currentAlpha, inverse, smoothness, intensity);
			}
		}
		return falloffMap;
	}

	public static float[,] GenerateFalloffMapCustom(int size, int inverse, AnimationCurve shapeCurve)
	{
		float[,] falloffMap = new float[size, size];

		for (int i = 0; i < size; i++)
		{
			for (int j = 0; j < size; j++)
			{
				float x = i / (float)size;
				float y = j / (float)size;

				falloffMap[i, j] = inverse * (shapeCurve.Evaluate(x) * shapeCurve.Evaluate(y) * 2 - 1);
			}
		}

		return falloffMap;
	}

	private static float Evaluate(float value, int inverse, float smoothness, float intensity)
	{
		//Reverse the value range.
		float a = (10f - smoothness + 1f) * inverse;
		float b = (10f - intensity + .1f);

		float valuePowerA = Mathf.Pow(value, a);

		// Function: x^a / (x^a + (b - bx)^a)
		return valuePowerA / (valuePowerA + Mathf.Pow(b - b * value, a));
	}
}