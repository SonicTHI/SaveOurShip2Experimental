using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vehicles;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace SaveOurShip2.Vehicles
{
    class AerialVehicleArrivalAction_LoadMapAndDefog : AerialVehicleArrivalAction_LoadMap
    {
        public AerialVehicleArrivalAction_LoadMapAndDefog() : base()
        {

        }

        public AerialVehicleArrivalAction_LoadMapAndDefog(VehiclePawn vehicle, LaunchProtocol launchProtocol, int tile, AerialVehicleArrivalModeDef arrivalModeDef)
        : base(vehicle, launchProtocol, tile, arrivalModeDef)
        {

        }

        public override bool Arrived(int tile)
        {
            LongEventHandler.QueueLongEvent((Action)delegate
            {
                Map map = GetOrGenerateMapUtility.GetOrGenerateMap(tile, null);
                MapLoaded(map);
                FloodFillerFog.FloodUnfog(CellFinderLoose.TryFindCentralCell(map, 7, 10, (IntVec3 x) => !x.Roofed(map)), map);
                ExecuteEvents();
                arrivalModeDef.Worker.VehicleArrived(vehicle, launchProtocol, map);
            }, "GeneratingMap", false, null, true, null);
            return true;
        }
    }
}
