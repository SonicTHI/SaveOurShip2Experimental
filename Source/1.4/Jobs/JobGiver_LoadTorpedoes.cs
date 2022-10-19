using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace RimWorld
{
    public class JobGiver_LoadTorpedoes : ThinkNode_JobGiver
    {

        public float maxDistFromPoint = -1f;

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            JobGiver_LoadTorpedoes obj = (JobGiver_LoadTorpedoes)base.DeepCopy(resolve);
            obj.maxDistFromPoint = maxDistFromPoint;
            return obj;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            Predicate<Thing> validator = delegate (Thing t)
            {
                if (t is Building_ShipTurretTorpedo torp && !torp.torpComp.FullyLoaded)
                {
                    if (!pawn.CanReserve(t))
                    {
                        return false;
                    }
                    return true;
                }
                return false;
            };
            Predicate<Thing> otherValidator = delegate (Thing t)
            {
                return t.def.IsWithinCategory(ThingCategoryDef.Named("SpaceTorpedoes")) && pawn.CanReserve(t);
            };
            Thing thing = GenClosest.ClosestThingReachable(GetRoot(pawn), pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial), PathEndMode.Touch, TraverseParms.For(pawn), maxDistFromPoint, validator);
            Thing otherThing = GenClosest.ClosestThingReachable(GetRoot(pawn), pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.HaulableAlways), PathEndMode.Touch, TraverseParms.For(pawn), maxDistFromPoint, otherValidator);
            if (thing != null && otherThing != null)
            {
                Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("LoadTorpedoTube"), thing, otherThing);
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
