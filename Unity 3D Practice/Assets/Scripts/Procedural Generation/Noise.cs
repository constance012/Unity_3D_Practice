using UnityEngine;

public static class Noise
{
	public static float[,] GenerateNoiseMap(int width, int height, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset)
	{
		float[,] noiseMap = new float[width, height];

		#region Initialize the offset array for each octave.
		// Generate a different map each time by sampling the points of each octave from a radically different location.
		System.Random prng = new System.Random(seed);

		Vector2[] octaveOffsets = new Vector2[octaves];

		for (int i = 0; i < octaves; i++)
		{
			// Optimal offset value not to break the Mathf.PerlinNoise function is between -100000 and 100000.
			float offsetX = prng.Next(-100000, 100000) + offset.x;
			float offsetY = prng.Next(-100000, 100000) + offset.y;

			octaveOffsets[i] = new Vector2(offsetX, offsetY);
		}
		#endregion

		float maxNoiseHeight = float.MinValue;
		float minNoiseHeight = float.MaxValue;

		// Get the half dimensions for scaling at the center of the map.
		float halfWidth = width / 2f;
		float halfHeight = width / 2f;

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				float amplitude = 1f; // Decrease overtime with persistance.
				float frequency = 1f; // Increase overtime with lacunarity.

				float noiseHeight = 0f;

				#region Loop through each octave and sample with the vertex's coordinates.
				for (int i = 0; i < octaves; i++) {
					float sampleX = (x - halfWidth) / scale * frequency + octaveOffsets[i].x * frequency;
					float sampleY = (y - halfHeight) / scale * frequency - octaveOffsets[i].y * frequency;

					float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;

					noiseHeight += perlinValue * amplitude;

					amplitude *= persistance;
					frequency *= lacunarity;
				}
				#endregion

				# region Set the min and max height for the current noise map.
				if (noiseHeight > maxNoiseHeight)
					maxNoiseHeight = noiseHeight;

				if (noiseHeight < minNoiseHeight)
					minNoiseHeight = noiseHeight;
				#endregion

				noiseMap[x, y] = noiseHeight;
			}
		}

		#region Clamp the noise height back to between 0 and 1.
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
			}
		}
		#endregion

		return noiseMap;
	}
}
