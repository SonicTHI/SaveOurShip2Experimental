using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimWorld
{
    public class PlaceWorker_SolarShip : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing)
        {
            Map currentMap = Find.CurrentMap;
            IntVec3 loc2 = center + IntVec3.South.RotatedBy(rot);
            IntVec3 loc3 = center + (IntVec3.South.RotatedBy(rot) * 2);
            IntVec3 loc4 = center + (IntVec3.South.RotatedBy(rot) * 3);
            GenDraw.DrawFieldEdges(new List<IntVec3>()
            {
            loc2,loc3,loc4
            }, GenTemperature.ColorSpotHot);

        }

        public override AcceptanceReport AllowsPlacing(BuildableDef def, IntVec3 center, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            for (int i = 1; i < 7; i++)
            {
                IntVec3 loc2 = center + (IntVec3.South.RotatedBy(rot) * i);
                if (i < 4 && (loc2.Impassable(map) || !loc2.InBounds(map)))
                    return (AcceptanceReport)TranslatorFormattedStringExtensions.Translate("MustPlaceSolarShipWithFreeSpaces");
                Building b = loc2.GetFirstBuilding(Find.CurrentMap);
                if (b != null && (b.def.defName.Equals("ShipInside_PassiveCooler") || b.def.defName.Equals("ShipInside_PassiveCoolerAdvanced") || b.def.defName.Equals("ShipInside_SolarGenerator")) && b.Rotation == rot.Opposite)
                    return (AcceptanceReport)TranslatorFormattedStringExtensions.Translate("MustPlaceCoolerWithFreeSpaces");
            }
            return (AcceptanceReport)true;
        }
    }
}
