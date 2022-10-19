using System;
using Verse;

namespace RimWorld
{
	public class CompProperties_EngineTrailEnergy : CompProperties
	{
		public int thrust = 0;
		public CompProperties_EngineTrailEnergy()
		{
			this.compClass = typeof(CompEngineTrailEnergy);
		}
	}
}

