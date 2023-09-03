using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using System.Linq;

public static class CameraSwitcher
{
	private static List<CinemachineVirtualCameraBase> Cameras = new List<CinemachineVirtualCameraBase>();

	public static CinemachineVirtualCameraBase ActiveCam { get; private set; } = null;

	public static CinemachineVirtualCamera FpsCam
	{
		get { return Cameras.Find(camera => camera.CompareTag("FirstPersonCam")) as CinemachineVirtualCamera; }
	}

	public static CinemachineFreeLook TpsCam
	{
		get { return Cameras.Find(camera => camera.CompareTag("ThirdPersonCam")) as CinemachineFreeLook; }
	}

	public static bool DoneInitializing
	{
		get { return Cameras != null && Cameras.Any(); }
	}

	public static bool IsActive(CinemachineVirtualCameraBase cam) => cam == ActiveCam;

	public static void SwitchCam(CinemachineVirtualCameraBase cam)
	{
		cam.Priority = 20;
		ActiveCam = cam;

		// Iterate through the list of cameras and set the priority of those which are not the active cam to 0.
		foreach (CinemachineVirtualCameraBase c in Cameras)
			if (c.Priority != 0 && c != ActiveCam)
				c.Priority = 0;
	}

	public static void Register(CinemachineVirtualCameraBase cam)
	{
		Cameras.Add(cam);
		Debug.Log("Registered camera: " + cam);
	}

	public static void Unregister(CinemachineVirtualCameraBase cam)
	{
		Cameras.Remove(cam);
		Debug.Log("Unregistered camera: " + cam);
	}
}
