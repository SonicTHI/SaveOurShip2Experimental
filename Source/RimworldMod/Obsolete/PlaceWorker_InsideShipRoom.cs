using System;
using System.Collections.Generic;
using Verse;

namespace RimWorld
{
	public class PlaceWorker_InsideShipRoom : PlaceWorker
	{
		static int lastCheckedTick = -1;
		static Dictionary<Room, bool> roomsChecked = new Dictionary<Room, bool>();

		public override AcceptanceReport AllowsPlacing(BuildableDef def, IntVec3 center, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
		{
			Room room = center.GetRoom(map);
			if (room != null && !room.TouchesMapEdge)
			{
				if(Find.TickManager.TicksGame - lastCheckedTick <= 60)
                {
					if (roomsChecked.ContainsKey(room) && roomsChecked[room])
						return true;
                }
				else
                {
					roomsChecked = new Dictionary<Room, bool>();
                }
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
				roomsChecked.Add(room, true);
				lastCheckedTick = Find.TickManager.TicksGame;
				return true;
			}
			return new AcceptanceReport(TranslatorFormattedStringExtensions.Translate("MustPlaceInsideShipFramework"));
		}
	}
}