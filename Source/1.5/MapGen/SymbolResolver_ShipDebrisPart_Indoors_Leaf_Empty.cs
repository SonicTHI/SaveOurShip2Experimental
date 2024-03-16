using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RimWorld.BaseGen
{
    public class SymbolResolver_ShipDebrisPart_Indoors_Leaf_Empty : SymbolResolver
    {
        public override bool CanResolve(ResolveParams rp)
        {
            return base.CanResolve(rp) && BaseGen.globalSettings.basePart_barracksResolved >= 1 && BaseGen.globalSettings.basePart_batteriesCoverage >= 1;
        }

        public override void Resolve(ResolveParams rp)
        {
            BaseGen.symbolStack.Push("shipdebrisempty", rp);
        }
    }
}
