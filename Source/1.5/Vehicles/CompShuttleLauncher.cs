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
				//only pods incombat if enemy t/w above, same in ShuttleTakeoff.FloatMenuMissions
				if (mapComp.IsPlayerShipMap)
                {
                    if (ModSettings_SoS.easyMode || mapComp.TargetMapComp.MapEnginePower < 0.02f || vehicle.VehicleDef == ResourceBank.ThingDefOf.SoS2_Shuttle_Personal)
					    yield return CommandBoard(vehicle);
				}
                else
                    yield return CommandGoHome(vehicle);

                if (vehicle.CompUpgradeTree != null)
				{
					var u = vehicle.CompUpgradeTree.upgrades;
					bool hasLaser = u.Contains("TurretLaserA") || u.Contains("TurretLaserB") || u.Contains("TurretLaserC");
					bool hasPlasma = u.Contains("TurretPlasmaA") || u.Contains("TurretPlasmaB") || u.Contains("TurretPlasmaC");
					bool hasTorpedo = u.Contains("TurretTorpedoA") || u.Contains("TurretTorpedoB") || u.Contains("TurretTorpedoC")
						&& vehicle.carryTracker.GetDirectlyHeldThings().Any(t => t.HasThingCategory(ResourceBank.ThingCategoryDefOf.SpaceTorpedoes));
					if (hasLaser)
						yield return CommandIntercept(vehicle);
					if (hasLaser || hasPlasma)
						yield return CommandStrafe(vehicle);
					if (hasTorpedo)
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
