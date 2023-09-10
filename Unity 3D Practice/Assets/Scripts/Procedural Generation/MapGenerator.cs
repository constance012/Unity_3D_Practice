using System;
using UnityEngine;

public class MapGenerator : Singleton<MapGenerator>
{
	/// <summary>
	/// Represents a type of terrain on the terrain map.
	/// </summary>
	[Serializable]
	public struct TerrainType
	{
		public string name;
		[Min(0f)] public float minHeight;
		public Color color;
	}

	public enum MapDrawMode { Noise, Falloff, Texture, Mesh }
	public enum FalloffShape { Square, Circle, Custom }

	[Header("MODE"), Tooltip("Which mode to generate the map?"), Space]
	public MapDrawMode drawMode;

	[Header("DATA"), Space]
	public NoiseData noiseData;
	public TerrainData terrainData;

	[Header("EDITOR PREVIEW"), Space]
	[Range(0, 6)] public int editorLOD;
	[SerializeField] private MapPreviewer previewer;

	[Header("TERRAIN LAYERS"), Space]
	[Tooltip("An array of different regions in the landmass, MUST be sorted as an ascending order in maxHeight in order to generate properly."), SerializeField]
	private TerrainType[] _regions;

	[Space, Tooltip("Auto update the map whenever a value is changed in the Inspector.")]
	public bool autoUpdate;

	// Private fields.
	private float[,] _falloffMap;

	// Ideal value, because 240 can be divisible by all even numbers from 2 to 12.
	// Minus 2 because of the border vertices of this chunk.
	public static int MapChunkSize
	{
		get
		{
			if (Instance == null)
				Instance = FindObjectOfType<MapGenerator>();

			if (Instance.terrainData.useFlatShading)
				return 95;

			return 239;
		}
	}

	public static int MapChunkSizeWithBorder => MapChunkSize + 2;

	protected override void Awake()
	{
		base.Awake();
		GenerateFalloff();
	}

	#region Draw map in editor.
	#if UNITY_EDITOR
	public void DrawMapInEditor()
	{
		MapData generatedData = GenerateMapData(Vector2.zero);

		Texture2D tex;

		switch (drawMode)
		{
			case MapDrawMode.Noise:
				tex = TextureGenerator.FromHeightMap(generatedData.heightMap);

				previewer.DrawTexture(tex);
				break;

			case MapDrawMode.Falloff:
				tex = TextureGenerator.FromHeightMap(_falloffMap);

				previewer.DrawTexture(tex);
				break;

			case MapDrawMode.Texture:
				tex = TextureGenerator.FromColorMap(generatedData.colorMap, MapChunkSize, MapChunkSize);

				previewer.DrawTexture(tex);
				break;

			case MapDrawMode.Mesh:
				tex = TextureGenerator.FromColorMap(generatedData.colorMap, MapChunkSize, MapChunkSize);
				MeshData meshData = MeshGenerator.ToTerrainMesh(generatedData.heightMap, terrainData.meshHeight, terrainData.meshHeightCurve, editorLOD, terrainData.useFlatShading);

				previewer.DrawMesh(meshData, tex);
				break;
		}
	}

	public void OnValuesUpdated()
	{
		if (!Application.isPlaying)
		{
			previewer.ManagePreviewObjects(drawMode);

			if (terrainData.useFalloffMap || drawMode == MapDrawMode.Falloff)
				GenerateFalloff();

			DrawMapInEditor();
		}
	}

	private void OnValidate()
	{
		if (terrainData != null)
		{
			terrainData.OnValuesUpdated -= OnValuesUpdated;
			terrainData.OnValuesUpdated += OnValuesUpdated;
		}

		if (noiseData != null)
		{
			noiseData.OnValuesUpdated -= OnValuesUpdated;
			noiseData.OnValuesUpdated += OnValuesUpdated;
		}
	}
	#endif
	#endregion

	#region Generate Methods.
	public MapData GenerateMapData(Vector2 chunkCenter)
	{
		// Generate one extra noise value for the border of this chunk.
		float[,] noiseMap = Noise.GenerateNoiseMap(MapChunkSizeWithBorder, MapChunkSizeWithBorder, noiseData.seed, noiseData.scale,
							noiseData.octaves, noiseData.persistence, noiseData.lacunarity, terrainData.maxPossibleHeightCutoff, chunkCenter + noiseData.offset, noiseData.normalizeMode);

		#region Generate the color map.
		Color[] colorMap = new Color[(MapChunkSizeWithBorder) * (MapChunkSizeWithBorder)];

		for (int y = 0; y < MapChunkSizeWithBorder; y++)
		{
			for (int x = 0; x < MapChunkSizeWithBorder; x++)
			{
				if (terrainData.useFalloffMap)
					noiseMap[x, y] = Mathf.Max(noiseMap[x, y] - _falloffMap[x, y], 0f);

				float currentHeight = noiseMap[x, y];

				// Loop through to find which region this height falls into.
				for (int i = 0; i < _regions.Length; i++)
				{
					if (currentHeight >= _regions[i].minHeight)
						colorMap[y * MapChunkSize + x] = _regions[i].color;
					else
						break;
				}
			}
		}
		#endregion

		return new MapData(noiseMap, colorMap);
	}

	public void GenerateFalloff()
	{
		int inverse = terrainData.inverseMap ? -1 : 1;

		switch (terrainData.falloffShape)
		{
			case FalloffShape.Square:
				_falloffMap = FalloffMapGenerator.GenerateFalloffMapSquare(MapChunkSizeWithBorder, inverse, terrainData.falloffSmoothness, terrainData.falloffIntensity);
				break;

			case FalloffShape.Circle:
				_falloffMap = FalloffMapGenerator.GenerateFalloffMapCircle(MapChunkSizeWithBorder, inverse, terrainData.falloffSmoothness, terrainData.falloffIntensity);
				break;

			case FalloffShape.Custom:
				_falloffMap = FalloffMapGenerator.GenerateFalloffMapCustom(MapChunkSizeWithBorder, inverse, terrainData.customShapeCurve);
				break;
		}
	}
	#endregion
}

/// <summary>
/// Contains the height data and color data of a single terrain chunk.
/// </summary>
public readonly struct MapData
{
	public readonly float[,] heightMap;
	public readonly Color[] colorMap;

	public MapData(float[,] heightMap, Color[] colorMap)
	{
		this.heightMap = heightMap;
		this.colorMap = colorMap;
	}
}