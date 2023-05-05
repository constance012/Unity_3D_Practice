namespace CSTGames.CommonEnums
{
	/// <summary>
	/// Represents types of treasure and the their maximum quantity when generated in a chest.
	/// </summary>
	public enum ItemCategory
	{
		Null,
		Equipment,
		Weapon,
		Food,
		Potion,
		Material,
		Mineral,
		MonsterPart,
		Coin,
		Special
	}

	/// <summary>
	/// Represents different actions in the game associated with different control keys.
	/// </summary>
	public enum KeybindingActions
	{
		None,
		PrimaryAttack,
		SecondaryAttack,
		Left,
		Right,
		Forward,
		Backward,
		Jump,
		Run,
		Crouch,
		Reload,
		Inventory,
		Interact,
		Pause,
		SwitchCamera,
		DropItemInHand
	}
}