using UnityEngine;

/// <summary>
/// This class controls the movement of the first person camera.
/// </summary>
public class MouseLook : MonoBehaviour
{
	// References.
	[SerializeField] private Transform player;
	[SerializeField] private Transform fpsCamPos;

	public static float mouseSensitivity { get; set; } = 100f;

	private float xRotation = 0f;
	private float yRotation = 0f;
	private bool isAlignedWithPlayer;
	
	private void Awake()
	{
		player = GameObject.FindWithTag("Player").transform;
		fpsCamPos = GameObject.FindWithTag("FPSCamPos").transform;
	}

	private void OnEnable()
	{
		transform.localRotation = Quaternion.LookRotation(player.forward, Vector3.up);
		xRotation = player.eulerAngles.x;
		yRotation = player.eulerAngles.y;
		
		isAlignedWithPlayer = true;
	}

	private void OnDisable()
	{
		isAlignedWithPlayer = false;
	}

	// Update is called once per frame
	private void Update()
	{
		if (!isAlignedWithPlayer)
			return;

		float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
		float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

		// Gameobject rotates counter clockwise along an axis if that axis rotation value is possitive.
		xRotation -= mouseY;
		xRotation = Mathf.Clamp(xRotation, -70f, 90f);  // Limit the angle of rotation.

		yRotation += mouseX;
		
		transform.position = fpsCamPos.position;

		if (PlayerActions.isAiming)
			transform.localRotation = Quaternion.LookRotation(fpsCamPos.forward, Vector3.up);
		else
			transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);

		player.Rotate(Vector3.up * mouseX);
	}
}
