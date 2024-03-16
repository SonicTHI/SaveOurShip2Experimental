using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld
{
    class CompGraphicOnOff : ThingComp
    {
        public override void ReceiveCompSignal(string signal)
        {
            if (parent.Map != null && (signal == "PowerTurnedOn" || signal == "PowerTurnedOff" || signal == "FlickedOn" || signal == "FlickedOff" || signal == "Refueled" || signal == "RanOutOfFuel" || signal == "ScheduledOn" || signal == "ScheduledOff"))
            {
                parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlag.Buildings | MapMeshFlag.Things);
            }
        }
    }
}
