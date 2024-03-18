using System;
using System.Collections.Generic;
using System.Linq;
using SaveOurShip2;
using Verse;

namespace RimWorld
{
	class HediffPawnIsHologram : Hediff
	{
		public static bool SafeRemoveFlag = false;

		public Building consciousnessSource;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_References.Look<Building>(ref consciousnessSource, "consciousnessSource");
		}

		public override void Notify_PawnKilled()
		{
			base.Notify_PawnKilled();
			consciousnessSource.TryGetComp<CompBuildingConsciousness>().HologramDestroyed(true);
		}

		public override void Tick()
		{
			base.Tick();
			if (Find.TickManager.TicksGame % 1000 == 0)
			{
				try
				{
					MissingParts().ForEach(hediff => HealMissingPart(hediff.Part));
					CureableHediffs().ForEach(hediff => HealthUtility.Cure(hediff));
				}
				catch (Exception e)
				{
					Log.Error("Error removing hediffs from formgel: " + e.StackTrace);
				}
			}
		}

		public void HealMissingPart(BodyPartRecord part)
		{
			HealthUtility.Cure(part, pawn);
			Hediff_Injury wound = HediffMaker.MakeHediff(HediffDef.Named("Bruise"), pawn, part) as Hediff_Injury;
			wound.Severity = part.def.GetMaxHealth(pawn) - 1;
			pawn.health.AddHediff(wound, part);
		}

		public List<Hediff> MissingParts()
		{
			return pawn.health.hediffSet.hediffs.Where(hediff => hediff is Hediff_MissingPart).ToList();
		}

		public List<Hediff> CureableHediffs()
		{
			return pawn.health.hediffSet.hediffs.Where(hediff => hediff.IsPermanent() || hediff.def.chronic || hediff.def.makesSickThought).ToList();
		}

		public override void PostRemoved()
		{
			base.PostRemoved();

			if (!SafeRemoveFlag)
				Log.Error("Formgel hediff removed from pawn " + pawn.Name + " in an unsafe manner. Please submit your log file to the SoS2 developers as a bug report.");
		}
	}
}
