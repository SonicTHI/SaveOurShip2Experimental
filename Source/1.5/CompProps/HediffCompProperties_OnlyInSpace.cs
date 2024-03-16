using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld
{
    public class HediffCompProperties_OnlyInSpace : HediffCompProperties
    {
        public HediffCompProperties_OnlyInSpace()
        {
            compClass = typeof(HediffCompOnlyInSpace);
        }
    }
}
