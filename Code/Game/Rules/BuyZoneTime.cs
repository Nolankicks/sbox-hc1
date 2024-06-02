using Sandbox;

namespace Facepunch;

public sealed class BuyZoneTime : Component, IRoundStartListener
{
	[Property] public float BuyTime { get; set; } = 30;
	[HostSync] public RealTimeUntil TimeUntilCannotBuy { get; private set; } = 0;

	void IRoundStartListener.PostRoundStart()
	{
		TimeUntilCannotBuy = BuyTime;
	}

	public bool CanBuy()
	{
		return !TimeUntilCannotBuy;
	}
}
