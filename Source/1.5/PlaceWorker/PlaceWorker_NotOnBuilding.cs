using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld
{
    class PlaceWorker_NotOnBuilding : PlaceWorker
    {
		public override AcceptanceReport AllowsPlacing(BuildableDef def, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
		{
			CellRect occupiedRect = GenAdj.OccupiedRect(loc, rot, def.Size);
			foreach (IntVec3 vec in occupiedRect)
			{
				foreach (Thing t in vec.GetThingList(map))
				{
					if (t is Building)
						return false;
				}
			}
			return true;
		}
	}
}
