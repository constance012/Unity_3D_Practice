using TMPro;
using UnityEngine;
using Cinemachine;

public class Minimap : MonoBehaviour
{
	[Header("References")]
	[Space]
	[SerializeField] private Camera minimapCam;
	[SerializeField] private Transform player;
	[SerializeField] private Transform lookingArrow;
	[SerializeField] private TextMeshProUGUI coordinateText;

	private CinemachineFreeLook tpsCam;

	private void Awake()
	{
		minimapCam = GameObject.FindWithTag("MinimapCam").GetComponent<Camera>();
		player = GameObject.FindWithTag("Player").transform;
		lookingArrow = transform.Find("Player Icon");
		coordinateText = transform.Find("Coordinate Text").GetComponent<TextMeshProUGUI>();
		tpsCam = GameObject.FindWithTag("ThirdPersonCam").GetComponent<CinemachineFreeLook>();
	}

	private void LateUpdate()
	{
		Vector3 playerPos = player.position;

		coordinateText.text = $"{Mathf.Round(playerPos.x)}, {Mathf.Round(playerPos.y)}, {Mathf.Round(playerPos.z)}";

		if (CameraSwitcher.IsActive(CameraSwitcher.tpsCam))
			lookingArrow.rotation = Quaternion.Euler(new Vector3(0f, 0f, -tpsCam.m_XAxis.Value));
		else
			lookingArrow.rotation = Quaternion.Euler(new Vector3(0f, 0f, -player.eulerAngles.y));

		playerPos.y = minimapCam.transform.position.y;
		
		minimapCam.transform.position = playerPos;
	}

	public void ZoomIn()
	{
		if (minimapCam.orthographicSize <= 5f)
			return;
		
		minimapCam.orthographicSize -= 2f;
	}

	public void ZoomOut()
	{
		if (minimapCam.orthographicSize >= 20f)
			return;

		minimapCam.orthographicSize += 2f;
	}
}
