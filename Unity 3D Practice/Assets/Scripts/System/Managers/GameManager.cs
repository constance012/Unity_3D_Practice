using UnityEngine;

public class GameManager : Singleton<GameManager>
{
	[Header("Debugging")]
	[Space]
	public bool allowPausingPlaymode;

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
