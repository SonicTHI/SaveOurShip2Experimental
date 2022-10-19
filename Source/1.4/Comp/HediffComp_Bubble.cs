using SaveOurShip2;
using System;
using System.Collections.Generic;
using RimworldMod;
using RimworldMod.VacuumIsNotFun;
using Verse;

namespace RimWorld
{
    class HediffComp_Bubble : HediffComp_SeverityPerDay
    {
        public override void CompPostPostRemoved()
        {
            Find.World.GetComponent<PastWorldUWO2>().PawnsInSpaceCache.RemoveAll(p => p.Key == this.Pawn.thingIDNumber);
        }
    }
}
