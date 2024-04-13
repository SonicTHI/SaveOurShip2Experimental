using System;
using Verse;

namespace SaveOurShip2
{
	public class CompProps_BecomeBuilding : CompProperties
	{
		public ThingDef buildingDef;
		public float fuelPerTile;
		public ThingDef skyfaller;

		public CompProps_BecomeBuilding ()
		{
			this.compClass = typeof(CompBecomeBuilding);
		}
	}
}

