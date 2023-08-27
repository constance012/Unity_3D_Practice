using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class EndlessTerrainSystem : Singleton<EndlessTerrainSystem>
{
	public enum TerrainSizeMode { Endless, FixedSize }
	
	[Header("Level of Detail Properties"), Space]
	public LODInfo[] detailLevels;

	[Header("Terrain Chunk Settings"), Space]
	[SerializeField] private TerrainSizeMode sizeMode;

	[SerializeField, Tooltip("How many chunks does this terrain consist of on each axis?" +
	"Even number inputs will be increased by 1 internally to center the map properly.")]
	private Vector2Int terrainDimensions;

	[Min(1f), Tooltip("How big would each terrain chunk be?")]
	public float chunkScale = 1f;
	
	[Space, SerializeField] private Material chunkMaterial;
	[SerializeField] private Transform chunkParent;
	
	[Header("Viewer Settings"), Space]
	public Transform viewer;
	public static float maxViewDistance;
	public static float maxViewDistanceSquared;

	// Constants.
	private const float viewerMoveDeltaBeforeChunkUpdate = 25f;
	private const float viewerMoveDeltaBeforeChunkUpdateSquared = viewerMoveDeltaBeforeChunkUpdate *
																	  viewerMoveDeltaBeforeChunkUpdate;
	// Static members.
	public static Vector2 viewerPosition;
	public static MapGenerator mapGenerator;
	public static List<TerrainChunk> outOfViewChunks = new List<TerrainChunk>();

	// Private fields.
	private Vector2 _viewerPositionPrevious;
	private Vector2Int _chunkCoordBounds;
	private Dictionary<Vector2, TerrainChunk> _terrainChunks = new Dictionary<Vector2, TerrainChunk>();
	private int _chunkSize;
	private int _chunksVisibleInView;

	protected override void Awake()
	{
		base.Awake();
		mapGenerator = GetComponent<MapGenerator>();
	}

	private void Start()
	{
		maxViewDistance = detailLevels[detailLevels.Length - 1].VisibleDistanceThreshold;
		maxViewDistanceSquared = maxViewDistance * maxViewDistance;

		_chunkSize = MapGenerator.MAP_CHUNK_SIZE - 1;
		_chunksVisibleInView = Mathf.RoundToInt(maxViewDistance / _chunkSize);

		if (sizeMode == TerrainSizeMode.FixedSize)
		{
			int coordBoundX = terrainDimensions.x % 2 == 0 ? terrainDimensions.x / 2 : (terrainDimensions.x - 1) / 2;
			int coordBoundY = terrainDimensions.y % 2 == 0 ? terrainDimensions.y / 2 : (terrainDimensions.y - 1) / 2;

			_chunkCoordBounds = new Vector2Int(coordBoundX, coordBoundY);
		}

		UpdateVisibleChunks();
	}

	private void LateUpdate()
	{
		viewerPosition.Set(viewer.position.x, viewer.position.z);
		viewerPosition /= chunkScale;

		float viewerMoveDeltaSquared = (viewerPosition - _viewerPositionPrevious).sqrMagnitude;

		if (viewerMoveDeltaSquared > viewerMoveDeltaBeforeChunkUpdateSquared)
		{
			Debug.Log("Chunks updating..");

			_viewerPositionPrevious = viewerPosition;
			UpdateVisibleChunks();
		}
	}

	private void UpdateVisibleChunks()
	{
		outOfViewChunks.ForEach(chunk => chunk.SetVisible(false));
		outOfViewChunks.Clear();

		// Normalized coordinates.
		int currentChunkX = Mathf.RoundToInt(viewerPosition.x / _chunkSize);
		int currentChunkY = Mathf.RoundToInt(viewerPosition.y / _chunkSize);

		for (int yOffset = -_chunksVisibleInView; yOffset <= _chunksVisibleInView; yOffset++)
		{
			for (int xOffset = -_chunksVisibleInView; xOffset <= _chunksVisibleInView; xOffset++)
			{
				Vector2 visibleChunkCoord = new Vector2(currentChunkX + xOffset, currentChunkY + yOffset);

				if (sizeMode == TerrainSizeMode.FixedSize && visibleChunkCoord.CompareBounds(_chunkCoordBounds) == 1)
					continue;

				if (_terrainChunks.ContainsKey(visibleChunkCoord, out TerrainChunk chunk))
				{
					chunk.UpdateSelf();
				}
				else
				{
					TerrainChunk newChunk = new TerrainChunk(visibleChunkCoord, _chunkSize, detailLevels, chunkParent, chunkMaterial);
					_terrainChunks.Add(visibleChunkCoord, newChunk);
				}
			}
		}
	}
}

/// <summary>
/// Represents a chunk of terrain in a terrain map.
/// </summary>
public class TerrainChunk
{
	public GameObject chunkObject;
	public Vector2 position;

	public bool IsVisible => chunkObject.activeInHierarchy;

	private LODInfo[] _detailLevels;
	private LODMesh[] _lodMeshes;
	private int _previousLODIndex = -1;

	private Bounds _bounds;
	private MeshRenderer _meshRenderer;
	private MeshFilter _meshFilter;

	private MapData _mapData;
	private bool _hasMapData;

	public TerrainChunk(Vector2 normalizedCoord, int size, LODInfo[] detailLevels, Transform parent, Material material)
	{
		this._detailLevels = detailLevels;
		this._lodMeshes = new LODMesh[detailLevels.Length];
		for (int i = 0; i < _lodMeshes.Length; i++)
		{
			_lodMeshes[i] = new LODMesh(detailLevels[i].levelOfDetail, UpdateSelf);
		}

		this.position = normalizedCoord * size;
		this._bounds = new Bounds(this.position, Vector2.one * size);
		
		// Create the chunk game object.
		this.chunkObject = new GameObject("Terrain Chunk");
		_meshRenderer = this.chunkObject.AddComponent<MeshRenderer>();
		_meshFilter = this.chunkObject.AddComponent<MeshFilter>();

		// Set up the transform.
		float chunkScale = EndlessTerrainSystem.Instance.chunkScale;
		Vector3 position3D = new Vector3(this.position.x, 50f, this.position.y);

		this.chunkObject.transform.parent = parent;
		this.chunkObject.transform.position = position3D * chunkScale;
		this.chunkObject.transform.localScale = Vector3.one * chunkScale;

		this.chunkObject.name += $" {this.chunkObject.transform.GetSiblingIndex()}";

		_meshRenderer.material = material;

		SetVisible(false);

		EndlessTerrainSystem.mapGenerator.RequestMapData(this.position, OnMapDataReceived);
	}

	public void UpdateSelf()
	{
		if (!_hasMapData)
			return;

		float viewerToNearestEdgeDistanceSquared = _bounds.SqrDistance(EndlessTerrainSystem.viewerPosition);
		bool visible = viewerToNearestEdgeDistanceSquared <= EndlessTerrainSystem.maxViewDistanceSquared; // Compare two squared values.

		//Debug.Log($"{viewerToNearestEdgeDistanceSquared}, {EndlessTerrainSystem.maxViewDistanceSquared}");

		if (visible)
		{
			int lodIndex = 0;

			for (int i = 0; i < _detailLevels.Length - 1; i++)
			{
				if (viewerToNearestEdgeDistanceSquared > _detailLevels[i].VisibleDistanceThresholdSquared)
					lodIndex++;
				else
					break;
			}

			if (lodIndex != _previousLODIndex)
			{
				LODMesh lodMesh = _lodMeshes[lodIndex];

				if (lodMesh.hasMesh)
				{
					_meshFilter.mesh = lodMesh.mesh;
					_previousLODIndex = lodIndex;
				}

				else if (!lodMesh.hasRequestedMesh)
					lodMesh.RequestMesh(_mapData);
			}

			EndlessTerrainSystem.outOfViewChunks.Add(this);
		}

		//Debug.Log(_bounds.center);
		SetVisible(visible);
	}

	public void SetVisible(bool visible)
	{
		if (chunkObject != null)
			chunkObject.SetActive(visible);
	}

	private void OnMapDataReceived(MapData mapData)
	{
		_mapData = mapData;
		_hasMapData = true;

		int chunkSize = MapGenerator.MAP_CHUNK_SIZE;
		Texture2D tex = TextureGenerator.FromColorMap(mapData.colorMap, chunkSize, chunkSize);
		_meshRenderer.material.SetTexture("_BaseMap", tex);

		UpdateSelf();
	}
}

/// <summary>
/// Represents a mesh with a specific level of detail.
/// </summary>
public class LODMesh
{
	public Mesh mesh;
	public bool hasRequestedMesh;
	public bool hasMesh;

	private int _levelOfDetail;
	private Action _chunkUpdateCalback;

	public LODMesh(int levelOfDetail, Action chunkUpdateCalback)
	{
		this._levelOfDetail = levelOfDetail;
		this._chunkUpdateCalback = chunkUpdateCalback;
	}

	public void RequestMesh(MapData mapData)
	{
		hasRequestedMesh = true;
		EndlessTerrainSystem.mapGenerator.RequestMeshData(mapData, _levelOfDetail, OnMeshDataReceived);
	}

	public void OnMeshDataReceived(MeshData meshData)
	{
		mesh = meshData.CreateMesh();
		hasMesh = true;

		_chunkUpdateCalback();
	}
}

[Serializable]
public struct LODInfo
{
	public int levelOfDetail;

	/// <summary>
	/// The maximum distance to the viewer of this level of detail to be visible.
	/// </summary>
	[field: SerializeField]
	public float VisibleDistanceThreshold { get; private set; }

	public float VisibleDistanceThresholdSquared => VisibleDistanceThreshold * VisibleDistanceThreshold;
}