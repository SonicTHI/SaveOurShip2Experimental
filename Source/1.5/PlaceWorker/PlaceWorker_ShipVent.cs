using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace SaveOurShip2
{
	public class PlaceWorker_ShipVent : PlaceWorker
	{
		public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing)
		{
			Map currentMap = Find.CurrentMap;
			IntVec3 loc1 = center + IntVec3.North.RotatedBy(rot);
			GenDraw.DrawFieldEdges(new List<IntVec3>()
			{
			loc1
			}, Color.yellow);
			Room room = loc1.GetRoom(currentMap);
			if (room == null)
				return;
			if (room.UsesOutdoorTemperature)
				return;
			GenDraw.DrawFieldEdges(room.Cells.ToList<IntVec3>(), Color.yellow);
		}

		public override AcceptanceReport AllowsPlacing(BuildableDef def, IntVec3 center, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
		{
			IntVec3 loc1 = center + IntVec3.North.RotatedBy(rot);
			if (loc1.Impassable(map))
				return (AcceptanceReport)"MustPlaceCoolerWithFreeSpaces".Translate();
			return (AcceptanceReport)true;
		}
	}
}
