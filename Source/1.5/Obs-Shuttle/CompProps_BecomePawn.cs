using System;
using Verse;

namespace SaveOurShip2
{
	public class CompProps_BecomePawn : CompProperties
	{
		public PawnKindDef pawnDef;

		public CompProps_BecomePawn ()
		{
			this.compClass = typeof(CompBecomePawn);
		}
	}
}

