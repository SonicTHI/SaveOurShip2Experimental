using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using RimWorld;

namespace SaveOurShip2
{
	public class JobGiver_LoadShipBuilding : ThinkNode_JobGiver
	{
		public float maxDistFromPoint = -1f;

		public override ThinkNode DeepCopy(bool resolve = true)
		{
			JobGiver_LoadShipBuilding obj = (JobGiver_LoadShipBuilding)base.DeepCopy(resolve);
			obj.maxDistFromPoint = maxDistFromPoint;
			return obj;
		}
		//looks for buildings that need fuel, checks if fuel is available, start refuel job
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
			{
				return null;
			}
			CompRefuelable refuelComp = null;
			Predicate<Thing> validator = delegate (Thing t)
			{
				if (t is Building b)
				{
					refuelComp = b.TryGetComp<CompRefuelable>();
					if (refuelComp == null || refuelComp.FuelPercentOfMax > 0.8f)
						return false;
					if (pawn.CanReserve(t))
					{
						return true;
					}
				}
				return false;
			};
			Predicate<Thing> otherValidator = delegate (Thing t)
			{
				return refuelComp != null && refuelComp.Props.fuelFilter.AllowedThingDefs.Contains(t.def) && pawn.CanReserve(t);
			};
			Thing thing = GenClosest.ClosestThingReachable(GetRoot(pawn), pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial), PathEndMode.Touch, TraverseParms.For(pawn), maxDistFromPoint, validator);
			Thing fuel = GenClosest.ClosestThingReachable(GetRoot(pawn), pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.HaulableAlways), PathEndMode.Touch, TraverseParms.For(pawn), maxDistFromPoint, otherValidator);
			if (thing != null && fuel != null)
			{
				Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("LoadShipBuilding"), thing, fuel);
				job.expiryInterval = 2000;
				job.checkOverrideOnExpire = true;
				return job;
			}
			return null;
		}

		protected IntVec3 GetRoot(Pawn pawn)
		{
			return pawn.Position;
		}
	}
}
