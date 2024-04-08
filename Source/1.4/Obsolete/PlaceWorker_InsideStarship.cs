using SaveOurShip2;
using System;
using Verse;

namespace RimWorld
{
	//dep
	/*public class PlaceWorker_InsideStarship : PlaceWorker
	{
		public override AcceptanceReport AllowsPlacing(BuildableDef def, IntVec3 center, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
		{
			Room room = center.GetRoom(map);
			if (room != null && !room.TouchesMapEdge)
			{
				foreach (IntVec3 vec in room.Cells)
				{
					if (!vec.Roofed(map))
					{
						return new AcceptanceReport(TranslatorFormattedStringExtensions.Translate("MustPlaceInsideShip"));
					}
				}
				if (def == ResourceBank.ThingDefOf.ShipSalvageBay || def == ResourceBank.ThingDefOf.ShipShuttleBay)
				{
					CellRect occupiedRect = new CellRect(center.x, center.z, 1, 1).ExpandedBy(2);
					foreach (IntVec3 vec in occupiedRect)
					{
						if (vec.Impassable(map))
							return false;
						foreach (Thing b in vec.GetThingList(map))
						{
							if (b.def == ResourceBank.ThingDefOf.ShipShuttleBay || b.def == ResourceBank.ThingDefOf.ShipSalvageBay || b.def.passability == Traversability.PassThroughOnly || b.def.IsBlueprint)
								return false;
						}
					}
				}
				return true;
			}
			return new AcceptanceReport(TranslatorFormattedStringExtensions.Translate("MustPlaceInsideShip"));
		}
	}*/
}

