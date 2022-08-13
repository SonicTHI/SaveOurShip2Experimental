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
        public bool otherMap = true;
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
            if (otherMap)
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
            List<IntVec3> positions = new List<IntVec3>();
            IntVec3 lowestCorner = new IntVec3(int.MaxValue, 0, int.MaxValue);
            foreach (Building building in cache)
            {
                if (building.Position.x < lowestCorner.x)
                    lowestCorner.x = building.Position.x;
                if (building.Position.z < lowestCorner.z)
                    lowestCorner.z = building.Position.z;
            }
            Sketch shipSketch = new Sketch();
            int bCount = 0;
            foreach (Building building in cache)
            {
                bCount++;
                if (building is Building_ShipBridge && !building.Destroyed)
                {
                    Messages.Message(TranslatorFormattedStringExtensions.Translate("ShipSalvageBridge"), MessageTypeDefOf.NeutralEvent);
                    return;
                }
                foreach (IntVec3 pos in GenAdj.CellsOccupiedBy(building))
                {
                    if (!positions.Contains(pos))
                        positions.Add(pos);
                }
            }
            Log.Message("Target wreck building count: " + bCount);
            int bMax = salvageBayNum * CompShipSalvageBay.salvageCapacity;
            if (bCount > bMax)
            {
                Messages.Message(TranslatorFormattedStringExtensions.Translate("ShipSalvageCount", bCount, bMax), MessageTypeDefOf.NeutralEvent);
                return;
            }
            foreach (IntVec3 pos in positions)
            {
                shipSketch.AddThing(ThingDef.Named("Ship_FakeBeam"), pos - lowestCorner, Rot4.North);
            }
            Map m = salvageBay.Map;
            MinifiedThingShipMove fakeMover = (MinifiedThingShipMove)new ShipMoveBlueprint(shipSketch).TryMakeMinified();
            fakeMover.shipRoot = b;
            fakeMover.includeRock = true;
            fakeMover.shipRotNum = 0;
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
