using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RimWorld
{
	class JobGiver_FightMechanitesNearPoint : ThinkNode_JobGiver
	{
		public float maxDistFromPoint = -1f;

		public override ThinkNode DeepCopy(bool resolve = true)
		{
			JobGiver_FightMechanitesNearPoint obj = (JobGiver_FightMechanitesNearPoint)base.DeepCopy(resolve);
			obj.maxDistFromPoint = maxDistFromPoint;
			return obj;
		}

		protected override Job TryGiveJob(Pawn pawn)
		{
			Predicate<Thing> validator = delegate (Thing t)
			{
				if (((AttachableThing)t).parent is Pawn)
				{
					return false;
				}
				if (!pawn.CanReserve(t))
				{
					return false;
				}
				return (!pawn.WorkTagIsDisabled(WorkTags.Firefighting)) ? true : false;
			};
			Thing thing = GenClosest.ClosestThingReachable(pawn.GetLord().CurLordToil.FlagLoc, pawn.Map, ThingRequest.ForDef(ShipInteriorMod2.MechaniteFire), PathEndMode.Touch, TraverseParms.For(pawn), maxDistFromPoint, validator);
			if (thing != null)
			{
				return JobMaker.MakeJob(JobDefOf.BeatFire, thing);
			}
			return null;
		}
	}
}
