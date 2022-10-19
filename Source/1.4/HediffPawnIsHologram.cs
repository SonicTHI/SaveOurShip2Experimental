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
        public ThingWithComps relay;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Building>(ref consciousnessSource, "consciousnessSource");
            Scribe_References.Look<ThingWithComps>(ref relay, "relay");
        }

        public override void Notify_PawnKilled()
        {
            base.Notify_PawnKilled();
            consciousnessSource.TryGetComp<CompBuildingConsciousness>().HologramDestroyed(true);
            if(relay!=null)
                relay.TryGetComp<CompHologramRelay>().StopRelaying(false);
        }

        public override void Tick()
        {
            base.Tick();
            if(Find.TickManager.TicksGame % 1000 == 0)
            {
                IEnumerable<Hediff> missingBits = pawn.health.hediffSet.GetHediffs<Hediff_MissingPart>();
                foreach(Hediff missingBit in missingBits)
                {
                    BodyPartRecord part = missingBit.Part;
                    pawn.health.RemoveHediff(missingBit);
                    Hediff wound = HediffMaker.MakeHediff(HediffDefOf.Bruise, pawn, part);
                    wound.Severity = part.def.GetMaxHealth(pawn) - 1;
                    pawn.health.AddHediff(wound, part);
                }
                IEnumerable<Hediff_Injury> injuries = pawn.health.hediffSet.GetHediffs<Hediff_Injury>();
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

                if (pawn.Map != consciousnessSource.Map && relay==null && pawn.CarriedBy==null && !pawn.InContainerEnclosed && !pawn.IsPrisoner)
                    consciousnessSource.TryGetComp<CompBuildingConsciousness>().HologramDestroyed(false);
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            List<Gizmo> gizmos = new List<Gizmo>();
            if(pawn.equipment.Primary==null || pawn.equipment.Primary.def.IsRangedWeapon)
            {
                gizmos.Add(new Command_Action
                {
                    action = delegate
                    {
                        consciousnessSource.TryGetComp<CompBuildingConsciousness>().SwitchToMelee();
                    },
                    defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipInsideEquipMelee"),
                    defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipInsideEquipMeleeDesc"),
                    icon = ContentFinder<Texture2D>.Get("Things/Item/Equipment/WeaponMelee/LongSword")
                });
            }
            else
            {
                gizmos.Add(new Command_Action
                {
                    action = delegate
                    {
                        consciousnessSource.TryGetComp<CompBuildingConsciousness>().SwitchToRanged();
                    },
                    defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipInsideEquipRanged"),
                    defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipInsideEquipRangedDesc"),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/FireAtWill")
                });
            }
            return gizmos;
        }
    }
}
