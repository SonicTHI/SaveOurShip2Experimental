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
        private Building_ShipBridge ship;

        public Dialog_NameShip(Building_ShipBridge ship)
        {
            this.ship = ship;
            curName = ship.ShipName;
        }

        protected override void SetName(string name)
        {
            if (name == ship.ShipName || string.IsNullOrEmpty(name))
                return;

            ship.ShipName = name;

            foreach(Building b in ShipUtility.ShipBuildingsAttachedTo(ship))
            {
                if (b is Building_ShipBridge)
                    ((Building_ShipBridge)b).ShipName = name;
            }
        }
    }
}
