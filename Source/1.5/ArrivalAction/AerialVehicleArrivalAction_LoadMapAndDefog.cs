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
                IntVec3 size;
                if (Find.World.worldObjects.MapParentAt(tile) is Site site)
                {
                    if (site.parts.Any(part => part.def.defName == "BlackBoxMission"))
                        size = new IntVec3(300, 1, 300);
                    else
                        size = site.PreferredMapSize;
                }
                else
                    size = Find.World.info.initialMapSize;
                Map map = GetOrGenerateMapUtility.GetOrGenerateMap(tile, size, null);
                MapLoaded(map);
                FloodFillerFog.FloodUnfog(CellFinderLoose.TryFindCentralCell(map, 7, 10, (IntVec3 x) => !x.Roofed(map)), map);
                ExecuteEvents();
                GetOrGenerateMapUtility.UnfogMapFromEdge(map);
                arrivalModeDef.Worker.VehicleArrived(vehicle, launchProtocol, map);
            }, "GeneratingMap", false, null, true, null);
            return true;
        }
    }
}
