using System;
using System.Collections.Generic;
using System.Threading;
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
	public NormalizeMode normalizeMode;

	[Header("MAP SEED"), Tooltip("A unique seed to generate a different map each time."), Space]
	public int seed;

	// Ideal value, because 240 can be divisible by all even numbers from 2 to 12.
	// Minus 2 because of the border vertices of this chunk.
	public const int MAP_CHUNK_SIZE = 239;
	public const int MAP_CHUNK_SIZE_WITH_BORDER = MAP_CHUNK_SIZE + 2;

	[Header("BASIC INFO"), Space]
	[Range(0, 6)] public int editorLOD;
	[Min(.0001f)] public float scale;

	[Header("MAP TWEAKING"), Space]
	[Range(1, 10), Tooltip("The amount of octaves. Refers to the individual layers of the noise.")]
	public int octaves = 4;

	[Range(0f, 1f), Tooltip("Controls the Decreasing in Amplitude of each octave.")]
	public float persistance = .5f;

	[Min(1f), Tooltip("Controls the Increasing in Frequency of each octave.")]
	public float lacunarity = 2;

	[Tooltip("The offset values of sample points")]
	public Vector2 offset;

	[Header("FALLOFF MAP"), Space]
	public FalloffShape falloffShape;
	[Tooltip("CUSTOM SHAPE ONLY: A curve to customize the shape of the falloff.")]
	public AnimationCurve customShapeCurve;

	[Range(1f, 10f)] public float falloffSmoothness;
	[Range(.1f, 10f)] public float falloffIntensity;
	
	[Tooltip("Invert the black and white areas of the map.")] 
	public bool inverseMap;
	public bool useFalloffMap;

	[Header("MESH CONFIGURATIONS"), Space]
	[Min(0f), Tooltip("A multiplier for the height of each vertex of the mesh.")]
	public float meshHeight;

	[Min(1f), Tooltip("GLOBAL NORMALIZE MODE ONLY: An estimate value to reduce the maximum possible noise height of the map.")]
	public float maxPossibleHeightCutoff;

	[Tooltip("Specifies how much the different terrain heights should be affected by the multiplier.")]
	public AnimationCurve meshHeightCurve;

	[Header("TERRAIN LAYERS"), Space]
	[Tooltip("An array of different regions in the landmass, MUST be sorted as an ascending order in maxHeight in order to generate properly."), SerializeField]
	private TerrainType[] _regions;

	[Space, Tooltip("Auto update the map whenever a value is changed in the Inspector.")]
	public bool autoUpdate;

	/// <summary>
	/// A queue holds all the thread info of MapData to execute their <c>callback</c> inside Unity's main thread.
	/// </summary>
	private Queue<ThreadInfo<MapData>> _mapDataThreadInfos = new Queue<ThreadInfo<MapData>>();

	/// <summary>
	/// A queue holds all the thread info of MeshData to execute their <c>callback</c> inside Unity's main thread.
	/// </summary>
	private Queue<ThreadInfo<MeshData>> _meshDataThreadInfos = new Queue<ThreadInfo<MeshData>>();

	private float[,] _falloffMap;

	protected override void Awake()
	{
		base.Awake();
		GenerateFalloff();
	}

	private void Update()
	{
		if (_mapDataThreadInfos.Count > 0)
		{
			for (int i = 0; i < _mapDataThreadInfos.Count; i++)
			{
				ThreadInfo<MapData> threadInfo = _mapDataThreadInfos.Dequeue();
				threadInfo.ExecuteCallback();
			}
		}

		if (_meshDataThreadInfos.Count > 0)
		{
			for (int i = 0; i < _meshDataThreadInfos.Count; i++)
			{
				ThreadInfo<MeshData> threadInfo = _meshDataThreadInfos.Dequeue();
				threadInfo.ExecuteCallback();
			}
		}
	}

	#region Draw map in editor.
	public void DrawMapInEditor()
	{
		MapData generatedData = GenerateMapData(Vector2.zero);

		MapPreviewer previewer = transform.GetComponentInChildren<MapPreviewer>();
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
				tex = TextureGenerator.FromColorMap(generatedData.colorMap, MAP_CHUNK_SIZE, MAP_CHUNK_SIZE);

				previewer.DrawTexture(tex);
				break;

			case MapDrawMode.Mesh:
				tex = TextureGenerator.FromColorMap(generatedData.colorMap, MAP_CHUNK_SIZE, MAP_CHUNK_SIZE);
				MeshData meshData = MeshGenerator.ToTerrainMesh(generatedData.heightMap, meshHeight, meshHeightCurve, editorLOD);

				previewer.DrawMesh(meshData, tex);
				break;
		}
	}
	#endregion

	#region Threading.
	public void RequestMapData(Vector2 chunkCenter, Action<MapData> callback)
	{
		ThreadStart threadStart = () => MapDataThread(chunkCenter, callback);
		new Thread(threadStart).Start();
	}

	private void MapDataThread(Vector2 chunkCenter, Action<MapData> callback)
	{
		MapData mapData = GenerateMapData(chunkCenter);

		// Lock the queue so no other threads can access it while one thread is executing this block of code.
		// And others have to wait for their turn.
		lock(_mapDataThreadInfos)
		{
			ThreadInfo<MapData> mapThreadInfo = new ThreadInfo<MapData>(mapData, callback);

			_mapDataThreadInfos.Enqueue(mapThreadInfo);
		}
	}

	public void RequestMeshData(MapData mapData, int levelOfDetail, Action<MeshData> callback)
	{
		ThreadStart threadStart = () => MeshDataThread(mapData, levelOfDetail, callback);
		new Thread(threadStart).Start();
	}

	private void MeshDataThread(MapData mapData, int levelOfDetail, Action<MeshData> callback)
	{
		MeshData meshData = MeshGenerator.ToTerrainMesh(mapData.heightMap, meshHeight, meshHeightCurve, levelOfDetail);

		lock (_meshDataThreadInfos)
		{
			ThreadInfo<MeshData> meshThreadInfo = new ThreadInfo<MeshData>(meshData, callback);

			_meshDataThreadInfos.Enqueue(meshThreadInfo);
		}
	}
	#endregion

	#region Generate Methods.
	private MapData GenerateMapData(Vector2 chunkCenter)
	{
		// Generate one extra noise value for the border of this chunk.
		float[,] noiseMap = Noise.GenerateNoiseMap(MAP_CHUNK_SIZE_WITH_BORDER, MAP_CHUNK_SIZE_WITH_BORDER, seed, scale,
							octaves, persistance, lacunarity, maxPossibleHeightCutoff, chunkCenter + offset, normalizeMode);

		#region Generate the color map.
		Color[] colorMap = new Color[(MAP_CHUNK_SIZE_WITH_BORDER) * (MAP_CHUNK_SIZE_WITH_BORDER)];

		for (int y = 0; y < MAP_CHUNK_SIZE_WITH_BORDER; y++)
		{
			for (int x = 0; x < MAP_CHUNK_SIZE_WITH_BORDER; x++)
			{
				if (useFalloffMap)
					noiseMap[x, y] = Mathf.Max(noiseMap[x, y] - _falloffMap[x, y], 0f);

				float currentHeight = noiseMap[x, y];

				// Loop through to find which region this height falls into.
				for (int i = 0; i < _regions.Length; i++)
				{
					if (currentHeight >= _regions[i].minHeight)
						colorMap[y * MAP_CHUNK_SIZE + x] = _regions[i].color;
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
		int inverse = inverseMap ? -1 : 1;

		switch (falloffShape)
		{
			case FalloffShape.Square:
				_falloffMap = FalloffMapGenerator.GenerateFalloffMapSquare(MAP_CHUNK_SIZE_WITH_BORDER, inverse, falloffSmoothness, falloffIntensity);
				break;

			case FalloffShape.Circle:
				_falloffMap = FalloffMapGenerator.GenerateFalloffMapCircle(MAP_CHUNK_SIZE_WITH_BORDER, inverse, falloffSmoothness, falloffIntensity);
				break;

			case FalloffShape.Custom:
				_falloffMap = FalloffMapGenerator.GenerateFalloffMapCustom(MAP_CHUNK_SIZE_WITH_BORDER, inverse, customShapeCurve);
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

/// <summary>
/// Holds the data and the callback method of a thread.
/// </summary>
/// <typeparam name="TData"></typeparam>
public readonly struct ThreadInfo<TData>
{
	public readonly TData data;
	public readonly Action<TData> callback;

	public ThreadInfo(TData data, Action<TData> callback)
	{
		this.data = data;
		this.callback = callback;
	}

	public void ExecuteCallback() => this.callback(this.data);
}