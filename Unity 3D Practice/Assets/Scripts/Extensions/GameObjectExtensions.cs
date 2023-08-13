using UnityEngine;

public static class GameObjectExtensions
{
	public static Vector3 WorldPosition(this GameObject gameObject) => gameObject.transform.position;
	public static Quaternion WorldRotation(this GameObject gameObject) => gameObject.transform.rotation;

	public static TComponent GetComponentWithTag<TComponent>(string tag) where TComponent : Component
	{
		GameObject gameObject = GameObject.FindWithTag(tag);
		return gameObject.GetComponent<TComponent>();
	}

	public static TComponent GetComponentWithTag<TComponent>(string tag, string childName) where TComponent : Component
	{
		GameObject gameObject = GameObject.FindWithTag(tag);
		Transform child = gameObject.transform.Find(childName);

		return child.GetComponent<TComponent>();
	}

	public static Transform FindChildTransformWithTag(string tag, string childName)
	{
		GameObject gameObject = GameObject.FindWithTag(tag);
		Transform child = gameObject.transform.Find(childName);

		return child;
	}

	public static void DisablePlayer(this GameObject player)
	{
		player.TryGetComponent(out PlayerActions actionScript);
		if (actionScript == null)
			return;

		Transform placeholderSphere = player.transform.Find("Placeholder Sphere");
		Transform model = player.transform.Find("Model");

		CameraManager.Instance.ToggleMovementScript(CameraSwitcher.activeCam, false);
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

		CameraManager.Instance.ToggleMovementScript(CameraSwitcher.activeCam, true);
		playerAnimator.Rebind();

		actionScript.enabled = true;
		actionScript.OnWeaponPickup(true);

		placeholderSphere.gameObject.SetActive(false);
		model.gameObject.SetActive(true);
	}
}
