using System;
using UnityEngine;

public static class GameObjectExtensions
{
	public static void DisablePlayer(this GameObject player)
	{
		player.TryGetComponent(out PlayerActions actionScript);
		if (actionScript == null)
			return;

		Transform placeholderSphere = player.transform.Find("Placeholder Sphere");
		Transform model = player.transform.Find("Model");

		CameraManager.instance.ToggleMovementScript(CameraSwitcher.activeCam, false);
		actionScript.OnWeaponDrop();
		actionScript.enabled = false;

		placeholderSphere.gameObject.SetActive(true);
		model.gameObject.SetActive(false);
	}

	public static void EnablePlayer(this GameObject player)
	{
		player.TryGetComponent(out PlayerActions actionScript);
		if (actionScript == null)
			return;

		Transform placeholderSphere = player.transform.Find("Placeholder Sphere");
		Transform model = player.transform.Find("Model");
		Animator playerAnimator = player.GetComponent<Animator>();

		CameraManager.instance.ToggleMovementScript(CameraSwitcher.activeCam, true);
		playerAnimator.Rebind();

		actionScript.enabled = true;
		actionScript.OnWeaponPickup(true);

		placeholderSphere.gameObject.SetActive(false);
		model.gameObject.SetActive(true);
	}
}
