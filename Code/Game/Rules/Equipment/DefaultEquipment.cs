﻿using Facepunch;

public sealed class DefaultEquipment : Component, IPlayerSpawnListener
{
	/// <summary>
	/// A weapon set that we'll give the player when they spawn.
	/// </summary>
	[Property] public List<WeaponData> Weapons { get; set; }

	void IPlayerSpawnListener.PrePlayerSpawn( PlayerController player )
	{
		if ( Weapons is null ) return;

		Log.Info( $"PrePlayerSpawn for {player}" );

		foreach ( var weapon in Weapons )
		{
			if ( !player.Inventory.HasWeapon( weapon.Slot ) )
				player.Inventory.GiveWeapon( weapon, false );
		}

		if ( player.Inventory.Weapons.Any() )
		{
			player.Inventory.SwitchToSlot( player.Inventory.Weapons.Count() - 1 );
		}
	}
}
