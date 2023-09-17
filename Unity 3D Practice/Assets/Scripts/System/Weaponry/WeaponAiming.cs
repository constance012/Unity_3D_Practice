using CSTGames.CommonEnums;
using UnityEngine;

public class WeaponAiming : MonoBehaviour
{
	[Header("Debugging"), Space]
	[SerializeField] private bool holdToAim;

	public static bool ForcedAiming { get; set; }
	public static bool IsAiming { get; private set; }

	// Private fields.
	private GameObject _crosshair;
	private Animator _rigAnimator;
	private bool _isFocusing;

	private void Update()
	{
		HandleAiming();

		if (!IsAiming)
			return;

		// Stop aiming when unarmed or is switching weapons.
		if (PlayerActions.CurrentActiveWeapon == null || PlayerActions.IsSwitchingWeapon)
		{
			SetAimingBehaviour(false, false);

			return;
		}

	}

	private void Awake()
	{
		_rigAnimator = transform.parent.GetComponent<Animator>();
		_crosshair = GameObjectExtensions.FindChildTransformWithTag("UICanvas", "Gun UI/Crosshair").gameObject;
	}

	#region Weapon Aiming.
	public void ForceStopAiming()
	{
		SetAimingBehaviour(false, IsAiming);
	}

	private void SetAimingBehaviour(bool state, bool setTrigger = true)
	{		
		if (state)
		{
			_rigAnimator.Play($"Start Aiming {PlayerActions.CurrentActiveWeapon.itemName}", 1);

			StartCoroutine(AnimationHandler.ChangeRigLayerWeight("weapon aiming ik", 1f, .7f));
		}
		else
		{
			if (setTrigger)
				_rigAnimator.SetTrigger(AnimationHandler.endAimingHash);

			StartCoroutine(AnimationHandler.ChangeRigLayerWeight("weapon aiming ik", 0f, .7f));
			
			_isFocusing = false;
			_rigAnimator.Play("Neutral", 2);
			_rigAnimator.ResetTrigger(AnimationHandler.endFocusHash);
		}

		CameraManager.Instance.SetAimingProperties(state);
		_crosshair.SetActive(state);
		IsAiming = state;
	}

	private void SetFocusingBehaviour(bool state)
	{
		if (state)
		{
			_rigAnimator.Play($"Start Focus {PlayerActions.CurrentActiveWeapon.itemName}", 2);
		}
		else
		{
			_rigAnimator.SetTrigger(AnimationHandler.endFocusHash);
		}

		CameraManager.Instance.cam1stAnimator.SetBool(AnimationHandler.isFocusingHash, state);
		//_crosshair.SetActive(!state);
		_isFocusing = state;
	}

	private void CheckForAimingInput()
	{
		if (PlayerActions.IsSwitchingWeapon)
			return;

		if (ForcedAiming)
		{
			IsAiming = true;
			_isFocusing = false;
			return;
		}

		if (holdToAim)
		{
			IsAiming = false;

			// Check aiming.
			if (Input.GetKey(KeyCode.Mouse1))
				IsAiming = true;
		}
		else if (Input.GetKeyDown(KeyCode.Mouse1))
			IsAiming = !IsAiming;

		if (IsAiming)
		{
			if (Input.mouseScrollDelta.y > 0f)
				_isFocusing = true;
			else if (Input.mouseScrollDelta.y < 0f)
				_isFocusing = false;
		}
	}

	private void HandleAiming()
	{
		bool wasAiming = IsAiming;
		bool wasFocusing = _isFocusing;

		CheckForAimingInput();

		// What to change when starts of stops aiming.
		if (wasAiming != IsAiming)
		{
			StopAllCoroutines();

			SetAimingBehaviour(IsAiming);
		}

		if (IsAiming && wasFocusing != _isFocusing)
		{
			SetFocusingBehaviour(_isFocusing);
		}
	}
	#endregion
}
