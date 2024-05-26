using System.Collections.Generic;
using UnityEngine;

public class NodeGrid : MonoBehaviour
{
	[Header("Gizmos Options"), Space]
	[SerializeField] private bool displayGizmos;
	
	[Header("Terrain Layers"), Space]
	public LayerMask unwalkableLayers;
    
	[Header("Grid Settings"), Space]
    public Vector2 gridWorldSize;
    public float nodeDiameter;

	[Header("Settings"), Space]
	[SerializeField, Tooltip("The small offset to the overlap circle's radius to achieve more precise collision detection against the terrain.")]
	[Range(0f, .3f)] private float castPrecision;

    // Private fields.
    private Node[,] _grid;
	private Vector3 _selfWorldPos;
	private int _width, _height;
	private float _nodeRadius;

	public int MaxSize => _width * _height;

	private void Awake()
	{
		_selfWorldPos = transform.position;
		_nodeRadius = nodeDiameter / 2f;

		// The amount of nodes in the X and Y axes.
		_width = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
		_height = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);

		CreateGrid();
	}

	#if UNITY_EDITOR
	private void LateUpdate()
	{
		if (transform.position != _selfWorldPos)
			transform.position =  _selfWorldPos;
	}
	#endif

	public List<Node> GetNeighbors(Node node)
	{
		List<Node> neighbors = new List<Node>();

		// Search in the 3x3 area around the node.
		for (int x = -1; x <= 1; x++)
		{
			for (int y = -1; y <= 1; y++)
			{
				// Skip the center node because it's the original node.
				if (x == 0 && y == 0)
					continue;

				int checkX = node.x + x;
				int checkY = node.y + y;

				if (checkX >= 0 && checkX < _width && checkY >= 0 && checkY < _height)
					neighbors.Add(_grid[checkX, checkY]);
			}
		}

		return neighbors;
	}

	/// <summary>
	/// Get a node corresponding node in the grid to the provided world position.
	/// </summary>
	/// <param name="worldPos"></param>
	/// <returns> The corresponding node retrieved from the grid. </returns>
	public Node FromWorldPosition(Vector3 worldPos)
	{
		// Finalize the coordinates based on the grid world position.
		float finalizedX = worldPos.x - _selfWorldPos.x;
		float finalizedY = worldPos.z - _selfWorldPos.y;

		float percentX = (finalizedX + gridWorldSize.x / 2) / gridWorldSize.x;
		float percentY = (finalizedY + gridWorldSize.y / 2) / gridWorldSize.y;

		percentX = Mathf.Clamp01(percentX);
		percentY = Mathf.Clamp01(percentY);

		int xCoordinate = Mathf.RoundToInt((_width - 1) * percentX);
		int yCoordinate = Mathf.RoundToInt((_height - 1) * percentY);

		return _grid[xCoordinate, yCoordinate];
	}

	public void SetCellWalkableState(Vector3 worldPos, bool walkable)
	{
		Node cell = FromWorldPosition(worldPos);
		cell.walkable = walkable;
	}

	private void CreateGrid()
	{
		_grid = new Node[_width, _height];
		Vector3 worldBottomLeft = transform.position - new Vector3(gridWorldSize.x / 2f, 1f, gridWorldSize.y / 2f);
		
		for (int x = 0; x < _width; x++)
		{
			for (int y = 0; y < _height; y++)
			{
				// Get the point at the center of each cell.
				Vector3 worldPoint = worldBottomLeft + new Vector3(x * nodeDiameter + _nodeRadius, 1f, y * nodeDiameter + _nodeRadius);

				bool walkable = !Physics.CheckSphere(worldPoint, _nodeRadius - castPrecision, unwalkableLayers);
				_grid[x, y] = new Node(walkable, worldPoint, x, y);
			}
		}
	}

	private void OnDrawGizmos()
	{
		Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1f, gridWorldSize.y));

		if (_grid != null && displayGizmos)
		{
			Vector3 cubeSize = Vector3.one * (nodeDiameter - .1f);

			foreach (Node node in _grid)
			{
				Gizmos.color = node.walkable ? Color.white : Color.red;
				Gizmos.DrawCube(node.worldPosition, cubeSize);
			}
		}
	}
}
