using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace SaveOurShip2
{
	public class WorkGiver_OperateScannerSpace : WorkGiver_Scanner
	{
		public override ThingRequest PotentialWorkThingRequest
		{
			get
			{
				return ThingRequest.ForDef(ResourceBank.ThingDefOf.ShipConsoleScience);
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
			return pawn.Map.listerBuildings.AllBuildingsColonistOfDef(ResourceBank.ThingDefOf.ShipConsoleScience).Cast<Thing>();
		}
		public override bool ShouldSkip(Pawn pawn, bool forced = false)
		{
			return false;
		}
		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (t.Faction != pawn.Faction || pawn.WorkTypeIsDisabled(WorkTypeDefOf.Research))
			{
				return false;
			}
			Building building = t as Building;
			return building != null && !building.IsForbidden(pawn) && pawn.CanReserve(building, 1, -1, null, forced) && building.TryGetComp<CompShipScanner>().CanUseNow && !building.IsBurning();
		}
		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			return new Job(DefDatabase<JobDef>.GetNamed("OperateScannerSpace"), t, 1500, true);
		}
	}
}