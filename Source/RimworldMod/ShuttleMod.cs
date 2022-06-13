using System;
using HugsLib;
using RimWorld;
using HarmonyLib;
using Verse;
using Verse.AI.Group;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse.Sound;
using Verse.AI;
using System.Linq;
using RimworldMod;
using RimworldMod.VacuumIsNotFun;

namespace SaveOurShip2
{
    //skyfaller harmony patches
    [HarmonyPatch(typeof(FlyShipLeaving), "LeaveMap")]
    public static class LeavingPodFix
    {
        [HarmonyPrefix]
        public static bool PatchThat(ref FlyShipLeaving __instance)
        {
            if (__instance.def.defName.Equals("PersonalShuttleSkyfaller") || __instance.def.defName.Equals("CargoShuttleSkyfaller") || __instance.def.defName.Equals("HeavyCargoShuttleSkyfaller") || __instance.def.defName.Equals("DropshipShuttleSkyfaller"))
            {
                if ((bool)typeof(FlyShipLeaving).GetField("alreadyLeft", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance)) {
                    __instance.Destroy(DestroyMode.Vanish);
                    return false;
                }
                if (__instance.groupID < 0)
                {
                    Log.Error("Drop pod left the map, but its group ID is " + __instance.groupID);
                    __instance.Destroy(DestroyMode.Vanish);
                    return false;
                }
                if (__instance.destinationTile < 0)
                {
                    Log.Error("Drop pod left the map, but its destination tile is " + __instance.destinationTile);
                    __instance.Destroy(DestroyMode.Vanish);
                    return false;
                }
                Lord lord = TransporterUtility.FindLord(__instance.groupID, __instance.Map);
                if (lord != null)
                {
                    __instance.Map.lordManager.RemoveLord(lord);
                }
                TravelingTransportPods travelingTransportPods;
                if (__instance.def.defName.Equals("PersonalShuttleSkyfaller"))
                    travelingTransportPods = (TravelingTransportPods)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("TravelingShuttlesPersonal"));
                else if (__instance.def.defName.Equals("CargoShuttleSkyfaller"))
                    travelingTransportPods = (TravelingTransportPods)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("TravelingShuttlesCargo"));
                else if (__instance.def.defName.Equals("HeavyCargoShuttleSkyfaller"))
                    travelingTransportPods = (TravelingTransportPods)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("TravelingShuttlesHeavy"));
                else
                    travelingTransportPods = (TravelingTransportPods)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("TravelingShuttlesDropship"));
                travelingTransportPods.Tile = __instance.Map.Tile;

                Thing t = __instance.Contents.innerContainer.Where(p => p is Pawn).FirstOrDefault();
                if (__instance.Map.GetComponent<ShipHeatMapComp>().InCombat && t != null)
                    travelingTransportPods.SetFaction(t.Faction);
                else
                    travelingTransportPods.SetFaction(Faction.OfPlayer);
                travelingTransportPods.destinationTile = __instance.destinationTile;
                travelingTransportPods.arrivalAction = __instance.arrivalAction;
                Find.WorldObjects.Add(travelingTransportPods);

                List<Thing> pods = new List<Thing>();
                pods.AddRange(__instance.Map.listerThings.ThingsInGroup(ThingRequestGroup.ActiveDropPod));
                for (int i = 0; i < pods.Count; i++)
                {
                    FlyShipLeaving dropPodLeaving = pods[i] as FlyShipLeaving;
                    if (dropPodLeaving != null && dropPodLeaving.groupID == __instance.groupID)
                    {
                        typeof(FlyShipLeaving).GetField("alreadyLeft", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(dropPodLeaving, true);
                        travelingTransportPods.AddPod(dropPodLeaving.Contents, true);
                        dropPodLeaving.Contents = null;
                        dropPodLeaving.Destroy(DestroyMode.Vanish);
                    }
                }
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(DropPodUtility), "MakeDropPodAt")]
    public static class TravelingPodFix {
        [HarmonyPrefix]
        public static bool PatchThat(IntVec3 c, Map map, ActiveDropPodInfo info)
        {
            bool hasShuttle = false;
            //ThingDef shuttleDef = null;
            ThingDef skyfaller = null;
            Thing foundShuttle = null;
            foreach (Thing t in info.innerContainer) {
                if (t.TryGetComp<CompBecomeBuilding>() != null) {
                    hasShuttle = true;
                    //shuttleDef = t.def;
                    skyfaller = t.TryGetComp<CompBecomeBuilding>().Props.skyfaller;
                    foundShuttle = t;
                    break;
                }
            }
            if (hasShuttle) {
                ActiveDropPod activeDropPod = (ActiveDropPod)ThingMaker.MakeThing(ThingDefOf.ActiveDropPod, null);
                activeDropPod.Contents = info;
                Skyfaller theShuttle = SkyfallerMaker.SpawnSkyfaller(skyfaller, activeDropPod, c, map);
                if (foundShuttle.TryGetComp<CompShuttleCosmetics>() != null)
                {
                    Graphic_Single graphic = new Graphic_Single();
                    CompProperties_ShuttleCosmetics Props = foundShuttle.TryGetComp<CompShuttleCosmetics>().Props;
                    int whichVersion = foundShuttle.TryGetComp<CompShuttleCosmetics>().whichVersion;
                    GraphicRequest req = new GraphicRequest(typeof(Graphic_Single), Props.graphicsHover[whichVersion].texPath + "_south", ShaderDatabase.Cutout, Props.graphics[whichVersion].drawSize, Color.white, Color.white, Props.graphics[whichVersion], 0, null, "");
                    graphic.Init(req);
                    typeof(Thing).GetField("graphicInt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(theShuttle, graphic);
                }
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(DropPodIncoming), "Impact")]
    public static class IncomingPodFix
    {
        [HarmonyPrefix]
        public static bool PatchThat(ref DropPodIncoming __instance)
        {
            //spawns pawns and shuttle at location
            if (__instance.def.defName.Equals("ShuttleIncomingPersonal") || __instance.def.defName.Equals("ShuttleIncomingCargo") || __instance.def.defName.Equals("ShuttleIncomingHeavy") || __instance.def.defName.Equals("ShuttleIncomingDropship"))
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector3 loc = __instance.Position.ToVector3Shifted() + Gen.RandomHorizontalVector(1f);
                    FleckMaker.ThrowDustPuff(loc, __instance.Map, 1.2f);
                }
                FleckMaker.ThrowLightningGlow(__instance.Position.ToVector3Shifted(), __instance.Map, 2f);

                Pawn myShuttle = null;
                ThingOwner container = ((ActiveDropPod)__instance.innerContainer[0]).Contents.innerContainer;

                for (int i = container.Count - 1; i >= 0; i--) {
                    if (container[i] is Pawn && container[i].TryGetComp<CompBecomeBuilding>() != null)
                        myShuttle = (Pawn)container[i];
                }

                var mapComp = __instance.Map.GetComponent<ShipHeatMapComp>();
                for (int i = container.Count - 1; i >= 0; i--)
                {
                    if (container[i] is Pawn)
                    {
                        GenPlace.TryPlaceThing(container[i], __instance.Position, __instance.Map, ThingPlaceMode.Near, delegate (Thing thing, int count) {
                            PawnUtility.RecoverFromUnwalkablePositionOrKill(thing.Position, thing.Map);
                            if (thing.Faction != Faction.OfPlayer && mapComp.InCombat && mapComp.ShipCombatOriginMap.GetComponent<ShipHeatMapComp>().ShipLord != null)
                                mapComp.ShipCombatOriginMap.GetComponent<ShipHeatMapComp>().ShipLord.AddPawn((Pawn)thing);
                            if (thing.TryGetComp<CompShuttleCosmetics>() != null)
                                CompShuttleCosmetics.ChangeShipGraphics((Pawn)thing, ((Pawn)thing).TryGetComp<CompShuttleCosmetics>().Props);
                        });
                    }
                    else if (myShuttle != null)
                        myShuttle.inventory.innerContainer.TryAddOrTransfer(container[i]);
                }

                __instance.innerContainer.ClearAndDestroyContents(DestroyMode.Vanish);
                CellRect cellRect = __instance.OccupiedRect();

                for (int j = 0; j < cellRect.Area * __instance.def.skyfaller.motesPerCell; j++)
                {
                    FleckMaker.ThrowDustPuff(cellRect.RandomVector3, __instance.Map, 2f);
                }
                if (__instance.def.skyfaller.cameraShake > 0f && __instance.Map == Find.CurrentMap)
                {
                    Find.CameraDriver.shaker.DoShake(__instance.def.skyfaller.cameraShake);
                }
                if (__instance.def.skyfaller.impactSound != null)
                {
                    __instance.def.skyfaller.impactSound.PlayOneShot(SoundInfo.InMap(new TargetInfo(__instance.Position, __instance.Map, false), MaintenanceType.None));
                }
                __instance.Destroy(DestroyMode.Vanish);

                if (myShuttle.Faction != Faction.OfPlayer)
                {
                    if (myShuttle.Position.Roofed(myShuttle.Map) && Rand.Chance(0.5f))
                    {
                        Traverse.Create(myShuttle.TryGetComp<CompRefuelable>()).Field("fuel").SetValue(0);
                        myShuttle.Destroy();
                    }
                    else
                        myShuttle.GetComp<CompBecomeBuilding>().transform();
                }
                else if (myShuttle.Position.Fogged(myShuttle.Map))
                    FloodFillerFog.FloodUnfog(myShuttle.Position, myShuttle.Map);
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Pawn), "GetGizmos")]
    public static class ShuttleGizmoFix
    {
        [HarmonyPostfix]
        public static void GetAllTheGizmos(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            if (__instance == null || __result == null)
                return;
            if (__instance.TryGetComp<CompBecomeBuilding>() != null) {
                List<Gizmo> newList = new List<Gizmo>();
                foreach (Gizmo g in __result) {
                    newList.Add(g);
                }
                if (__instance.drafter == null) {
                    __instance.drafter = new Pawn_DraftController(__instance);
                    __instance.equipment = new Pawn_EquipmentTracker(__instance);
                }
                IEnumerable<Gizmo> draftGizmos = (IEnumerable<Gizmo>)typeof(Pawn_DraftController).GetMethod("GetGizmos", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance.drafter, new object[] { });
                foreach (Gizmo c2 in draftGizmos)
                {
                    newList.Add(c2);
                }
                foreach (ThingComp comp in __instance.AllComps)
                {
                    foreach (Gizmo com in comp.CompGetGizmosExtra())
                    {
                        newList.Add(com);
                    }
                }
                __result = newList;
            }
        }
    }

    [HarmonyPatch(typeof(FloatMenuMakerMap), "CanTakeOrder")]
    public static class OrderFix
    {
        [HarmonyPostfix]
        public static void CommandTheDamnShuttle(Pawn pawn, ref bool __result)
        {
            if (pawn.TryGetComp<CompBecomeBuilding>() != null)
                __result = true;
        }
    }

    [HarmonyPatch(typeof(Caravan), "GetGizmos")]
    public static class OtherGizmoFix
    {
        [HarmonyPostfix]
        public static void AddTheStuffToTheYieldReturnedEnumeratorThingy(Caravan __instance, ref IEnumerable<Gizmo> __result)
        {
            if (__instance == null || __result == null)
                return;

            List<Gizmo> newList = new List<Gizmo>();
            foreach (Gizmo g in __result) {
                newList.Add(g);
            }

            float shuttleCarryWeight = 0;
            float pawnWeight = 0;
            float minRange = float.MaxValue;
            bool allFullyFueled = true;
            List<Pawn> shuttlesToRefuel = new List<Pawn>();
            List<Thing> CaravanThings = CaravanInventoryUtility.AllInventoryItems(__instance);
            foreach (Pawn p in __instance.pawns) {
                if (p.TryGetComp<CompBecomeBuilding>() != null) {
                    shuttleCarryWeight += p.TryGetComp<CompBecomeBuilding>().Props.buildingDef.GetCompProperties<CompProperties_Transporter>().massCapacity;
                    if (p.TryGetComp<CompRefuelable>() != null && p.TryGetComp<CompRefuelable>().Fuel / p.TryGetComp<CompBecomeBuilding>().Props.buildingDef.GetCompProperties<CompProperties_ShuttleLaunchable>().fuelPerTile < minRange) {
                        minRange = p.TryGetComp<CompRefuelable>().Fuel / p.TryGetComp<CompBecomeBuilding>().Props.buildingDef.GetCompProperties<CompProperties_ShuttleLaunchable>().fuelPerTile;
                    }
                    if (p.TryGetComp<CompRefuelable>() != null && p.TryGetComp<CompRefuelable>().FuelPercentOfMax < 0.8f) {
                        foreach (Thing t in CaravanThings) {
                            if (p.TryGetComp<CompRefuelable>().Props.fuelFilter.Allows(t.def))
                            {
                                shuttlesToRefuel.Add(p);
                                break;
                            }
                        }
                        allFullyFueled = false;
                    }
                } else if (p.TryGetComp<CompShuttleLaunchable>() == null) {
                    pawnWeight += p.def.BaseMass;
                }
            }
            if (shuttleCarryWeight > 0) {
                float totalMass = pawnWeight + __instance.MassUsage;
                Gizmo launchGizmo = new Command_Action
                {
                    defaultLabel = "Launch Caravan",
                    defaultDesc = "Load this caravan into shuttle(s) and launch it",
                    icon = CompShuttleLaunchable.LaunchCommandTex,
                    action = delegate
                    {
                        ShuttleCaravanUtility.LaunchMe(__instance, minRange, allFullyFueled);
                    }
                };

                if (totalMass > shuttleCarryWeight)
                    launchGizmo.Disable("Caravan is too heavy for shuttle(s) to carry: " + totalMass + "/" + shuttleCarryWeight);

                newList.Add(launchGizmo);
            }
            if (shuttlesToRefuel.Count > 0) {
                Gizmo refuelGizmo = new Command_Action {
                    defaultLabel = "Refuel Shuttles",
                    defaultDesc = "Use caravan inventory to refuel shuttle(s)",
                    icon = CompShuttleLaunchable.SetTargetFuelLevelCommand,
                    action = delegate {
                        ShuttleCaravanUtility.RefuelMe(__instance, shuttlesToRefuel);
                    }
                };

                newList.Add(refuelGizmo);
            }

            List<MinifiedThing> inactiveShuttles = new List<MinifiedThing>();
            foreach (Thing t in __instance.AllThings)
            {
                if (t is MinifiedThing)
                {
                    MinifiedThing building = (MinifiedThing)t;
                    if (building.InnerThing.TryGetComp<CompShuttleLaunchable>() != null)
                    {
                        inactiveShuttles.Add(building);
                    }
                }
            }
            List<MinifiedThing> fuelableShuttles = new List<MinifiedThing>();
            foreach (MinifiedThing building in inactiveShuttles)
            {
                if (building.InnerThing.TryGetComp<CompRefuelable>() == null)
                {
                    fuelableShuttles.Add(building);
                }
                else if (building.InnerThing.TryGetComp<CompRefuelable>().HasFuel)
                {
                    fuelableShuttles.Add(building);
                }
                else
                {
                    foreach (Thing tee in CaravanInventoryUtility.AllInventoryItems(__instance))
                    {
                        if (building.InnerThing.TryGetComp<CompRefuelable>().Props.fuelFilter.Allows(tee.def))
                        {
                            fuelableShuttles.Add(building);
                            break;
                        }
                    }
                }
            }
            if (fuelableShuttles.Count > 0)
            {
                Gizmo activateGizmo = new Command_Action
                {
                    defaultLabel = "Activate Shuttles",
                    defaultDesc = "Activate shuttle(s) and refuel them if possible",
                    icon = CompShuttleLaunchable.SetTargetFuelLevelCommand,
                    action = delegate {
                        ShuttleCaravanUtility.ActivateMe(__instance, fuelableShuttles);
                    }
                };

                newList.Add(activateGizmo);
            }

            __result = newList;
        }
    }

    [HarmonyPatch(typeof(MassUtility), "Capacity")]
    public static class FixShuttleCarryCap
    {
        [HarmonyPostfix]
        public static void RealCarryWeight(ref float __result, Pawn p)
        {
            if (p.TryGetComp<CompBecomeBuilding>() != null) {
                __result = p.TryGetComp<CompBecomeBuilding>().Props.buildingDef.GetCompProperties<CompProperties_Transporter>().massCapacity;
            }
        }
    }

    [HarmonyPatch(typeof(CaravanUIUtility), "AddPawnsSections")]
    public static class UIFix
    {
        [HarmonyPostfix]
        public static void replaceit(TransferableOneWayWidget widget, List<TransferableOneWay> transferables)
        {
            if (Find.WorldSelector.FirstSelectedObject == null || !(Find.WorldSelector.FirstSelectedObject is MapParent) || ((MapParent)Find.WorldSelector.FirstSelectedObject).Map == null || !((MapParent)Find.WorldSelector.FirstSelectedObject).Map.IsPlayerHome)
            {
                IEnumerable<TransferableOneWay> source = from x in transferables
                                                         where x.ThingDef.category == ThingCategory.Pawn
                                                         select x;
                widget.AddSection(TranslatorFormattedStringExtensions.Translate("SoSShuttles"), from x in source
                                                                                                where (((Pawn)x.AnyThing).TryGetComp<CompBecomeBuilding>() != null)
                                                                                                select x);
            }
        }
    }

    [HarmonyPatch(typeof(TravelingTransportPods))]
    [HarmonyPatch("Start", MethodType.Getter)]
    public static class FromSpaceship
    {
        [HarmonyPostfix]
        public static void StartAtShip(TravelingTransportPods __instance, ref Vector3 __result)
        {
            int initialTile = (int)typeof(TravelingTransportPods).GetField("initialTile", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
            foreach (WorldObject ship in Find.World.worldObjects.AllWorldObjects.Where(o => o is WorldObjectOrbitingShip))
                if (ship.Tile == initialTile)
                    __result = ship.DrawPos;
            foreach (WorldObject site in Find.World.worldObjects.AllWorldObjects.Where(o => o is SpaceSite || o is MoonBase))
                if (site.Tile == initialTile)
                    __result = site.DrawPos;
        }
    }

    [HarmonyPatch(typeof(TravelingTransportPods))]
    [HarmonyPatch("End", MethodType.Getter)]
    public static class ToSpaceship
    {
        [HarmonyPostfix]
        public static void EndAtShip(TravelingTransportPods __instance, ref Vector3 __result)
        {
            int destTile = __instance.destinationTile;
            foreach (WorldObject ship in Find.World.worldObjects.AllWorldObjects.Where(o => o is WorldObjectOrbitingShip))
                if (ship.Tile == destTile)
                    __result = ship.DrawPos;
            foreach (WorldObject site in Find.World.worldObjects.AllWorldObjects.Where(o => o is SpaceSite || o is MoonBase))
                if (site.Tile == destTile)
                    __result = site.DrawPos;
        }
    }

    [HarmonyPatch(typeof(TransportPodsArrivalAction_GiveToCaravan), "StillValid")]
    public static class MakeSureNotToLoseYourShuttle
    {
        static bool hasShuttle = false;
        [HarmonyPrefix]
        public static bool DisableMaybe(IEnumerable<IThingHolder> pods)
        {
            hasShuttle = false;
            foreach (IThingHolder pod in pods)
            {
                foreach (Thing t in pod.GetDirectlyHeldThings())
                {
                    if (t.TryGetComp<CompBecomeBuilding>() != null)
                    {
                        hasShuttle = true;
                        return false;
                    }
                }
            }
            return true;
        }
        [HarmonyPostfix]
        public static void AlwaysTrue(ref FloatMenuAcceptanceReport __result)
        {
            if (hasShuttle)
                __result = true;
        }
    }

    //This is slow and shitty, but Tynan didn't leave us many options to avoid a nullref
    [HarmonyPatch(typeof(PawnCapacitiesHandler), "CapableOf")]
    public static class ShuttlesCannotConstruct
    {
        public static void Postfix(PawnCapacityDef capacity, PawnCapacitiesHandler __instance, ref bool __result)
        {
            if(capacity==PawnCapacityDefOf.Manipulation)
            {
                Pawn pawn = (Pawn)typeof(PawnCapacitiesHandler).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
                if (pawn.TryGetComp<CompBecomeBuilding>() != null)
                    __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_MeleeVerbs), "ChooseMeleeVerb")]
    public static class ThatWasAnOldBug
    {
        public static bool Prefix(Pawn_MeleeVerbs __instance)
        {
            return __instance.Pawn.TryGetComp<CompBecomeBuilding>() == null;
        }
    }

    [HarmonyPatch(typeof(Skyfaller), "HitRoof")]
    public static class ShuttleBayAcceptsShuttle
    {
        [HarmonyPrefix]
        public static bool NoHitRoof(Skyfaller __instance)
        {
            if (__instance.Position.GetThingList(__instance.Map).Any(t =>
                t.def.defName.Equals("ShipShuttleBay") || t.def.defName.Equals("ShipShuttleBayLarge") || t.def.defName.Equals("ShipSalvageBay")))
            {
                return false;
            }
            if (__instance.Map.GetComponent<ShipHeatMapComp>().InCombat && __instance.def.defName.Equals("ShuttleIncomingPersonal"))
            {
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(TransportPodsArrivalActionUtility), "DropTravelingTransportPods")]
    public static class ShuttleBayArrivalPrecision
    {
        [HarmonyPrefix]
        public static bool LandInBay(List<ActiveDropPodInfo> dropPods, IntVec3 near, Map map)
        {
            if (map.Parent != null && map.Parent.def.defName.Equals("ShipOrbiting"))
            {
                TransportPodsArrivalActionUtility.RemovePawnsFromWorldPawns(dropPods);
                for (int i = 0; i < dropPods.Count; i++)
                {
                    DropPodUtility.MakeDropPodAt(near, map, dropPods[i]);
                }

                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(ShipLandingArea), "RecalculateBlockingThing")]
    public static class ShipLandingAreaUnderShipRoof
    {
        [HarmonyPrefix]
        public static bool IgnoreShipRoof(Map ___map, CellRect ___rect, ref bool ___blockedByRoof, ref Thing ___firstBlockingThing)
        {
            ___blockedByRoof = false;
            foreach (IntVec3 c in ___rect)
            {
                if (c.Roofed(___map) && ___map.roofGrid.RoofAt(c) == ShipInteriorMod2.shipRoofDef)
                {
                    List<Thing> thingList = c.GetThingList(___map);
                    for (int i = 0; i < thingList.Count; i++)
                    {
                        if ((!(thingList[i] is Pawn) && (thingList[i].def.Fillage != FillCategory.None || thingList[i].def.IsEdifice() || thingList[i] is Skyfaller)) && (!thingList[i].def.defName.Equals("ShipShuttleBay") && !thingList[i].def.defName.Equals("ShipShuttleBayLarge") && thingList[i].def != ShipInteriorMod2.hullPlateDef && thingList[i].def != ShipInteriorMod2.mechHullPlateDef && thingList[i].def != ShipInteriorMod2.archoHullPlateDef))
                        {
                            ___firstBlockingThing = thingList[i];
                            return false;
                        }
                    }
                }
                else
                    return true;
            }
            ___firstBlockingThing = null;
            return false;
        }
    }

    [HarmonyPatch(typeof(DropCellFinder), "TradeDropSpot")]
    public static class InSpaceDropStuffInsideMe
    {
        [HarmonyPostfix]
        public static void GiveItToMe(Map map, ref IntVec3 __result)
        {
            //find first salvagebay
            Building b = map.listerBuildings.allBuildingsColonist.Where(x => x.def == ThingDef.Named("ShipSalvageBay")).FirstOrDefault();
            if (map.IsSpace() && b != null)
                __result = b.Position;
        }
    }

    [HarmonyPatch(typeof(Dialog_LoadTransporters), "AddPawnsToTransferables", null)]
    public static class TransportPrisoners_Patch
    {
        [HarmonyPrefix]
        public static bool DownedPawns_AddToTransferables(Dialog_LoadTransporters __instance)
        {
            List<Pawn> list = CaravanFormingUtility.AllSendablePawns(
                (Map)typeof(Dialog_LoadTransporters)
                    .GetField("map", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance), true,
                true);
            for (int i = 0; i < list.Count; i++)
            {
                typeof(Dialog_LoadTransporters)
                    .GetMethod("AddToTransferables", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(__instance, new object[1] { list[i] });
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(QuestPart_DropPods), "GetRandomDropSpot")]
    public static class DropIntoShuttleBay
    {
        public static void Postfix(QuestPart_DropPods __instance, ref IntVec3 __result)
        {
            if (__instance.mapParent.Map.IsSpace())
            {
                IEnumerable<Thing> bays = __instance.mapParent.Map.listerThings.AllThings.Where(t => t.def.defName.Equals("ShipShuttleBay") || t.def.defName.Equals("ShipShuttleBayLarge") || t.def.defName.Equals("ShipSalvageBay"));
                if (bays.Any())
                {
                    __result = bays.RandomElement().Position;
                }
            }
        }
    }

    [HarmonyPatch(typeof(ShipLandingBeaconUtility), "GetLandingZones")]
    public static class RoyaltyShuttlesLandOnBays
    {
        public static void Postfix(Map map, ref List<ShipLandingArea> __result)
        {
            foreach (Building landingSpot in map.listerBuildings.AllBuildingsColonistOfDef(ThingDef.Named("ShipShuttleBay")))
            {
                ShipLandingArea area = new ShipLandingArea(landingSpot.OccupiedRect(), map);
                area.RecalculateBlockingThing();
                __result.Add(area);
            }
            foreach (Building landingSpot in map.listerBuildings.AllBuildingsColonistOfDef(ThingDef.Named("ShipShuttleBayLarge")))
            {
                ShipLandingArea area = new ShipLandingArea(landingSpot.OccupiedRect(), map);
                area.RecalculateBlockingThing();
                __result.Add(area);
            }
        }
    }

    /*[HarmonyPatch(typeof(ActiveDropPod),"PodOpen")]
	public static class ActivePodFix{
		[HarmonyPrefix]
		public static bool PatchThat (ref ActiveDropPod __instance)
		{
			if(__instance.def.defName.Equals("ActiveShuttle"))
			{
				ThingOwner stuffInPod = ((ActiveDropPodInfo)typeof(ActiveDropPod).GetField ("contents", BindingFlags.Instance | BindingFlags.NonPublic).GetValue (__instance)).innerContainer;
				Pawn shuttleLanded = null;
				List<Thing> fillTheShuttle = new List<Thing> ();
				for (int i = stuffInPod.Count - 1; i >= 0; i--)
				{
					Thing thing = stuffInPod[i];
					if (thing is Pawn) {
						Pawn pawn = (Pawn)thing;
						GenPlace.TryPlaceThing (thing, __instance.Position, __instance.Map, ThingPlaceMode.Near);
						if (thing.TryGetComp<CompBecomeBuilding> () != null)
							shuttleLanded = pawn;
						if (pawn.RaceProps.Humanlike) {
							TaleRecorder.RecordTale (TaleDefOf.LandedInPod, new object[] {
								pawn
							});
						}
						if (pawn.IsColonist && pawn.Spawned && !__instance.Map.IsPlayerHome) {
							pawn.drafter.Drafted = true;
						}
					} else
						fillTheShuttle.Add (thing);
				}
				if (shuttleLanded != null) {
					ThingOwner shuttleInventory = shuttleLanded.inventory.innerContainer;
					foreach (Thing thing in fillTheShuttle) {
						stuffInPod.Remove (thing);
						shuttleInventory.TryAdd (thing);
					}
				}
				stuffInPod.ClearAndDestroyContents(DestroyMode.Vanish);
				SoundDef.Named("DropPodOpen").PlayOneShot(new TargetInfo(__instance.Position, __instance.Map, false));
				__instance.Destroy(DestroyMode.Vanish);
				return false;
			}
			return true;
		}
	}*/

    /*[HarmonyPatch(typeof(Pawn))]
	[HarmonyPatch("IsColonist",MethodType.Getter)]
	public static class GizmoFix{
		[HarmonyPostfix]
		public static void GetAllTheGizmos(Pawn __instance, ref bool __result)
		{
			if (__instance.TryGetComp<CompBecomeBuilding> () != null && !System.Environment.StackTrace.Contains("AllMapsCaravansAndTravelingTransportPods_Colonists")) {
				__result=true;
				if (__instance.drafter == null) {
					__instance.drafter = new Pawn_DraftController (__instance);
				}
				if (__instance.equipment == null) {
					__instance.equipment = new Pawn_EquipmentTracker (__instance);
				}
			}
		}
	}*/
}