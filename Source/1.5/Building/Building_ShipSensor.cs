using RimWorld.Planet;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace SaveOurShip2
{
	public class Building_ShipSensor : Building
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
			if (Faction == Faction.OfPlayer && this.TryGetComp<CompPowerTrader>().PowerOn && Map.IsSpace())
			{
				giz.Add(new Command_Action
				{
					defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.ScanMap"),
					defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.ScanMapDesc"),
					icon = ContentFinder<Texture2D>.Get("UI/ScanMap", true),
					action = delegate
					{
						CameraJumper.TryShowWorld();
						Find.WorldTargeter.BeginTargeting(new Func<GlobalTargetInfo, bool>(ChoseWorldTarget), true, CompCryptoLaunchable.TargeterMouseAttachment);
					}
				});
				if (observedMap != null)
				{
					giz.Add(new Command_Action
					{
						defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.ScanMapStop"),
						defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.ScanMapStopDesc", observedMap),
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
			if (target.WorldObject != null && target.WorldObject is MapParent p && ShipInteriorMod2.allowedToObserve.Contains(p.def.defName))
			{
				observedMap = (MapParent)target.WorldObject;
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
					observedMap = GetOrGenerateMapUtility.GetOrGenerateMap(target.Tile, Find.World.info.initialMapSize, null).Parent;
					GetOrGenerateMapUtility.UnfogMapFromEdge(observedMap.Map);
					((Settlement)observedMap).Name = "Observed Area "+ this.thingIDNumber;
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
