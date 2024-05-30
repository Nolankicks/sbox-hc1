﻿

namespace Facepunch
{
	public partial class PlayerBody : Component
	{
		[Property] public SkinnedModelRenderer Renderer { get; set; }
		[Property] public Rigidbody Rigidbody { get; set; }
		[Property] public ModelPhysics Physics { get; set; }

		public bool IsRagdoll => Physics.Enabled;

		public Vector3 DamageTakenPosition { get; set; }
		public Vector3 DamageTakenForce { get; set; }

		private List<SkinnedModelRenderer> renderers;

		protected override void OnAwake()
		{
			renderers = new List<SkinnedModelRenderer>();
			renderers.AddRange( Components.GetAll<SkinnedModelRenderer>( FindMode.EverythingInSelfAndDescendants ));
		}

		internal void SetRagdoll( bool ragdoll )
		{
			Physics.Enabled = ragdoll;
			Rigidbody.Enabled = ragdoll;

			GameObject.Tags.Set( "ragdoll", ragdoll );

			if ( !ragdoll )
			{
				GameObject.Transform.LocalPosition = Vector3.Zero;
				GameObject.Transform.LocalRotation = Rotation.Identity;
			}

			ShowBodyParts( ragdoll );

			if ( ragdoll && DamageTakenForce.LengthSquared > 0f )
				ApplyRagdollImpulses( DamageTakenPosition, DamageTakenForce );
		}

		internal void ApplyRagdollImpulses( Vector3 position, Vector3 force )
		{
			if ( Physics == null || Physics.PhysicsGroup == null )
				return;

			foreach ( var body in Physics.PhysicsGroup.Bodies )
			{
				body.ApplyImpulseAt( position, force );
			}
		}

		public void ShowBodyParts( bool show )
		{
			// Disable the player's body so it doesn't render.
			var skinnedModels = Components.GetAll<SkinnedModelRenderer>( FindMode.EnabledInSelfAndDescendants );

			foreach ( var skinnedModel in skinnedModels )
			{
				skinnedModel.RenderType = show ?
					ModelRenderer.ShadowRenderType.On :
					ModelRenderer.ShadowRenderType.ShadowsOnly;
			}
		}
	}
}
