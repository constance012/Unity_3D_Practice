using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public static class CameraSwitcher
{
	static List<CinemachineVirtualCamera> cameras = new List<CinemachineVirtualCamera> ();

	public static CinemachineVirtualCamera activeCam = null;

	public static bool isActive(CinemachineVirtualCamera cam)
	{
		return cam == activeCam;
	}

	public static void SwitchCam(CinemachineVirtualCamera cam)
	{
		cam.Priority = 20;
		activeCam = cam;

		// Iterate through the list of cameras and set the priority of those which are not the active cam to 0.
		foreach (CinemachineVirtualCamera c in cameras)
			if (c.Priority != 0 && c != activeCam)
				c.Priority = 0;
	}

	public static void Register(CinemachineVirtualCamera cam)
	{
		cameras.Add(cam);
		Debug.Log("Registered camera: " + cam);
	}

	public static void Unregister(CinemachineVirtualCamera cam)
	{
		cameras.Remove(cam);
		Debug.Log("Unregistered camera: " + cam);
	}
}
