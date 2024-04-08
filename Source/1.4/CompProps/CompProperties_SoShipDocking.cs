using System;
using Verse;

namespace RimWorld
{
	public class CompProperties_SoShipDocking : CompProperties
	{
		public bool extender = false;
		public bool isPlating = false;
		public CompProperties_SoShipDocking()
		{
			this.compClass = typeof(CompSoShipDocking);
		}
	}
}

