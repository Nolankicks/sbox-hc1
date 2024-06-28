﻿using Sandbox.Events;

namespace Facepunch;

/// <summary>
/// Split players into two balanced teams. If <see cref="AllowLateJoiners"/> is true,
/// new players will be assigned as soon as they join. Otherwise, teams will only be
/// assigned when this game state is entered.
/// </summary>
public sealed class TeamAssigner : Component,
	IGameEventHandler<EnterStateEvent>,
	IGameEventHandler<PlayerConnectedEvent>,
	IGameEventHandler<PlayerJoinedEvent>
{
	[Property] public int MaxTeamSize { get; set; } = 5;

	/// <summary>
	/// If true, new players will be assigned as soon as they join. Otherwise, teams
	/// will only be assigned when this game state is entered.
	/// </summary>
	[Property] public bool AllowLateJoiners { get; set; } = true;

	void IGameEventHandler<EnterStateEvent>.OnGameEvent( EnterStateEvent eventArgs )
	{
		foreach ( var player in GameUtils.GetPlayers(Team.Unassigned) )
		{
			AssignTeam( player, true );
		}
	}

	private void AssignTeam( PlayerState player, bool dispatch )
	{
		var ts = GameUtils.GetPlayers( Team.Terrorist ).Count();
		var cts = GameUtils.GetPlayers( Team.CounterTerrorist ).Count();

		var assignTeam = Team.Unassigned;

		if ( ts < MaxTeamSize || cts < MaxTeamSize )
		{
			var compare = ts.CompareTo( cts );

			if ( compare == 0 )
			{
				compare = Random.Shared.Next( 2 ) * 2 - 1;
			}

			assignTeam = compare < 0 ? Team.Terrorist : Team.CounterTerrorist;
		}

		if ( dispatch )
		{
			player.AssignTeam( assignTeam );

			// Respawn the player's pawn since we might've changed their spawn
			if ( player.PlayerPawn.IsValid() )
				player.PlayerPawn.Respawn();
		}
		else
		{
			player.Team = assignTeam;
		}
	}

	void IGameEventHandler<PlayerConnectedEvent>.OnGameEvent( PlayerConnectedEvent eventArgs )
	{
		if ( AllowLateJoiners )
		{
			AssignTeam( eventArgs.PlayerState, false );
		}
	}

	void IGameEventHandler<PlayerJoinedEvent>.OnGameEvent( PlayerJoinedEvent eventArgs )
	{
		// Calling this will invoke callbacks for any ITeamAssignedListener listeners.
		eventArgs.Player.AssignTeam( eventArgs.Player.Team );
	}
}
