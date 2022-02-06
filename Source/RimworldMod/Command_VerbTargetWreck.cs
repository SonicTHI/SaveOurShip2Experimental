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
        public Building salvageBay;
        public int salvageBayNum;
        public Map targetMap;
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

        public override void ProcessInput(Event ev)
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
            List<Building> cache = FindAllAttached(b);
            List<IntVec3> positions = new List<IntVec3>();
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("ConfirmAbandonWreck", delegate
            {
                foreach (Building building in cache)
                {
                    if (building is Building_ShipBridge && !building.Destroyed && !building.Spawned)
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
                List<Thing> things = new List<Thing>();
                foreach (IntVec3 pos in positions)
                {
                    things.AddRange(pos.GetThingList(targetMap));
                }
                foreach (Thing t in things)
                {
                    if (t is Pawn)
                        t.Kill(new DamageInfo(DamageDefOf.Bomb, 100f));
                    t.Destroy();
                }
                foreach (IntVec3 pos in positions)
                {
                    targetMap.terrainGrid.SetTerrain(pos, TerrainDef.Named("EmptySpace"));
                }
            }));
        }
        public List<Building> FindAllAttached(Building root)
        {
            if (root == null || root.Destroyed)
            {
                return new List<Building>();
            }

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
                if (!containedThings.Any(thing => ((thing as Building)?.def.building.shipPart ?? false) || ((thing as Building)?.def.building.isNaturalRock ?? false)))
                {
                    continue;
                }

                foreach (var thing in containedThings)
                {
                    if (thing is Building building)
                    {
                        if (containedBuildings.Add(building))
                        {
                            cellsTodo.AddRange(
                                GenAdj.CellsOccupiedBy(building).Concat(GenAdj.CellsAdjacentCardinal(building))
                                    .Where(cell => !cellsDone.Contains(cell))
                            );
                        }
                    }
                }
            }
            return containedBuildings.ToList();
        }
    }
}
