using System;
using Verse;
using SaveOurShip2;

namespace RimWorld
{
	public class PlaceWorker_ShipHull : PlaceWorker
	{
		//not under mountain
		public override AcceptanceReport AllowsPlacing(BuildableDef def, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
		{
			CellRect occupiedRect = GenAdj.OccupiedRect(loc, rot, def.Size);
			foreach (IntVec3 vec in occupiedRect)
			{
				if (vec.Fogged(map) || map.roofGrid.RoofAt(loc) == RoofDefOf.RoofRockThick)
					return false;
			}
			return true;
			/*
			Room room = loc.GetRoom(map);
			if (room != null)
			{
				if (room.TouchesMapEdge)// || room.IsHuge)
					return true;
				foreach (IntVec3 vec in room.BorderCells)
				{
					bool hasShipPart = false;
					foreach (Thing t in vec.GetThingList(map))
					{
						if (t is Building && ((Building)t).def.building.shipPart)
						{
							hasShipPart = true;
							break;
						}
					}
					if (!hasShipPart)
						return new AcceptanceReport(TranslatorFormattedStringExtensions.Translate("MustPlaceInsideShipFramework"));
				}
				return true;
			}
			return new AcceptanceReport(TranslatorFormattedStringExtensions.Translate("MustNotPlaceOnShipPart"));
			/*
			if (loc.GetRoom(map) == null)
			{
				return new AcceptanceReport(TranslatorFormattedStringExtensions.Translate("MustNotPlaceOnShipPart"));
			}
			/*CellRect occupiedRect = GenAdj.OccupiedRect(loc, rot, def.Size);
			foreach (IntVec3 vec in occupiedRect)
			{
				foreach (Thing t in vec.GetThingList(map))
				{
					if (t is Building && t.GetRoom() != null)
						return new AcceptanceReport(TranslatorFormattedStringExtensions.Translate("MustNotPlaceOnShipPart"));
				}
			}
			return true;*/
		}
	}
}