using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld
{
    class PlaceWorker_ShipEngine : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            Building engineprev = map.listerBuildings.allBuildingsColonist.Where(x => x.TryGetComp<CompEngineTrail>() != null).FirstOrDefault();
            if (engineprev == null || (engineprev != null && engineprev.Rotation == rot))
                return AcceptanceReport.WasAccepted;
            else
                return AcceptanceReport.WasRejected;
        }
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            List<IntVec3> rect = GenAdj.CellsOccupiedBy(center, rot, def.size).ToList();
            int minx = 99999;
            int minz = 99999;
            foreach (IntVec3 vec in rect)
            {
                if (vec.x < minx)
                    minx = vec.x;
                if (vec.z < minz)
                    minz = vec.z;
            }
            CellRect rectToKill;
            if (def.size.z > 3)
            {
                rectToKill = new CellRect(minx, minz, rot.IsHorizontal ? def.size.z : def.size.x, rot.IsHorizontal ? def.size.x : def.size.z).MovedBy(CompEngineTrail.killOffsetL[rot.AsInt]).ExpandedBy(2);
            }
            else
            {
                rectToKill = new CellRect(minx, minz, rot.IsHorizontal ? def.size.z : def.size.x, rot.IsHorizontal ? def.size.x : def.size.z).MovedBy(CompEngineTrail.killOffset[rot.AsInt]).ExpandedBy(1);
            }
            if (rot.IsHorizontal)
                rectToKill.Width = rectToKill.Width * 2 - 3;
            else
                rectToKill.Height = rectToKill.Height * 2 - 3;
            GenDraw.DrawFieldEdges(rectToKill.Cells.ToList(), Color.red);
        }
    }
}
