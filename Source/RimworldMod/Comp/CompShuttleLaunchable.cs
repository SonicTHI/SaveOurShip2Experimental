using System;
using RimWorld;
using Verse;
using UnityEngine;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using SaveOurShip2;

namespace RimWorld
{
	[StaticConstructorOnStartup]
	public class CompShuttleLaunchable : ThingComp
	{
		private CompTransporter cachedCompTransporter;

		public static readonly Texture2D TargeterMouseAttachment = ContentFinder<Texture2D>.Get("UI/Overlays/LaunchableMouseAttachment", true);
		public static readonly Texture2D LaunchCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/LaunchShip", true);
		public static readonly Texture2D SetTargetFuelLevelCommand = ContentFinder<Texture2D>.Get("UI/Commands/SetTargetFuelLevel", true);

		public Building FuelingPortSource
		{
			get
			{
				return this.parent as Building;
			}
		}

		public CompProperties_ShuttleLaunchable Props
		{
			get
			{
				return (CompProperties_ShuttleLaunchable)this.props;
			}
		}

		public float FuelPerTile
		{
			get {
				return this.Props.fuelPerTile;
			}
		}

		public int MaxLaunchDistanceAtFuelLevel(float fuelLevel)
		{
			return Mathf.FloorToInt(fuelLevel/FuelPerTile);
		}

		public float FuelNeededToLaunchAtDist(float dist)
		{
			return dist * FuelPerTile;
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			foreach (Gizmo g in base.CompGetGizmosExtra())
			{
				yield return g;
			}
            if (parent.Faction != Faction.OfPlayer)
                yield break;
			if (this.LoadingInProgressOrReadyToLaunch)
			{
				Command_Action launch = new Command_Action();
				launch.defaultLabel = TranslatorFormattedStringExtensions.Translate("CommandLaunchGroup");
				launch.defaultDesc = TranslatorFormattedStringExtensions.Translate("CommandLaunchGroupDesc");
				launch.icon = CompShuttleLaunchable.LaunchCommandTex;
				launch.action = delegate
				{
					if (this.AnyInGroupHasAnythingLeftToLoad)
					{
						Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(TranslatorFormattedStringExtensions.Translate("ConfirmSendNotCompletelyLoadedPods",new NamedArgument[]
							{
								this.FirstThingLeftToLoadInGroup.LabelCapNoCount
							}), new Action(this.StartChoosingDestination), false, null));
					}
					else
					{
						this.StartChoosingDestination();
					}
				};
				if (!this.AllFuelingPortSourcesInGroupHaveAnyFuel)
				{
					launch.Disable(TranslatorFormattedStringExtensions.Translate("CommandLaunchGroupFailNoFuel"));
				}
				else if (this.AnyInGroupIsUnderRoof)
				{
					launch.Disable(TranslatorFormattedStringExtensions.Translate("CommandLaunchGroupFailUnderRoof"));
                }
                yield return launch;
            }
		}

		public Thing FirstThingLeftToLoadInGroup
		{
			get
			{
				return this.Transporter.FirstThingLeftToLoadInGroup;
			}
		}

		public bool AnyInGroupIsUnderRoof
		{
			get
			{
				List<CompTransporter> transportersInGroup = this.TransportersInGroup;
				for (int i = 0; i < transportersInGroup.Count; i++)
				{
					if (transportersInGroup[i].parent.Position.Roofed(this.parent.Map) && !(transportersInGroup[i].parent.Position.GetThingList(transportersInGroup[i].parent.Map).Any(t => t.def.defName.Equals("ShipShuttleBay"))))
					{
						return true;
					}
				}
				return false;
			}
		}
		public CompTransporter Transporter
		{
			get
			{
				if (this.cachedCompTransporter == null)
				{
					this.cachedCompTransporter = this.parent.GetComp<CompTransporter>();
				}
				return this.cachedCompTransporter;
			}
		}
		public bool LoadingInProgressOrReadyToLaunch
		{
			get
			{
				return this.Transporter.LoadingInProgressOrReadyToLaunch;
			}
		}

		public bool AllFuelingPortSourcesInGroupHaveAnyFuel
		{
			get
			{
				foreach (CompTransporter tr in TransportersInGroup) {
					if (tr.parent.GetComp<CompRefuelable> ().Fuel <= 0)
						return false;
				}
				return true;
			}
		}
		public override string CompInspectStringExtra()
		{
			if (!this.LoadingInProgressOrReadyToLaunch)
			{
				return null;
			}
			if (!this.AllFuelingPortSourcesInGroupHaveAnyFuel)
			{
				return TranslatorFormattedStringExtensions.Translate("NotReadyForLaunch") + ": " + TranslatorFormattedStringExtensions.Translate("NotAllFuelingPortSourcesInGroupHaveAnyFuel") + ".";
			}
			if (this.AnyInGroupHasAnythingLeftToLoad)
			{
				return TranslatorFormattedStringExtensions.Translate("NotReadyForLaunch") + ": " + TranslatorFormattedStringExtensions.Translate("TransportPodInGroupHasSomethingLeftToLoad") + ".";
			}
			return TranslatorFormattedStringExtensions.Translate("ReadyForLaunch");
		}
		public bool AnyInGroupHasAnythingLeftToLoad
		{
			get
			{
				return this.Transporter.AnyInGroupHasAnythingLeftToLoad;
			}
		}
		private int MaxLaunchDistance
		{
			get
			{
				if (!this.LoadingInProgressOrReadyToLaunch)
				{
					return 0;
				}
				return MaxLaunchDistanceAtFuelLevel(this.parent.GetComp<CompRefuelable>().Fuel);
			}
		}
		private void StartChoosingDestination()
		{
            var mapComp = this.parent.Map.GetComponent<ShipHeatMapComp>();
            CameraJumper.TryJump(CameraJumper.GetWorldTarget(this.parent));
            Find.WorldSelector.ClearSelection();
            if (this.parent.Map.Parent is WorldObjectOrbitingShip)
            {
                Find.WorldTargeter.BeginTargeting(new Func<GlobalTargetInfo, bool>(this.ChoseWorldTarget), true, CompShuttleLaunchable.TargeterMouseAttachment, true, null, delegate (GlobalTargetInfo target)
                {
                    if (!target.IsValid || this.parent.TryGetComp<CompRefuelable>() == null || this.parent.TryGetComp<CompRefuelable>().FuelPercentOfMax == 1.0f || ((this.parent.Map == mapComp.ShipCombatMasterMap || this.parent.Map == mapComp.ShipCombatOriginMap) && target.WorldObject is WorldObjectOrbitingShip && this.parent.TryGetComp<CompRefuelable>().FuelPercentOfMax >= 0.25f))
                    {
                        return null;
                    }
                    if (target.WorldObject != null && (target.WorldObject is SpaceSite))
                    {
                        if (this.parent.TryGetComp<CompRefuelable>().FuelPercentOfMax >= ((SpaceSite)target.WorldObject).fuelCost / 100f)
                            return null;
                        return TranslatorFormattedStringExtensions.Translate("MessageShuttleNeedsMoreFuel", ((SpaceSite)target.WorldObject).fuelCost);
                    }
                    if (target.WorldObject != null && (target.WorldObject is MoonBase))
                    {
                        if (this.parent.TryGetComp<CompRefuelable>().FuelPercentOfMax >= ((MoonBase)target.WorldObject).fuelCost / 100f)
                            return null;
                        return TranslatorFormattedStringExtensions.Translate("MessageShuttleNeedsMoreFuel", ((MoonBase)target.WorldObject).fuelCost);
                    }
                    return TranslatorFormattedStringExtensions.Translate("MessageShuttleMustBeFullyFueled");
                });
            }
            else if (this.parent.Map.Parent is SpaceSite)
            {
                Find.WorldTargeter.BeginTargeting(new Func<GlobalTargetInfo, bool>(this.ChoseWorldTarget), true, CompShuttleLaunchable.TargeterMouseAttachment, true, null, delegate (GlobalTargetInfo target)
                {
                    if (target.WorldObject == null || (!(target.WorldObject is SpaceSite) && !(target.WorldObject.def.defName.Equals("ShipOrbiting"))))
                    {
                        return TranslatorFormattedStringExtensions.Translate("MessageOnlyOtherSpaceSites");
                    }
                    if (this.parent.TryGetComp<CompRefuelable>().FuelPercentOfMax >= ((SpaceSite)this.parent.Map.Parent).fuelCost / 100f)
                        return null;
                    return TranslatorFormattedStringExtensions.Translate("MessageShuttleNeedsMoreFuel", ((SpaceSite)this.parent.Map.Parent).fuelCost);
                });
            }
            else if (this.parent.Map.Parent is MoonBase)
            {
                Find.WorldTargeter.BeginTargeting(new Func<GlobalTargetInfo, bool>(this.ChoseWorldTarget), true, CompShuttleLaunchable.TargeterMouseAttachment, true, null, delegate (GlobalTargetInfo target)
                {
                    if (target.WorldObject == null || (!(target.WorldObject is SpaceSite) && !(target.WorldObject.def.defName.Equals("ShipOrbiting"))))
                    {
                        return TranslatorFormattedStringExtensions.Translate("MessageOnlyOtherSpaceSites");
                    }
                    if (this.parent.TryGetComp<CompRefuelable>().FuelPercentOfMax >= ((MoonBase)this.parent.Map.Parent).fuelCost / 100f)
                        return null;
                    return TranslatorFormattedStringExtensions.Translate("MessageShuttleNeedsMoreFuel", ((MoonBase)this.parent.Map.Parent).fuelCost);
                });
            }
            else
            {
                int tile = this.parent.Map.Tile;
                Find.WorldTargeter.BeginTargeting(new Func<GlobalTargetInfo, bool>(this.ChoseWorldTarget), true, CompShuttleLaunchable.TargeterMouseAttachment, true, delegate
                {
                    GenDraw.DrawWorldRadiusRing(tile, this.MaxLaunchDistance);
                }, delegate (GlobalTargetInfo target)
                {
                    if (!target.IsValid)
                    {
                        return null;
                    }
                    if (target.Map != null && target.Map.Parent != null && target.Map.Parent.def.defName.Equals("ShipOrbiting"))
                    {
                        return null;
                    }
                    if (target.WorldObject != null && target.WorldObject.def.defName.Equals("ShipOrbiting"))
                        return null;
                    if (target.WorldObject != null && (target.WorldObject is SpaceSite || target.WorldObject is MoonBase))
                        return TranslatorFormattedStringExtensions.Translate("MustLaunchFromOrbit");
                    int num = Find.WorldGrid.TraversalDistanceBetween(tile, target.Tile);
                    if (num <= this.MaxLaunchDistance)
                    {
                        return null;
                    }
                    return TranslatorFormattedStringExtensions.Translate("TransportPodNotEnoughFuel");
                });
            }
        }
		private bool ChoseWorldTarget(GlobalTargetInfo target)
        {
            if (this.parent == null || this.parent.DestroyedOrNull())
                return false;
			if (!this.LoadingInProgressOrReadyToLaunch)
			{
				return true;
			}
			if (!target.IsValid)
			{
				Messages.Message(TranslatorFormattedStringExtensions.Translate("MessageTransportPodsDestinationIsInvalid"), MessageTypeDefOf.RejectInput);
				return false;
			}
            if (target.WorldObject != null && target.WorldObject is SpaceSite)
            {
                this.TryLaunch(target, new TransportPodsArrivalAction_VisitSite((SpaceSite)target.WorldObject, PawnsArrivalModeDefOf.EdgeDrop));
                return true;
            }
			MapParent targetMapParent = target.WorldObject as MapParent;
            if (targetMapParent != null && targetMapParent.HasMap)
			{
                //to orbiting ship
                if (targetMapParent is WorldObjectOrbitingShip)
                {
                    var mapComp = this.parent.Map.GetComponent<ShipHeatMapComp>();
                    IntVec3 shuttleBayPos = FirstShuttleBayOpen(targetMapParent.Map);
                    if (this.parent.Map.Parent is SpaceSite)
                    {
                        if (shuttleBayPos == IntVec3.Zero)
                        {
                            Messages.Message(TranslatorFormattedStringExtensions.Translate("NeedOpenShuttleBay"), MessageTypeDefOf.RejectInput);
                            return false;
                        }
                        this.TryLaunch(targetMapParent, new TransportPodsArrivalAction_LandInSpecificCell(targetMapParent, shuttleBayPos));
                        return true;
                    }
                    else if (this.parent.Map.Parent is MoonBase)
                    {
                        if (shuttleBayPos == IntVec3.Zero)
                        {
                            Messages.Message(TranslatorFormattedStringExtensions.Translate("NeedOpenShuttleBay"), MessageTypeDefOf.RejectInput);
                            return false;
                        }
                        this.TryLaunch(targetMapParent, new TransportPodsArrivalAction_LandInSpecificCell(targetMapParent, shuttleBayPos));
                        return true;
                    }
                    //from ground
                    else if (!mapComp.InCombat && this.parent.Map != mapComp.ShipCombatMasterMap && !(this.parent.Map == mapComp.ShipCombatOriginMap && targetMapParent.Map == mapComp.ShipCombatMasterMap))
                    {
                        if (this.parent.TryGetComp<CompRefuelable>() != null && this.parent.TryGetComp<CompRefuelable>().FuelPercentOfMax < 0.8f)
                        {
                            Messages.Message(TranslatorFormattedStringExtensions.Translate("MessageShuttleMustBeFueledToOrbit"), MessageTypeDefOf.RejectInput);
                            return false;
                        }
                    }
                    else
                    {
                        if (this.parent.TryGetComp<CompRefuelable>() != null && this.parent.TryGetComp<CompRefuelable>().FuelPercentOfMax < 0.25f)
                        {
                            Messages.Message(TranslatorFormattedStringExtensions.Translate("MessageShuttleMustBePartiallyFueled"), MessageTypeDefOf.RejectInput);
                            return false;
                        }
                    }
                    //if player ship and has shuttle bay land on it
                    if (targetMapParent.def.defName.Equals("ShipOrbiting") && shuttleBayPos != IntVec3.Zero)
                    {
                        this.TryLaunch(targetMapParent, new TransportPodsArrivalAction_LandInSpecificCell(targetMapParent, shuttleBayPos));
                        return true;
                    }
                    //only pods incombat if enemy t/w above
                    else if (mapComp.InCombat && mapComp.MasterMapComp.EnginePower < 0.2f)
                    {
                        foreach (CompTransporter t in this.TransportersInGroup)
                        {
                            if (!t.parent.def.defName.Equals("PersonalShuttle"))
                            {
                                Messages.Message(TranslatorFormattedStringExtensions.Translate("MessageShuttleOnlyPods"), MessageTypeDefOf.RejectInput);
                                return false;
                            }
                        }
                        ChooseMapTarget(targetMapParent, true);
                        return true;
                    }
                    else
                    {
                        ChooseMapTarget(targetMapParent);
                        return true;
                    }
                }
                else
                {
                    //ship to ground
                    if (this.parent.Map.Parent.def.defName.Equals("ShipOrbiting"))
                    {
                        if (this.parent.TryGetComp<CompRefuelable>() != null && this.parent.TryGetComp<CompRefuelable>().FuelPercentOfMax < 0.15f)
                        {
                            Messages.Message(TranslatorFormattedStringExtensions.Translate("MessageShuttleMustBeFueledToLand"), MessageTypeDefOf.RejectInput);
                            return false;
                        }
                    }
                    //planetside
                    else
                    {
                        int num = Find.WorldGrid.TraversalDistanceBetween(this.parent.Map.Tile, target.Tile);
                        if (num > this.MaxLaunchDistance)
                        {
                            Messages.Message(TranslatorFormattedStringExtensions.Translate("MessageTransportPodsDestinationIsTooFar",new NamedArgument[]
                                {
                        CompLaunchable.FuelNeededToLaunchAtDist((float)num).ToString("0.#")
                                }), MessageTypeDefOf.RejectInput);
                            return false;
                        }
                    }
                    ChooseMapTarget(targetMapParent);
                    return true;
                }
			}
			bool flag=false;
            //ship to ground
            if (targetMapParent != null && !(this.parent.Map.Parent is WorldObjectOrbitingShip) && !(targetMapParent is WorldObjectOrbitingShip))
            {
                int num = Find.WorldGrid.TraversalDistanceBetween(this.parent.Map.Tile, target.Tile);
                if (num > this.MaxLaunchDistance)
                {
                    Messages.Message(TranslatorFormattedStringExtensions.Translate("MessageTransportPodsDestinationIsTooFar",new NamedArgument[]
                        {
                        CompLaunchable.FuelNeededToLaunchAtDist((float)num).ToString("0.#")
                        }), MessageTypeDefOf.RejectInput);
                    return false;
                }
                Settlement settlement = targetMapParent as Settlement;
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                if (settlement != null && settlement.Visitable)
                {
                    list.Add(new FloatMenuOption(TranslatorFormattedStringExtensions.Translate("VisitSettlement",new NamedArgument[]
                        {
                            target.WorldObject.Label
                        }), delegate
                        {
                            if (!this.LoadingInProgressOrReadyToLaunch)
                            {
                                return;
                            }
                            this.TryLaunch(target, new TransportPodsArrivalAction_VisitSettlement(settlement));
                            CameraJumper.TryHideWorld();
                        }, MenuOptionPriority.Default, null, null, 0f, null, null));
                }
                if (settlement != null && settlement.Attackable)
                {
                    list.Add(new FloatMenuOption(TranslatorFormattedStringExtensions.Translate("AttackAndDropAtEdge",new NamedArgument[]
                        {
                            target.WorldObject.Label
                        }), delegate
                        {
                            TryLaunch(target, new TransportPodsArrivalAction_AttackSettlement(settlement, PawnsArrivalModeDefOf.EdgeDrop));
                            //CameraJumper.TryHideWorld();
                        }, MenuOptionPriority.Default, null, null, 0f, null, null));
                    list.Add(new FloatMenuOption(TranslatorFormattedStringExtensions.Translate("AttackAndDropInCenter",new NamedArgument[]
                        {
                            target.WorldObject.Label
                        }), delegate
                        {
                            TryLaunch(target, new TransportPodsArrivalAction_AttackSettlement(settlement, PawnsArrivalModeDefOf.CenterDrop));
                            //CameraJumper.TryHideWorld();
                        }, MenuOptionPriority.Default, null, null, 0f, null, null));
                }
                else if (targetMapParent is Site)
                {
                    list.Add(new FloatMenuOption(TranslatorFormattedStringExtensions.Translate("DropAtEdge",new NamedArgument[]
                        {
                            target.WorldObject.Label
                        }), delegate
                        {
                            TryLaunch(target, new TransportPodsArrivalAction_VisitSite((Site)targetMapParent, PawnsArrivalModeDefOf.EdgeDrop));
                            //CameraJumper.TryHideWorld();
                        }, MenuOptionPriority.Default, null, null, 0f, null, null));
                    list.Add(new FloatMenuOption(TranslatorFormattedStringExtensions.Translate("DropInCenter",new NamedArgument[]
                        {
                            target.WorldObject.Label
                        }), delegate
                        {
                            TryLaunch(target, new TransportPodsArrivalAction_VisitSite((Site)targetMapParent, PawnsArrivalModeDefOf.CenterDrop));
                            //CameraJumper.TryHideWorld();
                        }, MenuOptionPriority.Default, null, null, 0f, null, null));
                }
                if (list.Any<FloatMenuOption>())
                {
                    Find.WorldTargeter.closeWorldTabWhenFinished = false;
                    Find.WindowStack.Add(new FloatMenu(list));
                    return true;
                }
                flag = true;
            }
            //ship to caravan
            else if(target.WorldObject != null && target.WorldObject is Caravan && (this.parent.Map.Parent is WorldObjectOrbitingShip))
            {
                if (this.parent.TryGetComp<CompRefuelable>() != null && this.parent.TryGetComp<CompRefuelable>().FuelPercentOfMax < 0.15f)
                {
                    Messages.Message(TranslatorFormattedStringExtensions.Translate("MessageShuttleMustBeFueledToLand"), MessageTypeDefOf.RejectInput);
                    return false;
                }
                this.TryLaunch(target, new TransportPodsArrivalAction_GiveToCaravan((Caravan)target.WorldObject));
                return true;
            }
            else if (target.WorldObject != null && !(this.parent.Map.Parent is WorldObjectOrbitingShip))
            {
                int num = Find.WorldGrid.TraversalDistanceBetween(this.parent.Map.Tile, target.Tile);
                if (num > this.MaxLaunchDistance)
                {
                    Messages.Message(TranslatorFormattedStringExtensions.Translate("MessageTransportPodsDestinationIsTooFar",new NamedArgument[]
                        {
                        CompLaunchable.FuelNeededToLaunchAtDist((float)num).ToString("0.#")
                        }), MessageTypeDefOf.RejectInput);
                    return false;
                }
                flag = true;
            }
            else
                flag = true;
			if (!flag)
			{
				return false;
			}
			if (Find.World.Impassable(target.Tile))
			{
				Messages.Message(TranslatorFormattedStringExtensions.Translate("MessageTransportPodsDestinationIsInvalid"), MessageTypeDefOf.RejectInput);
				return false;
			}
            TryLaunch(target, new TransportPodsArrivalAction_FormCaravan());
            return true;
		}
        public void ChooseMapTarget(MapParent targetMapParent, bool podOnly = false)
        {
            Map myMap = this.parent.Map;
            Map map = targetMapParent.Map;
            Current.Game.CurrentMap = map;
            Targeter targeter = Find.Targeter;
            Action actionWhenFinished = delegate
            {
                if (Find.Maps.Contains(myMap))
                {
                    Current.Game.CurrentMap = myMap;
                }
            };
            TargetingParameters parms = TargetingParameters.ForDropPodsDestination();
            if (podOnly)
                parms.validator = ((TargetInfo x) => PodAssaultDropSpot(x.Cell, x.Map, allowFogged: false, canRoofPunch: true, allowIndoors: false));
            else
                parms.validator = ((TargetInfo x) => DropCellFinder.IsGoodDropSpot(x.Cell, x.Map, allowFogged: false, canRoofPunch: false, allowIndoors: false));
            targeter.BeginTargeting(parms, delegate (LocalTargetInfo x)
            {
                if (!this.LoadingInProgressOrReadyToLaunch)
                {
                    return;
                }
                if (podOnly)
                {
                    x = FindFirstRoom(x.Cell, Rot4.North, map, new IntVec2(7, 7));
                }
                this.TryLaunch(x.ToGlobalTargetInfo(map), new TransportPodsArrivalAction_LandInSpecificCell(targetMapParent, x.Cell));
            }, null, actionWhenFinished, CompShuttleLaunchable.TargeterMouseAttachment);
        }
        public IntVec3 FindFirstRoom(IntVec3 x, Rot4 rot, Map map, IntVec2 size)
        {
            List<IntVec3> validCells = new List<IntVec3>();
            foreach (IntVec3 intVec in GenAdj.CellsAdjacent8Way(x, rot, size))
            {
                Room room = intVec.GetRoom(map);
                if (intVec.InBounds(map) && intVec.Standable(map) && room != null && !room.TouchesMapEdge && !room.IsDoorway)
                {
                    bool prevent = false;
                    List<Thing> thingList = intVec.GetThingList(map);
                    for (int i = 0; i < thingList.Count; i++)
                    {
                        Thing thing = thingList[i];
                        if (thing.def.preventSkyfallersLandingOn)
                        {
                            prevent = true;
                            break;
                        }
                    }
                    if (!prevent)
                        validCells.Add(intVec);
                }
            }
            return validCells.RandomElement();
        }
        public static bool PodAssaultDropSpot(IntVec3 c, Map map, bool allowFogged, bool canRoofPunch, bool allowIndoors = true)
        {
            if (!c.InBounds(map) || !c.Standable(map))
            {
                return false;
            }
            if (!DropCellFinder.CanPhysicallyDropInto(c, map, canRoofPunch, allowIndoors))
            {
                if (DebugViewSettings.drawDestSearch)
                {
                    map.debugDrawer.FlashCell(c, 0f, "phys", 50);
                }
                return false;
            }
            if (Current.ProgramState == ProgramState.Playing && !allowFogged && c.Fogged(map))
            {
                return false;
            }
            List<Thing> thingList = c.GetThingList(map);
            for (int i = 0; i < thingList.Count; i++)
            {
                Thing thing = thingList[i];
                if (thing is IActiveDropPod || thing is Skyfaller)
                {
                    return false;
                }
                if (thing.def.IsEdifice())
                {
                    return false;
                }
                if (thing.def.preventSkyfallersLandingOn)
                {
                    return false;
                }
                if (thing.def.category != ThingCategory.Plant && GenSpawn.SpawningWipes(ThingDefOf.ActiveDropPod, thing.def))
                {
                    return false;
                }
            }
            //only next to ship walls
            for (int i = 0; i < 4; i++)
            {
                IntVec3 vec = c + GenAdj.CardinalDirections[i];
                if (!vec.Standable(map))
                {
                    foreach (Thing t in vec.GetThingList(map))
                    {
                        if (t is Building b && b.def.building.shipPart)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        public static IntVec3 FirstShuttleBayOpen(Map shipMap)
        {
            foreach(Thing thing in shipMap.spawnedThings.Where(t => t.def.defName.Equals("ShipShuttleBay")))
            {
                bool empty = true;
                foreach(IntVec3 pos in GenAdj.CellsOccupiedBy(thing))
                {
                    if (pos.GetThingList(shipMap).Any(x => x.def.passability == Traversability.Impassable || x.def.passability == Traversability.PassThroughOnly))
                    {
                        empty = false;
                        break;
                    }
                }
                if(empty)
                    return thing.Position;
            }
            return IntVec3.Zero;
        }
		public List<CompTransporter> TransportersInGroup
		{
			get
			{
				return this.Transporter.TransportersInGroup(this.parent.Map);
			}
		}
        public void TryLaunchSingle(GlobalTargetInfo target, TransportPodsArrivalAction arrivalAction)
        {
            if (!this.parent.Spawned)
            {
                Log.Error("Tried to launch " + this.parent + ", but it's unspawned.");
                return;
            }
            List<CompTransporter> transportersInGroup = this.TransportersInGroup;
            if (transportersInGroup == null)
            {
                Log.Error("Tried to launch " + this.parent + ", but it's not in any group.");
                return;
            }
            if (!this.LoadingInProgressOrReadyToLaunch || !this.AllFuelingPortSourcesInGroupHaveAnyFuel)
            {
                return;
            }
            Map map = this.parent.Map;
            var mapComp = map.GetComponent<ShipHeatMapComp>();
            var fuelComp = this.parent.TryGetComp<CompRefuelable>();
            float amount = 0;
            if ((target.WorldObject is WorldObjectOrbitingShip || mapComp.InCombat) && (map == mapComp.ShipCombatOriginMap || map == mapComp.ShipCombatMasterMap))
            {
                if (fuelComp != null)
                {
                    fuelComp.ConsumeFuel(((CompProperties_Refuelable)fuelComp.props).fuelCapacity * 0.25f);
                }
            }
            else if (map.Parent is SpaceSite && target.WorldObject != null)
            {
                if (target.WorldObject.def.defName.Equals("ShipOrbiting"))
                {
                    if (fuelComp != null)
                    {
                        fuelComp.ConsumeFuel(((CompProperties_Refuelable)fuelComp.props).fuelCapacity * ((SpaceSite)this.parent.Map.Parent).fuelCost / 100f);
                    }
                }
                else
                {
                    if (fuelComp != null)
                    {
                        fuelComp.ConsumeFuel(((CompProperties_Refuelable)fuelComp.props).fuelCapacity * ((SpaceSite)target.WorldObject).fuelCost / 100f);
                    }
                }
            }
            else if (map.Parent is MoonBase && target.WorldObject != null && target.WorldObject.def.defName.Equals("ShipOrbiting"))
            {
                if (fuelComp != null)
                {
                    fuelComp.ConsumeFuel(((CompProperties_Refuelable)fuelComp.props).fuelCapacity * ((MoonBase)this.parent.Map.Parent).fuelCost / 100f);
                }
            }
            else if (map.Parent is WorldObjectOrbitingShip || (target.WorldObject != null && target.WorldObject is WorldObjectOrbitingShip))
            {
                if (target.WorldObject != null && target.WorldObject is SpaceSite)
                {
                    amount = ((SpaceSite)target.WorldObject).fuelCost / 100 * fuelComp.Props.fuelCapacity;
                }
                else if (target.WorldObject != null && target.WorldObject is MoonBase)
                {
                    amount = ((MoonBase)target.WorldObject).fuelCost / 100 * fuelComp.Props.fuelCapacity;
                }
                else
                {
                    //if (this.parent.TryGetComp<CompRefuelable>() != null && this.parent.GetComp<CompRefuelable>().FuelPercentOfMax < 1)
                    //return;
                    if (map.Parent is WorldObjectOrbitingShip)
                        amount = fuelComp.Props.fuelCapacity * 0.15f;
                    else
                        amount = fuelComp.Props.fuelCapacity * 0.8f;
                }
            }
            else
            {
                int num = Find.WorldGrid.TraversalDistanceBetween(map.Tile, target.Tile);
                if (num > MaxLaunchDistanceAtFuelLevel(this.parent.GetComp<CompRefuelable>().Fuel))
                {
                    return;
                }
                amount = Mathf.Max(FuelNeededToLaunchAtDist((float)num), 1f);
            }
            this.Transporter.TryRemoveLord(map);
            int groupID = this.Transporter.groupID;
            for (int i = 0; i < transportersInGroup.Count; i++)
            {
                CompTransporter compTransporter = transportersInGroup[i];
                Building fuelingPortSource;
                if (compTransporter.Launchable != null)
                    fuelingPortSource = compTransporter.Launchable.FuelingPortSource;
                else
                    fuelingPortSource = compTransporter.parent as Building;
                if (fuelingPortSource != null)
                {
                    fuelingPortSource.TryGetComp<CompRefuelable>().ConsumeFuel(amount);
                }
                //shuttle to pawn
                Pawn meAsAPawn = CompBecomePawn.myPawn(this.parent, new IntVec3(), (int)fuelingPortSource.TryGetComp<CompRefuelable>().Fuel);
                Find.WorldPawns.PassToWorld(meAsAPawn, PawnDiscardDecideMode.KeepForever);
                meAsAPawn.SetFaction(parent.Faction);

                if (fuelingPortSource != null && fuelingPortSource.TryGetComp<CompRefuelable>() != null)
                    fuelingPortSource.TryGetComp<CompRefuelable>().ConsumeFuel(fuelingPortSource.TryGetComp<CompRefuelable>().Fuel);
                ThingOwner directlyHeldThings = compTransporter.GetDirectlyHeldThings();

                ActiveDropPod activeDropPod = (ActiveDropPod)ThingMaker.MakeThing(ThingDefOf.ActiveDropPod, null);
                activeDropPod.Contents = new ActiveDropPodInfo();
                activeDropPod.Contents.innerContainer.TryAddRangeOrTransfer(directlyHeldThings, true, true);

                activeDropPod.Contents.innerContainer.TryAddOrTransfer(meAsAPawn);

                FlyShipLeaving dropPodLeaving = (FlyShipLeaving)SkyfallerMaker.MakeSkyfaller(this.Props.skyfaller, activeDropPod);
                dropPodLeaving.groupID = groupID;
                dropPodLeaving.destinationTile = target.Tile;
                dropPodLeaving.arrivalAction = arrivalAction;

                directlyHeldThings.Clear();
                compTransporter.CleanUpLoadingVars(map);
                compTransporter.parent.Destroy(DestroyMode.Vanish);
                GenSpawn.Spawn(dropPodLeaving, compTransporter.parent.Position, map);
            }
        }
        public void TryLaunch(GlobalTargetInfo target, TransportPodsArrivalAction arrivalAction)
		{
			if (!this.parent.Spawned)
			{
				Log.Error("Tried to launch " + this.parent + ", but it's unspawned.");
				return;
			}
			List<CompTransporter> transportersInGroup = this.TransportersInGroup;
			if (transportersInGroup == null)
			{
				Log.Error("Tried to launch " + this.parent + ", but it's not in any group.");
				return;
			}
			if (!this.LoadingInProgressOrReadyToLaunch  || !this.AllFuelingPortSourcesInGroupHaveAnyFuel)
			{
				return;
			}
			Map map = this.parent.Map;
            var mapComp = map.GetComponent<ShipHeatMapComp>();
            var fuelComp = this.parent.TryGetComp<CompRefuelable>();
            float amount = 0;
            if((target.WorldObject is WorldObjectOrbitingShip || mapComp.InCombat) && (map== mapComp.ShipCombatOriginMap || map== mapComp.ShipCombatMasterMap))
            {
                if (fuelComp != null)
                {
                    fuelComp.ConsumeFuel(((CompProperties_Refuelable)fuelComp.props).fuelCapacity * 0.25f);
                }
            }
            else if (map.Parent is SpaceSite && target.WorldObject != null)
            {
                if (target.WorldObject.def.defName.Equals("ShipOrbiting"))
                {
                    if (fuelComp != null)
                    {
                        fuelComp.ConsumeFuel(((CompProperties_Refuelable)fuelComp.props).fuelCapacity * ((SpaceSite)this.parent.Map.Parent).fuelCost / 100f);
                    }
                }
                else
                {
                    if (fuelComp != null)
                    {
                        fuelComp.ConsumeFuel(((CompProperties_Refuelable)fuelComp.props).fuelCapacity * ((SpaceSite)target.WorldObject).fuelCost / 100f);
                    }
                }
            }
            else if (map.Parent is MoonBase && target.WorldObject != null && target.WorldObject.def.defName.Equals("ShipOrbiting"))
            { 
                    if (fuelComp != null)
                    {
                    fuelComp.ConsumeFuel(((CompProperties_Refuelable)fuelComp.props).fuelCapacity * ((MoonBase)this.parent.Map.Parent).fuelCost / 100f);
                    }
            }
            else if (map.Parent is WorldObjectOrbitingShip || (target.WorldObject != null && target.WorldObject is WorldObjectOrbitingShip))
            {
                if (target.WorldObject != null && target.WorldObject is SpaceSite)
                {
                    amount = ((SpaceSite)target.WorldObject).fuelCost / 100 * fuelComp.Props.fuelCapacity;
                }
                else if (target.WorldObject != null && target.WorldObject is MoonBase)
                {
                    amount = ((MoonBase)target.WorldObject).fuelCost / 100 * fuelComp.Props.fuelCapacity;
                }
                else
                {
                    //if (this.parent.TryGetComp<CompRefuelable>() != null && this.parent.GetComp<CompRefuelable>().FuelPercentOfMax < 1)
                        //return;
                    if (map.Parent is WorldObjectOrbitingShip)
                        amount = fuelComp.Props.fuelCapacity * 0.15f;
                    else
                        amount = fuelComp.Props.fuelCapacity * 0.8f;
                }
            }
            else
            {
                int num = Find.WorldGrid.TraversalDistanceBetween(map.Tile, target.Tile);
                if (num > MaxLaunchDistanceAtFuelLevel(this.parent.GetComp<CompRefuelable>().Fuel))
                {
                    return;
                }
                amount = Mathf.Max(FuelNeededToLaunchAtDist((float)num), 1f);
            }
			this.Transporter.TryRemoveLord(map);
			int groupID = this.Transporter.groupID;
			for (int i = 0; i < transportersInGroup.Count; i++)
			{
				CompTransporter compTransporter = transportersInGroup[i];
				Building fuelingPortSource;
				if (compTransporter.Launchable != null)
					fuelingPortSource = compTransporter.Launchable.FuelingPortSource;
				else
					fuelingPortSource = compTransporter.parent as Building;
				if (fuelingPortSource != null)
				{
					fuelingPortSource.TryGetComp<CompRefuelable>().ConsumeFuel(amount);
				}
                //shuttle to pawn
				Pawn meAsAPawn = CompBecomePawn.myPawn (this.parent, new IntVec3 (), (int)fuelingPortSource.TryGetComp<CompRefuelable> ().Fuel);
				Find.WorldPawns.PassToWorld (meAsAPawn, PawnDiscardDecideMode.KeepForever);
				meAsAPawn.SetFaction (parent.Faction);

                if (fuelingPortSource != null && fuelingPortSource.TryGetComp<CompRefuelable>()!=null)
                    fuelingPortSource.TryGetComp<CompRefuelable> ().ConsumeFuel (fuelingPortSource.TryGetComp<CompRefuelable> ().Fuel);
				ThingOwner directlyHeldThings = compTransporter.GetDirectlyHeldThings();

				ActiveDropPod activeDropPod = (ActiveDropPod)ThingMaker.MakeThing(ThingDefOf.ActiveDropPod, null);
                activeDropPod.Contents = new ActiveDropPodInfo();
				activeDropPod.Contents.innerContainer.TryAddRangeOrTransfer(directlyHeldThings, true, true);
				
                activeDropPod.Contents.innerContainer.TryAddOrTransfer (meAsAPawn);

                FlyShipLeaving dropPodLeaving = (FlyShipLeaving)SkyfallerMaker.MakeSkyfaller(this.Props.skyfaller, activeDropPod);
				dropPodLeaving.groupID = groupID;
				dropPodLeaving.destinationTile = target.Tile;
                dropPodLeaving.arrivalAction = arrivalAction;

				directlyHeldThings.Clear();
				compTransporter.CleanUpLoadingVars(map);
                compTransporter.parent.Destroy(DestroyMode.Vanish);
				GenSpawn.Spawn(dropPodLeaving, compTransporter.parent.Position, map);
			}
		}
	}
}

