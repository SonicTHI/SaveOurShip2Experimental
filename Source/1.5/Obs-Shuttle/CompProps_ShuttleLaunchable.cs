using System;
using Verse;

namespace SaveOurShip2
{
	public class CompProps_ShuttleLaunchable : CompProperties
	{
		public float fuelPerTile;
		public ThingDef skyfaller;

		public CompProps_ShuttleLaunchable()
		{
			this.compClass = typeof(CompShuttleLaunchable);
		}
	}
}