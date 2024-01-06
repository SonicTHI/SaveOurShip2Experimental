using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimWorld
{
    class HediffPawnIsHologram : HediffWithComps
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
                    pawn.health.hediffSet.GetMissingPartsCommonAncestors().ToList().ForEach(hediff => HealthUtility.Cure(hediff.Part, pawn));
                    CureableHediffs().ToList().ForEach(hediff => HealthUtility.Cure(hediff));
                }
                catch (Exception e)
                {
                    Log.Error("Error removing hediffs from formgel: " + e.StackTrace);
                }
            }
        }

        public IEnumerable<Hediff> CureableHediffs()
        {
            return pawn.health.hediffSet.hediffs.Where(hediff => hediff.IsPermanent() || hediff.def.chronic || hediff.def.makesSickThought);
        }

        public override void PostRemoved()
        {
            base.PostRemoved();
            if (!SafeRemoveFlag)
                Log.Error("Formgel hediff removed from pawn " + pawn.Name + " in an unsafe manner. Please submit your log file to the SoS2 developers as a bug report.");
        }
    }
}
