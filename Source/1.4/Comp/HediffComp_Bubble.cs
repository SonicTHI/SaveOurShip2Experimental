using SaveOurShip2;
using System;
using System.Collections.Generic;
using RimworldMod;
using Verse;

namespace RimWorld
{
    class HediffComp_Bubble : HediffComp_SeverityPerDay
    {
        public override void CompPostPostRemoved()
        {
            ShipInteriorMod2.WorldComp.PawnsInSpaceCache.RemoveAll(p => p.Key == this.Pawn.thingIDNumber);
        }
    }
}
