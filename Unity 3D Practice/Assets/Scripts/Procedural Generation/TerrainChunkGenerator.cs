using System;
using System.Collections.Generic;
using UnityEngine;

public class TerrainChunkGenerator : Singleton<TerrainChunkGenerator>
{
	public enum TerrainSizeMode { Endless, FixedSize }
	
	[Header("Level of Detail Properties"), Space]
	public LODInfo[] detailLevels;

	[Header("Terrain Chunk Settings"), Space]
	[SerializeField] private TerrainSizeMode sizeMode;

	[SerializeField, Tooltip("How many chunks does this terrain consist of on each axis?" +
	"Even number inputs will be increased by 1 internally to center the map properly.")]
	private Vector2Int terrainDimensions;

	public bool includeVegetation;
	
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

		_chunkSize = MapGenerator.MapChunkSize - 1;
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
		viewerPosition /= MapGenerator.Instance.terrainData.chunkScale;

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