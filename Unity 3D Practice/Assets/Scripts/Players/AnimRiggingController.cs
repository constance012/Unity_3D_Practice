using UnityEngine;
using UnityEngine.Animations.Rigging;
using CSTGames.CommonEnums;
using System.Collections;

public class AnimRiggingController : MonoBehaviour
{
	[SerializeField] private Animator animator;
	[SerializeField] private static RigBuilder rigBuilder;

	// Parameter ids.
	private int velXHash, velZHash;
	private int speedHash;
	private int isJumpingHash;
	private int useStrafeMovementHash;

	// Private fields.
	private Transform headLookTarget;
	public static bool isChangingRigWeight { get; private set; }

	private void Awake()
	{
		animator = GetComponent<Animator>();
		rigBuilder = GetComponent<RigBuilder>();

		headLookTarget = Camera.main.transform.Find("Head Look Target");
	}

	private void Start()
	{
		velXHash = Animator.StringToHash("Velocity X");
		velZHash = Animator.StringToHash("Velocity Z");
		speedHash = Animator.StringToHash("Speed");
		isJumpingHash = Animator.StringToHash("IsJumping");
		useStrafeMovementHash = Animator.StringToHash("UseStrafeMovement");
	}

	private void Update()
	{
		animator.SetFloat(speedHash, PlayerMovement.linearVelocity);
		animator.SetFloat(velXHash, PlayerMovement.velocityX);
		animator.SetFloat(velZHash, PlayerMovement.velocityZ);

		// Handle IK contraints.
		if (isChangingRigWeight)
			return;

		ConstrainLookAtIK();
	}

	/// <summary>
	/// Invoke method for the on jumping event.
	/// </summary>
	/// <param name="isJumping"></param>
	public void OnJumping(bool isJumping)
	{
		animator.SetBool(isJumpingHash, isJumping);
	}

	/// <summary>
	/// Invoke method for the on strafe switching event.
	/// </summary>
	/// <param name="useStrafe"></param>
	public void OnStrafeSwitching(bool useStrafe)
	{
		animator.SetBool(useStrafeMovementHash, useStrafe);
	}

	private void ConstrainLookAtIK()
	{
		Vector3 headLookAtLocal = transform.InverseTransformPoint(headLookTarget.position);

		// If the target is behind the player, then gradually decreases the weight to 0 in half a second.
		if (headLookAtLocal.z < 0f)
			StartCoroutine(ChangeRigLayerWeight("look at ik", RigLayerWeightControl.Decrease, 0f, .5f));
		else if (headLookAtLocal.z > 0f)
			StartCoroutine(ChangeRigLayerWeight("look at ik", RigLayerWeightControl.Increase, 1f, .5f));
	}

	public static IEnumerator ChangeRigLayerWeight(string rigLayerName, RigLayerWeightControl control, float newWeight, float duration = 1f)
	{
		isChangingRigWeight = true;

		rigLayerName = ("RigLayer_" + rigLayerName).ToLower();
		Rig targetRig = rigBuilder.layers.Find(rig => rig.name.ToLower().Equals(rigLayerName)).rig;

		if (targetRig == null)
		{
			isChangingRigWeight = false;
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

		targetRig.weight = control == RigLayerWeightControl.Increase ? Mathf.Clamp(targetRig.weight, 0f, newWeight)
																	 : Mathf.Clamp(targetRig.weight, newWeight, 1f);

		isChangingRigWeight = false;
	}
}
