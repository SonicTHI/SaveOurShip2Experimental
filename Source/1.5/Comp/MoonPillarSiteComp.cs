using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Verse;
using System.Linq;

namespace RimWorld.Planet
{
    public class MoonPillarSiteComp : EscapeShipComp
    {
        public override void CompTick()
        {
            if (Find.TickManager.TicksGame % 60 != 0)
                return;
            MapParent mapParent = (MapParent)this.parent;
            if (mapParent.HasMap)
            {
                List<Pawn> allPawnsSpawned = mapParent.Map.mapPawns.AllPawnsSpawned.ToList();
                bool flag = mapParent.Map.mapPawns.FreeColonistsSpawnedOrInPlayerEjectablePodsCount != 0;
                bool flag2 = false;
                for (int i = 0; i < allPawnsSpawned.Count; i++)
                {
                    Pawn pawn = allPawnsSpawned[i];
                    if (pawn.RaceProps.Humanlike)
                    {
                        if (pawn.HostFaction == null)
                        {
                            if (!pawn.Downed)
                            {
                                if (pawn.Faction != null && pawn.Faction.HostileTo(Faction.OfPlayer))
                                {
                                    flag2 = true;
                                }
                            }
                        }
                    }
                }
                bool flag3 = false;
                Map mapPlayer = ShipInteriorMod2.FindPlayerShipMap(); 
                if (mapPlayer != null)
                {
                    foreach (Building_ShipAdvSensor sensor in ShipInteriorMod2.WorldComp.Sensors)
                    {
                        if (sensor.observedMap == this.parent)
                        {
                            flag3 = true;
                        }
                    }
                }
                if (flag2 && !flag && !flag3)
                {
                    Find.WorldObjects.Remove(this.parent);
                    if (!ShipInteriorMod2.WorldComp.Unlocks.Contains("ArchotechPillarB"))
                    {
                        Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("MoonPillarLostLabel"), TranslatorFormattedStringExtensions.Translate("MoonPillarLost"), LetterDefOf.NegativeEvent);
                        ShipInteriorMod2.GenerateSite("MoonPillarSite");
                    }
                }
            }
        }

        [DebuggerHidden]
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
        {
            foreach (FloatMenuOption f in CaravanArrivalAction_VisitImpactSite.GetFloatMenuOptions(caravan, (MapParent)this.parent))
            {
                yield return f;
            }
        }

        [DebuggerHidden]
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo giz in base.GetGizmos())
                yield return giz;
            MapParent mapParent = this.parent as MapParent;
            if (mapParent.HasMap)
            {
                bool foundDrive = false;
                foreach(Thing t in mapParent.Map.spawnedThings)
                {
                    if(t.def == ResourceBank.ThingDefOf.ShipArchotechPillarB)
                    {
                        foundDrive = true;
                        break;
                    }
                }
                if(!foundDrive)
                    yield return SettlementAbandonUtility.AbandonCommand(mapParent);
            }
        }
    }
}