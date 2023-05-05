using UnityEngine;
using Cinemachine;
using CSTGames.CommonEnums;
using Unity.IO.LowLevel.Unsafe;

public class CameraManager : MonoBehaviour
{
	public static CameraManager instance { get; private set; }

	[Header("References")]
	[Space]
	[SerializeField] private CinemachineFreeLook thirdPersonCam;
	[SerializeField] private CinemachineVirtualCamera firstPersonCam;

	private Transform player;
	private Animator cam3rdAnimator;

	// Scripts
	private ThirdPersonMovement move3rdScript;
	private FirstPersonMovement move1stScript;
	private MouseLook cam1stLookScript;

	private int isAimingHash;

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

		cam3rdAnimator = thirdPersonCam.GetComponent<Animator>();

		player = GameObject.FindWithTag("Player").transform;
		move3rdScript = player.GetComponent<ThirdPersonMovement>();
		move1stScript = player.GetComponent<FirstPersonMovement>();

		cam1stLookScript = firstPersonCam.GetComponent<MouseLook>();
	}

	private void Start()
	{
		isAimingHash = Animator.StringToHash("IsAiming");
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
		bool wasAiming = cam3rdAnimator.GetBool(isAimingHash);

		if (PlayerActions.isAiming && PlayerActions.allowAimingAgain && !wasAiming)
		{
			firstPersonCam.m_Lens.NearClipPlane = .01f;
			cam3rdAnimator.SetBool(isAimingHash, true);
		}
		else if (!PlayerActions.isAiming && wasAiming)
		{
			firstPersonCam.m_Lens.NearClipPlane = .2f;
			cam3rdAnimator.SetBool(isAimingHash, false);
		}

		SwitchCamera();
	}

	private void SwitchCamera()
	{
		// Check for camera switching trigger key.
		if (InputManager.instance.GetKeyDown(KeybindingActions.SwitchCamera))
		{
			if (CameraSwitcher.IsActive(CameraSwitcher.tpsCam))
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
