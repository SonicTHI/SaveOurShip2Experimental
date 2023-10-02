using System;
using RimWorld;
using Verse;
using UnityEngine;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using SaveOurShip2;
using RimworldMod;

namespace RimWorld
{
	[StaticConstructorOnStartup]
	public class CompCryptoLaunchable : ThingComp
	{
		public static readonly Texture2D TargeterMouseAttachment = ContentFinder<Texture2D>.Get("UI/Overlays/LaunchableMouseAttachment", true);
		public static readonly Texture2D LaunchCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/LaunchShip", true);

		public CompProperties_ShuttleLaunchable Props
		{
			get
			{
				return (CompProperties_ShuttleLaunchable)this.props;
			}
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			foreach (Gizmo g in base.CompGetGizmosExtra())
			{
				yield return g;
			}
            if (parent.Faction != Faction.OfPlayer)
                yield break;
            Command_Action launch = new Command_Action
            {
                defaultLabel = TranslatorFormattedStringExtensions.Translate("CommandLaunchGroup"),
                defaultDesc = TranslatorFormattedStringExtensions.Translate("CommandLaunchGroupDesc"),
                icon = CompShuttleLaunchable.LaunchCommandTex,
                action = delegate
                {
                    this.StartChoosingDestination();
                }
            };
            if (this.NotReadyToLaunch)
                launch.Disable(TranslatorFormattedStringExtensions.Translate("CommandLaunchCryptoNotLoaded"));
            if (!this.parent.Map.IsSpace())
                launch.Disable();
            yield return launch;
        }

		public bool NotReadyToLaunch
		{
			get
			{
                if (this.parent is Building_CryptosleepCasket casket)
				    return casket.GetDirectlyHeldThings().NullOrEmpty();
                return !((Building_Bed)this.parent).AnyOccupants;
            }
		}

		private void StartChoosingDestination()
		{
            CameraJumper.TryJump(CameraJumper.GetWorldTarget(this.parent));
            Find.WorldSelector.ClearSelection();
            int tile = this.parent.Map.Tile;
            Find.WorldTargeter.BeginTargeting(new Func<GlobalTargetInfo, bool>(this.ChoseWorldTarget), true, CompShuttleLaunchable.TargeterMouseAttachment, true, delegate
            {
            }, delegate (GlobalTargetInfo target)
            {
                if (!target.IsValid)
                {
                    return null;
                }
                if (target.Map != null && target.Map.Parent != null && target.Map.Parent.def == ResourceBank.WorldObjectDefOf.ShipOrbiting)
                {
                    return null;
                }
                if (target.WorldObject != null && target.WorldObject.def == ResourceBank.WorldObjectDefOf.ShipOrbiting)
                    return null;
                if (target.WorldObject != null && (target.WorldObject is SpaceSite || target.WorldObject is MoonBase))
                    return TranslatorFormattedStringExtensions.Translate("MustLaunchFromOrbit");
                return null;
            });
        }

		public bool ChoseWorldTarget(GlobalTargetInfo target)
        {
            if (this.parent == null || this.parent.DestroyedOrNull())
                return false;
			if (this.NotReadyToLaunch)
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
                return false;
            }
			MapParent targetMapParent = target.WorldObject as MapParent;
            if (targetMapParent != null && targetMapParent.HasMap)
			{
                //to ground only
                if (targetMapParent is WorldObjectOrbitingShip || targetMapParent is SpaceSite || targetMapParent is MoonBase)
                {
                    Messages.Message(TranslatorFormattedStringExtensions.Translate("No"), MessageTypeDefOf.RejectInput);
                    return false;
                }
                else
                {
                    Site site = targetMapParent as Site;
                    this.TryLaunch(target, new TransportPodsArrivalAction_VisitSite(site, PawnsArrivalModeDefOf.EdgeDrop));
                    //Launch(targetMapParent);
                    return true;
                }
			}
			bool flag=false;
            //ship to ground
            if (targetMapParent != null && !(this.parent.Map.Parent is WorldObjectOrbitingShip) && !(targetMapParent is WorldObjectOrbitingShip))
            {
                Settlement settlement = targetMapParent as Settlement;
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                if (settlement != null && settlement.Visitable)
                {
                    list.Add(new FloatMenuOption(TranslatorFormattedStringExtensions.Translate("VisitSettlement",new NamedArgument[]
                        {
                            target.WorldObject.Label
                        }), delegate
                        {
                            if (this.NotReadyToLaunch)
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
                this.TryLaunch(target, new TransportPodsArrivalAction_GiveToCaravan((Caravan)target.WorldObject));
                return true;
            }
            else if (target.WorldObject != null && !(this.parent.Map.Parent is WorldObjectOrbitingShip))
            {
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
        public void Launch(MapParent targetMapParent)
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
            parms.validator = ((TargetInfo x) => DropCellFinder.IsGoodDropSpot(x.Cell, x.Map, allowFogged: false, canRoofPunch: false, allowIndoors: false));
            targeter.BeginTargeting(parms, delegate (LocalTargetInfo x)
            {
                if (this.NotReadyToLaunch)
                {
                    return;
                }
                this.TryLaunch(x.ToGlobalTargetInfo(map), new TransportPodsArrivalAction_LandInSpecificCell(targetMapParent, x.Cell));
            }, null, actionWhenFinished, CompShuttleLaunchable.TargeterMouseAttachment);
        }
		public void TryLaunch(GlobalTargetInfo target, TransportPodsArrivalAction arrivalAction)
		{
			if (!this.parent.Spawned)
			{
				Log.Error("Tried to launch " + this.parent + ", but it's unspawned.");
				return;
			}
			if (this.NotReadyToLaunch)
			{
				return;
			}
			Map map = this.parent.Map;
            int groupID = Find.UniqueIDsManager.GetNextTransporterGroupID();
            ActiveDropPod activeDropPod = (ActiveDropPod)ThingMaker.MakeThing(ThingDefOf.ActiveDropPod, null);
            activeDropPod.Contents = new ActiveDropPodInfo();

            if (this.parent is Building_CryptosleepCasket casket)
                activeDropPod.Contents.innerContainer.TryAddRangeOrTransfer(casket.GetDirectlyHeldThings(), true, true);
            else if (this.parent is Building_Bed bed)
            {
                Pawn pawn = bed.CurOccupants.First();
                pawn.DeSpawn();
                activeDropPod.Contents.innerContainer.TryAdd(pawn);
            }

            FlyShipLeaving dropPodLeaving = (FlyShipLeaving)SkyfallerMaker.MakeSkyfaller(ThingDefOf.DropPodLeaving, activeDropPod);
            dropPodLeaving.groupID = groupID;
            dropPodLeaving.destinationTile = target.Tile;
            dropPodLeaving.arrivalAction = arrivalAction;
            //directlyHeldThings.Clear();
            //compTransporter.CleanUpLoadingVars(map);
            GenSpawn.Spawn(dropPodLeaving, this.parent.Position, map);
            this.parent.Destroy(DestroyMode.Vanish);
        }
    }
}

