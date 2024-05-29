﻿namespace Facepunch;

partial class PlayerController
{
	/// <summary>
	/// Is the player holding use?
	/// </summary>
	[Sync] public bool IsUsing { get; set; }

	private void UpdateUse()
	{
		IsUsing = Input.Down( "Use" );

		if ( Input.Pressed( "Use" ) )
		{
			using ( Rpc.FilterInclude( Connection.Host ) )
			{
				TryUse( AimRay );
			}
		}
	}

	[Broadcast( NetPermission.OwnerOnly )]
	private void TryUse( Ray ray )
	{
		var hits = Scene.Trace.Ray( ray, UseDistance )
			.Size( 5f )
			.IgnoreGameObjectHierarchy( GameObject )
			.HitTriggers()
			.RunAll() ?? Array.Empty<SceneTraceResult>();

		var usable = hits
			.Select( x => x.GameObject.Components.Get<IUse>( FindMode.EnabledInSelf | FindMode.InAncestors ) )
			.FirstOrDefault( x => x is not null );

		if ( usable.IsValid() && usable.CanUse( this ) )
		{
			usable.OnUse( this );
		}
	}
}

