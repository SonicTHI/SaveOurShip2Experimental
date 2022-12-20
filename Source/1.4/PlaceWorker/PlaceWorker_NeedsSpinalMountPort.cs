using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld
{
    public class PlaceWorker_NeedsSpinalMountPort : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            Map currentMap = Find.CurrentMap;
            List<Building> allBuildingsColonist = currentMap.listerBuildings.allBuildingsColonist;
            for (int i = 0; i < allBuildingsColonist.Count; i++)
            {
                Building building = allBuildingsColonist[i];
                if (!Find.Selector.IsSelected(building) && building.TryGetComp<CompSpinalMount>() != null && building.TryGetComp<CompSpinalMount>().Props.emits)
                {
                    PlaceWorker_SpinalMountPort.DrawFuelingPortCell(building.Position, building.Rotation, building.def);
                }
            }

            if (!def.defName.Equals("ShipSpinalAmplifier") && !def.defName.Equals("ShipSpinalBarrelPsychic"))
            {
                CellRect rect;
                if (rot.AsByte == 0)
                    rect = new CellRect(center.x - 1, center.z + 3, 3, currentMap.Size.z - center.z - 3);
                else if (rot.AsByte == 1)
                    rect = new CellRect(center.x + 3, center.z - 1, currentMap.Size.x - center.x - 3, 3);
                else if (rot.AsByte == 2)
                    rect = new CellRect(center.x - 1, 0, 3, center.z - 2);
                else
                    rect = new CellRect(0, center.z - 1, center.x - 2, 3);
                GenDraw.DrawFieldEdges(rect.Cells.ToList(),Color.red);
            }
        }
        /*
        public override AcceptanceReport AllowsPlacing(BuildableDef def, IntVec3 center, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            bool flag = false;
            List<Building> allBuildingsColonist = map.listerBuildings.allBuildingsColonist;
            for (int i = 0; i < allBuildingsColonist.Count; i++)
            {
                Building building = allBuildingsColonist[i];
                if (building.Position==(center+new IntVec3(0,0,-1-(building.def.size.z/2)-(def.Size.z-1)/2)) && building.TryGetComp<CompSpinalMount>()!=null && building.TryGetComp<CompSpinalMount>().Props.emits)
                {
                    flag = true;
                    break;
                }
            }
            if (!flag)
            {
                return "MustPlaceNearSpinalMountPort".Translate();
            }
            return true;
        }*/
    }
}
