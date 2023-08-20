﻿using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
	[Serializable]
	public struct TerrainType
	{
		public string name;
		public float maxHeight;
		public Color color;
	}

	/// <summary>
	/// Holds the data and the callback method of this thread.
	/// </summary>
	/// <typeparam name="TData"></typeparam>
	private readonly struct ThreadInfo<TData>
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

	public enum MapDrawMode { Noise, Color, Mesh }

	[Header("MODE"), Tooltip("Which mode to generate the map?"), Space]
	public MapDrawMode drawMode;

	[Header("MAP SEED"), Tooltip("A unique seed to generate a different map each time."), Space]
	public int seed;

	// Ideal value, because 240 can be divisible by all even numbers from 2 to 12.
	public const int MAP_CHUNK_SIZE = 241;

	[Header("BASIC INFO"), Space]
	[Range(0, 6)] public int levelOfDetail;
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

	[Header("MESH CONFIGURATIONS"), Space]
	[Min(0f), Tooltip("A multiplier for the height of each vertex of the mesh.")]
	public float meshHeight;

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
		MapData generatedData = GenerateMapData();

		MapDrawer drawer = GetComponent<MapDrawer>();
		Texture2D tex;

		switch (drawMode)
		{
			case MapDrawMode.Noise:
				tex = TextureGenerator.FromHeightMap(generatedData.heightMap);

				drawer.DrawTexture(tex);
				break;

			case MapDrawMode.Color:
				tex = TextureGenerator.FromColorMap(generatedData.colorMap, MAP_CHUNK_SIZE, MAP_CHUNK_SIZE);

				drawer.DrawTexture(tex);
				break;

			case MapDrawMode.Mesh:
				tex = TextureGenerator.FromColorMap(generatedData.colorMap, MAP_CHUNK_SIZE, MAP_CHUNK_SIZE);
				MeshData meshData = MeshGenerator.ToTerrainMesh(generatedData.heightMap, meshHeight, meshHeightCurve, levelOfDetail);

				drawer.DrawMesh(meshData, tex);
				break;
		}
	}
	#endregion

	#region Threading.
	public void RequestMapData(Action<MapData> callback)
	{
		ThreadStart threadStart = () => MapDataThread(callback);
		new Thread(threadStart).Start();
	}

	private void MapDataThread(Action<MapData> callback)
	{
		MapData mapData = GenerateMapData();

		// Lock the queue so no other threads can access it while one thread is executing this block of code.
		// And others have to wait for their turn.
		lock(_mapDataThreadInfos)
		{
			ThreadInfo<MapData> mapThreadInfo = new ThreadInfo<MapData>(mapData, callback);

			_mapDataThreadInfos.Enqueue(mapThreadInfo);
		}
	}

	public void RequestMeshData(MapData mapData, Action<MeshData> callback)
	{
		ThreadStart threadStart = () => MeshDataThread(mapData, callback);
		new Thread(threadStart).Start();
	}

	private void MeshDataThread(MapData mapData, Action<MeshData> callback)
	{
		MeshData meshData = MeshGenerator.ToTerrainMesh(mapData.heightMap, meshHeight, meshHeightCurve, levelOfDetail);

		lock (_meshDataThreadInfos)
		{
			ThreadInfo<MeshData> meshThreadInfo = new ThreadInfo<MeshData>(meshData, callback);

			_meshDataThreadInfos.Enqueue(meshThreadInfo);
		}
	}
	#endregion

	private MapData GenerateMapData()
	{
		float[,] noiseMap = Noise.GenerateNoiseMap(MAP_CHUNK_SIZE, MAP_CHUNK_SIZE, seed, scale, octaves, persistance, lacunarity, offset);

		#region Generate the color map.
		Color[] colorMap = new Color[MAP_CHUNK_SIZE * MAP_CHUNK_SIZE];

		for (int y = 0; y < MAP_CHUNK_SIZE; y++)
		{
			for (int x = 0; x < MAP_CHUNK_SIZE; x++)
			{
				float currentHeight = noiseMap[x, y];

				// Loop through to find which region this height falls into.
				for (int i = 0; i < _regions.Length; i++)
					if (currentHeight <= _regions[i].maxHeight)
					{
						colorMap[y * MAP_CHUNK_SIZE + x] = _regions[i].color;
						break;
					}
			}
		}
		#endregion

		return new MapData(noiseMap, colorMap);
	}
}

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