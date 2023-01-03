using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimWorld
{
    class CompProperties_PowerPlantSolarShip : CompProperties_Power
    {
        public float bonusPower;

        public CompProperties_PowerPlantSolarShip()
        {
            this.compClass = typeof(CompPowerPlantSolarShip);
        }
    }
}
