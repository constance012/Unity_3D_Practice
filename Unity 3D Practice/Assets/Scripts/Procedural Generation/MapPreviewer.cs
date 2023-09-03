using UnityEditor;
using UnityEngine;

public class MapPreviewer : MonoBehaviour
{
	[Header("Water Surface"), Space]
	public GameObject waterPrefab;
	public bool includeWater;

	[Header("Texture Drawer"), Space]
	public Renderer textureRenderer;

	[Header("Mesh Drawer"), Space]
	[Min(1f)] public float meshScale;
	public MeshFilter meshFilter;
	public MeshRenderer meshRenderer;

	public bool drawMeshWireframe;

	private GameObject _waterSurfaceObject;

	private void Start()
	{
		textureRenderer.transform.parent.gameObject.SetActive(false);
		ClearWater();
	}

	public void DrawTexture(Texture2D texture)
	{
		// Set the texture in the edit mode.
		textureRenderer.sharedMaterial.SetTexture("_BaseMap", texture);
		textureRenderer.transform.localScale = new Vector3(texture.width / 10f, 1f, texture.height / 10f);
	}

	public void DrawMesh(MeshData meshData, Texture2D texture)
	{
		meshFilter.sharedMesh = meshData.CreateChunkMesh();
		
		meshRenderer.sharedMaterial.SetTexture("_BaseMap", texture);
		meshRenderer.transform.localScale = Vector3.one * meshScale;

		if (includeWater && waterPrefab != null)
		{
			_waterSurfaceObject = (GameObject)PrefabUtility.InstantiatePrefab(waterPrefab, meshRenderer.transform);
			PrefabUtility.UnpackPrefabInstance(_waterSurfaceObject, PrefabUnpackMode.Completely, InteractionMode.UserAction);
			_waterSurfaceObject.GetComponent<MeshFilter>().mesh = meshData.CreateWaterMesh();
		}
	}

	public void ClearWater()
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
