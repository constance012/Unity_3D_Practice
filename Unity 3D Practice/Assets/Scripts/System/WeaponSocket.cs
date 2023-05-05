using UnityEngine;
using CSTGames.CommonEnums;
using TMPro;

public class WeaponSocket : MonoBehaviour
{
	[SerializeField] private GameObject droppedItemPrefab;

	private Animator reloadTextAnim;
	private TextMeshProUGUI ammoText;

	private Weapon inHandWeapon;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

	private float timeForNextUse;

	private void Awake()
	{
		reloadTextAnim = GameObject.FindWithTag("UICanvas").transform.Find("Gun UI/Reload Text").GetComponent<Animator>();
		ammoText = GameObject.FindWithTag("UICanvas").transform.Find("Gun UI/Ammo Text").GetComponent<TextMeshProUGUI>();

		meshFilter = GetComponent<MeshFilter>();
		meshRenderer = GetComponent<MeshRenderer>();
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

		meshFilter.mesh = inHandWeapon.mesh;
		meshRenderer.materials = inHandWeapon.materials;

		transform.localPosition = inHandWeapon.localPositionInHand;
		transform.localRotation = Quaternion.Euler(inHandWeapon.eulerAnglesInHand);
		transform.localScale = inHandWeapon.inHandScale * Vector3.one;

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

		if (InputManager.instance.GetKeyDown(KeybindingActions.Reload))
			StartCoroutine(weapon.Reload());

		// Use the weapon in hand only if the player is aiming.
		if (!PlayerActions.isAiming)
			return;

		switch (weapon.useType)
		{
			case Weapon.UseType.Automatic:
				if (InputManager.instance.GetKey(KeybindingActions.PrimaryAttack) && timeForNextUse <= 0f)
				{
					weapon.Use();
					timeForNextUse = weapon.useSpeed;  // Interval before the next use.
				}
				break;
			case Weapon.UseType.Burst:
				if (InputManager.instance.GetKeyDown(KeybindingActions.PrimaryAttack))
				{
					for (int i = 0; i < 3; i++)
						weapon.Use();
				}
				break;
			case Weapon.UseType.Single:
				if (InputManager.instance.GetKeyDown(KeybindingActions.PrimaryAttack))
				{
					weapon.Use();
				}
				break;
		}

		ammoText.text = $"{weapon.currentMagazineAmmo} / {weapon.remainingAmmo}";
	}

	private void ClearGraphics()
	{
		meshFilter.mesh = null;
		meshRenderer.materials = new Material[0];

		transform.localPosition = Vector3.zero;
		transform.eulerAngles = Vector3.zero;
		transform.localScale = Vector3.one;
	}
}
