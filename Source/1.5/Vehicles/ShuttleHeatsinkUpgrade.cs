using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vehicles;

namespace SaveOurShip2.Vehicles
{
    class ShuttleHeatsinkUpgrade : Upgrade
    {
        public CompProps_ShipHeat sink;

        public override bool UnlockOnLoad => true;

        public override void Refund(VehiclePawn vehicle)
        {
            CompShipHeat heatComp = GetMatchingComp(vehicle);
            if (heatComp != null)
                vehicle.RemoveComp(heatComp);

            if (vehicle.GetComp<CompShipHeat>() == null)
                vehicle.RemoveComp(vehicle.GetComp<CompVehicleHeatNet>());
            else
                vehicle.GetComp<CompVehicleHeatNet>().RebuildHeatNet();
        }

        public override void Unlock(VehiclePawn vehicle, bool unlockingAfterLoad)
        {
            //Check if we've already unlocked this... unlocking on load is unpredictable at times
            if (GetMatchingComp(vehicle) != null)
                return;

            CompVehicleHeatNet net = vehicle.GetComp<CompVehicleHeatNet>();
            if (net == null)
            {
                net = new CompVehicleHeatNet();
                net.parent = vehicle;
                if (!unlockingAfterLoad)
                    vehicle.comps.Add(net);
                else
                    PostLoadNewComponents.CompsToAdd.Add(net);
            }
            CompShipHeatSink mySink = new CompShipHeatSink();
            mySink.Initialize(sink);
            mySink.parent = vehicle;
            if (!unlockingAfterLoad)
            {
                vehicle.comps.Add(mySink);
                vehicle.RecacheComponents();
                mySink.PostSpawnSetup(unlockingAfterLoad);
                net.RebuildHeatNet();
            }
            else
                PostLoadNewComponents.CompsToAdd.Add(mySink);
        }

        CompShipHeat GetMatchingComp(VehiclePawn vehicle)
        {
            CompShipHeat match = null;
            foreach (CompShipHeatSink heat in vehicle.GetComps<CompShipHeatSink>())
            {
                if (heat.Props.heatCapacity == sink.heatCapacity && heat.Props.heatLoss == sink.heatLoss && heat.Props.heatVent == sink.heatVent)
                {
                    match = heat;
                    break;
                }
            }
            return match;
        }
    }
}