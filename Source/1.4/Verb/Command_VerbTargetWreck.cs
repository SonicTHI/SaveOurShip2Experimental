using RimWorld;
using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimWorld
{
    public class Command_VerbTargetWreck : Command
    {
        public Map targetMap;
        public IntVec3 position;
        /*
        public override void MergeWith(Gizmo other)
        {
            base.MergeWith(other);
            Command_VerbTargetWreck command_VerbTargetShip = other as Command_VerbTargetWreck;
            if (command_VerbTargetShip == null)
            {
                Log.ErrorOnce("Tried to merge Command_VerbTarget with unexpected type", 73406263);
                return;
            }
        }*/

        public override void ProcessInput(Event ev) //td change to pass vec, change area attached
        {
            IntVec3 c = IntVec3.Invalid;
            base.ProcessInput(ev);
            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
            Targeter targeter = Find.Targeter;
            TargetingParameters parms = new TargetingParameters();
            parms.canTargetLocations = true;
            Find.Targeter.BeginTargeting(parms, (Action<LocalTargetInfo>)delegate (LocalTargetInfo x)
            {
                c = x.Cell;
            }, null, delegate { AfterTarget(c); });
        }

        public void AfterTarget(IntVec3 c)
        {
            if (c == IntVec3.Invalid || IntVec3.Zero.GetTerrain(targetMap) != ResourceBank.TerrainDefOf.EmptySpace) //moon
                return;

            var mapComp = targetMap.GetComponent<ShipHeatMapComp>();
            int index = mapComp.ShipIndexOnVec(c);
            HashSet<IntVec3> positions = null;
            if (index == -1) //rock
            {
                positions = FindRocksAttached(c);
                //Log.Message("SOS2: ".Colorize(Color.cyan) + targetMap + " found rocks attached: " + positions.Count);
                if (!positions.Any())
                    return;
            }
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(TranslatorFormattedStringExtensions.Translate("ShipSalvageAbandonConfirm"), delegate
            {
                ShipInteriorMod2.RemoveShipOrArea(targetMap, index, positions);
            }));
        }
        public HashSet<IntVec3> FindRocksAttached(IntVec3 root)
        {
            var cellsTodo = new HashSet<IntVec3> { root };
            var cellsDone = new HashSet<IntVec3>();
            var cellsFound = new HashSet<IntVec3>();
            while (cellsTodo.Count > 0)
            {
                var current = cellsTodo.First();
                cellsTodo.Remove(current);
                cellsDone.Add(current);
                if (current.GetThingList(targetMap).Any(t => t is Building b && b.def.building.isNaturalRock) || ShipInteriorMod2.IsRock(current.GetTerrain(targetMap)))
                {
                    cellsFound.Add(current);
                    cellsTodo.AddRange(GenAdj.CellsAdjacentCardinal(current, Rot4.North, new IntVec2(1, 1)).Where(v => !cellsDone.Contains(v) && targetMap.GetComponent<ShipHeatMapComp>().ShipIndexOnVec(v) == -1));
                }
            }
            return cellsFound;
        }
    }
}
