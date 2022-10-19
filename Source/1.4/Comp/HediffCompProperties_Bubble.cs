using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld
{
    public class HediffCompProperties_Bubble : HediffCompProperties_SeverityPerDay
    {
        public HediffCompProperties_Bubble()
        {
            compClass = typeof(HediffComp_Bubble);
        }
    }
}
