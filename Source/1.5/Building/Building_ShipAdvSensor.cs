using RimWorld.Planet;
using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimWorld
{
    public class Building_ShipAdvSensor : Building
    {
        public MapParent observedMap;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            ShipInteriorMod2.WorldComp.Sensors.Add(this);
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            ShipInteriorMod2.WorldComp.Sensors.Remove(this);
            base.DeSpawn(mode);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            List<Gizmo> giz = new List<Gizmo>();
            giz.AddRange(base.GetGizmos());
            if (this.Faction==Faction.OfPlayer && this.TryGetComp<CompPowerTrader>().PowerOn && this.Map.IsSpace())
            {
                giz.Add(new Command_Action
                {
                    defaultLabel = "SoSScanMap".Translate(),
                    defaultDesc = "SoSScanMapDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/ScanMap", true),
                    action = delegate
                    {
                        CameraJumper.TryShowWorld();
                        Find.WorldTargeter.BeginTargeting(new Func<GlobalTargetInfo, bool>(this.ChoseWorldTarget), true, CompShuttleLaunchable.TargeterMouseAttachment);
                    }
                });
                if (observedMap != null)
                {
                    giz.Add(new Command_Action
                    {
                        defaultLabel = "SoSStopScanMap".Translate(),
                        defaultDesc = "SoSStopScanMapDesc".Translate(observedMap),
                        icon = ContentFinder<Texture2D>.Get("UI/StopScanMap", true),
                        action = delegate
                        {
                            PossiblyDisposeOfObservedMap();
                            observedMap = null;
                        }
                    });
                }
            }
            return giz;
        }

        private bool ChoseWorldTarget(GlobalTargetInfo target)
        {
            PossiblyDisposeOfObservedMap();
            if (target.WorldObject != null && target.WorldObject is MapParent p && (p.def.defName.Contains("Settlement") || p.def.defName.Contains("MoonPillarSite") || p.def.defName.Contains("TribalPillarSite") || p.def.defName.Contains("ShipEngineImpactSite")))
            {
                this.observedMap = (MapParent)target.WorldObject;
                LongEventHandler.QueueLongEvent(delegate
                {
                    GetOrGenerateMapUtility.GetOrGenerateMap(target.WorldObject.Tile, target.WorldObject.def);
                }, "Generating map",false, delegate { });
                return true;
            }
            else if (target.WorldObject == null && !Find.World.Impassable(target.Tile))
            {
                LongEventHandler.QueueLongEvent(delegate
                {
                    SettleUtility.AddNewHome(target.Tile, Faction.OfPlayer);
                    this.observedMap = GetOrGenerateMapUtility.GetOrGenerateMap(target.Tile, Find.World.info.initialMapSize, null).Parent;
                    ((Settlement)this.observedMap).Name = "Observed Area "+this.thingIDNumber;
                }, "Generating map", false, delegate { });
                return true;
            }
            return false;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<MapParent>(ref observedMap, "ScannedMap");
        }

        public override string GetInspectString()
        {
            string inspectString = base.GetInspectString();
            if(observedMap!=null)
            {
                inspectString += "\nObserving " + observedMap.Label;
            }
            return inspectString;
        }

        protected override void ReceiveCompSignal(string signal)
        {
            if (observedMap!=null && (signal == "PowerTurnedOff" || signal == "FlickedOff"))
            {
                PossiblyDisposeOfObservedMap();
                observedMap = null;
            }
        }

        void PossiblyDisposeOfObservedMap()
        {
            if (observedMap != null && observedMap.Map !=null && !observedMap.Map.mapPawns.AnyColonistSpawned && !observedMap.Map.listerBuildings.allBuildingsColonist.Any() && observedMap.Faction==Faction.OfPlayer)
            {
                Current.Game.DeinitAndRemoveMap(observedMap.Map, false);
                Find.World.worldObjects.Remove(observedMap);
            }
        }
    }
}
