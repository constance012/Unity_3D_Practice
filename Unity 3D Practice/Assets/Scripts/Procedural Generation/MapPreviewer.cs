using UnityEditor;
using UnityEngine;
using static MapGenerator;

public class MapPreviewer : MonoBehaviour
{
	[ReadOnly] public bool showPreviewMesh = true;

	[Header("Water Surface"), Space]
	public GameObject waterPrefab;
	public bool includeWater;

	[Header("Texture Drawer"), Space]
	public Renderer textureRenderer;

	[Header("Mesh Drawer"), Space]
	public MeshFilter meshFilter;
	public MeshRenderer meshRenderer;

	public bool drawMeshWireframe;
	public bool showCollider;

	[Header("Auto Regenerate Map")]
	public bool autoUpdate;

	// Private fields.
	private GameObject _waterSurfaceObject;
	private MeshCollider _collider;

	private void Start()
	{
		meshRenderer.transform.parent.gameObject.SetActive(false);
		ClearWater();
	}

	#if UNITY_EDITOR
	public void DrawTexture(Texture2D texture)
	{
		// Set the texture in the edit mode.
		textureRenderer.sharedMaterial.SetTexture("_BaseMap", texture);
		textureRenderer.transform.localScale = new Vector3(texture.width / 10f, 1f, texture.height / 10f);
	}

	public void DrawMesh(MeshData meshData, Texture2D texture)
	{
		if (_collider == null)
			_collider = meshRenderer.GetComponent<MeshCollider>();

		meshFilter.sharedMesh = meshData.CreateChunkMesh();	
		_collider.sharedMesh = showCollider ? meshData.CreateChunkMesh() : null;
		
		meshRenderer.sharedMaterial.SetTexture("_BaseMap", texture);
		meshRenderer.transform.localScale = Vector3.one * MapGenerator.Instance.terrainData.chunkScale;

		if (includeWater && waterPrefab != null)
		{
			_waterSurfaceObject = (GameObject)PrefabUtility.InstantiatePrefab(waterPrefab, meshRenderer.transform);
			PrefabUtility.UnpackPrefabInstance(_waterSurfaceObject, PrefabUnpackMode.Completely, InteractionMode.UserAction);
			_waterSurfaceObject.GetComponent<MeshFilter>().mesh = meshData.CreateWaterMesh();
		}
	}

	public void ManagePreviewObjects(MapDrawMode drawMode)
	{
		ClearWater();

		if (drawMode == MapDrawMode.Mesh)
		{
			textureRenderer.gameObject.SetActive(false);
			meshRenderer.gameObject.SetActive(true);
		}
		else
		{
			textureRenderer.gameObject.SetActive(true);
			meshRenderer.gameObject.SetActive(false);
		}
	}
	#endif

	private void ClearWater()
	{
		DestroyImmediate(_waterSurfaceObject);

		while (meshRenderer.transform.Find(waterPrefab.name, out Transform leftover))
		{
			DestroyImmediate(leftover.gameObject);
		}
	}

	private void OnDrawGizmosSelected()
	{
		if (meshFilter != null && drawMeshWireframe)
		{
			Gizmos.color = new Color(0f, 0f, 0f, .2f);
			Gizmos.DrawWireMesh(
				meshFilter.sharedMesh,
				meshFilter.transform.position,
				meshFilter.transform.rotation,
				meshFilter.transform.localScale
			);
		}
	}
}
