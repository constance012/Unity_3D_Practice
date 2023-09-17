using UnityEngine;
using Cinemachine;
using CSTGames.CommonEnums;

public class CameraManager : Singleton<CameraManager>
{
	[Header("References")]
	[Space]
	[SerializeField] private CinemachineFreeLook thirdPersonCam;
	[SerializeField] private CinemachineVirtualCamera firstPersonCam;

	[HideInInspector] public Animator cam3rdAnimator;
	[HideInInspector] public Animator cam1stAnimator;

	// Private fields.
	private Transform _player;
	private ThirdPersonMovement _move3rdScript;
	private FirstPersonMovement _move1stScript;
	private FpsCamLook _cam1stLookScript;

	protected override void Awake()
	{
		base.Awake();

		thirdPersonCam = GameObjectExtensions.GetComponentWithTag<CinemachineFreeLook>("ThirdPersonCam");
		firstPersonCam = GameObjectExtensions.GetComponentWithTag<CinemachineVirtualCamera>("FirstPersonCam");

		cam3rdAnimator = thirdPersonCam.GetComponent<Animator>();
		cam1stAnimator = firstPersonCam.GetComponent<Animator>();

		_player = GameObject.FindWithTag("Player").transform;
		_move3rdScript = _player.GetComponent<ThirdPersonMovement>();
		_move1stScript = _player.GetComponent<FirstPersonMovement>();

		_cam1stLookScript = firstPersonCam.GetComponent<FpsCamLook>();
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
		if (InputManager.Instance.GetKeyDown(KeybindingActions.SwitchCamera))
			SwitchCamera();
	}

	public void ToggleMovementScript(CinemachineVirtualCameraBase activeCam, bool state)
	{
		if (activeCam == CameraSwitcher.TpsCam)
			_move3rdScript.enabled = state;
		else
			_move1stScript.enabled = state;
	}

	public void SetAimingProperties(bool IsAiming)
	{
		void SetRigProperties(CinemachineFreeLookOrbitRig rig, float height, float radius, Vector3 trackTargetOffset, Vector3 damping)
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

		cam3rdAnimator.SetBool(AnimationHandler.isAimingHash, IsAiming);

		if (IsAiming)
		{
			thirdPersonCam.m_SplineCurvature = 0f;
			SetRigProperties(CinemachineFreeLookOrbitRig.Top, 3f, 4.2f, new Vector3(0f, .95f, 0f), Vector3.zero);
			SetRigProperties(CinemachineFreeLookOrbitRig.Middle, 1.1f, 4f, new Vector3(0f, 1f, 0f), Vector3.zero);
			SetRigProperties(CinemachineFreeLookOrbitRig.Bottom, 0f, 1.2f, new Vector3(0f, 1.15f, 0f), Vector3.zero);
		}
		else
		{
			thirdPersonCam.m_SplineCurvature = .5f;
			SetRigProperties(CinemachineFreeLookOrbitRig.Top, 5f, .5f, new Vector3(0f, 0f, 0f), Vector3.one);
			SetRigProperties(CinemachineFreeLookOrbitRig.Middle, 1.8f, 4f, new Vector3(0f, 1f, 0f), Vector3.one);
			SetRigProperties(CinemachineFreeLookOrbitRig.Bottom, 0f, .5f, new Vector3(0f, 1f, 0f), Vector3.one);
		}
	}

	private void SwitchCamera()
	{
		if (CameraSwitcher.IsActive(CameraSwitcher.TpsCam))
			SwitchCamera(CameraPerspective.FirstPerson);
		else
			SwitchCamera(CameraPerspective.ThirdPerson);
	}

	private void SwitchCamera(CameraPerspective perspective)
	{
		if (perspective == CameraPerspective.ThirdPerson)
		{
			CameraSwitcher.SwitchCam(thirdPersonCam);

			_move3rdScript.enabled = true;
			_move1stScript.enabled = false;

			_cam1stLookScript.enabled = false;
		}
		else
		{
			CameraSwitcher.SwitchCam(firstPersonCam);

			_move3rdScript.enabled = false;
			_move1stScript.enabled = true;

			_cam1stLookScript.enabled = true;
		}
	}

	public enum CinemachineFreeLookOrbitRig { Top, Middle, Bottom }
	public enum CameraPerspective { ThirdPerson, FirstPerson }
}
