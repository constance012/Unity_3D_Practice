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
	public static float mouseX { get; private set; }
	public static float mouseY { get; private set; }

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
		player.rotation = Quaternion.Euler(CameraSwitcher.tpsCam.m_XAxis.Value * Vector3.up);

		xRotation = player.eulerAngles.x;
		yRotation = player.eulerAngles.y;

		transform.LookAt(fpsCamPos.forward);

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

		mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
		mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

		// Gameobject rotates counter clockwise along an axis if that axis rotation value is possitive.
		xRotation -= mouseY;
		xRotation = Mathf.Clamp(xRotation, -70f, 70f);  // Limit the angle of rotation.

		yRotation += mouseX;
		
		transform.position = fpsCamPos.position;

		if (PlayerActions.isAiming)
		{
			Quaternion aimingRotation = Quaternion.Euler(fpsCamPos.eulerAngles.x, fpsCamPos.eulerAngles.y, 0f);
			transform.rotation = Quaternion.Slerp(transform.rotation, aimingRotation, 10f * Time.fixedDeltaTime);
		}
		else
			transform.rotation = Quaternion.Euler(xRotation, yRotation, 0f);

		player.Rotate(Vector3.up * mouseX);
	}
}
