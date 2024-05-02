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
            if (mp != null && mp.def == ResourceBank.WorldObjectDefOf.ShipOrbiting) //target is ship
			{
				var mapComp = mp.Map.GetComponent<ShipMapComp>();
				if (mapComp.ShipMapState == ShipMapState.inCombat) //target is in combat
				{
					if (mapComp.ShipCombatTargetMap != mapComp.ShipCombatOriginMap) //target is enemy ship
					{
						foreach (FloatMenuOption giz in FloatMenuMissions(tile, mapComp))
							yield return giz;
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
			if (ShipInteriorMod2.ShuttleCanBoard(mapComp, vehicle))
                yield return FloatMenuOption_Board(tile);
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

        FloatMenuOption FloatMenuOption_Board(int tile)
        {
            return new FloatMenuOption("Mission: Boarding Party", delegate { LaunchShuttleToCombatManager(vehicle, ShuttleMission.BOARD); });
        }

        FloatMenuOption FloatMenuOption_Intercept(int tile)
        {
            return new FloatMenuOption("Mission: Intercept Torpedoes/Fighters", delegate { LaunchShuttleToCombatManager(vehicle, ShuttleMission.INTERCEPT); });
        }

        FloatMenuOption FloatMenuOption_Strafe(int tile)
        {
            return new FloatMenuOption("Mission: Strafe Enemy Ship", delegate { LaunchShuttleToCombatManager(vehicle, ShuttleMission.STRAFE); });
        }

        FloatMenuOption FloatMenuOption_Bomb(int tile)
        {
            return new FloatMenuOption("Mission: Torpedo Enemy Ship", delegate { LaunchShuttleToCombatManager(vehicle, ShuttleMission.BOMB); });
        }

        FloatMenuOption FloatMenuOption_ReturnFromEnemy(int tile)
        {
            return new FloatMenuOption("Return to Ship", delegate { LaunchShuttleToCombatManager(vehicle, ShuttleMission.BOARD); });
        }

        public static void LaunchShuttleToCombatManager(VehiclePawn vehicle, ShuttleMission mission)
        {
            vehicle.CompVehicleLauncher.inFlight = true;
            vehicle.CompVehicleLauncher.launchProtocol.OrderProtocol(LaunchProtocol.LaunchType.Takeoff);
            VehicleSkyfaller_Leaving vehicleSkyfaller_Leaving = (VehicleSkyfaller_Leaving)VehicleSkyfallerMaker.MakeSkyfaller(vehicle.CompVehicleLauncher.Props.skyfallerLeaving, vehicle);
            vehicleSkyfaller_Leaving.vehicle = vehicle;
            vehicleSkyfaller_Leaving.createWorldObject = false;
            GenSpawn.Spawn(vehicleSkyfaller_Leaving, vehicle.Position, vehicle.Map, vehicle.CompVehicleLauncher.launchProtocol.CurAnimationProperties.forcedRotation ?? vehicle.Rotation);
            ((ShuttleTakeoff)vehicle.CompVehicleLauncher.launchProtocol).TempMissionRef = vehicle.Map.GetComponent<ShipMapComp>().RegisterShuttleMission(vehicle, mission);
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
