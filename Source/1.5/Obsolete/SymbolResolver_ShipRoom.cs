using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using RimWorld.BaseGen;

namespace SaveOurShip2
{
	public class SymbolResolver_ShipRoom : SymbolResolver
	{
		public string interior;

		public override void Resolve(ResolveParams rp)
		{
			if(!rp.disableHives.HasValue || !rp.disableHives.Value)
				BaseGen.symbolStack.Push("shipdoors", rp);
			if (!this.interior.NullOrEmpty())
			{
				ResolveParams resolveParams = rp;
				resolveParams.rect = rp.rect.ContractedBy(1);
				if (this.interior.Equals("interior_storage"))
				{
					resolveParams.thingSetMakerDef = DefDatabase<ThingSetMakerDef>.GetNamed("SpaceLoot");
				}
				BaseGen.symbolStack.Push(this.interior, resolveParams);
			}
			BaseGen.symbolStack.Push("shipemptyRoom", rp);
		}
	}
}
