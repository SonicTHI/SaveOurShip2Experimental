using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

using RimWorld.BaseGen;

namespace SaveOurShip2
{
	public class SymbolResolver_ShipDebrisRoom : SymbolResolver
	{
		public string interior;

		public override void Resolve(ResolveParams rp)
		{
			if (!rp.disableHives.HasValue || !rp.disableHives.Value)
				BaseGen.symbolStack.Push("shipdoors", rp);
			if (!this.interior.NullOrEmpty())
			{
				ResolveParams resolveParams = rp;
				resolveParams.rect = rp.rect.ContractedBy(1);
				BaseGen.symbolStack.Push(this.interior, resolveParams);
			}
			BaseGen.symbolStack.Push("shipdebrisemptyRoom", rp);
		}
	}
}
