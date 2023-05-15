using UnityEngine;
using Cinemachine;
using CSTGames.CommonEnums;
using Unity.IO.LowLevel.Unsafe;
using System.Reflection;

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

		if (PlayerActions.isAiming != wasAiming)
		{
			if (PlayerActions.allowAimingAgain)
				SetAimingProperties(true);
			else
				SetAimingProperties(false);
		}

		SwitchCamera();
	}

	private void SetAimingProperties(bool isAiming)
	{
		void SetRigProperties(CinemachineOrbitRig rig, float height, float radius, Vector3 offset)
		{
			thirdPersonCam.m_Orbits[(int)rig].m_Height = height;
			thirdPersonCam.m_Orbits[(int)rig].m_Radius = radius;

			CinemachineComposer comp = thirdPersonCam.GetRig((int)rig).GetCinemachineComponent<CinemachineComposer>();
			comp.m_TrackedObjectOffset = offset;
		}

		if (isAiming)
		{
			firstPersonCam.m_Lens.NearClipPlane = .01f;

			thirdPersonCam.m_SplineCurvature = 0f;
			SetRigProperties(CinemachineOrbitRig.Top, 3f, 4.2f, new Vector3(0f, .95f, 0f));
			SetRigProperties(CinemachineOrbitRig.Middle, 1.1f, 4f, new Vector3(0f, 1f, 0f));
			SetRigProperties(CinemachineOrbitRig.Bottom, 0f, 1.2f, new Vector3(0f, 1.15f, 0f));

			cam3rdAnimator.SetBool(isAimingHash, true);
		}
		else
		{
			firstPersonCam.m_Lens.NearClipPlane = .2f;

			thirdPersonCam.m_SplineCurvature = .5f;
			SetRigProperties(CinemachineOrbitRig.Top, 5f, .5f, new Vector3(0f, 0f, 0f));
			SetRigProperties(CinemachineOrbitRig.Middle, 1.8f, 4f, new Vector3(0f, 1f, 0f));
			SetRigProperties(CinemachineOrbitRig.Bottom, 0f, .5f, new Vector3(0f, 1f, 0f));

			cam3rdAnimator.SetBool(isAimingHash, false);
		}
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

	public enum CinemachineOrbitRig { Top, Middle, Bottom }
}
