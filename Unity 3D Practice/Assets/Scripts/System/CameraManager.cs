using UnityEngine;
using Cinemachine;
using CSTGames.CommonEnums;

public class CameraManager : MonoBehaviour
{
	public static CameraManager instance { get; private set; }

	// References.
	[SerializeField] private CinemachineFreeLook thirdPersonCam;
	[SerializeField] private CinemachineVirtualCamera firstPersonCam;
	
	[SerializeField] private ThirdPersonMovement move3rdScript;
	[SerializeField] private FirstPersonMovement move1stScript;
	[SerializeField] private MouseLook cam1stLookScript;

	[SerializeField] private AnimStateController stateController;

	private void Awake()
	{
		if (instance == null)
			instance = this;
		else
		{
			Debug.LogWarning("More than 1 instance of Camera Manger found!!!");
			instance = null;
			return;
		}

		thirdPersonCam = GameObject.FindWithTag("ThirdPersonCam").GetComponent<CinemachineFreeLook>();
		firstPersonCam = GameObject.FindWithTag("FirstPersonCam").GetComponent<CinemachineVirtualCamera>();

		move3rdScript = GameObject.FindWithTag("Player").GetComponent<ThirdPersonMovement>();
		move1stScript = GameObject.FindWithTag("Player").GetComponent<FirstPersonMovement>();
		stateController = GameObject.FindWithTag("Player").GetComponent<AnimStateController>();

		cam1stLookScript = GameObject.FindWithTag("FirstPersonCam").GetComponent<MouseLook>();
	}

	private void OnEnable()
	{
		// Default cam is third person.
		thirdPersonCam.Priority = 20;
		firstPersonCam.Priority = 0;

		move3rdScript.enabled = true;

		move1stScript.enabled = false;
		cam1stLookScript.enabled = false;
	}

	// Update is called once per frame
	private void Update()
	{
		// Check for camera switching trigger key.
		if (InputManager.instance.GetKeyDown(KeybindingActions.SwitchCamera))
			SwitchCam();
	}

	private void SwitchCam()
	{
		// Switch to first person.
		if (thirdPersonCam.Priority != 0)
		{
			firstPersonCam.Priority = 20;
			thirdPersonCam.Priority = 0;

			move1stScript.enabled = true;
			cam1stLookScript.enabled = true;

			move3rdScript.enabled = false;

			stateController.playerMovement = stateController.gameObject.GetComponent<FirstPersonMovement>();
		}

		// Switch to third person.
		else if (firstPersonCam.Priority != 0)
		{
			thirdPersonCam.Priority = 20;
			firstPersonCam.Priority = 0;

			move3rdScript.enabled = true;

			move1stScript.enabled = false;
			cam1stLookScript.enabled = false;

			stateController.playerMovement = stateController.gameObject.GetComponent<ThirdPersonMovement>();
		}
	}
}
