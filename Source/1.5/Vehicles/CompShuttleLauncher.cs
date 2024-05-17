using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Vehicles;
using Verse;

namespace SaveOurShip2.Vehicles
{
    class CompShuttleLauncher : CompVehicleLauncher
    {
        public float retreatAtHealth;

        //Some reasoning is probably necessary for the following horrible kludges. Shuttle upgrades are added at runtime through CompUpgradeTree, and won't be saved by PostExpose.
        //Sooooo... we're doing it here. Values are applied in PostSpawnNewComponents, in HarmonyPatches.cs.
        public float serializedShieldGenHealth;
        public float serializedHeat;


        public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			var mapComp = parent.Map.GetComponent<ShipMapComp>();
			foreach (Gizmo giz in base.CompGetGizmosExtra())
                yield return giz;
            if (parent.Faction != Faction.OfPlayer)
                yield break;
            yield return new ShuttleRetreatGizmo(this);
			VehiclePawn vehicle = (VehiclePawn)parent;
			if (mapComp?.ShipMapState == ShipMapState.inCombat && vehicle.handlers[0].handlers.Count > 0)
			{
                bool launchDisabled = vehicle.Map.roofGrid.Roofed(vehicle.Position) && !ShipInteriorMod2.CanLaunchUnderRoof(vehicle);
				if (mapComp.IsPlayerShipMap)
				{
					Command_Action board = CommandBoard(vehicle);
					if (launchDisabled)
                    {
                        board.Disable();
						board.disabledReason = TranslatorFormattedStringExtensions.Translate("SoS.ShuttleMissionBoardingDisabled");
					}
					else if (!ShipInteriorMod2.ShuttleShouldBoard(mapComp.TargetMapComp, vehicle))
					{
						board.defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.ShuttleMissionBoardingWarn");
						board.defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.ShuttleMissionBoardingWarnDesc");
					}
					yield return board;
				}
                else
                    yield return CommandGoHome(vehicle);
				//samey in ShuttleTakeoff.FloatMenuMissions
				if (vehicle.CompUpgradeTree != null)
				{
                    bool hasLaser = ShipInteriorMod2.ShuttleHasLaser(vehicle);
					if (hasLaser)
					{
						Command_Action intercept = CommandIntercept(vehicle);
						yield return intercept;
						if (launchDisabled)
						{
							intercept.Disable();
							intercept.disabledReason = TranslatorFormattedStringExtensions.Translate("SoS.ShuttleMissionBoardingDisabled");
						}
					}
					if (hasLaser || ShipInteriorMod2.ShuttleHasPlasma(vehicle))
					{
						Command_Action strafe = CommandStrafe(vehicle);
						yield return strafe;
						if (launchDisabled)
						{
							strafe.Disable();
							strafe.disabledReason = TranslatorFormattedStringExtensions.Translate("SoS.ShuttleMissionBoardingDisabled");
						}
					}
					if (ShipInteriorMod2.ShuttleHasTorp(vehicle))
					{
						Command_Action bomb = CommandBomb(vehicle);
						yield return bomb;
						if (launchDisabled)
						{
							bomb.Disable();
							bomb.disabledReason = TranslatorFormattedStringExtensions.Translate("SoS.ShuttleMissionBoardingDisabled");
						}
					}
				}
            }
        }
        Command_Action CommandBoard(VehiclePawn vehicle)
        {
            return new Command_Action
            {
                action = delegate
                {
                    ShuttleTakeoff.LaunchShuttleToCombatManager(vehicle, ShipMapComp.ShuttleMission.BOARD);
                },
                defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.ShuttleMissionBoarding"),
                defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.ShuttleMissionBoardingDesc"),
                icon = ContentFinder<Texture2D>.Get("UI/ShuttleMissionBoarding", true)
            };
        }

        Command_Action CommandGoHome(VehiclePawn vehicle)
        {
            return new Command_Action
            {
                action = delegate
                {
                    ShuttleTakeoff.LaunchShuttleToCombatManager(vehicle, ShipMapComp.ShuttleMission.BOARD);
                },
                defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.ShuttleMissionBoardingReturn"),
                defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.ShuttleMissionBoardingReturnDesc"),
                icon = ContentFinder<Texture2D>.Get("UI/ShuttleMissionBoarding", true)
            };
        }

        Command_Action CommandIntercept(VehiclePawn vehicle)
        {
            return new Command_Action
            {
                action = delegate
                {
                    ShuttleTakeoff.LaunchShuttleToCombatManager(vehicle, ShipMapComp.ShuttleMission.INTERCEPT);
                },
                defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.ShuttleMissionIntercept"),
                defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.ShuttleMissionInterceptDesc"),
                icon = ContentFinder<Texture2D>.Get("UI/ShuttleMissionIntercept", true)
            };
        }

        Command_Action CommandStrafe(VehiclePawn vehicle)
        {
            return new Command_Action
            {
                action = delegate
                {
                    ShuttleTakeoff.LaunchShuttleToCombatManager(vehicle, ShipMapComp.ShuttleMission.STRAFE);
                },
                defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.ShuttleMissionStrafe"),
                defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.ShuttleMissionStrafeDesc"),
                icon = ContentFinder<Texture2D>.Get("UI/ShuttleMissionStrafe", true)
            };
        }

        Command_Action CommandBomb(VehiclePawn vehicle)
        {
            return new Command_Action
            {
                action = delegate
                {
                    ShuttleTakeoff.LaunchShuttleToCombatManager(vehicle, ShipMapComp.ShuttleMission.BOMB);
                },
                defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.ShuttleMissionBomb"),
                defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.ShuttleMissionBombDesc"),
                icon = ContentFinder<Texture2D>.Get("UI/ShuttleMissionBomb", true)
            };
        }

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			var bay = parent.Position.GetThingList(parent.Map).Where(t => t.TryGetComp<CompShipBay>() != null)?.FirstOrDefault();
			if (bay != null)
			{
				Log.Message("Deregistered shuttle reserved area on bay at: " + parent.Position);
				bay.TryGetComp<CompShipBay>().UnReserveArea(parent.Position, parent as VehiclePawn);
			}
        }
		public override void PostExposeData()
        {
            base.PostExposeData();
            if(Scribe.mode==LoadSaveMode.Saving)
            {
                CompVehicleHeatNet heatNet = Vehicle.GetComp<CompVehicleHeatNet>();
                if (heatNet != null && heatNet.myNet != null)
                    serializedHeat = heatNet.myNet.StorageUsed;
                VehicleComponent shieldGen = Vehicle.statHandler.components.FirstOrDefault(comp => comp.props.key == "shieldGenerator");
                if (shieldGen != null)
                    serializedShieldGenHealth = shieldGen.health;
                else
                    serializedShieldGenHealth = 1;
            }
            Scribe_Values.Look<float>(ref retreatAtHealth, "retreatAtHealth");
            Scribe_Values.Look<float>(ref serializedHeat, "heat");
            Scribe_Values.Look<float>(ref serializedShieldGenHealth, "shieldGenHealth");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                PostSpawnNewComponents.ShieldGenHealth = serializedShieldGenHealth;
                PostSpawnNewComponents.StoredHeat = serializedHeat;
            }
        }
    }
}
