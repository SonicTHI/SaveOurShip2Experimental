using System;
using UnityEngine;
using Verse;
using Verse.Sound;
using System.Collections.Generic;
using System.Linq;
using SaveOurShip2;
using Verse.AI;

namespace RimWorld
{
    public class WeatherEvent_VacuumDamage : WeatherEvent
    {
        public override bool Expired
        {
            get
            {
                return true;
            }
        }
        public WeatherEvent_VacuumDamage(Map map) : base(map)
        {
        }
        public override void WeatherEventTick()
        {
        }

        public override void FireEvent()
        {
            List<Pawn> allPawns = map.mapPawns.AllPawnsSpawned.Where(p => !p.Dead).ToList();
            foreach (Pawn pawn in allPawns)
            {
                CachedPawnSpaceModifiers pawnSpaceModifiers = ShipInteriorMod2.GetPawnSpaceModifiersModifiers(pawn);
                if (pawnSpaceModifiers.CanSurviveVacuum)
                {
                    continue;
                }

                Room room = pawn.Position.GetRoom(map);

                if (ShipInteriorMod2.ExposedToOutside(room))
                {
                    if (ActivateSpaceBubble(pawn))
                    {
                        continue;
                    }

                    DoPawnDecompressionDamage(pawn, pawnSpaceModifiers);
                    DoPawnHypoxiaDamage(pawn, pawnSpaceModifiers, 0.025f);
                    RunFromVacuum(pawn); // Spam logs ...
                }
                else if (!map.GetComponent<ShipHeatMapComp>().VecHasLS(pawn.Position)) // in ship, no air
                {
                    if (ActivateSpaceBubble(pawn))
                    {
                        continue;
                    }

                    DoPawnHypoxiaDamage(pawn, pawnSpaceModifiers);
                }
            }
        }

        public bool ActivateSpaceBubble(Pawn pawn)
        {
            Verb verb = pawn?.apparel?.AllApparelVerbs?.FirstOrDefault(a => a is Verb_SpaceBubblePop);
            if (!verb?.Available() ?? true)
            {
                return false;
            }
            verb.TryStartCastOn(pawn);
            return true;
        }

        public void RunFromVacuum(Pawn pawn)
        {
            //find first nonvac area and run to it - enemy only
            var mapComp = pawn.Map.GetComponent<ShipHeatMapComp>();
            if (pawn.Faction != Faction.OfPlayer && !pawn.Downed && pawn.CurJob.def != DefDatabase<JobDef>.GetNamed("FleeVacuum"))
            {
                Predicate<Thing> otherValidator = delegate (Thing t)
                {
                    return t is Building_ShipAirlock && !((Building_ShipAirlock)t).Outerdoor();
                };
                Thing b = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(ResourceBank.ThingDefOf.ShipAirlock), PathEndMode.Touch, TraverseParms.For(pawn), 99f, otherValidator);
                Job Flee = new Job(DefDatabase<JobDef>.GetNamed("FleeVacuum"), b);
                pawn.jobs.StartJob(Flee, JobCondition.InterruptForced);
            }
        }

        public static void DoPawnHypoxiaDamage(Pawn pawn, CachedPawnSpaceModifiers pawnSpaceModifiers, float severity = 0.0125f, float extraFactor = 1.0f)
        {
            float pawnResistance = pawnSpaceModifiers?.HypoxiaResistance ?? 0.0f;
            float serevityMultiplier = Mathf.Max(1.0f - pawnResistance, 0.0f);
            float severityOffset = severity * serevityMultiplier * extraFactor;
            if (severityOffset >= 0.0f)
            {
                HealthUtility.AdjustSeverity(pawn, ResourceBank.HediffDefOf.SpaceHypoxia, severityOffset);
            }

        }

        public static void DoPawnDecompressionDamage(Pawn pawn, CachedPawnSpaceModifiers pawnSpaceModifiers, float severity = 1.0f, float extraFactor = 1.0f)
        {
            float pawnResistance = pawnSpaceModifiers?.DecompressionResistance ?? 0.0f;
            float serevityMultiplier = Mathf.Max(1.0f - pawnResistance, 0.0f);
            float severityOffset = severity * serevityMultiplier * extraFactor;
            if (severityOffset >= 0.0f)
            {
                pawn.TakeDamage(new DamageInfo(DefDatabase<DamageDef>.GetNamed("VacuumDamage"), severityOffset));
            }

        }
    }
}
