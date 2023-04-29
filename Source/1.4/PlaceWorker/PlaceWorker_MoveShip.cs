using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using SaveOurShip2;

namespace RimWorld
{
    public class PlaceWorker_MoveShip : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            if (thing is ShipMoveBlueprint ship)
            {
                ship.DrawGhost(center);
            }
        }

        public override AcceptanceReport AllowsPlacing(BuildableDef def, IntVec3 center, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            if (thing is ShipMoveBlueprint ship)
            {
                bool targetMapLarger = false; //if target map is larger, allow only up to origin map size
                if (ShipInteriorMod2.shipOriginMap != null && (ShipInteriorMod2.shipOriginMap.Size.x < map.Size.x || ShipInteriorMod2.shipOriginMap.Size.z < map.Size.z))
                {
                    targetMapLarger = true;
                }
                AcceptanceReport result = true;
                foreach (SketchEntity current in ship.shipSketch.Entities)
                {
                    IntVec3 c = current.pos + center;
                    if (GenGrid.InNoBuildEdgeArea(c, map) || current.IsSpawningBlocked(c, map) || map.fogGrid.IsFogged(c) || map.roofGrid.Roofed(c) || (targetMapLarger && (c.x > ShipInteriorMod2.shipOriginMap.Size.x || c.z > ShipInteriorMod2.shipOriginMap.Size.z)))
                    {
                        current.DrawGhost(c, new Color(0.8f, 0.2f, 0.2f, 0.35f));
                        result = false;
                    }
                }
                return result;
            }
            return true;
        }
    }
}
