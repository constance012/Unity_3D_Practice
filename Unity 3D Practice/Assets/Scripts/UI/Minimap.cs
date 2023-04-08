using TMPro;
using UnityEngine;

public class Minimap : MonoBehaviour
{
	[Header("References")]
	[Space]
	[SerializeField] private Camera minimapCam;
	[SerializeField] private Transform player;
	[SerializeField] private TextMeshProUGUI coordinateText;

	private void Awake()
	{
		minimapCam = GameObject.FindWithTag("MinimapCam").GetComponent<Camera>();
		player = GameObject.FindWithTag("Player").transform;
		coordinateText = transform.Find("Coordinate").GetComponent<TextMeshProUGUI>();
	}

	private void LateUpdate()
	{
		Vector3 playerPos = player.position;

		coordinateText.text = $"{Mathf.Round(playerPos.x)}, {Mathf.Round(playerPos.y)}, {Mathf.Round(playerPos.z)}";

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
