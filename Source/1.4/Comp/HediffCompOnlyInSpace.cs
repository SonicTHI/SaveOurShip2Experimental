using SaveOurShip2;
using System;
using System.Collections.Generic;

using Verse;

namespace RimWorld
{
	class HediffCompOnlyInSpace : HediffComp
	{
		public override void CompPostTick(ref float severityAdjustment)
		{
			if (parent.pawn.Spawned && parent.pawn.Map.IsSpace())
				parent.pawn.health.RemoveHediff(parent);
		}
	}
}
