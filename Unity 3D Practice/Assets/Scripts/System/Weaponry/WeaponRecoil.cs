using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class WeaponRecoil : MonoBehaviour
{
	[Header("References")]
	[Space]
	[SerializeField] private CinemachineImpulseSource cameraShake;
	[SerializeField, ReadOnly] private AudioSource audioSource;

	[ReadOnly] public GameObject magazine;
	[ReadOnly] public Transform caseEjector;
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
		audioSource = GetComponent<AudioSource>();

		caseEjector = transform.Find("Case Ejector");
		magazine = transform.Find("Magazine").gameObject;

		_thirdPersonCam = CameraSwitcher.tpsCam;
		_firstPersonCam = CameraSwitcher.fpsCam;
	}

	private void Update()
	{
		ProcessRecoil();	
	}

	public void GenerateRecoil()
	{
		_timeToRecoil = duration;

		audioSource.Play();
		cameraShake.GenerateImpulse(Camera.main.transform.forward);

		rigAnimator.Play($"Recoil {this.name}", 3);
	}

	private void ProcessRecoil()
	{
		if (_timeToRecoil > 0f)
		{
			float horizontalRecoil = Random.Range(-recoilForces.x, recoilForces.x);
			float verticalRecoil = recoilForces.y / 1000f;

			_thirdPersonCam.m_YAxis.Value -= (verticalRecoil * Time.deltaTime) / duration;
			_thirdPersonCam.m_XAxis.Value += (horizontalRecoil * Time.deltaTime) / duration;

			_timeToRecoil -= Time.deltaTime;
		}
	}
}
