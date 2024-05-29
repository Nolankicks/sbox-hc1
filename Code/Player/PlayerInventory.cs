using Sandbox.Diagnostics;

namespace Facepunch;

/// <summary>
/// The player's inventory.
/// </summary>
public partial class PlayerInventory : Component
{
	[RequireComponent] PlayerController Player { get; set; }

	/// <summary>
	/// What weapons do we have right now?
	/// </summary>
	public IEnumerable<Weapon> Weapons => WeaponGameObject.Components.GetAll<Weapon>( FindMode.EverythingInSelfAndDescendants );

	/// <summary>
	/// A <see cref="GameObject"/> that will hold all of our weapons.
	/// </summary>
	[Property] public GameObject WeaponGameObject { get; set; }

	/// <summary>
	/// Can we unequip the current weapon so we have no weapons out?
	/// </summary>
	[Property] public bool CanUnequipCurrentWeapon { get; set; } = false;

	/// <summary>
	/// Players current cash balance
	/// </summary>
	[HostSync] public int Balance { get; private set; } = 999999;

	/// <summary>
	/// Does this player have a defuse kit?
	/// </summary>
	[HostSync] public bool HasDefuseKit { get; private set; }

	/// <summary>
	/// Gets the player's current weapon.
	/// </summary>
	public Weapon CurrentWeapon => Player.CurrentWeapon;
	
	public void Clear()
	{
		Assert.True( Networking.IsHost );
		
		foreach ( var wpn in Weapons )
		{
			wpn.GameObject.Destroy();
			wpn.Enabled = false;
		}
	}
	
	[Broadcast]
	private void DropWeapon( Guid weaponId )
	{
		var weapon = Scene.Directory.FindComponentByGuid( weaponId ) as Weapon;
		if ( !weapon.IsValid() )
			return;
		
		if ( weapon.Resource.Slot == WeaponSlot.Melee )
			return;

		if ( !Networking.IsHost )
			return;
		
		var droppedWeapon = DroppedWeapon.Create( weapon.Resource, Player.AimRay.Position + Player.AimRay.Forward * 32f, Rotation.From( 0, Player.EyeAngles.yaw + 90, 90 ) );
		droppedWeapon.Rigidbody.ApplyForce( Player.AimRay.Forward * 0.5f );
		droppedWeapon.GameObject.NetworkSpawn();
		
		RemoveWeapon( weapon );
	}

	protected override void OnUpdate()
	{
		if ( !Player.IsLocallyControlled )
			return;

		if ( Input.Pressed( "Drop" ) )
		{
			DropWeapon( CurrentWeapon.Id );
		}

		for ( int i = 0; i < Weapons.Count(); i++ )
		{
			if ( Input.Pressed( $"Slot{i + 1}" ) )
			{
				SwitchToSlot( i );
			}
		}
	}

	public void HolsterCurrent()
	{
		Assert.True( !IsProxy || Networking.IsHost );
		Player.SetCurrentWeapon( null );
	}

	public void SwitchToSlot( int slot )
	{
		Assert.True( !IsProxy || Networking.IsHost );
		
		var weapon = Weapons.ElementAt( slot );
		if ( !weapon.IsValid() ) return;

		if ( Player.CurrentWeapon != weapon )
			SwitchWeapon( weapon );
		else if ( CanUnequipCurrentWeapon )
			HolsterCurrent();
	}

	/// <summary>
	/// Tries to set the player's current weapon to a specific one, which has to be in the player's inventory.
	/// </summary>
	/// <param name="weapon"></param>
	public void SwitchWeapon( Weapon weapon )
	{
		Assert.True( !IsProxy || Networking.IsHost );
		
		if ( !Weapons.Contains( weapon ) )
			return;
		
		Player.SetCurrentWeapon( weapon );
	}

	/// <summary>
	/// Removes the given weapon and destroys it.
	/// </summary>
	public void RemoveWeapon( Weapon weapon )
	{
		if ( !Networking.IsHost )
		{
			Log.Warning( "Tried to remove weapon while not host" );
			return;
		}
		
		if ( !Weapons.Contains( weapon ) ) return;

		if ( CurrentWeapon == weapon )
			SwitchWeapon( Weapons.FirstOrDefault( x => x != weapon ) );

		weapon.GameObject.Destroy();
	}

	public void GiveWeapon( WeaponData resource, bool makeActive = true )
	{
		if ( !Networking.IsHost )
		{
			Log.Warning( "Tried to give weapon while not host" );
			return;
		}

		// If we're in charge, let's make some weapons.
		if ( resource == null )
		{
			Log.Warning( "A player loadout without a weapon? Nonsense." );
			return;
		}

		if ( !CanTakeWeapon( resource ) )
			return;

		var slotCurrent = Weapons.FirstOrDefault( weapon => weapon.Resource.Slot == resource.Slot );
		if ( slotCurrent.IsValid() )
			DropWeapon( slotCurrent.Id );

		if ( !resource.MainPrefab.IsValid() )
		{
			Log.Error( $"Weapon doesn't have a prefab? {resource}, {resource.MainPrefab}, {resource.ViewModelPrefab}" );
			return;
		}

		// Create the weapon prefab and put it on the weapon GameObject.
		var weaponGameObject = resource.MainPrefab.Clone( new CloneConfig()
		{
			Transform = new(),
			Parent = WeaponGameObject
		} );
		var weaponComponent = weaponGameObject.Components.Get<Weapon>( FindMode.EverythingInSelfAndDescendants );
		weaponGameObject.NetworkSpawn( Player.Network.OwnerConnection );

		if ( makeActive )
			Player.SetCurrentWeapon( weaponComponent );
		
		Log.Info( $"Spawned weapon {weaponGameObject} for {Player}" );
	}

	public bool HasWeapon( WeaponData resource )
	{
		return Weapons.Any( weapon => weapon.Enabled && weapon.Resource == resource );
	}

	public bool HasWeapon( WeaponSlot slot )
	{
		return Weapons.Any( weapon => weapon.Enabled && weapon.Resource.Slot == slot );
	}

	public bool CanTakeWeapon( WeaponData resource )
	{
		switch ( resource.Slot )
		{
			case WeaponSlot.Utility:
				// TODO: grenade limits
				return true;

			default:
				return true;
		}
	}
	
	public void GiveCash( int amount )
	{
		using var _ = Rpc.FilterInclude( Connection.Host );
		GiveCashHost( amount );
	}

	[Broadcast]
	private void GiveCashHost( int amount )
	{
		Assert.True( Networking.IsHost );
		Balance += amount;
	}

	public void BuyWeapon( int resourceId )
	{
		using var _ = Rpc.FilterInclude( Connection.Host );
		BuyWeaponHost( resourceId );
	}

	[Broadcast]
	private void BuyWeaponHost( int resourceId )
	{
		Assert.True( Networking.IsHost );

		var weaponData = ResourceLibrary.Get<WeaponData>(resourceId);

		if ( weaponData == null )
		{
			Log.Warning( $"Attempted purchase but WeaponData (Id: {weaponData}) not known!" );
			return;
		}

		if ( Balance < weaponData.Price )
			return;

		Balance -= weaponData.Price;
		GiveWeapon( weaponData );
	}
}
