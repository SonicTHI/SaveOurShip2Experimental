using System;
using Verse;

namespace RimWorld
{
	public class CompProperties_ShuttleLaunchable : CompProperties
	{
		public float fuelPerTile;
		public ThingDef skyfaller;

		public CompProperties_ShuttleLaunchable()
		{
			this.compClass = typeof(CompShuttleLaunchable);
		}
	}
}