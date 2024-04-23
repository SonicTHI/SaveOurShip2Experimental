using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SaveOurShip2
{
	public class PlaceWorker_ShipHeatConduit : PlaceWorker
	{
		public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
		{
			List<Thing> thingList = loc.GetThingList(map);
			for (int i = 0; i < thingList.Count; i++)
			{
				if (thingList[i].TryGetComp<CompShipHeat>() != null)
				{
					return false;
				}
				if (thingList[i].def.entityDefToBuild != null)
				{
					ThingDef thingDef = thingList[i].def.entityDefToBuild as ThingDef;
					if (thingDef != null && thingDef.comps.Any(c=>c.compClass.IsAssignableFrom(typeof(CompShipHeat))))
					{
						return false;
					}
				}
			}
			return true;
		}
	}
}

