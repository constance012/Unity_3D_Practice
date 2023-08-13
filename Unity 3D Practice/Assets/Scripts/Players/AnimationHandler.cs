using UnityEngine;
using UnityEngine.Animations.Rigging;
using CSTGames.CommonEnums;
using System.Collections;

public class AnimationHandler : MonoBehaviour
{
	[SerializeField] private Animator animator;
	[SerializeField] private static RigBuilder rigBuilder;

	// Animators' parameter hashes.
	public static int velXHash, velZHash;
	public static int speedHash;
	public static int useStrafeMovementHash;
	public static int isJumpingHash;

	public static int holsterWeaponHash;
	public static int IsAimingHash;
	public static int endAimingHash;

	public static int endReloadingHash;

	// Private fields.
	private Transform _headLookTarget;
	public static bool IsChangingRigWeight { get; private set; }

	private void Awake()
	{
		animator = GetComponent<Animator>();
		rigBuilder = GetComponent<RigBuilder>();

		_headLookTarget = Camera.main.transform.Find("Head Look Target");
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
		IsAimingHash = Animator.StringToHash("IsAiming");
		endAimingHash = Animator.StringToHash("EndAiming");

		endReloadingHash = Animator.StringToHash("EndReloading");
	}

	private void Update()
	{
		animator.SetFloat(speedHash, PlayerMovement.linearVelocity);
		animator.SetFloat(velXHash, PlayerMovement.velocityX);
		animator.SetFloat(velZHash, PlayerMovement.velocityZ);

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
		Vector3 headLookAtLocal = transform.InverseTransformPoint(_headLookTarget.position);

		// If the target is behind the player, then gradually decreases the weight to 0 in half a second.
		if (headLookAtLocal.z < 0f)
			StartCoroutine(ChangeRigLayerWeight("look at ik", RigLayerWeightControl.Decrease, 0f, .5f));
		else if (headLookAtLocal.z > 0f)
			StartCoroutine(ChangeRigLayerWeight("look at ik", RigLayerWeightControl.Increase, 1f, .5f));
	}

	public static IEnumerator ChangeRigLayerWeight(string rigLayerName, RigLayerWeightControl control, float newWeight, float duration = 1f)
	{
		IsChangingRigWeight = true;

		rigLayerName = ("RigLayer_" + rigLayerName).ToLower();
		Rig targetRig = rigBuilder.layers.Find(rig => rig.name.ToLower().Equals(rigLayerName)).rig;

		if (targetRig == null)
		{
			IsChangingRigWeight = false;
			yield break;
		}

		switch (control)
		{
			case RigLayerWeightControl.Increase:
				while (targetRig.weight < newWeight)
				{
					targetRig.weight += Time.deltaTime / duration;

					yield return null;
				}
				break;

			case RigLayerWeightControl.Decrease:
				while (targetRig.weight > newWeight)
				{
					targetRig.weight -= Time.deltaTime / duration;

					yield return null;
				}
				break;
		}

		IsChangingRigWeight = false;
	}
}
