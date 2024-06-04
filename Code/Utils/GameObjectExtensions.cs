namespace Facepunch;

public static partial class GameObjectExtensions
{
	/// <summary>
	/// Take damage.
	/// </summary>
	/// <param name="go"></param>
	/// <param name="damage"></param>
	/// <param name="position"></param>
	/// <param name="force"></param>
	/// <param name="attackerId"></param>
	public static void TakeDamage( this GameObject go, float damage, Vector3 position, Vector3 force, Guid attackerId, Guid inflictorId = default, string hitbox = "" )
	{
		foreach ( var damageable in go.Root.Components.GetAll<HealthComponent>() )
		{
			damageable.TakeDamage( damage, position, force, attackerId, inflictorId, hitbox );
		}
	}

	public static void CopyPropertiesTo( this Component src, Component dst )
	{
		var json = src.Serialize().AsObject();

		json.Remove( "__guid" );

		Log.Info( json );

		dst.DeserializeImmediately( json );
	}
}
