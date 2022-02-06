using System;
using Verse;

namespace RimWorld
{
	public class PlaceWorker_InsideShipRoom : PlaceWorker
	{
		public override AcceptanceReport AllowsPlacing(BuildableDef def, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
		{
			Room room = loc.GetRoom(map);
			if (room != null && !room.TouchesMapEdge)
			{
				foreach (IntVec3 vec in room.BorderCells)
				{
					bool hasShipPart = false;
					foreach (Thing t in vec.GetThingList(map))
					{
						if (t is Building)
						{
							Building b = t as Building;
							if (b.def.building.shipPart)
								hasShipPart = true;
						}
					}
					if (!hasShipPart)
						return new AcceptanceReport(TranslatorFormattedStringExtensions.Translate("MustPlaceInsideShipFramework"));
				}
				return true;
			}
			return new AcceptanceReport(TranslatorFormattedStringExtensions.Translate("MustPlaceInsideShipFramework"));
		}
	}
}