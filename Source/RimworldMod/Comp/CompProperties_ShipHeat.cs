using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld
{
    public class CompProperties_ShipHeat : CompProperties
    {
        public float heatCapacity;
        public float heatPerPulse;
        public bool ventHeatToSpace = false;
        public int heatVentTick = 2400;
        public int energyToFire;
        public int threat = 0;
        public float optRange = 0;
        public float maxRange = 400;
        public float projectileSpeed = 1;
        public bool pointDefense = false;
        public float heatGeneratedPerTickActive = 0;
        public SoundDef singleFireSound=null;
        public bool antiEntropic = false;
        public float heatPurge = 0;
    }
}
