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
		CameraSwitcher.Register(firstPersonCam);
		CameraSwitcher.Register(thirdPersonCam);

		CameraSwitcher.SwitchCam(thirdPersonCam);

		move3rdScript.enabled = true;

		move1stScript.enabled = false;
		cam1stLookScript.enabled = false;
	}

	private void OnDestroy()
	{
		CameraSwitcher.Unregister(firstPersonCam);
		CameraSwitcher.Unregister(thirdPersonCam);
	}

	// Update is called once per frame
	private void Update()
	{
		// Check for camera switching trigger key.
		if (InputManager.instance.GetKeyDown(KeybindingActions.SwitchCamera))
		{
			if (CameraSwitcher.IsActive(thirdPersonCam))
			{
				CameraSwitcher.SwitchCam(firstPersonCam);

				move1stScript.enabled = true;
				cam1stLookScript.enabled = true;

				move3rdScript.enabled = false;
			}
			else
			{
				CameraSwitcher.SwitchCam(thirdPersonCam);

				move3rdScript.enabled = true;

				move1stScript.enabled = false;
				cam1stLookScript.enabled = false;
			}
		}
	}
}
