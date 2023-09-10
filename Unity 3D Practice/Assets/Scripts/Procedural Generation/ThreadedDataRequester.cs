using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class ThreadedDataRequester : Singleton<ThreadedDataRequester>
{
	/// <summary>
	/// A queue holds all the thread info of MapData to execute their <c>callback</c> inside Unity's main thread.
	/// </summary>
	private Queue<ThreadInfo> _dataThreadInfos = new Queue<ThreadInfo>();

	private void Update()
	{
		if (_dataThreadInfos.Count > 0)
		{
			for (int i = 0; i < _dataThreadInfos.Count; i++)
			{
				ThreadInfo threadInfo = _dataThreadInfos.Dequeue();
				threadInfo.ExecuteCallback();
			}
		}
	}

	#region Threading.
	public static void RequestData(Func<object> generateMethod, Action<object> callback)
	{
		ThreadStart threadStart = () => Instance.DataThread(generateMethod, callback);
		new Thread(threadStart).Start();
	}

	private void DataThread(Func<object> generateMethod, Action<object> callback)
	{
		object data = generateMethod?.Invoke();

		// Lock the queue so no other threads can access it while one thread is executing this block of code.
		// And others have to wait for their turn.
		lock (_dataThreadInfos)
		{
			ThreadInfo mapThreadInfo = new ThreadInfo(data, callback);

			_dataThreadInfos.Enqueue(mapThreadInfo);
		}
	}

	public static void RequestData(int count, Func<int, object> generateMethod, Action<object> callback)
	{
		ThreadStart threadStart = () => Instance.DataThread(count, generateMethod, callback);
		new Thread(threadStart).Start();
	}

	private void DataThread(int count, Func<int, object> generateMethod, Action<object> callback)
	{
		// Lock the queue so no other threads can access it while one thread is executing this block of code.
		// And others have to wait for their turn.
		lock (_dataThreadInfos)
		{
			for (int i = 0; i < count; i++)
			{
				object data = generateMethod?.Invoke(i);
				
				ThreadInfo mapThreadInfo = new ThreadInfo(data, callback);

				_dataThreadInfos.Enqueue(mapThreadInfo);
			}
		}
	}
	#endregion
}

/// <summary>
/// Holds the data and the callback method of a thread.
/// </summary>
/// <typeparam name="TData"></typeparam>
public readonly struct ThreadInfo
{
	public readonly object data;
	public readonly Action<object> callback;

	public ThreadInfo(object data, Action<object> callback)
	{
		this.data = data;
		this.callback = callback;
	}

	public void ExecuteCallback() => this.callback?.Invoke(this.data);
}
