using UnityEngine;
using CSTGames.CommonEnums;

/// <summary>
/// Manages all the keyboard input for the game.
/// </summary>
public class InputManager : MonoBehaviour
{
	public static InputManager instance { get; private set; }

	[Header("Keyset Reference")]
	[Space]
	[SerializeField] private Keyset keySet;

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

	public KeyCode GetKeyForAction(KeybindingActions action)
	{
		foreach (Keyset.Key key in keySet.keyList)
			if (key.action == action)
				return key.keyCode;

		return KeyCode.None;
	}

	/// <summary>
	/// Returns true while the user holds down the key for the specified action.
	/// </summary>
	/// <param name="action"></param>
	/// <returns></returns>
	public bool GetKey(KeybindingActions action)
	{
		foreach (Keyset.Key key in keySet.keyList)
			if (key.action == action)
				return Input.GetKey(key.keyCode);

		return false;
	}

	/// <summary>
	/// Returns true during the frame the user starts pressing down the key for the specified action.
	/// </summary>
	/// <param name="action"></param>
	/// <returns></returns>
	public bool GetKeyDown(KeybindingActions action)
	{
		foreach (Keyset.Key key in keySet.keyList)
			if (key.action == action)
				return Input.GetKeyDown(key.keyCode);

		return false;
	}

	/// <summary>
	/// Returns true during the frame the user releases the key for the specified action.
	/// </summary>
	/// <param name="action"></param>
	/// <returns></returns>
	public bool GetKeyUp(KeybindingActions action)
	{
		foreach (Keyset.Key key in keySet.keyList)
			if (key.action == action)
				return Input.GetKeyUp(key.keyCode);

		return false;
	}

	/// <summary>
	/// Returns the value of the axis based on which key is being held.
	/// </summary>
	/// <param name="axis"></param>
	/// <returns></returns>
	public float GetAxisRaw(string axis)
	{
		axis = axis.ToLower().Trim();
		
		switch (axis)
		{
			case "horizontal":
				if (GetKey(KeybindingActions.Right))
					return 1f;
				
				else if (GetKey(KeybindingActions.Left))
					return -1f;

				else
					return 0f;

			case "vertical":
				if (GetKey(KeybindingActions.Forward))
					return 1f;
				
				else if (GetKey(KeybindingActions.Backward))
					return -1f;

				else
					return 0f;
		}

		return 0f;
	}
}
