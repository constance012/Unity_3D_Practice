using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using CSTGames.Utility;

public class AnimationHandler : MonoBehaviour
{
	[SerializeField] private Animator animator;

	// Animators' parameter hashes.
	public static int velXHash, velZHash;
	public static int speedHash;
	public static int useStrafeMovementHash;
	public static int isJumpingHash;

	public static int holsterWeaponHash;
	public static int isAimingHash;
	public static int endAimingHash;

	public static int isFocusingHash;
	public static int endFocusHash;

	public static int startReloadingHash;
	public static int endReloadingHash;
	public static bool IsChangingRigWeight { get; private set; }

	// Private fields.
	private static RigBuilder _rigBuilder;
	private Transform _cameraLookTarget;
	private float _headLookOvershoot = 1f;

	private void Awake()
	{
		animator = GetComponent<Animator>();
		_rigBuilder = GetComponent<RigBuilder>();

		_cameraLookTarget = Camera.main.transform.Find("Camera Look Target");
	}

	private void Start()
	{
		// Player's animator.
		velXHash = Animator.StringToHash("Velocity X");
		velZHash = Animator.StringToHash("Velocity Z");
		speedHash = Animator.StringToHash("Speed");
		useStrafeMovementHash = Animator.StringToHash("UseStrafeMovement");
		isJumpingHash = Animator.StringToHash("IsJumping");

		// Rig layers animator.
		holsterWeaponHash = Animator.StringToHash("HolsterWeapon");
		isAimingHash = Animator.StringToHash("IsAiming");
		endAimingHash = Animator.StringToHash("EndAiming");

		isFocusingHash = Animator.StringToHash("IsFocusing");
		endFocusHash = Animator.StringToHash("EndFocus");

		startReloadingHash = Animator.StringToHash("StartReloading");
		endReloadingHash = Animator.StringToHash("EndReloading");
	}

	private void Update()
	{
		animator.SetFloat(speedHash, PlayerMovement.LinearVelocity);
		animator.SetFloat(velXHash, PlayerMovement.VelocityX);
		animator.SetFloat(velZHash, PlayerMovement.VelocityZ);

		Debug.Log(IsChangingRigWeight);

		// Handle IK contraints.
		if (IsChangingRigWeight)
			return;

		ConstrainLookAtIK();
	}

	/// <summary>
	/// Callback method for the on strafe switching event of PlayerMovement script.
	/// </summary>
	/// <param name="useStrafe"></param>
	public void OnStrafeSwitching(bool useStrafe)
	{
		animator.SetBool(useStrafeMovementHash, useStrafe);
	}

	private void ConstrainLookAtIK()
	{
		Vector3 headLookAtLocal = transform.InverseTransformPoint(_cameraLookTarget.position);

		// If the target is behind the player, then gradually decreases the weight to 0 in half a second.
		if (Mathf.Sign(headLookAtLocal.z) != _headLookOvershoot)
		{
			if (headLookAtLocal.z < 0f)
				StartCoroutine(ChangeRigLayerWeight("look at ik",  0f, 1f));
			else
				StartCoroutine(ChangeRigLayerWeight("look at ik", 1f, 1f));
			
			_headLookOvershoot = Mathf.Sign(headLookAtLocal.z);
		}
	}

	public static IEnumerator ChangeRigLayerWeight(string rigLayerName, float newWeight, float duration = 1f)
	{
		IsChangingRigWeight = true;

		rigLayerName = ("RigLayer_" + rigLayerName).ToLower();
		Rig targetRig = _rigBuilder.layers.Find(rig => rig.name.ToLower().Equals(rigLayerName)).rig;

		if (targetRig == null)
		{
			IsChangingRigWeight = false;
			yield break;
		}

		float currentTime = 0f;

		if (duration > 0f)
		{
			while (currentTime < duration)
			{
				float w = NumberManipulator.RangeConvert(currentTime / duration, 0f, 1f, targetRig.weight, newWeight);
				targetRig.weight = w;

				currentTime += Time.deltaTime;
				yield return null;
			}
		}
		else
			targetRig.weight = newWeight;

		IsChangingRigWeight = false;
	}
}
