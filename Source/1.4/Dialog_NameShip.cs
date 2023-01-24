using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
    public class Dialog_NameShip : Dialog_Rename
    {
        private Building_ShipBridge bridge;

        public Dialog_NameShip(Building_ShipBridge b)
        {
            this.bridge = b;
            curName = b.ShipName;
        }

        public override void SetName(string name)
        {
            if (name == bridge.ShipName || string.IsNullOrEmpty(name))
                return;

            bridge.ShipName = name;

            foreach (Building b in ShipUtility.ShipBuildingsAttachedTo(bridge))
            {
                if (b is Building_ShipBridge bridge)
                    bridge.ShipName = name;
            }
        }
    }
}