using UnityEngine;

public class Node : IHeapItem<Node>
{
	public bool walkable;
	public Vector3 worldPosition;

	/// <summary>
	/// The X coordinate of this node in the grid.
	/// </summary>
	public int x;

	/// <summary>
	/// The Y coordinate of this node in the grid.
	/// </summary>
	public int y;

	public int gCost;
	public int hCost;

	public int FCost => gCost + hCost;

	public Node Parent { get; set; }
	public int HeapIndex { get; set; }

	public Node(bool walkable, Vector3 worldPosition, int xCoordinate, int yCoordinate)
	{
		this.walkable = walkable;
		this.worldPosition = worldPosition;

		this.x = xCoordinate;
		this.y = yCoordinate;
	}

	public int CompareTo(Node node2)
	{
		int fDiff = node2.FCost - this.FCost;

		if (fDiff != 0)
			return fDiff;

		return node2.hCost - this.hCost;
	}
}
