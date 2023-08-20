using UnityEngine;

public static class MeshGenerator
{
	public static MeshData ToTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve heightCurve, int levelOfDetail)
	{
		AnimationCurve thisHeightCurve = new AnimationCurve(heightCurve.keys);

		int width = heightMap.GetLength(0);
		int height = heightMap.GetLength(1);

		// Get the top left offset so that the mesh will be centered on the screen.
		float topLeftX = (width - 1) / -2f;
		float topLeftZ = (height - 1) / 2f;

		int meshVerticesStep = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
		int verticesPerAxis = (width - 1) / meshVerticesStep + 1;

		MeshData meshData = new MeshData(verticesPerAxis, verticesPerAxis);

		int vertexIndex = 0;

		for (int y = 0; y < height; y += meshVerticesStep)
		{
			for (int x = 0; x < width; x += meshVerticesStep)
			{
				float vertexHeight = thisHeightCurve.Evaluate(heightMap[x, y]) * heightMultiplier;

				meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, vertexHeight, topLeftZ - y);
				meshData.UVs[vertexIndex] = new Vector2(x / (float)width, y / (float)height);

				// Add 2 triangles that made up a square at this vertex, ignore all vertices along the right and bottom of the map.
				if (x < width - 1 && y < height - 1)
				{
					// Add vertices in the clock-wise order.
					meshData.AddTriangle(vertexIndex, vertexIndex + verticesPerAxis + 1, vertexIndex + verticesPerAxis);
					meshData.AddTriangle(vertexIndex + verticesPerAxis + 1, vertexIndex, vertexIndex + 1);
				}

				vertexIndex++;
			}
		}

		return meshData;
	}
}

public class MeshData
{
	public Vector3[] vertices;
	public int[] triangles;

	/// <summary>
	/// UV(W) coordinates are a normalized (0 to 1) 2-Dimensional coordinate system.
	/// <para />
	/// The origin (0,0) exists in the BOTTOM LEFT corner of the space.
	/// </summary>
	public Vector2[] UVs;

	private int _triangleIndex;

	public MeshData(int width, int height)
	{
		vertices = new Vector3[width * height];
		UVs = new Vector2[width * height];

		// Each triangle is made up by 3 vertices, and 2 triangles form a square.
		int length = (width - 1) * (height - 1) * 6;
		triangles = new int[length];
	}

	/// <summary>
	/// Inserts a triangle into this mesh, defined by 3 vertices.
	/// </summary>
	/// <param name="vertexA"></param>
	/// <param name="vertexB"></param>
	/// <param name="vertexC"></param>
	public void AddTriangle(int vertexA, int vertexB, int vertexC)
	{
		triangles[_triangleIndex] = vertexA;
		triangles[_triangleIndex + 1] = vertexB;
		triangles[_triangleIndex + 2] = vertexC;

		_triangleIndex += 3;
	}

	public Mesh CreateMesh()
	{
		Mesh mesh = new Mesh();

		mesh.vertices = this.vertices;
		mesh.triangles = this.triangles;
		mesh.uv = this.UVs;

		// Recalculate normal vectors so the lighting will work properly.
		mesh.RecalculateNormals();

		return mesh;
	}
}