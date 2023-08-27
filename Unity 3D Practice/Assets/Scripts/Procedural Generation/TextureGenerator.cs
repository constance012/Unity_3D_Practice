using UnityEngine;

public static class TextureGenerator
{
	/// <summary>
	/// Generates a texture from the provided color map.
	/// </summary>
	/// <param name="colorMap"></param>
	/// <param name="width"></param>
	/// <param name="height"></param>
	/// <returns></returns>
	public static Texture2D FromColorMap(Color[] colorMap, int width, int height)
	{
		Texture2D tex = new Texture2D(width, height);

		tex.filterMode = FilterMode.Point;
		tex.wrapMode = TextureWrapMode.Clamp;

		tex.SetPixels(colorMap);
		tex.Apply();

		return tex;
	}

	/// <summary>
	/// Generates a black-white texture from the provided height map.
	/// </summary>
	/// <param name="heightMap"></param>
	/// <returns></returns>
	public static Texture2D FromHeightMap(float[,] heightMap)
	{
		int width = heightMap.GetLength(0);
		int height = heightMap.GetLength(1);

		Color[] monoColorMap = new Color[width * height];

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				monoColorMap[y * width + x] = Color.Lerp(Color.black, Color.white, heightMap[x, y]);
			}
		}

		return FromColorMap(monoColorMap, width, height);
	}
}
