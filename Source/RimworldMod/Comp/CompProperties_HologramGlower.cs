using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimWorld
{
    class CompProperties_HologramGlower : CompProperties_Glower
    {
        public CompProperties_HologramGlower()
        {
            this.compClass = typeof(CompHologramGlower);
        }
    }
}
