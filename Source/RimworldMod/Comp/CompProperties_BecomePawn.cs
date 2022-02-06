using System;
using Verse;

namespace RimWorld
{
	public class CompProperties_BecomePawn : CompProperties
	{
		public PawnKindDef pawnDef;

		public CompProperties_BecomePawn ()
		{
			this.compClass = typeof(CompBecomePawn);
		}
	}
}

