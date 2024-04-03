using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace RimWorld
{
	public class JobGiver_AIHackBridge : ThinkNode_JobGiver
	{

		public float maxDistFromPoint = 99f;

		public override ThinkNode DeepCopy(bool resolve = true)
		{
			JobGiver_AIBreachAirlock obj = (JobGiver_AIBreachAirlock)base.DeepCopy(resolve);
			obj.maxDistFromPoint = maxDistFromPoint;
			return obj;
		}

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!pawn.RaceProps.Humanlike)
			{
				if (!pawn.RaceProps.IsMechanoid)
					return null;
			}
			else
			{
				if (!pawn.HostileTo(Faction.OfPlayer) || pawn.skills.GetSkill(SkillDefOf.Intellectual).TotallyDisabled || pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) == 0)
				{
					return null;
				}
			}
			Thing thing = null;
			Predicate<Thing> validator = delegate (Thing t)
			{
				if (t is Building_ShipBridge b && t.Faction != pawn.Faction && pawn.CanReserve(t))
				{
					return true;
				}
				return false;
			};
			thing = GenClosest.ClosestThingReachable(GetRoot(pawn), pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial), PathEndMode.Touch, TraverseParms.For(pawn), maxDistFromPoint, validator);
			if (thing != null)
			{
				//Log.Message("Hackjob for:" + thing);
				Job job = JobMaker.MakeJob(ResourceBank.JobDefOf.HackEnemyShip, thing);
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
