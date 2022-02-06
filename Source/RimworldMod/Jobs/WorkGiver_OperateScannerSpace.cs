using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace RimWorld
{
    public class WorkGiver_OperateScannerSpace : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
                return ThingRequest.ForDef(ThingDef.Named("ShipPilotSeat"));
            }
        }

        public override PathEndMode PathEndMode
        {
            get
            {
                return PathEndMode.InteractionCell;
            }
        }

        public override Danger MaxPathDanger(Pawn pawn)
        {
            return Danger.Deadly;
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.Map.listerBuildings.AllBuildingsColonistOfDef(ThingDef.Named("ShipPilotSeat")).Cast<Thing>();
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            return false;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t.Faction != pawn.Faction)
            {
                return false;
            }
            Building building = t as Building;
            if (building == null)
            {
                return false;
            }
            if (building.IsForbidden(pawn))
            {
                return false;
            }
            LocalTargetInfo target = building;
            if (!pawn.CanReserve(target, 1, -1, null, forced))
            {
                return false;
            }
            CompLongRangeMineralScannerSpace compLongRangeMineralScanner = building.TryGetComp<CompLongRangeMineralScannerSpace>();
            return compLongRangeMineralScanner.CanUseNow && !building.IsBurning();
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return new Job(DefDatabase<JobDef>.GetNamed("OperateScannerSpace"), t, 1500, true);
        }
    }
}