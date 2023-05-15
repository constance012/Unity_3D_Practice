using UnityEngine;
using CSTGames.CommonEnums;
using TMPro;
using System.Collections;

public class WeaponSocket : MonoBehaviour
{
	[SerializeField] private GameObject droppedItemPrefab;

	private Animator reloadTextAnim;
	private TextMeshProUGUI ammoText;

	private Weapon inHandWeapon;

	private Transform weaponPivot;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

	private Transform rightHandGrip;
	private Transform rightElbowHint;

	private Transform leftHandGrip;
	private Transform leftElbowHint;

	private ParticleSystem muzzleFlash;
	private ParticleSystem hitEffect;

	private float timeForNextUse;
	private bool burstCompleted = true;

	private void Awake()
	{
		reloadTextAnim = GameObject.FindWithTag("UICanvas").transform.Find("Gun UI/Reload Text").GetComponent<Animator>();
		ammoText = GameObject.FindWithTag("UICanvas").transform.Find("Gun UI/Ammo Text").GetComponent<TextMeshProUGUI>();

		weaponPivot = transform.Find("Weapon Pivot");
		meshFilter = weaponPivot.Find("Weapon").GetComponent<MeshFilter>();
		meshRenderer = meshFilter.GetComponent<MeshRenderer>();

		rightHandGrip = weaponPivot.Find("Right Hand Grip");
		rightElbowHint = rightHandGrip.Find("Right Elbow Hint");

		leftHandGrip = weaponPivot.Find("Left Hand Grip");
		leftElbowHint = leftHandGrip.Find("Left Elbow Hint");

		muzzleFlash = weaponPivot.Find("Muzzle Flash").GetComponent<ParticleSystem>();
		hitEffect = transform.Find("Effects/Hit Effect").GetComponent<ParticleSystem>();
	}

	private void Start()
	{
		reloadTextAnim.GetComponent<TextMeshProUGUI>().text = $"Press {InputManager.instance.GetKeyForAction(KeybindingActions.Reload)} to reload.";

		ammoText.gameObject.SetActive(inHandWeapon != null);
	}

	public void Update()
	{
		if (inHandWeapon == null)
			return;

		timeForNextUse -= Time.deltaTime;

		// Drop the weapon in hand.
		if (InputManager.instance.GetKeyDown(KeybindingActions.DropItemInHand))
			DropWeapon();

		if (inHandWeapon.weaponType == Weapon.WeaponType.Ranged)
			UseRangedWeapon();
	}

	public void GrabWeapon(Weapon weapon)
    {
		inHandWeapon = weapon;

		// Set the mesh.
		meshFilter.mesh = inHandWeapon.mesh;
		meshRenderer.materials = inHandWeapon.materials;

		// Set the hands' grip references.
		rightHandGrip.localPosition = inHandWeapon.rightHandGrip.localPosition;
		rightHandGrip.localRotation = Quaternion.Euler(inHandWeapon.rightHandGrip.localEulerAngles);
		rightElbowHint.localPosition = inHandWeapon.rightHandGrip.elbowLocalPosition;

		leftHandGrip.localPosition = inHandWeapon.leftHandGrip.localPosition;
		leftHandGrip.localRotation = Quaternion.Euler(inHandWeapon.leftHandGrip.localEulerAngles);
		leftElbowHint.localPosition = inHandWeapon.leftHandGrip.elbowLocalPosition;

		// Set the scale.
		weaponPivot.localScale = inHandWeapon.inHandScale * Vector3.one;

		// Set the muzzle flash position.
		muzzleFlash.transform.localPosition = inHandWeapon.particlesLocalPosisiton;
		muzzleFlash.transform.rotation = Quaternion.LookRotation(weaponPivot.right, muzzleFlash.transform.up);

		ammoText.gameObject.SetActive(true);
    }

	public void HideWeapon()
	{
		ClearGraphics();
		inHandWeapon = null;
		ammoText.gameObject.SetActive(false);
	}

    public void DropWeapon()
    {
		ClearGraphics();

		droppedItemPrefab.GetComponent<ItemPickup>().itemPrefab = inHandWeapon;
		Instantiate(droppedItemPrefab);
		droppedItemPrefab.GetComponent<Rigidbody>().AddForce(transform.root.forward * 10f, ForceMode.Impulse);

		inHandWeapon = null;
		ammoText.gameObject.SetActive(false);
	}

	private void UseRangedWeapon()
	{
		RangedWeapon weapon = inHandWeapon as RangedWeapon;

		if (weapon.promptReload && !reloadTextAnim.GetBool("Slide In"))
			reloadTextAnim.SetBool("Slide In", true);
		else if (!weapon.promptReload && reloadTextAnim.GetBool("Slide In"))
			reloadTextAnim.SetBool("Slide In", false);

		if (InputManager.instance.GetKeyDown(KeybindingActions.Reload) && !weapon.isReloading)
			StartCoroutine(Reload(weapon));

		// Use the weapon in hand only if the player is aiming.
		if (!PlayerActions.isAiming)
			return;
		
		switch (weapon.useType)
		{
			case Weapon.UseType.Automatic:
				if (InputManager.instance.GetKey(KeybindingActions.PrimaryAttack) && timeForNextUse <= 0f)
				{
					Shoot(weapon);
					timeForNextUse = weapon.useSpeed;  // Interval before the next use.
				}
				break;
			
			case Weapon.UseType.Burst:
				if (InputManager.instance.GetKeyDown(KeybindingActions.PrimaryAttack) && burstCompleted)
					StartCoroutine(BurstFire(weapon));
				break;
			
			case Weapon.UseType.Single:
				if (InputManager.instance.GetKeyDown(KeybindingActions.PrimaryAttack))
					Shoot(weapon);
				break;
		}
	}

	private void ClearGraphics()
	{
		meshFilter.mesh = null;
		meshRenderer.materials = new Material[0];

		rightHandGrip.localPosition = Vector3.zero;
		rightHandGrip.eulerAngles = Vector3.zero;
		rightElbowHint.localPosition = Vector3.zero;

		leftHandGrip.localPosition = Vector3.zero;
		leftHandGrip.eulerAngles = Vector3.zero;
		leftElbowHint.localPosition = Vector3.zero;

		weaponPivot.localScale = Vector3.one;

		muzzleFlash.transform.localPosition = Vector3.zero;
		muzzleFlash.transform.rotation = Quaternion.identity;
	}

	private void Shoot(RangedWeapon weapon)
	{
		RaycastHit hitInfo;

		if (weapon.Fire(out hitInfo))
		{
			muzzleFlash.Emit(1);

			if (hitInfo.point != Vector3.zero)
			{
				hitEffect.transform.position = hitInfo.point;
				hitEffect.transform.forward = hitInfo.normal;
				hitEffect.Emit(1);
			}

			ammoText.text = $"{weapon.currentMagazineAmmo} / {weapon.remainingAmmo}";
		}
	}

	private IEnumerator BurstFire(RangedWeapon weapon)
	{
		burstCompleted = false;

		for (int i = 0; i < 3; i++)
		{
			yield return new WaitForSeconds(.1f);

			Shoot(weapon);
		}

		burstCompleted = true;
	}

	private IEnumerator Reload(RangedWeapon weapon)
	{
		weapon.promptReload = false;
		weapon.isReloading = true;

		if (weapon.remainingAmmo == 0)
		{
			weapon.isReloading = false;
			yield break;
		}

		yield return new WaitForSeconds(weapon.reloadTime);

		weapon.Reload();
		ammoText.text = $"{weapon.currentMagazineAmmo} / {weapon.remainingAmmo}";
	}
}
