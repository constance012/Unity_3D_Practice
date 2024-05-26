using System;
using System.Collections.Generic;
using UnityEngine;

public class PathRequester : Singleton<PathRequester>
{
	[Header("References"), Space]
	[SerializeField] private AStarBrain brain;
	[SerializeField] private NodeGrid grid;

	// Private fields.
	private Queue<PathResult> _results = new Queue<PathResult>();

	private void Update()
	{
		if (_results.Count > 0)
		{
			int n = _results.Count;
			for (int i = 0; i < n; i++)
			{
				PathResult result = _results.Dequeue();
				result.InvokeCallback();
			}
		}
	}

	public static void Request(PathRequestData request)
	{
		Instance.brain.FindPath(request, Instance.OnPathFinishedProcessing);
	}
	
	public void ChangeGridCellState(Vector3 worldPos, bool walkable)
	{
		grid.SetCellWalkableState(worldPos, walkable);
	}
	
	private void OnPathFinishedProcessing(PathResult result)
	{
		lock(_results)
		{
			_results.Enqueue(result);
		}
	}
}

public struct PathResult
{
	public Vector3[] path;
	public bool success;
	public GameObject requester;
	public Action<Vector3[], bool> callback;

	public PathResult(Vector3[] path, bool success, GameObject requester, Action<Vector3[], bool> callback)
	{
		this.path = path;
		this.success = success;
		this.requester = requester;
		this.callback = callback;
	}

	public void InvokeCallback()
	{
		if (requester != null)
			callback(path, success);
	}
}

public struct PathRequestData
{
	public Vector3 pathStart;
	public Vector3 pathEnd;
	public GameObject requester;
	public Action<Vector3[], bool> callback;

	public PathRequestData(Vector3 start, Vector3 end, GameObject requester, Action<Vector3[], bool> callback)
	{
		this.pathStart = start;
		this.pathEnd = end;
		this.requester = requester;
		this.callback = callback;
	}
}
