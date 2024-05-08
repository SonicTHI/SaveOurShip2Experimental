using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld.BaseGen;

namespace SaveOurShip2
{
	public class SymbolResolver_ShipDebrisPart_Indoors_Leaf_Crypto : SymbolResolver
	{
		public override bool CanResolve(ResolveParams rp)
		{
			return base.CanResolve(rp) && BaseGen.globalSettings.basePart_barracksResolved >= 1 && BaseGen.globalSettings.basePart_batteriesCoverage >= 1 && BaseGen.globalSettings.basePart_breweriesCoverage < 1;
		}

		public override void Resolve(ResolveParams rp)
		{
			ResolveParams parms = rp;
			parms.disableHives = true;
			BaseGen.symbolStack.Push("chargeBatteries", rp);
			BaseGen.symbolStack.Push("shipdebriscrypto", parms);
			BaseGen.globalSettings.basePart_breweriesCoverage++;
		}
	}
}
