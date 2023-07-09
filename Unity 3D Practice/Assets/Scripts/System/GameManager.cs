using UnityEngine;

public class GameManager : MonoBehaviour
{
	public static GameManager instance { get; private set; }

	[Header("Debugging")]
	[Space]
	public bool allowPausingPlaymode;

	private void Awake()
	{
		if (instance == null)
			instance = this;
		else
		{
			Debug.LogWarning("More than one instance of Input Manager found!!");
			Destroy(gameObject);
			return;
		}
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.P) && allowPausingPlaymode)
		{
			Cursor.lockState = CursorLockMode.None;
			Debug.Break();
		}

		Cursor.lockState = Input.GetKey(KeyCode.LeftAlt) ? CursorLockMode.None : CursorLockMode.Locked;
	}
}
