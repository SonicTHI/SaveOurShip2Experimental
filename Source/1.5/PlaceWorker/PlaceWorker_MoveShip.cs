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
        public override void DrawGhost(ThingDef def, IntVec3 loc, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            if (thing is ShipMoveBlueprint ship)
            {
                ship.DrawGhost(loc);
            }
        }

        public override AcceptanceReport AllowsPlacing(BuildableDef def, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
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
                    IntVec3 vec = loc + current.pos;

                    if (GenGrid.InNoBuildEdgeArea(vec, map) || current.IsSpawningBlocked(vec, map) || map.roofGrid.Roofed(vec) || (targetMapLarger && (vec.x > ShipInteriorMod2.shipOriginMap.Size.x || vec.z > ShipInteriorMod2.shipOriginMap.Size.z)))
                    {
                        current.DrawGhost(vec, new Color(0.8f, 0.2f, 0.2f, 0.3f));
                        result = false;
                        continue;
                    }
                    if (vec.InBounds(map))
                    {
                        foreach (Thing t in vec.GetThingList(map))
                        {
                            if (t is Building b)
                            {
                                if (b.def.passability == Traversability.Impassable || b is Building_SteamGeyser)
                                {
                                    current.DrawGhost(vec, new Color(0.8f, 0.2f, 0.2f, 0.3f));
                                    result = false;
                                    break;
                                }
                            }
                        }
                    }
                }
                return result;
            }
            return true;
        }
    }
}
