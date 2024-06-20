using Facepunch.UI;
using Sandbox.Audio;

namespace Facepunch;

public class FlashbangEffect : Component
{
	[Property] public SoundEvent SoundEffect { get; set; }
	[Property] public float LifeTime { get; set; }
	
	private TimeUntil TimeUntilEnd { get; set; }
	private FlashbangOverlay Overlay { get; set; }
	private ChromaticAberration Aberration { get; set; }
	private Bloom Bloom { get; set; }
	
	private DspProcessor DspProcessor { get; set; }
	private SoundHandle Sound { get; set; }
	private IDisposable RenderHook { get; set; }
	
	protected override void OnEnabled()
	{
		Bloom ??= Components.Create<Bloom>();
		Overlay ??= Components.Create<FlashbangOverlay>();
		Aberration ??= Components.Create<ChromaticAberration>();
		DspProcessor ??= new( "weird.4" );
		
		Mixer.Master.AddProcessor( DspProcessor );
		
		base.OnEnabled();
	}

	protected override void OnDisabled()
	{
		if ( Sound.IsValid() )
			Sound.Volume = 0f;
		
		Mixer.Master.RemoveProcessor( DspProcessor );
		
		base.OnDisabled();
	}

	protected override void OnDestroy()
	{
		Bloom?.Destroy();
		Overlay?.Destroy();
		Aberration?.Destroy();
		Sound?.Stop();
		
		base.OnDestroy();
	}

	protected override void OnStart()
	{
		TimeUntilEnd = LifeTime;
		
		Bloom.Mode = SceneCamera.BloomAccessor.BloomMode.Screen;
		Bloom.Threshold = 0f;
		Bloom.ThresholdWidth = 0f;
		Bloom.Strength = 10f;

		Aberration.Scale = 1f;
		Aberration.Offset = new( 6f, 10f, 3f );

		Overlay.EndTime = LifeTime;

		if ( SoundEffect is not null )
		{
			Sound = Sandbox.Sound.Play( SoundEffect );
			Sound.Volume = 1f;
		}
		
		base.OnStart();
	}

	protected override void OnUpdate()
	{
		var f = TimeUntilEnd.Relative / LifeTime;
		Aberration.Scale = f;
		Bloom.Strength = 10f * f;
		DspProcessor.Mix = f;
		
		if ( Sound.IsValid() )
			Sound.Volume = f;
		
		base.OnUpdate();
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		
		if ( TimeUntilEnd )
		{
			Destroy();
		}
	}
}
