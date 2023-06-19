using System;
using System.IO;
using UnityEngine;
using CSTGames.CommonEnums;

/// <summary>
/// A scriptable object for creating a set of keys use in keybinding.
/// </summary>
[CreateAssetMenu(fileName = "New Keyset", menuName = "Keybinding/Keyset")]
public class Keyset : ScriptableObject
{
	[System.Serializable]
	public struct Key
	{
		public KeybindingActions action;
		public KeyCode keyCode;
	}

	public Key[] keyList;

	/// <summary>
	///  Saves the Keyset's data into a json file. Creates one if the file doesn't already exist.
	/// </summary>
	/// <param name="fileName"></param>
	public void SaveKeysetToJson(string fileName)
	{
		string fullPath = Path.Combine(Application.persistentDataPath, "Keyset Data", fileName + ".json");
		string parentDirectory = Path.GetDirectoryName(fullPath);

		try
		{
			if (!Directory.Exists(parentDirectory))
				Directory.CreateDirectory(parentDirectory);

			Debug.Log($"Saving data to {fullPath}");

			string serializedData = JsonUtility.ToJson(this, true);

			using (FileStream file = new FileStream(fullPath, FileMode.Create))
			{
				using (StreamWriter writer = new StreamWriter(file))
				{
					writer.Write(serializedData);
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogError($"Error occured when trying to save keyset data to file.\n" +
						   $"At path: {fullPath}.\n" +
						   $"Reason: {ex.Message}.");
		}
	}

	/// <summary>
	/// Loads data to the Keyset from a .json file. Uses the default file if the previously selected file is missing.
	/// </summary>
	/// <param name="fileName"></param>
	public void LoadKeysetFromJson(string fileName)
	{
		string fullPath = Path.Combine(Application.persistentDataPath, "Keyset Data", fileName + ".json");

		Debug.Log($"Reading data at {fullPath}");

		if (File.Exists(fullPath))
		{
			ReadData(fullPath);
		}
		else
		{
			fileName = "Default";
			fullPath = Path.Combine(Application.persistentDataPath, "Keyset Data", fileName + ".json");

			if (!File.Exists(fullPath))
				SaveKeysetToJson(fileName);

			ReadData(fullPath);
		}
	}

	private void ReadData(string fullPath)
	{
		try
		{
			string serializedData = "";

			using (FileStream file = new FileStream(fullPath, FileMode.Open))
			{
				using (StreamReader reader = new StreamReader(file))
				{
					serializedData = reader.ReadToEnd();
				}
			}

			JsonUtility.FromJsonOverwrite(serializedData, this);
		}
		catch (Exception ex)
		{
			Debug.LogError($"Error occured when trying to load keyset data from file.\n" +
					   $"At path: {fullPath}.\n" +
					   $"Reason: {ex.Message}.");
		}
	}
}
