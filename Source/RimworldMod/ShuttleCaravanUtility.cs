using System;
using RimWorld.Planet;
using Verse;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace SaveOurShip2
{
	public static class ShuttleCaravanUtility
	{
		public static Caravan myVan;
		public static float myMaxLaunchDistance;

		public static void RefuelMe(Caravan van, List<Pawn> shuttles)
		{
			foreach (Pawn p in shuttles) {
				foreach (Thing t in CaravanInventoryUtility.AllInventoryItems(van)) {
					if (p.TryGetComp<CompRefuelable> ().Props.fuelFilter.Allows (t.def)) {
                        List<Thing> theFuels = new List<Thing>();
                        theFuels.Add(t);
						p.TryGetComp<CompRefuelable> ().Refuel(theFuels);
						if (p.TryGetComp<CompRefuelable> ().FuelPercentOfMax >= 1)
							break;
					}
				}
			}
		}

        public static void ActivateMe(Caravan van, List<MinifiedThing> shuttles)
        {
            List<MinifiedThing> toRemove = new List<MinifiedThing>();
            foreach(MinifiedThing p in shuttles)
            {
                if (p.InnerThing.TryGetComp<CompRefuelable>() != null)
                {
                    foreach (Thing t in CaravanInventoryUtility.AllInventoryItems(van))
                    {
                        if (p.InnerThing.TryGetComp<CompRefuelable>().Props.fuelFilter.Allows(t.def))
                        {
                            List<Thing> theFuels = new List<Thing>();
                            theFuels.Add(t);
                            p.InnerThing.TryGetComp<CompRefuelable>().Refuel(theFuels);
                            if (p.InnerThing.TryGetComp<CompRefuelable>().FuelPercentOfMax >= 1)
                                break;
                        }
                    }
                    Pawn pawn = CompBecomePawn.myPawn(p.InnerThing, IntVec3.Zero, (int)p.InnerThing.TryGetComp<CompRefuelable>().Fuel);
                    Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
                    van.AddPawn(pawn, true);
                }
                else
                {
                    Log.Message("But archotech shuttles aren't supposed to be out yet!"); 
					Pawn pawn = CompBecomePawn.myPawn(p.InnerThing, IntVec3.Zero, 0);
					pawn.SetFactionDirect(Faction.OfPlayer);
					van.AddPawn(pawn, true);
                }
                toRemove.Add(p);
            }
            foreach(MinifiedThing p in toRemove)
            {
                p.ParentHolder.GetDirectlyHeldThings().Remove(p);
            }
        }

		public static void LaunchMe(Caravan van, float MaxLaunchDistance, bool allFullyFueled)
		{
			CameraJumper.TryJump(van.Tile);
			myVan = van;
			myMaxLaunchDistance = MaxLaunchDistance;
			int tile = van.Tile;
			Find.WorldSelector.ClearSelection();
			Find.WorldTargeter.BeginTargeting(new Func<GlobalTargetInfo, bool>(ShuttleCaravanUtility.ChoseWorldTarget), true, CompShuttleLaunchable.TargeterMouseAttachment, false, delegate
				{
					GenDraw.DrawWorldRadiusRing(tile, (int)MaxLaunchDistance);
				}, delegate(GlobalTargetInfo target)
				{
					if (!target.IsValid)
					{
						return null;
					}
                    if (target.WorldObject != null && (target.WorldObject is SpaceSite || target.WorldObject is MoonBase))
                    {
                        return TranslatorFormattedStringExtensions.Translate("MustLaunchFromOrbit");
                    }
					int num = Find.WorldGrid.TraversalDistanceBetween(tile, target.Tile);
					if (num <= MaxLaunchDistance || (target.WorldObject != null &&target.WorldObject.def.defName.Equals("ShipOrbiting")))
					{
                        if(allFullyFueled)
                            myMaxLaunchDistance = 42069;
						return null;
					}
					return TranslatorFormattedStringExtensions.Translate("TransportPodNotEnoughFuel");
				});
		}

		public static bool ChoseWorldTarget(GlobalTargetInfo target)
		{
			if (!target.IsValid)
			{
				Messages.Message(TranslatorFormattedStringExtensions.Translate("MessageTransportPodsDestinationIsInvalid"), MessageTypeDefOf.RejectInput);
				return false;
			}
			int num = Find.WorldGrid.TraversalDistanceBetween(myVan.Tile, target.Tile);
			if (num > myMaxLaunchDistance)
			{
				Messages.Message(TranslatorFormattedStringExtensions.Translate("MessageTransportPodsDestinationIsTooFar",new NamedArgument[]
					{
						CompLaunchable.FuelNeededToLaunchAtDist((float)num).ToString("0.#")
					}), MessageTypeDefOf.RejectInput);
				return false;
			}
            bool flag;
            MapParent mapParent = target.WorldObject as MapParent;
			if (mapParent != null && mapParent.HasMap)
			{
                if (mapParent.def.defName.Equals("ShipOrbiting"))
                {
                    if (myMaxLaunchDistance < 42069)
                    {
                        Messages.Message(TranslatorFormattedStringExtensions.Translate("MessageShuttleMustBeFueledToOrbit"), MessageTypeDefOf.RejectInput);
                        return false;
                    }
                    IntVec3 shuttleBayPos = CompShuttleLaunchable.FirstShuttleBayOpen(mapParent.Map, myVan.pawns.InnerListForReading.Where(pawn=>pawn.TryGetComp<CompBecomeBuilding>()!=null).FirstOrDefault().TryGetComp<CompBecomeBuilding>().Props.buildingDef);
                    if (shuttleBayPos == IntVec3.Zero)
                    {
                        Messages.Message(TranslatorFormattedStringExtensions.Translate("NeedOpenShuttleBay"), MessageTypeDefOf.RejectInput);
                        return false;
                    }
                    TryLaunch(mapParent, new TransportPodsArrivalAction_LandInSpecificCell(mapParent, shuttleBayPos), myVan, true);
                    return true;
                }
                Map map = mapParent.Map;
				CameraJumper.TryHideWorld ();
				Current.Game.CurrentMap = map;
				Targeter arg_13B_0 = Find.Targeter;
				arg_13B_0.BeginTargeting(TargetingParameters.ForDropPodsDestination(), delegate(LocalTargetInfo x)
					{
						TryLaunch(x.ToGlobalTargetInfo(map), new TransportPodsArrivalAction_LandInSpecificCell(mapParent, x.Cell), myVan, false);
						CameraJumper.TryShowWorld();
					}, null, null, CompShuttleLaunchable.TargeterMouseAttachment);
				return true;
			}
			else if (mapParent != null)
			{
				Settlement settlement = mapParent as Settlement;
				List<FloatMenuOption> list = new List<FloatMenuOption>();
				if (settlement != null && settlement.Visitable)
				{
					list.Add(new FloatMenuOption(TranslatorFormattedStringExtensions.Translate("VisitSettlement",new NamedArgument[]
						{
							target.WorldObject.Label
						}), delegate
						{
							TryLaunch(target, new TransportPodsArrivalAction_VisitSettlement(settlement), myVan, false);
							//CameraJumper.TryHideWorld();
						}, MenuOptionPriority.Default, null, null, 0f, null, null));
				}
                if (settlement != null && settlement.Attackable)
                {
                    list.Add(new FloatMenuOption(TranslatorFormattedStringExtensions.Translate("AttackAndDropAtEdge",new NamedArgument[]
                        {
                            target.WorldObject.Label
                        }), delegate
                        {
                            TryLaunch(target, new TransportPodsArrivalAction_AttackSettlement(settlement, PawnsArrivalModeDefOf.EdgeDrop), myVan, false);
                            //CameraJumper.TryHideWorld();
                        }, MenuOptionPriority.Default, null, null, 0f, null, null));
                    list.Add(new FloatMenuOption(TranslatorFormattedStringExtensions.Translate("AttackAndDropInCenter",new NamedArgument[]
                        {
                            target.WorldObject.Label
                        }), delegate
                        {
                            TryLaunch(target, new TransportPodsArrivalAction_AttackSettlement(settlement, PawnsArrivalModeDefOf.CenterDrop), myVan, false);
                            //CameraJumper.TryHideWorld();
                        }, MenuOptionPriority.Default, null, null, 0f, null, null));
                }
                else if (mapParent is Site)
                {
                    list.Add(new FloatMenuOption(TranslatorFormattedStringExtensions.Translate("DropAtEdge",new NamedArgument[]
                        {
                            target.WorldObject.Label
                        }), delegate
                        {
                            TryLaunch(target, new TransportPodsArrivalAction_VisitSite((Site)mapParent, PawnsArrivalModeDefOf.EdgeDrop), myVan, false);
                            //CameraJumper.TryHideWorld();
                        }, MenuOptionPriority.Default, null, null, 0f, null, null));
                    list.Add(new FloatMenuOption(TranslatorFormattedStringExtensions.Translate("DropInCenter",new NamedArgument[]
                        {
                            target.WorldObject.Label
                        }), delegate
                        {
                            TryLaunch(target, new TransportPodsArrivalAction_VisitSite((Site)mapParent, PawnsArrivalModeDefOf.CenterDrop), myVan, false);
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
			else
			{
				flag = true;
			}
			if (!flag)
			{
				return false;
			}
			if (Find.World.Impassable(target.Tile))
			{
				Messages.Message(TranslatorFormattedStringExtensions.Translate("MessageTransportPodsDestinationIsInvalid"), MessageTypeDefOf.RejectInput);
				return false;
			}
			TryLaunch(target, new TransportPodsArrivalAction_FormCaravan(), myVan, false);
			return true;
		}

		private static void TryLaunch(GlobalTargetInfo target, TransportPodsArrivalAction arrivalAction, Caravan van, bool spaceship)
		{
			TravelingTransportPods travelingTransportPods = (TravelingTransportPods)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("TravelingShuttlesCargo"));
			travelingTransportPods.Tile = van.Tile;
			travelingTransportPods.SetFaction(Faction.OfPlayer);
			travelingTransportPods.destinationTile = target.Tile;
			travelingTransportPods.arrivalAction=arrivalAction;
			Find.WorldObjects.Add(travelingTransportPods);
			int num = Find.WorldGrid.TraversalDistanceBetween(van.Tile, target.Tile);
			bool gotStuff = false;
			ActiveDropPodInfo loadPod = null;
			foreach (Pawn p in van.pawns) {
				if (p.TryGetComp<CompBecomeBuilding> () != null) {
                    float amount = p.TryGetComp<CompRefuelable>().Fuel-1;
                    if(!spaceship)
                        amount = p.TryGetComp<CompBecomeBuilding> ().Props.fuelPerTile * num;
					p.TryGetComp<CompRefuelable> ().ConsumeFuel (amount);
					ActiveDropPodInfo thePod = new ActiveDropPodInfo ();
					if (!gotStuff) {
						thePod.innerContainer.TryAddRangeOrTransfer (CaravanInventoryUtility.AllInventoryItems (van), true); //TODO fix
						gotStuff = true;
						loadPod = thePod;
					}
                    p.SetFaction(Faction.OfPlayer);
					travelingTransportPods.AddPod (thePod, true);
				}
			}
			loadPod.innerContainer.TryAddRangeOrTransfer (van.pawns);
			Find.WorldObjects.Remove (van);
		}
	}
}

