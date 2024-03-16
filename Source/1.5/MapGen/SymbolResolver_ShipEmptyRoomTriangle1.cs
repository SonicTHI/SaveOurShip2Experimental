using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld.BaseGen
{
    public class SymbolResolver_ShipEmptyRoomTriangle1 : SymbolResolver
    {
        public override void Resolve(ResolveParams rp)
        {
            ResolveParams resolveParams = rp;
            BaseGen.symbolStack.Push("shipedgeWallsTriangle1", resolveParams);
            ResolveParams resolveParams2 = rp;
            BaseGen.symbolStack.Push("shipfloorTriangle1", resolveParams2);
        }
    }
}