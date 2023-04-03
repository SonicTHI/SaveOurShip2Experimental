using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;

namespace RimWorld
{
    class CompHologramRemover : ThingComp
    {
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            if (parent is Pawn p)
            {
                p.kindDef = PawnKindDefOf.Colonist;
                p.def = ThingDefOf.Human;
                p.ageTracker.RecalculateLifeStageIndex();
            }
            else
                parent.Destroy();
        }
    }
}
