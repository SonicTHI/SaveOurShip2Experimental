using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimWorld
{
    class HediffPawnIsHologram : Hediff
    {
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
            if(Find.TickManager.TicksGame % 1000 == 0)
            {
                IEnumerable<Hediff> missingBits = GetHediffs<Hediff_MissingPart>();
                foreach(Hediff missingBit in missingBits)
                {
                    BodyPartRecord part = missingBit.Part;
                    pawn.health.RemoveHediff(missingBit);
                    Hediff wound = HediffMaker.MakeHediff(HediffDefOf.Bruise, pawn, part);
                    wound.Severity = part.def.GetMaxHealth(pawn) - 1;
                    pawn.health.AddHediff(wound, part);
                }
                IEnumerable<Hediff_Injury> injuries = GetHediffs<Hediff_Injury>();
                foreach(Hediff injury in injuries)
                {
                    if (injury.IsPermanent())
                        pawn.health.RemoveHediff(injury);
                }
                IEnumerable<Hediff> diseases = pawn.health.hediffSet.hediffs.Where(hediff => hediff.def.makesSickThought || hediff.def.chronic);
                foreach(Hediff disease in diseases)
                {
                    pawn.health.RemoveHediff(disease);
                }

                if (pawn.Map != consciousnessSource.Map && pawn.CarriedBy==null && !pawn.InContainerEnclosed && !pawn.IsPrisoner)
                    consciousnessSource.TryGetComp<CompBuildingConsciousness>().HologramDestroyed(false);
            }
        }
        public IEnumerable<T> GetHediffs<T>() where T : Hediff //1.4 more removed shit, thx TY
        {
            int num;
            for (int i = 0; i < pawn.health.hediffSet.hediffs.Count; i = num)
            {
                T t = pawn.health.hediffSet.hediffs[i] as T;
                if (t != null)
                {
                    yield return t;
                }
                num = i + 1;
            }
            yield break;
        }
    }
}
