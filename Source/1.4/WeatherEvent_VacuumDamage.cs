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
            List<Pawn> allPawns = this.map.mapPawns.AllPawnsSpawned;
            foreach (Pawn pawn in allPawns)
            {
                PawnSpaceModifiers pawnSpaceModifiers = ShipInteriorMod2.GetPawnSpaceModifiersModifiers(pawn);
                if (pawnSpaceModifiers.CanSurviveVacuum)
                {
                    continue;
                }

                Room room = pawn.Position.GetRoom(this.map);
                bool exposedToOutside = ShipInteriorMod2.ExposedToOutside(room);

                if (ShipInteriorMod2.ExposedToOutside(room))
                {
                    if (this.ActivateSpaceBubble(pawn))
                    {
                        continue;
                    }

                    this.RunFromVacuum(pawn);
                    WeatherEvent_VacuumDamage.DoPawnDecompressionDamage(pawn, pawnSpaceModifiers);
                    WeatherEvent_VacuumDamage.DoPawnHypoxiaDamage(pawn, pawnSpaceModifiers, 0.025f);
                }
                else if (!this.map.GetComponent<ShipHeatMapComp>().VecHasLS(pawn.Position)) // in ship, no air
                {
                    if (this.ActivateSpaceBubble(pawn))
                    {
                        continue;
                    }

                    WeatherEvent_VacuumDamage.DoPawnHypoxiaDamage(pawn, pawnSpaceModifiers);
                }
            }
        }

        public bool ActivateSpaceBubble(Pawn pawn)
        {
            return pawn.apparel?.AllApparelVerbs?.First(v => v is Verb_SpaceBubblePop)?.TryStartCastOn(pawn, false, true) ?? false;
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

        public static void DoPawnHypoxiaDamage(Pawn pawn, PawnSpaceModifiers pawnSpaceModifiers, float severity = 0.0125f, float extraFactor = 1.0f)
        {
            Log.Message("DoPawnHypoxiaDamage - Pawn ID: " + pawn.thingIDNumber);
            float pawnResistance = pawnSpaceModifiers?.HypoxiaResistance ?? 0.0f;
            float serevityMultiplier = Mathf.Max(1.0f - pawnResistance, 0.0f);
            float severityOffset = severity * serevityMultiplier * extraFactor;
            if ((double)severityOffset == 0.0)
            {
                return;
            }

            float randomMultiplier = Mathf.Lerp(0.85f, 1.15f, Rand.ValueSeeded(pawn.thingIDNumber ^ 74374237)); // Add a variation between -15% and +15%
            float randomizedSeverityOffset = severityOffset * randomMultiplier;
            HealthUtility.AdjustSeverity(pawn, ResourceBank.HediffDefOf.SpaceHypoxia, randomizedSeverityOffset);
        }

        public static void DoPawnDecompressionDamage(Pawn pawn, PawnSpaceModifiers pawnSpaceModifiers, float severity = 1.0f, float extraFactor = 1.0f)
        {
            Log.Message("DoPawnDecompressionDamage - Pawn ID: " + pawn.thingIDNumber);
            float pawnResistance = pawnSpaceModifiers?.DecompressionResistance ?? 0.0f;
            float serevityMultiplier = Mathf.Max(1.0f - pawnResistance, 0.0f);
            float severityOffset = severity * serevityMultiplier * extraFactor;
            if ((double)severityOffset == 0.0)
            {
                return;
            }

            float randomMultiplier = Mathf.Lerp(0.85f, 1.15f, Rand.ValueSeeded(pawn.thingIDNumber ^ 74374237)); // Add a variation between -15% and +15%
            float randomizedSeverityOffset = severityOffset * randomMultiplier;
            pawn.TakeDamage(new DamageInfo(DefDatabase<DamageDef>.GetNamed("VacuumDamage"), randomizedSeverityOffset));
        }

        public static bool IsAffectedByVacuumDamage(Pawn pawn)
        {
            if (pawn.Dead)
            {
                return false;
            }

            if (pawn.InBed() && pawn.CurrentBed() is Building_SpaceCrib crib)
            {
                crib.UpdateState(true);
                return false;
            }

            return true;
        }
    }
}
