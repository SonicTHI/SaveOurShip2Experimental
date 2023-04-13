using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
using SaveOurShip2;

namespace RimWorld
{
    public class CompSoShipPart : ThingComp
    {
        //CompSoShipPart types that are cached:
        //xml tagged building shipPart: anything attachable - walls, hullfoam, plating, engines, corners, hardpoints, spinal barrels
        //SoShipPart isHull (props isHull): walls, corners, hullfoam fills these if destroyed, wrecks form from these
        //SoShipPart isPlating (props isPlating): can not be placed under buildings, wrecks form from these
        //SoShipPart hermetic: hold air in vacuum - walls, corners, engines, hullfoam, extenders, spinal barrels
        //SoShipPart: other parts that are cached - not attached, no corePath

        public CompProperties_SoShipPart Props
        {
            get
            {
                return (CompProperties_SoShipPart)props;
            }
        }
        public ShipHeatMapComp mapComp;
        public override string CompInspectStringExtra()
        {
            StringBuilder stringBuilder = new StringBuilder();

            if (Prefs.DevMode)
            {
                if (!mapComp.ShipCells.ContainsKey(parent.Position))
                {
                    stringBuilder.Append("cache is null for this pos!");
                }
                var shipCells = mapComp.ShipCells[parent.Position];
                int index = -1;
                int path = -1;
                if (shipCells != null)
                {
                    index = shipCells.Item1;
                    path = shipCells.Item2;
                }
                if (parent.def.building.shipPart)
                    stringBuilder.Append("shipIndex: " + index + " / corePath: " + path);
                else
                    stringBuilder.Append("shipIndex: " + index);
            }
            return stringBuilder.ToString();
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            mapComp = parent.Map.GetComponent<ShipHeatMapComp>();
            //plating - check adj, merge
            //hull - check on + adj, merge
            //other - check on + adj, merge
            HashSet<IntVec3> cellsUnder = new HashSet<IntVec3>();
            HashSet<IntVec3> cellsToMerge = new HashSet<IntVec3>();
            foreach (IntVec3 vec in GenAdj.CellsOccupiedBy(parent)) //init cells if not already in ShipCells
            {
                if (!mapComp.ShipCells.ContainsKey(vec))
                {
                    mapComp.ShipCells.Add(vec, null);
                }
                cellsUnder.Add(vec);
            }
            if (mapComp.CacheOff) //on load, moveship, enemy ship spawn cache is off
            {
                return;
            }
            //plating or shipPart: chk all cardinal, if any plating or shipPart has valid shipIndex, set to this
            //plating or shipPart with different or no shipIndex: merge connected to this ship
            foreach (IntVec3 vec in GenAdj.CellsAdjacentCardinal(parent))
            {
                if (!mapComp.ShipCells.ContainsKey(vec))
                    continue;
                cellsToMerge.Add(vec);
            }
            if (cellsToMerge.Any()) //if any other parts under or adj, merge in order to: largest ship, any ship, wreck
            {
                int mergeToIndex;
                cellsToMerge.AddRange(cellsUnder);
                IntVec3 mergeTo = IntVec3.Zero;
                int mass = 0;
                foreach (IntVec3 vec in cellsToMerge) //find largest ship
                {
                    var shipCell = mapComp.ShipCells[vec];
                    if (shipCell != null && shipCell.Item1 != -1 && mapComp.ShipsOnMapNew.ContainsKey(shipCell.Item1) && mapComp.ShipsOnMapNew[shipCell.Item1].Mass > mass)
                    {
                        mergeTo = vec;
                        mass = mapComp.ShipsOnMapNew[shipCell.Item1].Mass;
                    }
                }
                if (mergeTo != IntVec3.Zero)
                {
                    mergeToIndex = mapComp.ShipCells[mergeTo].Item1;
                }
                else //no ships, attach to wreck
                {
                    mergeTo = cellsToMerge.FirstOrDefault();
                    mergeToIndex = mergeTo.GetThingList(parent.Map).FirstOrDefault(b => b.TryGetComp<CompSoShipPart>() != null).thingIDNumber;
                }
                mapComp.AttachAll(mergeTo, mergeToIndex);
            }
            else //else make new entry
            {
                foreach (IntVec3 vec in cellsUnder)
                    mapComp.ShipCells[vec] = new Tuple<int, int>(parent.thingIDNumber, -1);
            }
        }
        public void PreDeSpawn() //called in building.destroy, before comps get removed
        {
            if (ShipInteriorMod2.AirlockBugFlag) //moveship cleans entire area, caches //td
            {
                return;
            }
            var ship = mapComp.ShipsOnMapNew[mapComp.ShipCells[parent.Position].Item1];
            HashSet<Building> Buildings = new HashSet<Building>();
            HashSet<IntVec3> Area = new HashSet<IntVec3>();
            foreach (IntVec3 vec in GenAdj.CellsOccupiedBy(parent)) //check if any other ship parts exist, if not remove ship area
            {
                bool partExists = false;
                foreach (Thing t in vec.GetThingList(parent.Map))
                {
                    if (t is Building b)
                    {
                        if (b != parent && b.TryGetComp<CompSoShipPart>() != null)
                        {
                            partExists = true;
                        }
                        else
                        {
                            Buildings.Add(b);
                        }
                    }
                }
                if (!partExists) //no shippart remains, remove from area, remove non ship buildings if fully off ship
                {
                    Area.Add(vec);
                    mapComp.ShipCells.Remove(vec);
                    if (ship != null)
                    {
                        foreach (Building b in Buildings) //remove other buildings that are no longer supported by ship parts
                        {
                            bool allOffShip = false;
                            foreach (IntVec3 v in GenAdj.CellsOccupiedBy(b))
                            {
                                if (!mapComp.ShipCells.ContainsKey(vec))
                                {
                                    allOffShip = true;
                                    break;
                                }
                            }
                            if (allOffShip)
                            {
                                ship.BuildingCount--;
                                ship.Mass -= b.def.Size.x * b.def.Size.z * 3;
                            }
                        }
                    }
                }
            }
            //remove from cache
            if (ship != null)
            {
                ship.RemoveFromCache(parent as Building);
                ship.AreaDestroyed.AddRange(Area);
                if (!mapComp.InCombat) //perform check immediately
                    ship.CheckForDetach();
            }
        }
    }
}