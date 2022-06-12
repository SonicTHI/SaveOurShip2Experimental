using System;
using Verse;

namespace RimWorld
{
	public class CompProperties_SoSshipPart : CompProperties
	{
		public bool hull = false;

		public CompProperties_SoSshipPart()
		{
			compClass = typeof(CompSoSshipPart);
		}
	}
}
