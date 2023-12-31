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
            Building b=null;
            base.ProcessInput(ev);
            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
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
            if (b == null && IntVec3.Zero.GetTerrain(b.Map) != ResourceBank.TerrainDefOf.EmptySpace) //moon
                return;
            HashSet<IntVec3> positions = ShipInteriorMod2.FindAreaAttached(b, true);
            if (positions.Contains(position) || !positions.Any())
                return;
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(TranslatorFormattedStringExtensions.Translate("ShipSalvageAbandonConfirm"), delegate
            {
                ShipInteriorMod2.RemoveShip(null, false, positions, targetMap);
            }));
        }
    }
}
