using System;
using Verse;

namespace RimWorld
{
	public class CompProperties_SoSUnlock : CompProperties
	{
		public string unlock;

		public CompProperties_SoSUnlock()
		{
			this.compClass = typeof(CompSoSUnlock);
		}
	}
}
