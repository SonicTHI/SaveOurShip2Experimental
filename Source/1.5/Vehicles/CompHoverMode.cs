using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Vehicles;
using Verse;

namespace SaveOurShip2.Vehicles
{
    class CompHoverMode : VehicleComp
    {
        public override void CompTick()
        {
            base.CompTick();
            VehiclePawn shuttle = parent as VehiclePawn;
            if (shuttle.Spawned && shuttle.ignition.Drafted)
                LaunchProtocol.ThrowFleck(ResourceBank.FleckDefOf.SoS2Exhaust_Short, parent.DrawPos, parent.Map, 0.8f, 1, Rand.Range(0,360), 15, 0);
        }
    }
}
