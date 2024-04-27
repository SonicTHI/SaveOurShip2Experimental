using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            var mapComp = vehicle.Map.GetComponent<ShipMapComp>();
			bool isPlayerMap = mapComp?.ShipCombatOriginMap == vehicle.Map;
            bool isEnemyMap = mapComp?.ShipCombatTargetMap == vehicle.Map;
            if (!isPlayerMap && !isEnemyMap)
                return base.GetFloatMenuOptionsAt(tile);
            if (isPlayerMap)
            {
                if (mapComp.ShipCombatTargetMap?.Tile == tile) //Launch mission against enemy ship
                    return FloatMenuMissions(tile, mapComp);
                else
                    return base.GetFloatMenuOptionsAt(tile);
            }

            if (mapComp.ShipCombatOriginMap?.Tile == tile) //Return home
                return FloatMenuOption_ReturnFromEnemy(tile);
            else if (mapComp.ShipCombatTargetMap?.Tile == tile) //Target enemy ship to change mission
                return FloatMenuMissions(tile, mapComp);
            else
                return base.GetFloatMenuOptionsAt(tile);
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

        IEnumerable<FloatMenuOption> FloatMenuOption_ReturnFromEnemy(int tile)
        {
            yield return new FloatMenuOption("Return to Ship", delegate { LaunchShuttleToCombatManager(vehicle, ShuttleMission.BOARD); });
        }

        public static void LaunchShuttleToCombatManager(VehiclePawn vehicle, ShuttleMission mission)
        {
            vehicle.CompVehicleLauncher.inFlight = true;
            vehicle.CompVehicleLauncher.launchProtocol.OrderProtocol(LaunchProtocol.LaunchType.Takeoff);
            VehicleSkyfaller_Leaving vehicleSkyfaller_Leaving = (VehicleSkyfaller_Leaving)VehicleSkyfallerMaker.MakeSkyfaller(vehicle.CompVehicleLauncher.Props.skyfallerLeaving, vehicle);
            vehicleSkyfaller_Leaving.vehicle = vehicle;
            vehicleSkyfaller_Leaving.createWorldObject = false;
            GenSpawn.Spawn(vehicleSkyfaller_Leaving, vehicle.Position, vehicle.Map, vehicle.CompVehicleLauncher.launchProtocol.CurAnimationProperties.forcedRotation ?? vehicle.Rotation);
            ((ShuttleTakeoff)vehicle.CompVehicleLauncher.launchProtocol).TempMissionRef = vehicle.Map.GetComponent<ShipMapComp>().OriginMapComp.RegisterShuttleMission(vehicle, mission);
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
