using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class FirearmMono : MonoBehaviour
{
	[Header("References")]
	[Space]
	[ReadOnly] public GameObject magazine;
	[ReadOnly] public Transform caseEjector;
	[HideInInspector] public Animator rigAnimator;
	[HideInInspector] public Animator firearmAnimator;

	[Header("Recoil")]
	[Space]
	[SerializeField] private CinemachineImpulseSource cameraShake;
	public Vector2 recoilForces;
	[Min(0f)]
	public float duration;

	[Header("Audio")]
	[Space]
	[SerializeField, Range(-3f, 3f)] private float pitch;

	// Private fields.
	private CinemachineFreeLook _thirdPersonCam;
	private CinemachineVirtualCamera _firstPersonCam;

	private float _timeToRecoil;

	private void Awake()
	{
		cameraShake = GetComponent<CinemachineImpulseSource>();
		firearmAnimator = GetComponent<Animator>();

		caseEjector = transform.FindAny("Case Ejector");
		magazine = transform.Find("Magazine").gameObject;

		_thirdPersonCam = CameraSwitcher.TpsCam;
		_firstPersonCam = CameraSwitcher.FpsCam;
	}

	private void Update()
	{
		ProcessRecoil();	
	}

	public void GenerateRecoil(string weaponName)
	{
		_timeToRecoil = duration;

		firearmAnimator.TryPlay("Fire", 0, 0f);
		
		AudioManager.Instance.Play("Gun Shot", 0, pitch);
		cameraShake.GenerateImpulse(Camera.main.transform.forward);

		rigAnimator.Play($"Recoil {weaponName}", 4, 0f);
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
