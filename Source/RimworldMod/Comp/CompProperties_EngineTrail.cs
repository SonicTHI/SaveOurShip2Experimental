using System;
using Verse;

namespace RimWorld
{
	public class CompProperties_EngineTrail : CompProperties
	{
		public int thrust = 0;
		public int fuelUse = 0;
		public bool energy = false;
		public CompProperties_EngineTrail()
		{
			this.compClass = typeof(CompEngineTrail);
		}
	}
}

