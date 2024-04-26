using SmashTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vehicles;
using Verse;

namespace SaveOurShip2.Vehicles
{
    class ShuttleShieldUpgrade : Upgrade
    {
        public CompProps_ShipHeat shield;

        public override bool UnlockOnLoad => true;

        public override void Refund(VehiclePawn vehicle)
        {
            vehicle.RemoveComp(vehicle.GetComp<CompShipHeatShield>());
            VehicleComponent shieldGenerator = vehicle.statHandler.components.First(comp => comp.props.key == "shieldGenerator");
            shieldGenerator.SetHealthModifier = 1;
            shieldGenerator.health = 1;
            if (vehicle.GetComp<CompShipHeat>() == null)
                vehicle.RemoveComp(vehicle.GetComp<CompVehicleHeatNet>());
            else
                vehicle.GetComp<CompVehicleHeatNet>().RebuildHeatNet();
        }

        public override void Unlock(VehiclePawn vehicle, bool unlockingAfterLoad)
        {
            //Check if we've already unlocked this... unlocking on load is unpredictable at times
            if (vehicle.GetComp<CompShipHeatShield>() != null)
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
            VehicleComponent shieldGenerator = vehicle.statHandler.componentsByKeys["shieldGenerator"];
            shieldGenerator.SetHealthModifier = 50;
            shieldGenerator.health = 50;
            CompShipHeatShield myShield = new CompShipHeatShield();
            myShield.parent = vehicle;
            myShield.Initialize(shield);
            if (!unlockingAfterLoad)
            {
                vehicle.comps.Add(myShield);
                vehicle.RecacheComponents();
                myShield.PostSpawnSetup(unlockingAfterLoad);
                net.RebuildHeatNet();
            }
            else
                PostLoadNewComponents.CompsToAdd.Add(myShield);
        }
    }
}
