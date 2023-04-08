using UnityEngine;

public class MouseLook : MonoBehaviour
{
	// References.
	[SerializeField] private Transform player;

	// Fields.
	public float mouseSensitivity = 150f;

	private float xRotation = 0f;
	//private test t = test.first;
	//private Rigidbody rb;
	
	private void Awake()
	{
		player = GameObject.FindWithTag("Player").GetComponent<Transform>();
	}

	// Start is called before the first frame update
	private void Start()
	{
		Cursor.lockState = CursorLockMode.Locked;
	}

	// Update is called once per frame
	private void Update()
	{
		float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
		float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

		xRotation -= mouseY;  // Decreasing because it'll inverse if increasing.
		xRotation = Mathf.Clamp(xRotation, -70f, 90f);  // Limit the angle of rotation.

		transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
		player.Rotate(Vector3.up * mouseX);
	}
}

public enum test
{
	first,
	second
}
