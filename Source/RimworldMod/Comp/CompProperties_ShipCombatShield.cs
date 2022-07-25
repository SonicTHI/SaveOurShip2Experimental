using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld
{
    public class CompProperties_ShipCombatShield : CompProperties
    {
        public float radius = 40;
        public float heatMultiplier = 1.0f;
        public string color = "white";

        public CompProperties_ShipCombatShield()
        {
            compClass = typeof(CompShipCombatShield);
        }
    }
}
