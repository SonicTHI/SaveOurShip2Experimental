using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

using RimWorld.BaseGen;

namespace SaveOurShip2
{
	public class SymbolResolver_ShipDebrisPart_Indoors_Leaf_Danger : SymbolResolver
	{
		public override bool CanResolve(ResolveParams rp)
		{
			return base.CanResolve(rp) && BaseGen.globalSettings.basePart_barracksResolved < Rand.RangeInclusive(1,2);
		}

		public override void Resolve(ResolveParams rp)
		{
			BaseGen.symbolStack.Push("shipdebrisdanger", rp);
			BaseGen.globalSettings.basePart_barracksResolved++;
		}
	}
}
