using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vehicles;

namespace SaveOurShip2.Vehicles
{
    class ShuttleAutodocUpgrade : Upgrade
    {
        public CompProps_ShuttleAutoDoc props;

        public override bool UnlockOnLoad => true;

        public override void Refund(VehiclePawn vehicle)
        {
            vehicle.RemoveComp(vehicle.GetComp<CompShuttleAutoDoc>());
        }

        public override void Unlock(VehiclePawn vehicle, bool unlockingAfterLoad)
        {
            if (vehicle.GetComp<CompShuttleAutoDoc>()==null)
            {
                CompShuttleAutoDoc comp = new CompShuttleAutoDoc();
                comp.Initialize(props);
                comp.parent = vehicle;
                if (unlockingAfterLoad)
                    PostLoadNewComponents.CompsToAdd.Add(comp);
                else
                {
                    vehicle.comps.Add(comp);
                    vehicle.RecacheComponents();
                    comp.PostSpawnSetup(false);
                }
            }
        }
    }
}
