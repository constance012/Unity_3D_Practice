using System.Collections;
using UnityEngine;

public class Unit : Seeker
{
	[Header("Settings"), Space]
	[SerializeField] private float speed;

	private bool _forcedStopMoving;

	protected override IEnumerator ExecuteFoundPath(int previousIndex = -1)
	{
		if (_path.Length == 0)
			yield break;

		Vector3 currentWaypoint = previousIndex == -1 ? _path[0] : _path[previousIndex];

		//Debug.Log($"{gameObject.name} following path...");

		while (!_forcedStopMoving)
		{
			float distanceToCurrent = Vector3.Distance(transform.position, currentWaypoint);

			if (distanceToCurrent <= .15f)
			{
				_waypointIndex++;

				// If there's no more waypoints to move, then simply returns out of the coroutine.
				if (_waypointIndex >= _path.Length)
				{
					Debug.Log("No waypoint left.");
					StopFollowPath();
					yield break;
				}

				currentWaypoint = _path[_waypointIndex];
			}

			transform.position = Vector3.MoveTowards(transform.position, currentWaypoint, speed * Time.deltaTime);

			yield return null;
		}

		StopFollowPath();
	}

	protected void StopFollowPath()
	{
		_waypointIndex = 0;
		_path = new Vector3[0];
		_forcedStopMoving = true;
	}
}
