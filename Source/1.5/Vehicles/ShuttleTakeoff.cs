using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Vehicles;
using Verse;
using static SaveOurShip2.ShipMapComp;

namespace SaveOurShip2.Vehicles
{
    class ShuttleTakeoff : VTOLTakeoff
    {
        public ShuttleMissionData TempMissionRef;

        public ShuttleTakeoff()
        {

        }

        public ShuttleTakeoff(VTOLTakeoff reference, VehiclePawn vehicle) : base(reference, vehicle)
        {

        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptionsAt(int tile)
		{
            //td not sure the limits we want on this, right now you could assault from anywhere, should it be only from ship-ship?
            var mp = Find.World.worldObjects.MapParentAt(tile);
            if (mp != null && (mp.def == ResourceBank.WorldObjectDefOf.ShipOrbiting || mp.def == ResourceBank.WorldObjectDefOf.ShipEnemy)) //target is ship
			{
				var mapComp = mp.Map.GetComponent<ShipMapComp>();
				if (mapComp.ShipMapState == ShipMapState.inCombat) //target is in combat
				{
					if (mapComp.map != mapComp.ShipCombatOriginMap) //target is enemy ship
					{
						foreach (FloatMenuOption giz in FloatMenuMissions(tile, mapComp))
							yield return giz;
                        yield break;
					}
					else //target is player ship
					{
						yield return FloatMenuOption_ReturnFromEnemy(tile);
					}
				}
			}
            List<FloatMenuOption> baseOptions = new List<FloatMenuOption>(base.GetFloatMenuOptionsAt(tile));
            if(baseOptions.Count==0)
            {
                if (mp!=null&&!mp.HasMap)
                {
                    foreach (FloatMenuOption option in VehicleArrivalActionUtility.GetFloatMenuOptions(() => true, () => new AerialVehicleArrivalAction_LoadMapAndDefog(vehicle, this, tile, AerialVehicleArrivalModeDefOf.TargetedLanding), TranslatorFormattedStringExtensions.Translate("VF_LandVehicleTargetedLanding", mp, vehicle, tile), vehicle, tile))
                        yield return option;
                }
            }
            else
            {
                foreach (FloatMenuOption option in baseOptions)
                    yield return option;
            }
            
        }

        public IEnumerable<FloatMenuOption> FloatMenuMissions(int tile, ShipMapComp mapComp)
		{
			yield return FloatMenuOption_Board(tile, mapComp);
			//samey in CompShuttleLauncher.CompGetGizmosExtra
			if (vehicle.CompUpgradeTree != null)
			{
				bool hasLaser = ShipInteriorMod2.ShuttleHasLaser(vehicle);
				if (hasLaser)
					yield return FloatMenuOption_Intercept(tile);
				if (hasLaser || ShipInteriorMod2.ShuttleHasPlasma(vehicle))
					yield return FloatMenuOption_Strafe(tile);
				if (ShipInteriorMod2.ShuttleHasTorp(vehicle))
					yield return FloatMenuOption_Bomb(tile);
			}
        }

        FloatMenuOption FloatMenuOption_Board(int tile, ShipMapComp mapComp)
        {
            string text = "SoS.ShuttleMissionFloatBoardWarn".Translate();
            if (ShipInteriorMod2.ShuttleShouldBoard(mapComp, vehicle))
                text = "SoS.ShuttleMissionFloatBoard".Translate();
            return new FloatMenuOption(text, delegate { LaunchShuttleToCombatManager(vehicle, ShuttleMission.BOARD); });
        }

        FloatMenuOption FloatMenuOption_Intercept(int tile)
        {
            return new FloatMenuOption("SoS.ShuttleMissionFloatIntercept".Translate(), delegate { LaunchShuttleToCombatManager(vehicle, ShuttleMission.INTERCEPT); });
        }

        FloatMenuOption FloatMenuOption_Strafe(int tile)
        {
            return new FloatMenuOption("SoS.ShuttleMissionFloatStrafe".Translate(), delegate { LaunchShuttleToCombatManager(vehicle, ShuttleMission.STRAFE); });
        }

        FloatMenuOption FloatMenuOption_Bomb(int tile)
        {
            return new FloatMenuOption("SoS.ShuttleMissionFloatTorpedo".Translate(), delegate { LaunchShuttleToCombatManager(vehicle, ShuttleMission.BOMB); });
        }

        FloatMenuOption FloatMenuOption_ReturnFromEnemy(int tile)
        {
            return new FloatMenuOption("SoS.ShuttleMissionFloatReturn".Translate(), delegate { LaunchShuttleToCombatManager(vehicle, ShuttleMission.BOARD); });
        }

        public static void LaunchShuttleToCombatManager(VehiclePawn vehicle, ShuttleMission mission)
        {
            vehicle.CompVehicleLauncher.inFlight = true;
            vehicle.CompVehicleLauncher.launchProtocol.OrderProtocol(LaunchProtocol.LaunchType.Takeoff);
            VehicleSkyfaller_Leaving vehicleSkyfaller_Leaving = (VehicleSkyfaller_Leaving)VehicleSkyfallerMaker.MakeSkyfaller(vehicle.CompVehicleLauncher.Props.skyfallerLeaving, vehicle);
            vehicleSkyfaller_Leaving.vehicle = vehicle;
            vehicleSkyfaller_Leaving.createWorldObject = false;
            GenSpawn.Spawn(vehicleSkyfaller_Leaving, vehicle.Position, vehicle.Map, vehicle.CompVehicleLauncher.launchProtocol.CurAnimationProperties.forcedRotation ?? vehicle.Rotation);
            ((ShuttleTakeoff)vehicle.CompVehicleLauncher.launchProtocol).TempMissionRef = ShipInteriorMod2.FindPlayerShipMap().GetComponent<ShipMapComp>().RegisterShuttleMission(vehicle, mission);
            CameraJumper.TryHideWorld();
            vehicle.EventRegistry[VehicleEventDefOf.AerialVehicleLaunch].ExecuteEvents();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<ShuttleMissionData>(ref TempMissionRef, "missionRef");
        }
    }
}
