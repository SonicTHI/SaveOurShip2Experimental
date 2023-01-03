using System;
using Verse;

namespace RimWorld
{
	public class CompProperties_BecomeBuilding : CompProperties
	{
		public ThingDef buildingDef;
		public float fuelPerTile;
        public ThingDef skyfaller;

		public CompProperties_BecomeBuilding ()
		{
			this.compClass = typeof(CompBecomeBuilding);
		}
	}
}

