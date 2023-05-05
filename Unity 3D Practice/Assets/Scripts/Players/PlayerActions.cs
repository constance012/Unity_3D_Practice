using Cinemachine;
using System.Linq;
using UnityEngine;

public class PlayerActions : MonoBehaviour
{
	[Header("References")]
	[Space]
	[SerializeField] private Animator animator;
	[SerializeField] private GameObject crosshair;
	[SerializeField] private Transform fpsCamPos;

	public static Weapon[] weapons = new Weapon[4];

	[HideInInspector] public WeaponSocket weaponSocket;
	private Weapon currentWeapon;

	// Animator's hashes.
	private int aimingVertical, grabRifle, hideRifle, startAiming, stopAiming;

	// Properties.
	public static bool isAiming { get; private set; }
	public static bool allowAimingAgain { get; set; } = true;

	private readonly Vector3 fpsAimingLocalPos = new Vector3(0.00469999993f, -0.0322999991f, 0.0869999975f);

	private void Awake()
	{
		animator = GetComponent<Animator>();
		weaponSocket = GameObject.FindWithTag("WeaponSocket").GetComponent<WeaponSocket>();

		crosshair = GameObject.FindWithTag("UICanvas").transform.Find("Gun UI/Crosshair").gameObject;
		
		fpsCamPos = GameObject.FindWithTag("FPSCamPos").transform;
	}

	private void Start()
	{
		aimingVertical = Animator.StringToHash("Aiming Vertical");
		grabRifle = Animator.StringToHash("Pull Out Rifle");
		hideRifle = Animator.StringToHash("Put Away Rifle");
		startAiming = Animator.StringToHash("Start Aiming");
		stopAiming = Animator.StringToHash("End Aiming");
	}

	private void Update()
	{
		SelectWeapon();

		if (currentWeapon == null)
			return;

		HandleAiming();
	}

	private void SelectWeapon()
	{
		// If the player has no weapon, then returns.
		if (weapons.All(weapon => weapon == null))
			return;

		bool isWeaponHeld = currentWeapon != null;

		if (Input.GetKey(KeyCode.Alpha1) && isWeaponHeld)
		{
			currentWeapon = null;

			animator.SetTrigger(hideRifle);
		}

		if (Input.GetKey(KeyCode.Alpha2) && !isWeaponHeld)
		{
			currentWeapon = weapons[0];
			weaponSocket.GrabWeapon(currentWeapon);

			animator.SetLayerWeight(1, 1f);
			animator.SetTrigger(grabRifle);
		}
	}

	private void HandleAiming()
	{
		bool wasAiming = isAiming;

		isAiming = false;

		if (Input.GetKey(KeyCode.Mouse1) && allowAimingAgain)
		{
			isAiming = true;
			fpsCamPos.rotation = Quaternion.LookRotation(weaponSocket.transform.right, fpsCamPos.up);

			float aimingY = animator.GetFloat(aimingVertical);
			float mouseY = Input.GetAxis("Mouse Y") * .0075f * 10f;

			aimingY += mouseY;
			aimingY = Mathf.Clamp(aimingY, -1f, 1f);

			animator.SetFloat(aimingVertical, aimingY);
		}

		int layerIndex = animator.GetLayerIndex("Rifle Aiming");
		
		// What to change when starts of stops aiming.
		if (wasAiming != isAiming)
		{
			if (isAiming)
			{
				if (!CameraSwitcher.IsActive(CameraSwitcher.fpsCam))
					crosshair.SetActive(true);
				
				animator.SetLayerWeight(layerIndex, 1f);
				animator.SetTrigger(startAiming);

				fpsCamPos.localPosition = fpsAimingLocalPos;
			}
			else
			{
				crosshair.SetActive(false);

				animator.SetTrigger(stopAiming);
				animator.SetFloat(aimingVertical, 0f);

				fpsCamPos.localPosition = new Vector3(0f, 0.065200001f, 0.194900006f);
				fpsCamPos.localRotation = Quaternion.identity;
			}
		}
	}
}
