using UnityEngine;

/// <summary>
/// Creates a billboard effect for UI elements in world space.
/// </summary>
public class Billboard : MonoBehaviour
{
	private Transform _mainCam;

	private void Awake()
	{
		_mainCam = Camera.main.transform;
	}

	private void LateUpdate()
	{
		transform.LookAt(transform.position + _mainCam.forward);
	}
}
