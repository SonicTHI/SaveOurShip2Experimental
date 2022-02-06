using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld.BaseGen
{
    public class SymbolResolver_ShipDebrisPart_Outdoors : SymbolResolver
    {
        public override void Resolve(ResolveParams rp)
        {
            bool flag = rp.rect.Width > 23 || rp.rect.Height > 23 || ((rp.rect.Width >= 11 || rp.rect.Height >= 11) && Rand.Bool);
            ResolveParams resolveParams = rp;
            if (flag)
            {
                BaseGen.symbolStack.Push("shipdebrispart_outdoors_division", resolveParams);
            }
            else
            {
                BaseGen.symbolStack.Push("shipdebrispart_outdoors_leafPossiblyDecorated", resolveParams);
            }
        }
    }
}
