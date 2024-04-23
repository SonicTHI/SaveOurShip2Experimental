using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace SaveOurShip2
{
	class ThoughtWorker_AmArchotechSpore : ThoughtWorker
	{
		protected override ThoughtState CurrentStateInternal(Pawn p)
		{
			return p.health.hediffSet.GetFirstHediffOfDef(ResourceBank.HediffDefOf.SoSHologramArchotech) != null;
		}
	}
}
