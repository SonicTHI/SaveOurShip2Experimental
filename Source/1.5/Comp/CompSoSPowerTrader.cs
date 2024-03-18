using System;
using Verse;

namespace RimWorld
{
	class CompSoSPowerTrader : CompPowerTrader
	{
		public override void LostConnectParent()
		{
			if (parent.Spawned)
			{
				ConnectToTransmitter(parent.Position.GetFirstThingWithComp<CompPowerTransmitter>(parent.Map).TryGetComp<CompPowerTransmitter>());
			}
		}
	}
}
