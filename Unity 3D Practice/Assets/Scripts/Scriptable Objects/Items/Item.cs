using System;
using UnityEngine;
using CSTGames.CommonEnums;

[CreateAssetMenu(fileName = "New Item", menuName = "Items/Base Item")]
public class Item : ScriptableObject
{
	[Serializable]
	public struct Rarity
	{
		public string title;
		public Color color;
	}

	[Header("ID")]
	[Space]

	public string id;
	[ContextMenu("Generate ID")]
	private void GenerateID()
	{
		id = Guid.NewGuid().ToString();
	}

	[Header("Basic Info")]
	[Space]
	public ItemCategory category;
	public int slotIndex = -1;

	public string itemName;
	[TextArea(5, 10)] public string description;

	public Rarity rarity;
	public Sprite icon;

	[Header("Base Properties")]
	[Space]
	public Mesh mesh;
	public Material[] materials;

	public float weight;

	public virtual void Use()
	{
		Debug.Log($"Using {itemName}.");
	}
}
