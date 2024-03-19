using UnityEngine;
using Verse;
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
				if (pawn.CanSurviveVacuum())
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

					RunFromVacuum(pawn);
					DoPawnDecompressionDamage(pawn);
					DoPawnHypoxiaDamage(pawn, 0.025f);
				}
				else if (!map.GetComponent<ShipHeatMapComp>().VecHasLS(pawn.Position)) // in ship, no air
				{
					if (ActivateSpaceBubble(pawn))
					{
						continue;
					}

					DoPawnHypoxiaDamage(pawn);
				}
			}
		}

		public bool ActivateSpaceBubble(Pawn pawn)
		{
			Verb verb = pawn?.apparel?.AllApparelVerbs?.FirstOrDefault(apparel => apparel is Verb_SpaceBubblePop);
			if (verb?.Available() ?? false)
			{
				verb.TryStartCastOn(pawn);
				return true;
			}
			return false;
		}

		public void RunFromVacuum(Pawn pawn)
		{
			// find first nonvac area and run to it - enemy only
			JobDef fleeVacuumDef = ResourceBank.JobDefOf.FleeVacuum;
			if (PreventPawnFleeVacuum(pawn) || pawn.CurJobDef == fleeVacuumDef)
			{
				return;
			}

			Thing closestThing = ClosestThingReachable(pawn);
			if (closestThing != null)
			{
				IntVec3 v = ((Building_ShipAirlock)closestThing).VacuumSafeSpot();
				if (v.Standable(pawn.Map))
				{
					Job fleeVacuumJob = new Job(fleeVacuumDef, closestThing);
					pawn.jobs.StartJob(fleeVacuumJob, JobCondition.InterruptForced);
				}
			}
		}

		public static void DoPawnHypoxiaDamage(Pawn pawn, float severity = 0.0125f, float extraFactor = 1.0f)
		{
			float pawnResistance = pawn.HypoxiaResistance();
			float serevityMultiplier = Mathf.Max(1.0f - pawnResistance, 0.0f);
			float severityOffset = severity * serevityMultiplier * extraFactor;
			if (severityOffset >= 0.0f)
			{
				HealthUtility.AdjustSeverity(pawn, ResourceBank.HediffDefOf.SpaceHypoxia, severityOffset);
			}

		}

		public static void DoPawnDecompressionDamage(Pawn pawn, float severity = 1.0f, float extraFactor = 1.0f)
		{
			float pawnResistance = pawn.DecompressionResistance();
			float serevityMultiplier = Mathf.Max(1.0f - pawnResistance, 0.0f);
			float severityOffset = severity * serevityMultiplier * extraFactor;
			if (severityOffset >= 0.0f)
			{
				pawn.TakeDamage(new DamageInfo(DefDatabase<DamageDef>.GetNamed("VacuumDamage"), severityOffset));
			}

		}

		private bool PreventPawnFleeVacuum(Pawn pawn)
		{
			return !pawn.Spawned || pawn.Dead || pawn.Downed || pawn.Faction == Faction.OfPlayer;
		}

		private Thing ClosestThingReachable(Pawn pawn)
		{
			bool validator(Thing thing)
			{
				return thing is Building_ShipAirlock airlock && airlock.Outerdoor() && airlock.VacuumSafeSpot() != IntVec3.Invalid;
			}
			return GenClosest.ClosestThingReachable(pawn.Position,
													pawn.Map,
													ThingRequest.ForDef(ResourceBank.ThingDefOf.ShipAirlock),
													PathEndMode.Touch,
													TraverseParms.For(pawn),
													99f,
													validator);
		}
	}
}
