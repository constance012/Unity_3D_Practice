using UnityEngine;

public static class MeshGenerator
{
	/// <summary>
	/// Generates a mesh data object from the provided height map.
	/// </summary>
	/// <param name="heightMap"></param>
	/// <param name="heightMultiplier"></param>
	/// <param name="heightCurve"></param>
	/// <param name="levelOfDetail"></param>
	/// <returns>A mesh data object to create a new mesh.</returns>
	public static MeshData ToTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve heightCurve, int levelOfDetail, bool useFlatShading)
	{
		AnimationCurve threadSafeCurve = new AnimationCurve(heightCurve.keys);
		
		int meshVerticesStep = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;

		int borderedSize = heightMap.GetLength(0);
		
		int meshSizeActual = borderedSize - 2 * meshVerticesStep;
		int meshSizeUnsimplified = borderedSize - 2;

		// Get the top left offset so that the mesh will be centered on the screen.
		float topLeftX = (meshSizeUnsimplified - 1) / -2f;
		float topLeftZ = (meshSizeUnsimplified - 1) / 2f;

		int verticesPerAxis = (meshSizeActual - 1) / meshVerticesStep + 1;

		MeshData meshData = new MeshData(verticesPerAxis, useFlatShading);

		int[,] vertexIndicesMap = new int[borderedSize, borderedSize];
		int meshVertexIndex = 0;
		int borderVertexIndex = -1;

		// Populate the vertex indices map.
		for (int y = 0; y < borderedSize; y += meshVerticesStep)
		{
			for (int x = 0; x < borderedSize; x += meshVerticesStep)
			{
				bool isBorderVertex = y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1; 

				if (isBorderVertex)
				{
					vertexIndicesMap[x, y] = borderVertexIndex;
					borderVertexIndex--;
				}
				else
				{
					vertexIndicesMap[x, y] = meshVertexIndex;
					meshVertexIndex++;
				}
			}
		}

		// Set up the mesh data.
		for (int y = 0; y < borderedSize; y += meshVerticesStep)
		{
			for (int x = 0; x < borderedSize; x += meshVerticesStep)
			{
				int vertexIndex = vertexIndicesMap[x, y];

				Vector2  uvCoord = new Vector2((x - meshVerticesStep) / (float)meshSizeActual, (y - meshVerticesStep) / (float)meshSizeActual);

				float vertexHeight = threadSafeCurve.Evaluate(heightMap[x, y]) * heightMultiplier;
				Vector3 vertexPosition = new Vector3(topLeftX + uvCoord.x * meshSizeUnsimplified,
													 vertexHeight,
													 topLeftZ - uvCoord.y * meshSizeUnsimplified);

				meshData.AddVertex(vertexPosition, uvCoord, vertexIndex);

				// Add 2 triangles (CAD and BDA) that made up a square at this vertex,
				// ignore all vertices along the right and bottom of the map.
				// A--B
				// |\\|
				// C--D
				if (x < borderedSize - 1 && y < borderedSize - 1)
				{
					int A = vertexIndicesMap[x, y];
					int B = vertexIndicesMap[x + meshVerticesStep, y];
					int C = vertexIndicesMap[x, y + meshVerticesStep];
					int D = vertexIndicesMap[x + meshVerticesStep, y + meshVerticesStep];

					// Add vertices in the clock-wise order.
					meshData.AddTriangle(C, A, D);
					meshData.AddTriangle(B, D, A);
				}
			}
		}

		if (useFlatShading)
			meshData.FlatShadingSurfaces();
		else
			meshData.BakeVertexNormals();

		return meshData;
	}
}

/// <summary>
/// Contains data of a mesh of a single terrain chunk.
/// </summary>
public class MeshData
{
	/// <summary>
	/// An array of vertex positions in the mesh.
	/// </summary>
	private Vector3[] _meshVertices;

	/// <summary>
	/// An array of vertex indices' order that form all the triangles of the mesh.
	/// </summary>
	private int[] _meshTriangles;

	/// <summary>
	/// UV(W) coordinates are a normalized (0 to 1) 2-Dimensional coordinate system.
	/// <para />
	/// The origin (0,0) exists in the BOTTOM LEFT corner of the space.
	/// </summary>
	private Vector2[] _UVs;

	private Vector3[] _borderVertices;
	private int[] _borderTriangles;

	private Vector3[] bakedNormals;

	private int _meshTriangleIndex = 0;
	private int _borderTriangleIndex = 0;
	private bool _useFlatShading;

	public MeshData(int verticesPerAxis, bool useFlatShading)
	{
		_useFlatShading = useFlatShading;
		
		// Initialize the mesh data.
		_meshVertices = new Vector3[verticesPerAxis * verticesPerAxis];

		// Each triangle is made up by 3 vertices, and 2 triangles form a square.
		int meshTriangleIndices = (verticesPerAxis - 1) * (verticesPerAxis - 1) * 6;
		_meshTriangles = new int[meshTriangleIndices];

		_UVs = new Vector2[verticesPerAxis * verticesPerAxis];

		// Initialize the border data of this mesh.
		_borderVertices = new Vector3[verticesPerAxis * 4 + 4];

		// The border has additional 4 squares for each corner.
		int borderTriangles = verticesPerAxis * 6 * 4;
		_borderTriangles = new int[borderTriangles];
	}

	public void AddVertex(Vector3 position, Vector2 uvCoord, int index)
	{
		if (index < 0)
		{
			// Invert the index since it started at -1 and went downwards.
			_borderVertices[-index - 1] = position;
		}
		else
		{
			_meshVertices[index] = position;
			_UVs[index] = uvCoord;
		}
	}

	/// <summary>
	/// Inserts a triangle into this mesh, defined by 3 vertices.
	/// </summary>
	/// <param name="vertexA"></param>
	/// <param name="vertexB"></param>
	/// <param name="vertexC"></param>
	public void AddTriangle(int vertexA, int vertexB, int vertexC)
	{
		if (vertexA < 0 || vertexB < 0 || vertexC < 0)
		{
			_borderTriangles[_borderTriangleIndex] = vertexA;
			_borderTriangles[_borderTriangleIndex + 1] = vertexB;
			_borderTriangles[_borderTriangleIndex + 2] = vertexC;

			_borderTriangleIndex += 3;
		}
		else
		{
			_meshTriangles[_meshTriangleIndex] = vertexA;
			_meshTriangles[_meshTriangleIndex + 1] = vertexB;
			_meshTriangles[_meshTriangleIndex + 2] = vertexC;

			_meshTriangleIndex += 3;
		}
	}

	public Mesh CreateChunkMesh()
	{
		Mesh mesh = new Mesh();

		mesh.vertices = this._meshVertices;
		mesh.triangles = this._meshTriangles;
		mesh.uv = this._UVs;

		// Recalculate normal vectors so the lighting will work properly.
		if (_useFlatShading)
			mesh.RecalculateNormals();
		else
			mesh.normals = bakedNormals;

		return mesh;
	}

	public Mesh CreateWaterMesh()
	{
		Mesh water = new Mesh();

		Vector3[] waterVertices = new Vector3[_meshVertices.Length];

		for (int i = 0; i < waterVertices.Length; i++)
		{
			waterVertices[i] = new Vector3(_meshVertices[i].x, 0f, _meshVertices[i].z);
		}

		water.vertices = waterVertices;
		water.triangles = _meshTriangles;
		water.uv = _UVs;

		water.RecalculateNormals();

		return water;
	}

	/// <summary>
	/// Bake the vertex normals separate from the Unity's main thread to prevent heavy performance impacts.
	/// </summary>
	public void BakeVertexNormals()
	{
		bakedNormals = RecalculateVertexNormals();
	}

	/// <summary>
	/// Makes triangles receive the same lighting across their surfaces - in other words, to be flat-shaded.
	/// </summary>
	public void FlatShadingSurfaces()
	{
		Vector3[] flatShadedVertices = new Vector3[_meshTriangles.Length];
		Vector2[] flatShadedUVs = new Vector2[_meshTriangles.Length];

		for (int i = 0; i < flatShadedVertices.Length; i++)
		{
			int vertexIndex = _meshTriangles[i];
			flatShadedVertices[i] = _meshVertices[vertexIndex];
			flatShadedUVs[i] = _UVs[vertexIndex];

			_meshTriangles[i] = i;
		}

		_meshVertices = flatShadedVertices;
		_UVs = flatShadedUVs;
	}

	private Vector3[] RecalculateVertexNormals()
	{
		Vector3[] vertexNormals = new Vector3[_meshVertices.Length];

		#region Add the mesh's triangle normals to each vertex.
		int triangleCount = _meshTriangles.Length / 3;

		for (int i = 0; i < triangleCount; i++)
		{
			_meshTriangleIndex = i * 3;

			// Get the indices of vertices that made up this triangle.
			int vertexIndexA = _meshTriangles[_meshTriangleIndex];
			int vertexIndexB = _meshTriangles[_meshTriangleIndex + 1];
			int vertexIndexC = _meshTriangles[_meshTriangleIndex + 2];

			// Get the normal vector of this triangle.
			Vector3 triangleNormal = GetTriangleNormal(vertexIndexA, vertexIndexB, vertexIndexC);

			// Add that normal vector to each of the vertices that are part of that triangle and normalize them.
			(vertexNormals[vertexIndexA] += triangleNormal).Normalize();
			(vertexNormals[vertexIndexB] += triangleNormal).Normalize();
			(vertexNormals[vertexIndexC] += triangleNormal).Normalize();
		}
		#endregion

		#region Add the border's triangle normals to each vertex.
		int borderTriangleCount = _borderTriangles.Length / 3;

		for (int i = 0; i < borderTriangleCount; i++)
		{
			_borderTriangleIndex = i * 3;

			// Get the indices of vertices that made up this border triangle.
			int vertexIndexA = _borderTriangles[_borderTriangleIndex];
			int vertexIndexB = _borderTriangles[_borderTriangleIndex + 1];
			int vertexIndexC = _borderTriangles[_borderTriangleIndex + 2];

			// Get the normal vector of this triangle.
			Vector3 triangleNormal = GetTriangleNormal(vertexIndexA, vertexIndexB, vertexIndexC);

			// Add that normal vector to each of the mesh (index >= 0) vertices that are part of that triangle and normalize them.
			if (vertexIndexA >= 0)
				(vertexNormals[vertexIndexA] += triangleNormal).Normalize();

			if (vertexIndexB >= 0)
				(vertexNormals[vertexIndexB] += triangleNormal).Normalize();

			if (vertexIndexC >= 0)
				(vertexNormals[vertexIndexC] += triangleNormal).Normalize();
		}
		#endregion

		return vertexNormals;
	}

	private Vector3 GetTriangleNormal(int vertexIndexA, int vertexIndexB, int vertexIndexC)
	{
		Vector3 vertexA = (vertexIndexA < 0) ? _borderVertices[-vertexIndexA - 1] : _meshVertices[vertexIndexA];
		Vector3 vertexB = (vertexIndexB < 0) ? _borderVertices[-vertexIndexB - 1] : _meshVertices[vertexIndexB];
		Vector3 vertexC = (vertexIndexC < 0) ? _borderVertices[-vertexIndexC - 1] : _meshVertices[vertexIndexC];

		Vector3 edgeAB = vertexB - vertexA;
		Vector3 edgeAC = vertexC - vertexA;

		return Vector3.Cross(edgeAB, edgeAC).normalized;
	}
}