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
				bool hasPlating = false;
				foreach (Thing t in vec.GetThingList(map))
				{
					if (t is Building b && b.Faction == Faction.OfPlayer)
					{
						var shipPart = b.TryGetComp<CompSoShipPart>();
						if (shipPart != null && (shipPart.Props.isPlating || (shipPart.Props.isHardpoint && def.defName.Contains("Turret"))))
						{
							hasPlating = true;
						}
						if (b.TryGetComp<CompShipSalvageBay>() != null)
							return false;
					}
				}
				if (!hasPlating)
					return new AcceptanceReport(TranslatorFormattedStringExtensions.Translate("MustPlaceOnShipHull"));
			}
			return true;
		}
	}
}