using UnityEngine;
using CSTGames.CommonEnums;

public class AnimStateController : MonoBehaviour
{
	[SerializeField] private Animator animator;

	// Parameter ids.
	//private int velXHash, velZHash;
	private int speedHash;
	private int isJumpingHash;

	private void Awake()
	{
		animator = GetComponent<Animator>();
	}

	private void Start()
	{
		//velXHash = Animator.StringToHash("Velocity X");
		//velZHash = Animator.StringToHash("Velocity Z");
		speedHash = Animator.StringToHash("Speed");
		isJumpingHash = Animator.StringToHash("IsJumping");
	}

	private void Update()
	{
		animator.SetFloat(speedHash, PlayerMovement.linearVelocity);
	}

	public void OnJumping(bool isJumping)
	{
		animator.SetBool(isJumpingHash, isJumping);
	}
}
