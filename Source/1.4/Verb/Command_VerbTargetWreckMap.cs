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
            int bMax = sourceMap.listerBuildings.allBuildingsColonist.Where(t => t.TryGetComp<CompShipSalvageBay>() != null).Count() * CompShipSalvageBay.salvageCapacity;
            ShipInteriorMod2.MoveShipSketch(b, sourceMap, rotb, true, bMax, false);
        }
    }
}
