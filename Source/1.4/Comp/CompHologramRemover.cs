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
            if (parent is Pawn)
            {
                ((Pawn)parent).kindDef = PawnKindDefOf.Colonist;
                ((Pawn)parent).def = ThingDefOf.Human;
                typeof(Pawn_AgeTracker).GetMethod("RecalculateLifeStageIndex", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(((Pawn)parent).ageTracker, new object[] { });
            }
            else
                parent.Destroy();
        }
    }
}
