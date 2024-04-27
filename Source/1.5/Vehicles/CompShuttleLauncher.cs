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
				if (mapComp.IsPlayerShipMap)
				{
					Command_Action board = CommandBoard(vehicle);
					if (!ShipInteriorMod2.ShuttleCanBoard(mapComp.TargetMapComp, vehicle))
                    {
                        board.Disable();
						board.disabledReason = TranslatorFormattedStringExtensions.Translate("SoS.ShuttleMissionBoardingDisabled");
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
                        yield return CommandIntercept(vehicle);
					if (hasLaser || ShipInteriorMod2.ShuttleHasPlasma(vehicle))
						yield return CommandStrafe(vehicle);
					if (ShipInteriorMod2.ShuttleHasTorp(vehicle))
						yield return CommandBomb(vehicle);
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

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<float>(ref retreatAtHealth, "retreatAtHealth");
        }
    }
}
