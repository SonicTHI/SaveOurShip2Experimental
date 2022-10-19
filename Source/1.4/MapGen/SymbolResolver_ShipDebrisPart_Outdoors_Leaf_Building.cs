using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RimWorld.BaseGen
{
    public class SymbolResolver_ShipDebrisPart_Outdoors_Leaf_Building : SymbolResolver
    {
        public override bool CanResolve(ResolveParams rp)
        {
            return base.CanResolve(rp);// && (BaseGen.globalSettings.basePart_emptyNodesResolved >= BaseGen.globalSettings.minEmptyNodes || BaseGen.globalSettings.basePart_buildingsResolved < BaseGen.globalSettings.minBuildings);
        }

        public override void Resolve(ResolveParams rp)
        {
            ResolveParams resolveParams = rp;
            BaseGen.symbolStack.Push("shipdebrispart_indoors", resolveParams);
            BaseGen.globalSettings.basePart_buildingsResolved++;
        }
    }
}
