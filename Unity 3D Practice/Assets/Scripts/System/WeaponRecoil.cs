using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class WeaponRecoil : MonoBehaviour
{
	[Header("References")]
	[Space]
	[SerializeField] private CinemachineImpulseSource cameraShake;
	[HideInInspector] public Animator rigAnimator;

	[Header("Recoil Values")]
	[Space]
	public Vector2 recoilForces;

	[Min(0f)]
	public float duration;

	// Private fields.
	private float _timeToRecoil;
	
	private CinemachineFreeLook _thirdPersonCam;
	private CinemachineVirtualCamera _firstPersonCam;

	private void Awake()
	{
		cameraShake = GetComponent<CinemachineImpulseSource>();

		_thirdPersonCam = CameraSwitcher.tpsCam;
		_firstPersonCam = CameraSwitcher.fpsCam;
	}

	private void Update()
	{
		float horizontalRecoil = recoilForces.x * Mathf.Sin(Time.time * 4f) / 10f;
		float verticalRecoil = recoilForces.y / 1000f;

		if (_timeToRecoil > 0f)
		{
			_thirdPersonCam.m_YAxis.Value -= (verticalRecoil * Time.deltaTime) / duration;
			_thirdPersonCam.m_XAxis.Value += (horizontalRecoil * Time.deltaTime) / duration;

			_timeToRecoil -= Time.deltaTime;
		}
	}

	public void GenerateRecoil()
	{
		_timeToRecoil = duration;

		cameraShake.GenerateImpulse(Camera.main.transform.forward);

		rigAnimator.Play($"Recoil {this.name}", 1, 0f);
	}
}
