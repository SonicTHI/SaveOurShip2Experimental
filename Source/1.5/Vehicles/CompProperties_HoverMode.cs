using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vehicles;

namespace SaveOurShip2.Vehicles
{
    class CompProperties_HoverMode : VehicleCompProperties
    {
        public FleckData hoverFleck;

        public CompProperties_HoverMode()
        {
            base.compClass = typeof(CompHoverMode);
        }
    }
}
