using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld.BaseGen
{
    public class SymbolResolver_ShipDoors : SymbolResolver
    {
        private const float ExtraDoorChance = 0.25f;

        public override void Resolve(ResolveParams rp)
        {
            if (Rand.Chance(0.25f) || (rp.rect.Width >= 10 && rp.rect.Height >= 10 && Rand.Chance(0.8f)))
            {
                BaseGen.symbolStack.Push("extraShipDoor", rp);
            }
            BaseGen.symbolStack.Push("shipensureCanReachMapEdge", rp);
        }
    }
}