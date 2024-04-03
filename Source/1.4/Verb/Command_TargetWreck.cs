using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimWorld
{
    public class Command_TargetWreck : Command
    {
        public Building salvageBay;
        public int salvageBayNum;
        public byte rotb = 0;
        public Map sourceMap;
        public Map targetMap;

        public override void ProcessInput(Event ev)
        {
            Building b=null;
            base.ProcessInput(ev);
            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
            if (sourceMap != targetMap)
                CameraJumper.TryJump(targetMap.Center, targetMap);
            Targeter targeter = Find.Targeter;
            TargetingParameters parms = new TargetingParameters();
            parms.canTargetBuildings = true;
            Find.Targeter.BeginTargeting(parms, (Action<LocalTargetInfo>)delegate (LocalTargetInfo x)
            {
                b = x.Cell.GetFirstBuilding(targetMap);
            }, null, delegate { AfterTarget(b); });
        }

        public void AfterTarget(Building b)
        {
            if (b == null)
                return;
            int bMax = sourceMap.GetComponent<ShipHeatMapComp>().MaxSalvageWeightOnMap();
            var mapComp = b.Map.GetComponent<ShipHeatMapComp>();
            int shipIndex = mapComp.ShipIndexOnVec(b.Position);
            if (shipIndex != -1)
            {
                var ship = mapComp.ShipsOnMapNew[shipIndex];
                float bCountF = ship.BuildingCount * 2.5f;
                if (bCountF > bMax) //moving this ship with another ship //td compare size, check bays and fuel
                {
                    Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.SalvageCount", (int)bCountF, bMax), MessageTypeDefOf.NeutralEvent);
                    return;
                }
                ship.CreateShipSketch(sourceMap, rotb);
            }
            else //legacy move for rocks, etc //td rework
                MoveShipSketch(b, sourceMap, rotb, true, bMax, false);
        }
        //legacy move system that can work with rocks
        public Sketch GenerateShipSketch(HashSet<IntVec3> positions, Map map, IntVec3 lowestCorner, byte rotb = 0)
        {
            Sketch sketch = new Sketch();
            IntVec3 rot = new IntVec3(0, 0, 0);
            foreach (IntVec3 pos in positions)
            {
                if (rotb == 1)
                {
                    rot.x = map.Size.x - pos.z;
                    rot.z = pos.x;
                    sketch.AddThing(ResourceBank.ThingDefOf.Ship_FakeBeam, rot - lowestCorner, Rot4.North);
                }
                else if (rotb == 2)
                {
                    rot.x = map.Size.x - pos.x;
                    rot.z = map.Size.z - pos.z;
                    sketch.AddThing(ResourceBank.ThingDefOf.Ship_FakeBeam, rot - lowestCorner, Rot4.North);
                }
                else
                    sketch.AddThing(ResourceBank.ThingDefOf.Ship_FakeBeam, pos - lowestCorner, Rot4.North);
            }
            return sketch;
        }
        public void MoveShipSketch(Building b, Map targetMap, byte rotb = 0, bool salvage = false, int bMax = 0, bool includeRock = false)
        {
            List<Building> cachedParts;
            if (b is Building_ShipBridge bridge)
                cachedParts = bridge.Ship.Buildings.ToList();
            else
                cachedParts = FindBuildingsAttached(b, includeRock);

            IntVec3 lowestCorner = new IntVec3(int.MaxValue, 0, int.MaxValue);
            HashSet<IntVec3> positions = new HashSet<IntVec3>();
            int bCount = 0;
            foreach (Building building in cachedParts)
            {
                bCount++;
                if (b.Position.x < lowestCorner.x)
                    lowestCorner.x = b.Position.x;
                if (b.Position.z < lowestCorner.z)
                    lowestCorner.z = b.Position.z;
                foreach (IntVec3 pos in GenAdj.CellsOccupiedBy(building))
                    positions.Add(pos);
            }
            if (rotb == 1)
            {
                int temp = lowestCorner.x;
                lowestCorner.x = b.Map.Size.z - lowestCorner.z;
                lowestCorner.z = temp;
            }
            else if (rotb == 2)
            {
                lowestCorner.x = b.Map.Size.x - lowestCorner.x;
                lowestCorner.z = b.Map.Size.z - lowestCorner.z;
            }
            float bCountF = bCount * 2.5f;
            if (salvage && bCountF > bMax)
            {
                Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.SalvageCount", (int)bCountF, bMax), MessageTypeDefOf.NeutralEvent);
                cachedParts.Clear();
                positions.Clear();
                return;
            }
            Sketch shipSketch = GenerateShipSketch(positions, targetMap, lowestCorner, rotb);
            MinifiedThingShipMove fakeMover = (MinifiedThingShipMove)new ShipMoveBlueprint(shipSketch).TryMakeMinified();
            fakeMover.shipRoot = b;
            fakeMover.includeRock = includeRock;
            fakeMover.shipRotNum = rotb;
            fakeMover.bottomLeftPos = lowestCorner;
            ShipInteriorMod2.shipOriginMap = b.Map;
            fakeMover.originMap = sourceMap;
            fakeMover.targetMap = targetMap;
            fakeMover.Position = b.Position;
            fakeMover.SpawnSetup(targetMap, false);
            List<object> selected = new List<object>();
            foreach (object ob in Find.Selector.SelectedObjects)
                selected.Add(ob);
            foreach (object ob in selected)
                Find.Selector.Deselect(ob);
            Current.Game.CurrentMap = targetMap;
            Find.Selector.Select(fakeMover);
            InstallationDesignatorDatabase.DesignatorFor(ResourceBank.ThingDefOf.ShipMoveBlueprint).ProcessInput(null);
        }
        public List<Building> FindBuildingsAttached(Building root, bool includeRock = false)
        {
            if (root == null || root.Destroyed)
                return new List<Building>();

            var map = root.Map;
            var containedBuildings = new HashSet<Building>();
            var cellsTodo = new HashSet<IntVec3>();
            var cellsDone = new HashSet<IntVec3>();
            cellsTodo.AddRange(GenAdj.CellsOccupiedBy(root));
            cellsTodo.AddRange(GenAdj.CellsAdjacentCardinal(root));
            while (cellsTodo.Count > 0)
            {
                var current = cellsTodo.First();
                cellsTodo.Remove(current);
                cellsDone.Add(current);
                var containedThings = current.GetThingList(map);
                if (containedThings.Any(t => t is Building b && (b.def.building.shipPart || (includeRock && b.def.building.isNaturalRock))))
                {
                    foreach (var t in containedThings)
                    {
                        if (t is Building b)
                        {
                            containedBuildings.Add(b);
                            if (b.def.building.shipPart)
                                cellsTodo.AddRange(GenAdj.CellsOccupiedBy(b).Concat(GenAdj.CellsAdjacentCardinal(b)).Where(v => !cellsDone.Contains(v)));
                        }
                    }
                }
            }
            return containedBuildings.ToList();
        }
    }
}
