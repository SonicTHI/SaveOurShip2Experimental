using RimWorld;
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
    public class Command_VerbTargetWreckMap : Command
    {
        public Building salvageBay;
        public int salvageBayNum;
        public byte rotb = 0;
        public Map sourceMap;
        public Map targetMap;
        
        public override void MergeWith(Gizmo other)
        {
            /*base.MergeWith(other);
            Command_VerbTargetWreck command_VerbTargetShip = other as Command_VerbTargetWreck;
            if (command_VerbTargetShip == null)
            {
                Log.ErrorOnce("Tried to merge Command_VerbTarget with unexpected type", 73406263);
                return;
            }*/
        }

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
            }, (Pawn)null, delegate { AfterTarget(b); });
        }

        public void AfterTarget(Building b)
        {
            if (b == null)
                return;
            List<Building> cache = ShipInteriorMod2.FindBuildingsAttached(b, true);
            HashSet<IntVec3> positions = new HashSet<IntVec3>();
            IntVec3 lowestCorner = new IntVec3(int.MaxValue, 0, int.MaxValue);
            //gen ship sketch
            Sketch sketch = new Sketch();
            int bCount = 0;
            foreach (Building building in cache)
            {
                if (building is Building_ShipBridge && !building.Destroyed)
                {
                    Messages.Message(TranslatorFormattedStringExtensions.Translate("ShipSalvageBridge"), MessageTypeDefOf.NeutralEvent);
                    return;
                }
                bCount++;
                if (building.Position.x < lowestCorner.x)
                    lowestCorner.x = building.Position.x;
                if (building.Position.z < lowestCorner.z)
                    lowestCorner.z = building.Position.z;
                foreach (IntVec3 pos in GenAdj.CellsOccupiedBy(building))
                {
                    positions.Add(pos);
                }
            }
            if (rotb == 2)
            {
                lowestCorner.x = targetMap.Size.x - lowestCorner.x;
                lowestCorner.z = targetMap.Size.z - lowestCorner.z;
            }
            Log.Message("Target wreck building count: " + bCount);
            int bMax = sourceMap.listerBuildings.allBuildingsColonist.Where(t => t.TryGetComp<CompShipSalvageBay>() != null).Count() * CompShipSalvageBay.salvageCapacity;
            if (bCount > bMax)
            {
                Messages.Message(TranslatorFormattedStringExtensions.Translate("ShipSalvageCount", bCount, bMax), MessageTypeDefOf.NeutralEvent);
                return;
            }
            IntVec3 rot = new IntVec3(0, 0, 0);
            foreach (IntVec3 pos in positions)
            {
                if (rotb == 2)
                {
                    rot.x = targetMap.Size.x - pos.x;
                    rot.z = targetMap.Size.z - pos.z;
                    sketch.AddThing(ThingDef.Named("Ship_FakeBeam"), rot - lowestCorner, Rot4.North);
                }
                else
                    sketch.AddThing(ThingDef.Named("Ship_FakeBeam"), pos - lowestCorner, Rot4.North);
            }
            //move ship sketch
            Map m = salvageBay.Map;
            MinifiedThingShipMove fakeMover = (MinifiedThingShipMove)new ShipMoveBlueprint(sketch).TryMakeMinified();
            fakeMover.shipRoot = b;
            fakeMover.includeRock = true;
            fakeMover.shipRotNum = rotb;
            fakeMover.bottomLeftPos = lowestCorner;
            ShipInteriorMod2.shipOriginMap = b.Map;
            fakeMover.targetMap = m;
            fakeMover.Position = b.Position;
            fakeMover.SpawnSetup(m, false);
            List<object> selected = new List<object>();
            foreach (object ob in Find.Selector.SelectedObjects)
                selected.Add(ob);
            foreach (object ob in selected)
                Find.Selector.Deselect(ob);
            Current.Game.CurrentMap = m;
            Find.Selector.Select(fakeMover);
            InstallationDesignatorDatabase.DesignatorFor(ThingDef.Named("ShipMoveBlueprint")).ProcessInput(null);
        }
    }
}
