using UnityEngine;

/// <summary>
/// This class controls the movement of the first person camera.
/// </summary>
public class FpsCamLook : MonoBehaviour
{
	// References.
	[SerializeField] private Transform player;
	[SerializeField] private Transform fpsCamPos;

	public static float MouseSensitivity { get; set; } = 100f;
	public static float MouseX { get; set; }
	public static float MouseY { get; set; }

	private float _xRotation = 0f;
	private float _yRotation = 0f;
	private bool _isAlignedWithPlayer;
	
	private void Awake()
	{
		player = GameObject.FindWithTag("Player").transform;
		fpsCamPos = GameObject.FindWithTag("FPSCamPos").transform;
	}

	private void OnEnable()
	{
		if (!CameraSwitcher.DoneInitializing)
			return;

		player.rotation = Quaternion.Euler(CameraSwitcher.TpsCam.m_XAxis.Value * Vector3.up);

		_xRotation = player.eulerAngles.x;
		_yRotation = player.eulerAngles.y;

		transform.LookAt(fpsCamPos.forward);

		_isAlignedWithPlayer = true;
	}

	private void OnDisable()
	{
		_isAlignedWithPlayer = false;
	}

	private void LateUpdate()
	{
		if (!_isAlignedWithPlayer)
			return;

		MouseX = Input.GetAxis("Mouse X") * MouseSensitivity * Time.deltaTime;
		MouseY = Input.GetAxis("Mouse Y") * MouseSensitivity * Time.deltaTime;

		// Gameobject rotates counter clockwise along an axis if that axis rotation value is possitive.
		_xRotation -= MouseY;

		if (WeaponAiming.IsAiming)
			_xRotation = Mathf.Clamp(_xRotation, -40f, 40f);  // Limit the angle of rotation.
		else
			_xRotation = Mathf.Clamp(_xRotation, -70f, 70f);  // Limit the angle of rotation.

		_yRotation += MouseX;
		
		transform.position = fpsCamPos.position;

		transform.rotation = Quaternion.Euler(_xRotation, _yRotation, 0f);

		player.Rotate(Vector3.up * MouseX);
	}
}
