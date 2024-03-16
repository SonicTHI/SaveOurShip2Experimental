using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld.BaseGen
{
    public class SymbolResolver_ShipDebrisPart_Outdoors_LeafPossiblyDecorated : SymbolResolver
    {
        public override void Resolve(ResolveParams rp)
        {
            if (rp.rect.Width >= 10 && rp.rect.Height >= 10 && Rand.Chance(0.75f))
            {
                BaseGen.symbolStack.Push("shipdebrispart_outdoors_leafDecorated", rp);
            }
            else
            {
                BaseGen.symbolStack.Push("shipdebrispart_outdoors_leaf", rp);
            }
        }
    }
}
