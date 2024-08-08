﻿namespace Facepunch;

public partial class Vehicle : Component, IRespawnable, ICustomMinimapIcon, ITeam, IUse, IDescription, IDamageListener
{
	[Property, Group( "Components" )] public Rigidbody Rigidbody { get; set; }
	[Property, Group( "Components" )] public ModelRenderer Model { get; set; }

	/// <summary>
	/// What team does this pawn belong to?
	/// </summary>
	public virtual Team Team { get; set; } = Team.Unassigned;

	/// <summary>
	/// An accessor for health component if we have one.
	/// </summary>
	[Property] public virtual HealthComponent HealthComponent { get; set; }

	/// <summary>
	/// What to spawn when we explode?
	/// </summary>
	[Property, Group( "Effects" )] public GameObject Explosion { get; set; }
	[Property, Group( "Effects" )] public float FireThreshold { get; set; } = 30f;
	[Property, Group( "Effects" )] public GameObject Fire { get; set; } 

	[Property, Group( "Vehicle" )] public List<Wheel> Wheels { get; set; }
	[Property, Group( "Vehicle" )] public List<VehicleSeat> Seats { get; set; }
	[Property, Group( "Vehicle" )] public float Torque { get; set; } = 15000f;
	[Property, Group( "Vehicle" )] public float BoostTorque { get; set; } = 20000f;
	[Property, Group( "Vehicle" )] public float AccelerationRate { get; set; } = 1.0f;
	[Property, Group( "Vehicle" )] public float DecelerationRate { get; set; } = 0.5f;
	[Property, Group( "Vehicle" )] public float BrakingRate { get; set; } = 2.0f;

	[Property, Group( "Description" )] public string DisplayName { get; set; }

	public VehicleInputState InputState { get; } = new();

	private float _currentTorque;
	private GameObject _fire;

	protected override void OnFixedUpdate()
	{
		// Evaluate all input-driving seats, and if all of them have nobody in it, reset the input
		// Otherwise the vehicle will just go forever
		if ( Seats.Where( x => x.HasInput ).All( x => !x.Player.IsValid() ) )
		{
			InputState.Reset();
		}

		if ( IsProxy )
			return;

		float torque = InputState.isBoosting ? BoostTorque : Torque;
		float verticalInput = InputState.direction.x;
		float targetTorque = verticalInput * torque;

		bool isBraking = Math.Sign( verticalInput * _currentTorque ) == -1;
		bool isDecelerating = verticalInput == 0;

		float lerpRate = AccelerationRate;
		if ( isBraking )
			lerpRate = BrakingRate;
		else if ( isDecelerating )
			lerpRate = DecelerationRate;

		_currentTorque = _currentTorque.LerpTo( targetTorque, lerpRate * Time.Delta );

		foreach ( Wheel wheel in Wheels )
		{
			wheel.ApplyMotorTorque( _currentTorque );
		}

		var groundVel = Rigidbody.Velocity.WithZ( 0f );
		if ( verticalInput == 0f && groundVel.Length < 32f )
		{
			var z = Rigidbody.Velocity.z;
			Rigidbody.Velocity = Vector3.Zero.WithZ( z );
		}
	}

	public void OnKill( DamageInfo damageInfo )
	{
		foreach ( var seat in Seats )
		{
			seat.Eject();
		}

		Explosion?.Clone( Transform.Position );
		GameObject.Destroy();
	}

	bool IMinimapElement.IsVisible( Pawn viewer )
	{
		return viewer.Team == Team;
	}

	public UseResult CanUse( PlayerPawn player )
	{
		// You're already in a vehicle somehow
		if ( player.CurrentSeat.IsValid() && player.CurrentSeat.Vehicle == this ) return false;

		// Can't get in vehicle of enemies
		if ( Seats.Any( x => x.Player.IsValid() && !x.Player.IsFriendly( player ) ) ) return "Occupied by an enemy";

		// Seats are all filled
		if ( Seats.All( x => !x.CanEnter( player ) ) ) return $"{DisplayName} is full";

		return true;
	}

	public void OnUse( PlayerPawn player )
	{
		// Already in the vehicle, fuck off
		if ( Seats.FirstOrDefault( x => x.CanEnter( player ) ) is { } availableSeat )
		{
			availableSeat.Enter( player );
		}
	}

	public void OnDamaged( DamageInfo damageInfo )
	{
		Log.Info( HealthComponent.Health );
		if ( HealthComponent.Health < FireThreshold && !_fire.IsValid() )
		{
			_fire = Fire.Clone( GameObject, Vector3.Zero, Rotation.Identity, Vector3.One );
		}
	}

	protected override void OnDestroy()
	{
		if ( _fire.IsValid() )
			_fire.Destroy();
	}

	string IMinimapIcon.IconPath => "ui/icons/vehicle.png";
	string ICustomMinimapIcon.CustomStyle => $"";
	Vector3 IMinimapElement.WorldPosition => Transform.Position;
}
