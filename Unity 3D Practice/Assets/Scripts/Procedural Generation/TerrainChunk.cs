using System;
using UnityEngine;
using static UnityEngine.Object;

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

	private Bounds _edgeBounds;
	private Bounds _rendererBounds;

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
		this._edgeBounds = new Bounds(this.position, Vector2.one * size);
		this._hasVegetation = !TerrainChunkGenerator.Instance.includeVegetation;

		// Create the chunk game object.
		this.chunkObject = new GameObject("Terrain Chunk");

		// Add and reference components.
		_meshRenderer = this.chunkObject.AddComponent<MeshRenderer>();
		_chunkMeshFilter = this.chunkObject.AddComponent<MeshFilter>();
		_collider = this.chunkObject.AddComponent<MeshCollider>();

		// Set up the transform.
		float chunkScale = MapGenerator.Instance.terrainData.chunkScale;
		Vector3 position3D = new Vector3(this.position.x, 0f, this.position.y);

		this.chunkObject.transform.parent = TerrainChunkGenerator.Instance.chunkParent;
		this.chunkObject.layer = TerrainChunkGenerator.Instance.chunkParent.gameObject.layer;
		this.chunkObject.transform.position = position3D * chunkScale;
		this.chunkObject.transform.localScale = Vector3.one * chunkScale;

		this.chunkObject.name += $" {this.chunkObject.transform.GetSiblingIndex()}";
		_vegetationRoot = this.chunkObject.transform.CreateEmptyChild("Generables").transform;

		// Asign the material.
		_meshRenderer.material = TerrainChunkGenerator.Instance.chunkMaterial;

		// Instantiate the water surface.
		GameObject water = Instantiate(TerrainChunkGenerator.Instance.waterPrefab, this.chunkObject.transform);
		water.transform.localPosition = Vector3.zero;
		water.name = "Water Surface";
		_waterMeshFilter = water.GetComponent<MeshFilter>();

		SetVisible(false);

		ThreadedDataRequester.RequestData(() => MapGenerator.Instance.GenerateMapData(this.position), OnMapDataReceived);
	}

	public void UpdateSelf()
	{
		if (!_hasMapData)
			return;

		float viewerToNearestEdgeDistanceSquared = _edgeBounds.SqrDistance(TerrainChunkGenerator.viewerPosition);
		bool visible = viewerToNearestEdgeDistanceSquared <= TerrainChunkGenerator.maxViewDistanceSquared; // Compare two squared values.

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
					_rendererBounds = _meshRenderer.bounds;

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

			// Only request vegetation after received a chunk mesh.
			if (!_hasVegetation && !_hasRequestedVegetation && _previousLODIndex != -1)
			{
				int generableCount = PrefabGenerator.Instance.generables.Count;

				ThreadedDataRequester.RequestData(generableCount, RequestGenerableData, OnGenerableDataReceived);
				_hasRequestedVegetation = true;
			}

			TerrainChunkGenerator.outOfViewChunks.Add(this);
		}

		//Debug.Log(_bounds.center);
		SetVisible(visible);
	}

	public void SetVisible(bool visible)
	{
		if (chunkObject != null)
			chunkObject.SetActive(visible);
	}

	private void OnMapDataReceived(object mapDataObject)
	{
		_mapData = (MapData)mapDataObject;
		_hasMapData = true;

		int chunkSize = MapGenerator.MapChunkSize;
		Texture2D tex = TextureGenerator.FromColorMap(_mapData.colorMap, chunkSize, chunkSize);
		_meshRenderer.material.SetTexture("_BaseMap", tex);

		UpdateSelf();
	}

	private object RequestGenerableData(int index)
	{
		TerrainData terrainData = MapGenerator.Instance.terrainData;
		return PrefabGenerator.Instance.GenerateData(index, _rendererBounds, terrainData.meshHeightCurve, terrainData.meshHeight);
	}

	private void OnGenerableDataReceived(object dataObject)
	{
		GenerableData data = (GenerableData)dataObject;
		PrefabGenerator.Instance.generables[data.index].Generate(_vegetationRoot, data, VegetationGenerateSuccess);
	}

	private void VegetationGenerateSuccess(bool success)
	{
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

		TerrainData terrainData = MapGenerator.Instance.terrainData;
		Func<MeshData> generateMethod = () => MeshGenerator.ToTerrainMesh(mapData.heightMap,
											terrainData.meshHeight, terrainData.meshHeightCurve, _levelOfDetail, terrainData.useFlatShading);

		ThreadedDataRequester.RequestData(generateMethod, OnMeshDataReceived);
	}

	public void OnMeshDataReceived(object meshDataObject)
	{
		chunkMesh = ((MeshData)meshDataObject).CreateChunkMesh();
		waterMesh = ((MeshData)meshDataObject).CreateWaterMesh();
		hasMesh = true;

		_chunkUpdateCalback();
	}
}
