using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vehicles;

namespace SaveOurShip2.Vehicles
{
    class ShuttlePassengerUpgrade : Upgrade
    {
        public int passengers;

        public override bool UnlockOnLoad => true;

        public override void Refund(VehiclePawn vehicle)
        {
            vehicle.handlers.Where(handler => handler.role.key == "passenger").FirstOrDefault().role.slots -= passengers;
        }

        public override void Unlock(VehiclePawn vehicle, bool unlockingAfterLoad)
        {
            //Check for existing passengers... unlocking on load is unpredictable
            VehicleHandler passengerHandler = vehicle.handlers.Where(handler => handler.role.key == "passenger").FirstOrDefault();
            if (passengerHandler.role.slots>0)
                return;

            passengerHandler.role.slots += passengers;
        }
    }
}