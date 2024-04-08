using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimWorld
{
	class ThoughtWorker_AmArchotechSpore : ThoughtWorker
	{
		protected override ThoughtState CurrentStateInternal(Pawn p)
		{
			return p.health.hediffSet.GetFirstHediffOfDef(ResourceBank.HediffDefOf.SoSHologramArchotech) != null;
		}
	}
}
