using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrainSystem : MonoBehaviour
{
	[Header("Terrain Chunk Material"), Space]
	[SerializeField] private Material chunkMaterial;
	
	[Header("Viewer Settings"), Space]
	public Transform viewer;
	public const float MAX_VIEW_DISTANCE = 300f;

	// Static members.
	public static Vector2 viewerPosition;
	public static MapGenerator mapGenerator;

	// Private fields.
	private Dictionary<Vector2, TerrainChunk> _terrainChunks = new Dictionary<Vector2, TerrainChunk>();
	private List<TerrainChunk> _outOfViewChunks = new List<TerrainChunk>();
	private int _chunkSize;
	private int _chunksVisibleInView;

	private void Awake()
	{
		mapGenerator = GetComponent<MapGenerator>();
	}

	private void Start()
	{
		_chunkSize = MapGenerator.MAP_CHUNK_SIZE - 1;
		_chunksVisibleInView = Mathf.RoundToInt(MAX_VIEW_DISTANCE / _chunkSize);
	}

	private void LateUpdate()
	{
		viewerPosition.Set(viewer.position.x, viewer.position.z);
		UpdateVisibleChunks();
	}

	private void UpdateVisibleChunks()
	{
		_outOfViewChunks.ForEach(chunk => chunk.SetVisible(false));
		_outOfViewChunks.Clear();

		// Normalized coordinates.
		int currentChunkX = Mathf.RoundToInt(viewerPosition.x / _chunkSize);
		int currentChunkY = Mathf.RoundToInt(viewerPosition.y / _chunkSize);

		for (int yOffset = -_chunksVisibleInView; yOffset <= _chunksVisibleInView; yOffset++)
		{
			for (int xOffset = -_chunksVisibleInView; xOffset <= _chunksVisibleInView; xOffset++)
			{
				Vector2 visibleChunkCoord = new Vector2(currentChunkX + xOffset, currentChunkY + yOffset);

				if (_terrainChunks.ContainsKey(visibleChunkCoord, out TerrainChunk chunk))
				{
					chunk.UpdateSelf();

					if (chunk.IsVisible)
						_outOfViewChunks.Add(chunk);
				}
				else
				{
					TerrainChunk newChunk = new TerrainChunk(visibleChunkCoord, _chunkSize, this.transform, chunkMaterial);
					_terrainChunks.Add(visibleChunkCoord, newChunk);
				}
			}
		}
	}
}

public class TerrainChunk
{
	public GameObject meshObject;
	public Vector2 position;

	public bool IsVisible => meshObject.activeInHierarchy;

	private Bounds _bounds;
	private MeshRenderer _meshRenderer;
	private MeshFilter _meshFilter;

	public TerrainChunk(Vector2 normalizedCoord, int size, Transform parent, Material material)
	{
		this.position = normalizedCoord * size;
		this._bounds = new Bounds(this.position, Vector2.one * size);
		
		Vector3 position3D = new Vector3(this.position.x, 50f, this.position.y);

		this.meshObject = new GameObject("Terrain Chunk");
		_meshRenderer = this.meshObject.AddComponent<MeshRenderer>();
		_meshFilter = this.meshObject.AddComponent<MeshFilter>();

		this.meshObject.transform.parent = parent;
		this.meshObject.transform.position = position3D;
		this.meshObject.name += $" {this.meshObject.transform.GetSiblingIndex()}";

		_meshRenderer.material = material;

		SetVisible(false);

		EndlessTerrainSystem.mapGenerator.RequestMapData(OnMapDataReceived);
	}

	public void UpdateSelf()
	{
		float viewerToNearestEdgeDistance = _bounds.SqrDistance(EndlessTerrainSystem.viewerPosition);
		bool visible = viewerToNearestEdgeDistance <= EndlessTerrainSystem.MAX_VIEW_DISTANCE *
													  EndlessTerrainSystem.MAX_VIEW_DISTANCE; // Compare two squared values.
		//Debug.Log(_bounds.center);
		SetVisible(visible);
	}

	public void SetVisible(bool visible) => meshObject.SetActive(visible);

	private void OnMapDataReceived(MapData mapData)
	{
		EndlessTerrainSystem.mapGenerator.RequestMeshData(mapData, OnMeshDataReceived);
	}

	private void OnMeshDataReceived(MeshData meshData)
	{
		_meshFilter.mesh = meshData.CreateMesh();
	}
}