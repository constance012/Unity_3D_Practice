using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Object;

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
	
	[Space] public Material chunkMaterial;
	public Transform chunkParent;
	public GameObject waterPrefab;
	
	[Header("Viewer Settings"), Space]
	public Transform viewer;
	public static float maxViewDistance;
	public static float maxViewDistanceSquared;

	// Constants.
	private const float viewerMoveDeltaBeforeChunkUpdate = 100f;
	private const float viewerMoveDeltaBeforeChunkUpdateSquared = viewerMoveDeltaBeforeChunkUpdate *
																	  viewerMoveDeltaBeforeChunkUpdate;
	// Static members.
	public static Vector2 viewerPosition;
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
					TerrainChunk newChunk = new TerrainChunk(visibleChunkCoord, _chunkSize, detailLevels);
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
	private LODMesh _colliderLODMesh;
	private int _previousLODIndex = -1;

	private Bounds _bound;

	private MeshRenderer _meshRenderer;
	private MeshFilter _chunkMeshFilter;
	private MeshFilter _waterMeshFilter;
	private MeshCollider _collider;

	private MapData _mapData;
	private bool _hasMapData;

	private Transform _vegetationRoot;
	private bool _hasVegetation;
	private bool _hasRequestedVegetation;

	public TerrainChunk(Vector2 normalizedCoord, int size, LODInfo[] detailLevels)
	{
		this._detailLevels = detailLevels;
		this._lodMeshes = new LODMesh[detailLevels.Length];
		for (int i = 0; i < detailLevels.Length; i++)
		{
			_lodMeshes[i] = new LODMesh(detailLevels[i].levelOfDetail, UpdateSelf);

			if (detailLevels[i].useForCollider)
				_colliderLODMesh = _lodMeshes[i];
		}

		this.position = normalizedCoord * size;
		this._bound = new Bounds(this.position, Vector2.one * size);
		
		// Create the chunk game object.
		this.chunkObject = new GameObject("Terrain Chunk");

		// Add and reference components.
		_meshRenderer = this.chunkObject.AddComponent<MeshRenderer>();
		_chunkMeshFilter = this.chunkObject.AddComponent<MeshFilter>();
		_collider = this.chunkObject.AddComponent<MeshCollider>();

		// Set up the transform.
		float chunkScale = EndlessTerrainSystem.Instance.chunkScale;
		Vector3 position3D = new Vector3(this.position.x, 0f, this.position.y);

		this.chunkObject.transform.parent = EndlessTerrainSystem.Instance.chunkParent;
		this.chunkObject.layer = EndlessTerrainSystem.Instance.chunkParent.gameObject.layer;
		this.chunkObject.transform.position = position3D * chunkScale;
		this.chunkObject.transform.localScale = Vector3.one * chunkScale;

		this.chunkObject.name += $" {this.chunkObject.transform.GetSiblingIndex()}";
		_vegetationRoot = this.chunkObject.transform.CreateEmptyChild("Generables").transform;

		// Asign the material.
		_meshRenderer.material = EndlessTerrainSystem.Instance.chunkMaterial;

		// Instantiate the water surface.
		GameObject water = Instantiate(EndlessTerrainSystem.Instance.waterPrefab, this.chunkObject.transform);
		water.transform.localPosition = Vector3.zero;
		water.name = "Water Surface";
		_waterMeshFilter = water.GetComponent<MeshFilter>();

		SetVisible(false);

		MapGenerator.Instance.RequestMapData(this.position, OnMapDataReceived);
	}

	public void UpdateSelf()
	{
		if (!_hasMapData)
			return;

		float viewerToNearestEdgeDistanceSquared = _bound.SqrDistance(EndlessTerrainSystem.viewerPosition);
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
					_chunkMeshFilter.mesh = lodMesh.chunkMesh;
					_waterMeshFilter.mesh = lodMesh.waterMesh;
					_previousLODIndex = lodIndex;
				}

				else if (!lodMesh.hasRequestedMesh)
					lodMesh.RequestMesh(_mapData);
			}

			if (lodIndex == 0)
			{
				if (_colliderLODMesh.hasMesh)
					_collider.sharedMesh = _colliderLODMesh.chunkMesh;
				else if (!_colliderLODMesh.hasRequestedMesh)
					_colliderLODMesh.RequestMesh(_mapData);
			}

			if (!_hasVegetation && !_hasRequestedVegetation && _previousLODIndex != -1)
			{
				PrefabGenerator.Instance.RequestPrefabGeneration(_meshRenderer.bounds, MapGenerator.Instance.meshHeightCurve, MapGenerator.Instance.meshHeight, OnGenerableDataReceived);
				_hasRequestedVegetation = true;
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

	private void OnGenerableDataReceived(GenerableData data)
	{
		bool success = PrefabGenerator.Instance.generables[data.index].Generate(_vegetationRoot, data);
		_hasVegetation = success;
		_hasRequestedVegetation = false;
	}
}

/// <summary>
/// Represents a mesh with a specific level of detail.
/// </summary>
public class LODMesh
{
	public Mesh chunkMesh;
	public Mesh waterMesh;

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
		MapGenerator.Instance.RequestMeshData(mapData, _levelOfDetail, OnMeshDataReceived);
	}

	public void OnMeshDataReceived(MeshData meshData)
	{
		chunkMesh = meshData.CreateChunkMesh();
		waterMesh = meshData.CreateWaterMesh();
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

	public bool useForCollider;
}