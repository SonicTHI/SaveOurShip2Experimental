using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace RimWorld
{
    public class WorkGiver_Warden_SacrificeToArchotech : WorkGiver_Warden
	{
		private static string IncapableOfViolenceLowerTrans;

		public override PathEndMode PathEndMode => PathEndMode.Touch;

		public static void ResetStaticData()
		{
			IncapableOfViolenceLowerTrans = "IncapableOfViolenceLower".Translate();
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (!ShouldTakeCareOfPrisoner(pawn, t, forced))
			{
				return null;
			}
			if (((Pawn)t).guest.interactionMode != DefDatabase<PrisonerInteractionModeDef>.GetNamed("SacrificeToArchotech") || !pawn.CanReserve(t))
			{
				return null;
			}
			List<Thing> spores = pawn.Map.listerThings.ThingsOfDef(ResourceBank.ThingDefOf.ShipArchotechSpore);
			if (spores.NullOrEmpty())
				return null;
            Thing spore = null;
            foreach (Thing thing in spores)
			{
				if (pawn.CanReserve(thing))
				{
					spore = thing;
					break;
				}
            }
            if (spore == null)
                return null;
            if (pawn.WorkTagIsDisabled(WorkTags.Violent))
			{
				JobFailReason.Is(IncapableOfViolenceLowerTrans);
				return null;
			}
			return JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("SacrificeToArchotech"), t, spore);
		}
	}
}
