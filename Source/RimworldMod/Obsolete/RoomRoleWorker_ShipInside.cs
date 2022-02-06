using System;
using System.Collections.Generic;
using Verse;

namespace RimWorld
{
	public class RoomRoleWorker_ShipInside : RoomRoleWorker
	{
		public override float GetScore(Room room)
		{
			if (room.OpenRoofCount > 0)
				return 0f;
			foreach (IntVec3 current in room.BorderCells)
			{
				bool hasShipPart = false;
				foreach (Thing aThing in current.GetThingList (room.Map))
				{
					if (aThing is Building)
					{
						Building theThing = aThing as Building;
						if (theThing.def.building.shipPart)
							hasShipPart = true;
					}
				}
				if (!hasShipPart)
					return 0f;
			}
			foreach (IntVec3 tile in room.Cells)
			{
				if (!tile.GetRoof(room.Map).defName.Equals("RoofShip"))
					return 0f;
			}
			return float.MaxValue;
		}
	}
}