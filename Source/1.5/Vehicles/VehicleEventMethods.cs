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
            if (net != null)
                net.RebuildHeatNet();
        }

        public static void ShuttleLanded(DefaultTakeoff landing)
        {
            CompVehicleHeatNet net = landing.vehicle.TryGetComp<CompVehicleHeatNet>();
            if (net != null)
                net.RebuildHeatNet();
        }
    }
}
