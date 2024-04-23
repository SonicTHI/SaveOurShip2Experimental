using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace SaveOurShip2.Vehicles
{
    class CompVehicleHeatNet : ThingComp
    {
        public ShipHeatNet myNet;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            RebuildHeatNet();
        }

        public void RebuildHeatNet()
        {
            Log.Message("Rebuilding heat net for shuttle");
            myNet = new ShipHeatNet();
            foreach (CompShipHeat comp in parent.GetComps<CompShipHeat>())
            {
                myNet.Register(comp);
                comp.myNet = myNet;
            }
        }

        public override string CompInspectStringExtra()
        {
            return "Heat stored: " + myNet.StorageUsed + "/" + myNet.StorageCapacity;
        }
    }
}
