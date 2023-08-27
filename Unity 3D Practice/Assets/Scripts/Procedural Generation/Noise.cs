using UnityEngine;

public enum NormalizeMode { Global, Local }

public static class Noise
{
	public static float[,] GenerateNoiseMap(int width, int height, int seed, float scale,
		int octaves, float persistance, float lacunarity, float possibleHeightCutoff, Vector2 offset, NormalizeMode normalizeMode)
	{
		float[,] noiseMap = new float[width, height];

		#region Initialize the offset array for each octave.
		// Generate a different map each time by sampling the points of each octave from a radically different location.
		System.Random prng = new System.Random(seed);

		Vector2[] octaveOffsets = new Vector2[octaves];

		float maxPossibleHeight = 0f;
		float amplitude = 1f; // Decrease overtime with persistance.
		float frequency; // Increase overtime with lacunarity.

		for (int i = 0; i < octaves; i++)
		{
			// Optimal offset value not to break the Mathf.PerlinNoise function is between -100000 and 100000.
			float offsetX = (prng.Next(-100000, 100000) + offset.x);
			float offsetY = (prng.Next(-100000, 100000) + offset.y);

			octaveOffsets[i] = new Vector2(offsetX, offsetY);

			maxPossibleHeight += amplitude;
			amplitude *= persistance;
		}
		#endregion

		float maxLocalNoiseHeight = float.MinValue;
		float minLocalNoiseHeight = float.MaxValue;

		// Get the half dimensions for scaling at the center of the map.
		float halfWidth = width / 2f;
		float halfHeight = width / 2f;

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				amplitude = 1f;
				frequency = 1f;

				float noiseHeight = 0f;

				#region Loop through each octave and sample with the vertex's coordinates.
				for (int i = 0; i < octaves; i++) {
					//float sampleX = (x - halfWidth) / scale * frequency + octaveOffsets[i].x * frequency;
					//float sampleY = (y - halfHeight) / scale * frequency - octaveOffsets[i].y * frequency;

					float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;
					float sampleY = (y - halfHeight - octaveOffsets[i].y) / scale * frequency;

					float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;

					noiseHeight += perlinValue * amplitude;

					amplitude *= persistance;
					frequency *= lacunarity;
				}
				#endregion

				# region Set the min and max height for the current noise map.
				if (noiseHeight > maxLocalNoiseHeight)
					maxLocalNoiseHeight = noiseHeight;

				if (noiseHeight < minLocalNoiseHeight)
					minLocalNoiseHeight = noiseHeight;
				#endregion

				noiseMap[x, y] = noiseHeight;
			}
		}

		#region Clamp the noise height back to between 0 and 1.
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				switch (normalizeMode)
				{
					case NormalizeMode.Global:
						float normalizedHeight = (noiseMap[x, y] + 1) / (2f * maxPossibleHeight / possibleHeightCutoff);
						noiseMap[x, y] = Mathf.Max(normalizedHeight, 0f); // Assume that the height is not less than 0.
						break;
					
					case NormalizeMode.Local:
						noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
						break;
				}
			}
		}

		//Debug.Log($"{minLocalNoiseHeight}, {maxLocalNoiseHeight}");
		//Debug.Log($"Global: {maxPossibleHeight}");
		#endregion

		return noiseMap;
	}
}
