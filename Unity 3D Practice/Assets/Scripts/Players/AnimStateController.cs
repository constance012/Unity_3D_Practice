using UnityEngine;
using CSTGames.CommonEnums;

public class AnimStateController : MonoBehaviour
{
	[SerializeField] private Animator animator;
	public PlayerMovement playerMovement;

	// Parameter ids.
	private int velHash;

	private void Awake()
	{
		animator = GetComponent<Animator>();
	}

	private void Start()
	{
		velHash = Animator.StringToHash("Velocity");
	}

	private void Update()
	{
		if (playerMovement.isRunning)
			animator.SetFloat(velHash, playerMovement.velocity / PlayerMovement.maxRunningSpeed);
		else if (playerMovement.isWalking)
			animator.SetFloat(velHash, .1f);
		else
			animator.SetFloat(velHash, 0f);
	}
}
