using System;
using System.Collections.Generic;
using Verse;

namespace RimWorld
{
	public class RoomRoleWorker_ShipFramework : RoomRoleWorker
	{
		public override float GetScore(Room room)
		{
			foreach (IntVec3 current in room.BorderCells) {
				Building edifice = current.GetEdifice(room.Map);
                if (edifice == null || !(edifice.def.building.shipPart))
                {
                    bool hasEdificeOnHull=false;
                    foreach (Thing aThing in current.GetThingList(room.Map))
                    {
                        if (aThing is Building)
                        {
                            Building theThing = aThing as Building;
                            if (theThing.def.building.shipPart)
                            {
                                hasEdificeOnHull = true;
                            }
                        }
                    }
                    if (!hasEdificeOnHull)
                    {
                        return 0f;
                    }
                }
			}
            
			return float.MaxValue/2;
		}
	}
}
