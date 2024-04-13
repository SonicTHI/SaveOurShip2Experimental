using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace SaveOurShip2
{
	class ThoughtWorker_IsArchotechSpore : ThoughtWorker
	{
		protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn other)
		{
			return other.health.hediffSet.GetFirstHediffOfDef(ResourceBank.HediffDefOf.SoSHologramArchotech)!=null;
		}
	}
}
