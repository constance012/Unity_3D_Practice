using System;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine;

public class AStarBrain : MonoBehaviour
{
	[Header("Grid of Nodes"), Space]
	[SerializeField] private NodeGrid grid;

	// Private fields.
	private Heap<Node> _open;
	private HashSet<Node> _closed;

	private void Start()
	{
		_open = new Heap<Node>(grid.MaxSize);
		_closed = new HashSet<Node>();
	}

	public void FindPath(PathRequestData request, Action<PathResult> onFinishedProcessing)
	{
		Stopwatch sw = new Stopwatch();
		sw.Start();

		bool pathFound = false;

		Node startNode = grid.FromWorldPosition(request.pathStart);
		Node endNode = grid.FromWorldPosition(request.pathEnd);

		// Only start finding path if both nodes are walkable.
		if (startNode.walkable && endNode.walkable)
		{
			_open.Clear();
			_closed.Clear();

			_open.AddLast(startNode);

			while (_open.Count > 0)
			{
				Node current = _open.RemoveFirst();
				_closed.Add(current);

				if (current == endNode)
				{
					sw.Stop();
					//UnityDebug.Log($"Path found in: {sw.ElapsedMilliseconds} ms, or {sw.ElapsedTicks} ticks.");
					pathFound = true;

					break;
				}

				foreach (Node neighbor in grid.GetNeighbors(current))
				{
					if (!neighbor.walkable || _closed.Contains(neighbor))
						continue;

					int newCostToNeighbor = current.gCost + GetDistanceBetween(current, neighbor);

					if (newCostToNeighbor < neighbor.gCost || !_open.Contains(neighbor))
					{
						// Set the new F cost.
						neighbor.gCost = newCostToNeighbor;
						neighbor.hCost = GetDistanceBetween(neighbor, endNode);
						neighbor.Parent = current;

						if (!_open.Contains(neighbor))
							_open.AddLast(neighbor);
						else
							_open.UpdateItemPriority(neighbor);
					}
				}
			}
		}

		// Construct the path.
		Vector3[] waypoints = new Vector3[0];
		if (pathFound)
		{
			waypoints = ConstructPath(startNode, endNode);
			pathFound = waypoints.Length > 0;
		}
		
		// Invoke the requester's callback.
		onFinishedProcessing(new PathResult(waypoints, pathFound, request.requester, request.callback));
	}

	private Vector3[] ConstructPath(Node startNode, Node endNode)
	{
		List<Node> path = new List<Node>();
		Node current = endNode;

		while (current != startNode)
		{
			path.Add(current);
			current = current.Parent;
		}

		Vector3[] waypoints = SimplifyPath(path, startNode);
		Array.Reverse(waypoints);

		return waypoints;
	}

	private Vector3[] SimplifyPath(List<Node> path, Node startNode)
	{
		List<Vector3> waypoints = new List<Vector3>();
		Vector2 oldDir = Vector2.zero;

		for (int i = 1; i < path.Count; i++)
		{
			Vector2 newDir = new Vector2(path[i - 1].x - path[i].x, path[i - 1].y - path[i].y);

			if (oldDir != newDir)
				waypoints.Add(path[i - 1].worldPosition);

			oldDir = newDir;

			// If this is the starting node of the path, means it's at the last index of the list.
			// Add it as a waypoint if the direction from the provided start node to it changed.
			if (i == path.Count - 1)
			{
				Vector2 dirToLastNode = new Vector2(path[i].x - startNode.x, path[i].y - startNode.y);

				if (oldDir != dirToLastNode)
					waypoints.Add(path[i].worldPosition);
			}
		}

		return waypoints.ToArray();
	}

	private int GetDistanceBetween(Node a, Node b)
	{
		int distX = Mathf.Abs(a.x - b.x);
		int distY = Mathf.Abs(a.y - b.y);

		// Go diagonally (14) on the shorter axis and continue on a straight line (10).
		if (distX > distY)
			return 14 * distY + 10 * (distX - distY);
		
		return 14 * distX + 10 * (distY - distX);
	}
}
