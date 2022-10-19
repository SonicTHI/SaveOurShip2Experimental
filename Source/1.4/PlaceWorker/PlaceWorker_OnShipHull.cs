using System;
using Verse;
using SaveOurShip2;

namespace RimWorld
{
	public class PlaceWorker_OnShipHull : PlaceWorker
	{
		public override AcceptanceReport AllowsPlacing(BuildableDef def, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
		{
			CellRect occupiedRect = GenAdj.OccupiedRect(loc, rot, def.Size);
			foreach (IntVec3 vec in occupiedRect)
			{
				bool hasShipPart = false;
				foreach (Thing t in vec.GetThingList(map))
				{
					if (t is Building b)
					{
						var shipPart = b.TryGetComp<CompSoShipPart>();
						if (shipPart != null && shipPart.Props.isPlating)
						{
							hasShipPart = true;
							break;
						}
					}
				}
				if (!hasShipPart)
					return new AcceptanceReport(TranslatorFormattedStringExtensions.Translate("MustPlaceOnShipHull"));
			}
			//special check for bays
			if (def.defName.Equals("ShipSalvageBay") || def.defName.Equals("ShipShuttleBay") || def.defName.Equals("ShipShuttleBayLarge"))
			{
				occupiedRect = new CellRect(loc.x, loc.z, 1, 1).ExpandedBy(2);
				foreach (IntVec3 vec in occupiedRect)
				{
					if (vec.Impassable(map))
						return false;
					foreach (Thing b in vec.GetThingList(map))
					{
						if (b.def.defName.Equals("ShipShuttleBay") || b.def.defName.Equals("ShipSalvageBay") || b.def.defName.Equals("ShipShuttleBayLarge") || b.def.passability == Traversability.PassThroughOnly || b.def.IsBlueprint)
							return false;
					}
				}
			}
			return true;
		}
	}
}