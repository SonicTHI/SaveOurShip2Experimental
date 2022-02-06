using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimWorld
{
    public class CompProperties_BuildingConsciousness : CompProperties
    {
        public bool canMergeHuman=false;
        public bool mustBeDead=false;
        public bool canMergeAI=false;
        public bool healOnMerge=false;
        public HediffDef holoHediff;
        public ThingDef holoWeapon;

        public CompProperties_BuildingConsciousness()
        {
            this.compClass = typeof(CompBuildingConsciousness);
        }
    }
}
