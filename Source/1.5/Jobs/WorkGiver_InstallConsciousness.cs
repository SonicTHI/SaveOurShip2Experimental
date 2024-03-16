using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;

namespace RimWorld
{
    class WorkGiver_InstallConsciousness : WorkGiver_Scanner //Also used for installing resurrector serum into afterlife caskets, to bring back a pawn's body
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);

        public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            CompBuildingConsciousness consc = t.TryGetComp<CompBuildingConsciousness>();
            if (consc == null)
                return false;
            return ((consc.WhichPawn != null && consc.Consciousness==null && ((consc.Props.mustBeDead && ((Pawn)consc.WhichPawn).Dead) || (!consc.Props.mustBeDead && pawn==consc.WhichPawn) || consc.WhichPawn.def==ThingDefOf.AIPersonaCore)) || (consc.RezPlz != null)) && pawn.CanReserveAndReach(t,PathEndMode.ClosestTouch,Danger.Deadly);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            CompBuildingConsciousness consc = t.TryGetComp<CompBuildingConsciousness>();
            if (consc == null)
                return null;

            if (consc.RezPlz != null)
            {
                Job val2 = new Job(DefDatabase<JobDef>.GetNamed("ResurrectHologram"), t, consc.RezPlz);
                val2.count = 1;
                return val2;
            }

            if (consc.Props.mustBeDead)
            {
                Job val2 = new Job(DefDatabase<JobDef>.GetNamed("InstallConsciousness"), t, ((Pawn)consc.WhichPawn).Corpse);
                val2.count = 1;
                return (Job)(object)val2;
            }
            else if(consc.WhichPawn.def == ThingDefOf.AIPersonaCore)
            {
                Job val2 = new Job(DefDatabase<JobDef>.GetNamed("InstallConsciousness"), t, consc.WhichPawn);
                val2.count = 1;
                return (Job)(object)val2;
            }
            else
            {
                return new Job(DefDatabase<JobDef>.GetNamed("MergeWithSpore"), t, pawn);
            }
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            var mapComp = pawn.Map.GetComponent<ShipHeatMapComp>();
            List<Thing> parents = new List<Thing>();
            foreach (CompBuildingConsciousness consc in mapComp.Spores)
                parents.Add(consc.parent);
            return parents;
        }
    }
}
