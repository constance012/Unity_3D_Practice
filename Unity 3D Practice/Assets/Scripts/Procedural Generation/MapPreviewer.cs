using UnityEngine;

public class MapPreviewer : MonoBehaviour
{
	[Header("Texture Drawer"), Space]
	public Renderer textureRenderer;

	[Header("Mesh Drawer"), Space]
	public MeshFilter meshFilter;
	public MeshRenderer meshRenderer;

	public bool drawMeshWireframe;

	public void DrawTexture(Texture2D texture)
	{
		// Set the texture in the edit mode.
		textureRenderer.sharedMaterial.SetTexture("_BaseMap", texture);
		textureRenderer.transform.localScale = new Vector3(texture.width / 10f, 1f, texture.height / 10f);
	}

	public void DrawMesh(MeshData meshData, Texture2D texture)
	{
		meshFilter.sharedMesh = meshData.CreateMesh();
		
		meshRenderer.sharedMaterial.SetTexture("_BaseMap", texture);
		meshRenderer.transform.localScale = Vector3.one * texture.width / 10f;
	}

	public void OnDrawGizmosSelected()
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
