using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class ThreadedPathRequester : Singleton<ThreadedPathRequester>
{
	[Header("References"), Space]
	[SerializeField] private AStarBrain brain;
	[SerializeField] private NodeGrid grid;

	// Private fields.
	private Queue<PathResult> _results = new Queue<PathResult>();
	private Queue<PathRequestData> _requests = new Queue<PathRequestData>();
	private PathRequestData _currentRequest;
	private Thread _thread;
	private bool _gameRunning = true;

	protected override void Awake()
	{
		base.Awake();

		ThreadStart threadStart = delegate
		{
			PathRequestsHandler();
		};
		_thread = new Thread(threadStart);
		_thread.Start();
	}

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
		lock(Instance._requests)
		{
			Instance._requests.Enqueue(request);
		}
	}
	
	public void ChangeGridCellState(Vector3 worldPos, bool walkable)
	{
		grid.SetCellWalkableState(worldPos, walkable);
	}

	public void OnBrowserClosed()
	{
		_gameRunning = false;
		_thread.Join();
	}

	private void PathRequestsHandler()
	{
		while (_gameRunning)
		{
			if (_requests.Count > 0)
			{
				lock(_requests)
				{
					_currentRequest = _requests.Dequeue();
				}
				brain.FindPath(_currentRequest, OnPathFinishedProcessing);
			}
		}
	}
	
	private void OnPathFinishedProcessing(PathResult result)
	{
		lock(_results)
		{
			_results.Enqueue(result);
		}
	}
}