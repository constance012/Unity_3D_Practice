using UnityEngine;
using Cinemachine;

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
	private float timeToRecoil;
	
	private CinemachineFreeLook thirdPersonCam;
	private CinemachineVirtualCamera firstPersonCam;

	private void Awake()
	{
		cameraShake = GetComponent<CinemachineImpulseSource>();

		thirdPersonCam = CameraSwitcher.tpsCam;
		firstPersonCam = CameraSwitcher.fpsCam;
	}

	private void Update()
	{
		float horizontalRecoil = recoilForces.x * Mathf.Sin(Time.time * 4f) / 10f;
		float verticalRecoil = recoilForces.y / 1000f;

		if (timeToRecoil > 0f)
		{
			thirdPersonCam.m_YAxis.Value -= (verticalRecoil * Time.deltaTime) / duration;
			thirdPersonCam.m_XAxis.Value += (horizontalRecoil * Time.deltaTime) / duration;
			timeToRecoil -= Time.deltaTime;
		}
	}

	public void GenerateRecoil()
	{
		timeToRecoil = duration;

		cameraShake.GenerateImpulse(Camera.main.transform.forward);

		rigAnimator.Play($"Recoil {this.name}", 1, 0f);
	}
}
