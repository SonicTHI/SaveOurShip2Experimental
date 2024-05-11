using System;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	public class PlaceWorker_ShipPlating : PlaceWorker
	{
		//not on ship hull, not under any building that blocks path
		public override AcceptanceReport AllowsPlacing(BuildableDef def, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
		{
			CellRect occupiedRect = GenAdj.OccupiedRect(loc, rot, def.Size);
			foreach (IntVec3 vec in occupiedRect)
			{
				if (vec.Fogged(map) || map.roofGrid.RoofAt(loc) == RoofDefOf.RoofRockThick)
					return false;
				foreach (Thing t in vec.GetThingList(map))
				{
					if (t is Building b)
					{
						if (b.def.passability == Traversability.Impassable || b.def.building.shipPart || b is Building_Door || b.Faction != Faction.OfPlayer || (b.TryGetComp<CompForbiddable>()?.Forbidden ?? false))
							return false;
					}
					else if (t is Blueprint_Build) //td no idea why this cant be checked for def.shipPart, etc.
					{
						return false;
					}
				}
			}
			return true;
		}
	}
}