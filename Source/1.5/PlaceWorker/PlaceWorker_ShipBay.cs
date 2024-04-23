using System;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	public class PlaceWorker_ShipBay: PlaceWorker_OnShipHull
	{
		public override AcceptanceReport AllowsPlacing(BuildableDef def, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
		{
			CellRect occupiedRect = new CellRect(loc.x - def.Size.x/2, loc.z - def.Size.z/2, def.Size.x, def.Size.z);
			foreach (IntVec3 vec in occupiedRect)
			{
				if (vec.Impassable(map))
					return false;
				foreach (Thing b in vec.GetThingList(map))
				{
					if (b.TryGetComp<CompShipSalvageBay>() != null || b.def == ResourceBank.ThingDefOf.ShipShuttleBay || b.def == ResourceBank.ThingDefOf.ShipShuttleBayLarge || b.def.passability == Traversability.PassThroughOnly || b.def.IsBlueprint)
						return false;
				}
			}
			return true;
		}
	}
}