using System;
using Verse;

namespace RimWorld
{
	public class CompProperties_SoShipPart : CompProperties
	{
		public bool isPlating = false;
		public bool isHull = false;

		public CompProperties_SoShipPart()
		{
			compClass = typeof(CompSoShipPart);
		}
	}
}
