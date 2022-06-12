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
    public class CompSoSshipPart : ThingComp
    {
        public int ship = -1;
        public CompProperties_SoSshipPart Props
        {
            get
            {
                return (CompProperties_SoSshipPart)props;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            //if cache=null add to precache, else add/merge - should prevent massive merges at load/shipspawn
            var mapComp = this.parent.Map.GetComponent<ShipHeatMapComp>();
            if (mapComp.shipsOnMap == null)
            {
                PreCache.Add(this.parent as Building);
            }
            else
            {
                //chk tile, if ship, add
                foreach (Thing t in this.parent.Position.GetThingList(this.parent.Map))
                {
                    if (t is Building b && b.TryGetComp<CompSoSshipPart>() != null && b.TryGetComp<CompSoSshipPart>().ship > -1)
                    {
                        ship = b.TryGetComp<CompSoSshipPart>().ship;
                        return;
                    }
                }
                //chk cardinal, if one add, if more add+merge
                foreach (IntVec3 vec in GenAdj.CellsAdjacentCardinal(this.parent))
                {
                    foreach (Thing t in vec.GetThingList(this.parent.Map))
                    {
                        if (t is Building b && b.TryGetComp<CompSoSshipPart>() != null && b.TryGetComp<CompSoSshipPart>().ship > -1)
                        {
                            if (ship > -1)
                            {
                                //add
                                ship = b.TryGetComp<CompSoSshipPart>().ship;
                            }
                            else
                            {
                                //merge ship

                            }
                        }
                    }
                }
                //register new ship
                if (ship == -1)
                {
                    //ship = mapComp.;
                }
                //add to all relevant lists in ship
                //parent.Map.GetComponent<ShipHeatMapComp>().Shields.Add(this);
            }
        }
        public override void PostDeSpawn(Map map)
        {
            //map.GetComponent<ShipHeatMapComp>().Shields.Remove(this);
            base.PostDeSpawn(map);
        }

        public void SoShipAttached(Building root, int shipIndex)
        {
            if (root == null || root.Destroyed)
            {
                return;
            }

        }
        //all ship parts are added to this before the first cache is built
        public List<Building> PreCache;
        public void newte(List<ShipCache> shipsOnMap)
        {

            //find attached to first, add to new ship, remove from precache till empty
            int ind = 0;
            do
            {
                shipsOnMap.Add(new ShipCache());

                var cellsTodo = new HashSet<IntVec3>();
                var cellsDone = new HashSet<IntVec3>();

                cellsTodo.AddRange(GenAdj.CellsOccupiedBy(PreCache[0]));
                cellsTodo.AddRange(GenAdj.CellsAdjacentCardinal(PreCache[0]));

                while (cellsTodo.Count > 0)
                {
                    var current = cellsTodo.First();
                    cellsTodo.Remove(current);
                    cellsDone.Add(current);

                    var containedThings = current.GetThingList(this.parent.Map);
                    if (!containedThings.Any(thing => (thing as Building)?.TryGetComp<CompSoSshipPart>() != null))
                    {
                        continue;
                    }
                    foreach (var thing in containedThings)
                    {
                        if (thing is Building building && shipsOnMap[ind].Buildings.Add(building))
                        {
                            building.TryGetComp<CompSoSshipPart>().ship = ind;

                            //add to all relevant lists

                            if (building is Building_ShipBridge bridge)
                            {
                                shipsOnMap[ind].Bridges.Add(bridge);
                                shipsOnMap[ind].BridgesAtStart.Add(bridge);
                                bridge.shipIndex = ind;
                                Log.Message("Added bridge: " + bridge + " on ship: " + ind);
                            }
                            else if (building is Building_ShipTurret turret)
                                shipsOnMap[ind].Turrets.Add(turret);
                            else if (building.TryGetComp<CompPowerBattery>() != null)
                                shipsOnMap[ind].Batteries.Add(building.GetComp<CompPowerBattery>());
                            else if (building.TryGetComp<CompShipHeatSink>() != null)
                                shipsOnMap[ind].HeatSinks.Add(building.GetComp<CompShipHeatSink>());
                            /*else if (building.TryGetComp<CompShipCombatShield>() != null)
                                shipsOnMap[ind].CombatShields.Add(building.GetComp<CompShipCombatShield>());
                            else if (building.TryGetComp<CompHullFoamDistributor>() != null)
                                shipsOnMap[ind].FoamDistributors.Add(building.GetComp<CompHullFoamDistributor>());
                            else if (building.TryGetComp<CompShipLifeSupport>() != null)
                                shipsOnMap[ind].LifeSupports.Add(building.GetComp<CompShipLifeSupport>());*/

                            var heatPurge = building.TryGetComp<CompShipHeatPurge>();
                            if (heatPurge != null)
                                shipsOnMap[ind].HeatPurges.Add(heatPurge);

                            var trail = building.TryGetComp<CompEngineTrail>();
                            var refuelable = building.TryGetComp<CompRefuelable>();
                            var flickable = building.TryGetComp<CompFlickable>();
                            var trailEnergy = building.TryGetComp<CompEngineTrailEnergy>();
                            var powered = building.TryGetComp<CompPowerTrader>();
                            if (trail != null)
                                shipsOnMap[ind].Engines.Add(new Tuple<CompEngineTrail, CompFlickable, CompRefuelable>(trail, flickable, refuelable));
                            else if (trailEnergy != null)
                                shipsOnMap[ind].EnginesEnergy.Add(new Tuple<CompEngineTrailEnergy, CompFlickable, CompPowerTrader>(trailEnergy, flickable, powered));

                            //add surounding cells if hull
                            if (building.TryGetComp<CompSoSshipPart>().Props.hull)
                            {
                                cellsTodo.AddRange(
                                    GenAdj.CellsOccupiedBy(building).Concat(GenAdj.CellsAdjacentCardinal(building))
                                        .Where(cell => !cellsDone.Contains(cell))
                                );
                            }
                            PreCache.Remove(building);
                        }
                    }
                }
                ind++;
            }
            while (!PreCache.NullOrEmpty());
        }
    }
}