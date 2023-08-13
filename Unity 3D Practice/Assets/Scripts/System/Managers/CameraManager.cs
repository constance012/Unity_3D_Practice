using UnityEngine;
using Cinemachine;
using CSTGames.CommonEnums;

public class CameraManager : Singleton<CameraManager>
{
	[Header("References")]
	[Space]
	[SerializeField] private CinemachineFreeLook thirdPersonCam;
	[SerializeField] private CinemachineVirtualCamera firstPersonCam;

	private Transform player;
	private Animator cam3rdAnimator;

	// Scripts
	private ThirdPersonMovement move3rdScript;
	private FirstPersonMovement move1stScript;
	private FpsCamLook cam1stLookScript;

	protected override void Awake()
	{
		base.Awake();

		thirdPersonCam = GameObjectExtensions.GetComponentWithTag<CinemachineFreeLook>("ThirdPersonCam");
		firstPersonCam = GameObjectExtensions.GetComponentWithTag<CinemachineVirtualCamera>("FirstPersonCam");

		cam3rdAnimator = thirdPersonCam.GetComponent<Animator>();

		player = GameObject.FindWithTag("Player").transform;
		move3rdScript = player.GetComponent<ThirdPersonMovement>();
		move1stScript = player.GetComponent<FirstPersonMovement>();

		cam1stLookScript = firstPersonCam.GetComponent<FpsCamLook>();
	}

	private void OnEnable()
	{
		CameraSwitcher.Register(firstPersonCam);
		CameraSwitcher.Register(thirdPersonCam);

		SwitchCamera(CameraPerspective.ThirdPerson);
	}

	private void OnDestroy()
	{
		CameraSwitcher.Unregister(firstPersonCam);
		CameraSwitcher.Unregister(thirdPersonCam);
	}

	// Update is called once per frame
	private void LateUpdate()
	{
		bool wasAiming = cam3rdAnimator.GetBool(AnimationHandler.IsAimingHash);

		if (PlayerActions.IsAiming != wasAiming)
			SetAimingProperties(PlayerActions.IsAiming);

		if (InputManager.Instance.GetKeyDown(KeybindingActions.SwitchCamera))
			SwitchCamera();
	}

	public void ToggleMovementScript(CinemachineVirtualCameraBase activeCam, bool state)
	{
		if (activeCam == CameraSwitcher.tpsCam)
			move3rdScript.enabled = state;
		else
			move1stScript.enabled = state;
	}

	private void SetAimingProperties(bool IsAiming)
	{
		void SetRigProperties(CinemachineOrbitRig rig, float height, float radius, Vector3 trackTargetOffset, Vector3 damping)
		{
			thirdPersonCam.m_Orbits[(int)rig].m_Height = height;
			thirdPersonCam.m_Orbits[(int)rig].m_Radius = radius;

			CinemachineComposer comp = thirdPersonCam.GetRig((int)rig).GetCinemachineComponent<CinemachineComposer>();
			comp.m_TrackedObjectOffset = trackTargetOffset;

			CinemachineOrbitalTransposer trans = thirdPersonCam.GetRig((int)rig).GetCinemachineComponent<CinemachineOrbitalTransposer>();
			trans.m_XDamping = damping.x;
			trans.m_YDamping = damping.y;
			trans.m_ZDamping = damping.z;
		}

		if (IsAiming)
		{
			thirdPersonCam.m_SplineCurvature = 0f;
			SetRigProperties(CinemachineOrbitRig.Top, 3f, 4.2f, new Vector3(0f, .95f, 0f), Vector3.zero);
			SetRigProperties(CinemachineOrbitRig.Middle, 1.1f, 4f, new Vector3(0f, 1f, 0f), Vector3.zero);
			SetRigProperties(CinemachineOrbitRig.Bottom, 0f, 1.2f, new Vector3(0f, 1.15f, 0f), Vector3.zero);

			cam3rdAnimator.SetBool(AnimationHandler.IsAimingHash, true);
		}
		else
		{
			thirdPersonCam.m_SplineCurvature = .5f;
			SetRigProperties(CinemachineOrbitRig.Top, 5f, .5f, new Vector3(0f, 0f, 0f), Vector3.one);
			SetRigProperties(CinemachineOrbitRig.Middle, 1.8f, 4f, new Vector3(0f, 1f, 0f), Vector3.one);
			SetRigProperties(CinemachineOrbitRig.Bottom, 0f, .5f, new Vector3(0f, 1f, 0f), Vector3.one);

			cam3rdAnimator.SetBool(AnimationHandler.IsAimingHash, false);
		}
	}

	private void SwitchCamera()
	{
		if (CameraSwitcher.IsActive(CameraSwitcher.tpsCam))
			SwitchCamera(CameraPerspective.FirstPerson);
		else
			SwitchCamera(CameraPerspective.ThirdPerson);
	}

	private void SwitchCamera(CameraPerspective perspective)
	{
		if (perspective == CameraPerspective.ThirdPerson)
		{
			CameraSwitcher.SwitchCam(thirdPersonCam);

			move3rdScript.enabled = true;
			move1stScript.enabled = false;

			cam1stLookScript.enabled = false;
		}
		else
		{
			CameraSwitcher.SwitchCam(firstPersonCam);

			move3rdScript.enabled = false;
			move1stScript.enabled = true;

			cam1stLookScript.enabled = true;
		}
	}

	public enum CinemachineOrbitRig { Top, Middle, Bottom }
	public enum CameraPerspective { ThirdPerson, FirstPerson }
}
