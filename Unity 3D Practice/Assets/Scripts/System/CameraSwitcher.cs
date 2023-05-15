using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public static class CameraSwitcher
{
	static List<CinemachineVirtualCameraBase> cameras = new List<CinemachineVirtualCameraBase>();

	public static CinemachineVirtualCameraBase activeCam { get; private set; } = null;

	public static CinemachineVirtualCamera fpsCam
	{
		get { return cameras.Find(camera => camera.CompareTag("FirstPersonCam")) as CinemachineVirtualCamera; }
	}

	public static CinemachineFreeLook tpsCam
	{
		get { return cameras.Find(camera => camera.CompareTag("ThirdPersonCam")) as CinemachineFreeLook; }
	}

	public static bool IsActive(CinemachineVirtualCameraBase cam) => cam == activeCam;

	public static void SwitchCam(CinemachineVirtualCameraBase cam)
	{
		cam.Priority = 20;
		activeCam = cam;

		// Iterate through the list of cameras and set the priority of those which are not the active cam to 0.
		foreach (CinemachineVirtualCameraBase c in cameras)
			if (c.Priority != 0 && c != activeCam)
				c.Priority = 0;
	}

	public static void Register(CinemachineVirtualCameraBase cam)
	{
		cameras.Add(cam);
		Debug.Log("Registered camera: " + cam);
	}

	public static void Unregister(CinemachineVirtualCameraBase cam)
	{
		cameras.Remove(cam);
		Debug.Log("Unregistered camera: " + cam);
	}
}
