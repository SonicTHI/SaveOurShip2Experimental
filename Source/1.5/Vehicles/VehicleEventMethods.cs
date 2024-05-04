using SaveOurShip2.Vehicles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vehicles;
using Verse;

namespace SaveOurShip2
{
    public static class VehicleEventMethods
    {
        public static void ShuttleLaunched(DefaultTakeoff takeoff)
        {
            CompVehicleHeatNet net = takeoff.vehicle.TryGetComp<CompVehicleHeatNet>();
            net?.RebuildHeatNet();
            if (((ShuttleTakeoff)takeoff).TempMissionRef != null)
            {
                ((ShuttleTakeoff)takeoff).TempMissionRef.liftedOffYet = true;
                ((ShuttleTakeoff)takeoff).TempMissionRef = null;
            }
        }

        public static void ShuttleLanded(DefaultTakeoff landing)
        {
            CompVehicleHeatNet net = landing.vehicle.TryGetComp<CompVehicleHeatNet>();
            net?.RebuildHeatNet();
			var bay = landing.vehicle.Position.GetThingList(landing.vehicle.Map).Where(t => t.TryGetComp<CompShipBay>() != null)?.FirstOrDefault();
			if (bay != null)
			{
				bay.TryGetComp<CompShipBay>().UnReserveArea(landing.Position, landing.vehicle);
			}
		}
    }
}
