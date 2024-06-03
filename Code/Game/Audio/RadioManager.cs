namespace Facepunch;

/// <summary>
/// Handles certain events and plays radio sounds.
/// </summary>
public partial class RadioManager : Component, 
	IRoundStartListener, 
	IBombPlantedListener, 
	IBombDefusedListener,
	IKillListener
{
	public static RadioManager Instance { get; private set; }

	protected override void OnStart()
	{
		Instance = this;
	}

	void IRoundStartListener.PostRoundStart()
	{
		RadioSounds.Play( Team.Terrorist, RadioSound.RoundStarted );
		RadioSounds.Play( Team.CounterTerrorist, RadioSound.RoundStarted );
	}

	void IBombPlantedListener.OnBombPlanted( PlayerController planter, GameObject bomb, BombSite bombSite )
	{
		RadioSounds.Play( Team.Terrorist, RadioSound.BombPlanted );
		RadioSounds.Play( Team.CounterTerrorist, RadioSound.BombPlanted );
	}

	void IBombDefusedListener.OnBombDefused( PlayerController planter, GameObject bomb, BombSite bombSite )
	{
		RadioSounds.Play( Team.Terrorist, RadioSound.BombDefused );
		RadioSounds.Play( Team.CounterTerrorist, RadioSound.BombDefused );
	}

	private int GetAliveCount( Team team )
	{
		return GameUtils.GetPlayers( team ).Where( x => x.HealthComponent.State == LifeState.Alive ).Count();
	}

	void IKillListener.OnPlayerKilled( Component killer, Component victim, float damage, Vector3 position, Vector3 force, Component inflictor, bool isHeadshot )
	{
		var victimTeam = victim.GameObject.GetTeam();
		RadioSounds.Play( victimTeam, RadioSound.TeammateDies );

		if ( killer.IsValid() )
		{
			var killerTeam = killer.GameObject.GetTeam();

			if ( killerTeam == victimTeam ) return;

			if ( GetAliveCount( victimTeam ) == 2 )
			{
				RadioSounds.Play( killerTeam, RadioSound.TwoEnemiesLeft );
			}
			else if ( GetAliveCount( victimTeam ) == 1 )
			{
				RadioSounds.Play( killerTeam, RadioSound.OneEnemyLeft );
			}
		}
	}
}
