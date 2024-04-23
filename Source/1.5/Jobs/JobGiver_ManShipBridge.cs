using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using RimWorld;

namespace SaveOurShip2
{
	public class JobGiver_ManShipBridge : ThinkNode_JobGiver
	{

		public float maxDistFromPoint = -1f;

		public override ThinkNode DeepCopy(bool resolve = true)
		{
			JobGiver_ManShipBridge obj = (JobGiver_ManShipBridge)base.DeepCopy(resolve);
			obj.maxDistFromPoint = maxDistFromPoint;
			return obj;
		}

		protected override Job TryGiveJob(Pawn pawn)
		{
			Predicate<Thing> validator = delegate (Thing t)
			{
				if (t.def.hasInteractionCell && t.def.HasComp(typeof(CompMannable)) && t.Faction == pawn.Faction && pawn.CanReserve(t))
					return true;
				return false;
			};
			Thing thing = GenClosest.ClosestThingReachable(GetRoot(pawn), pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial), PathEndMode.InteractionCell, TraverseParms.For(pawn), maxDistFromPoint, validator);
			if (thing != null)
			{
				Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("ManShipBridge"), thing);
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
