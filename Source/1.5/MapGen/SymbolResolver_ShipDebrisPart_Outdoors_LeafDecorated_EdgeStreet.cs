using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RimWorld.BaseGen
{
	public class SymbolResolver_ShipDebrisPart_Outdoors_LeafDecorated_EdgeStreet : SymbolResolver
	{
		public override void Resolve(ResolveParams rp)
		{
			ResolveParams resolveParams = rp;
			resolveParams.floorDef = (rp.pathwayFloorDef ?? BaseGenUtility.RandomBasicFloorDef(rp.faction, false));
			BaseGen.symbolStack.Push("debrisedgeStreet", resolveParams);
			ResolveParams resolveParams2 = rp;
			resolveParams2.rect = rp.rect.ContractedBy(1);
			BaseGen.symbolStack.Push("shipdebrispart_outdoors_leaf", resolveParams2);
		}
	}
}