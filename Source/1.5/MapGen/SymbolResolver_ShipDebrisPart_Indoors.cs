using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

using RimWorld.BaseGen;

namespace SaveOurShip2
{
	public class SymbolResolver_ShipDebrisPart_Indoors : SymbolResolver
	{
		public override void Resolve(ResolveParams rp)
		{
			bool flag = rp.rect.Width > 13 || rp.rect.Height > 13 || ((rp.rect.Width >= 9 || rp.rect.Height >= 9) && Rand.Chance(0.3f));
			if (flag)
			{
				BaseGen.symbolStack.Push("shipdebrispart_indoors_division", rp);
			}
			else
			{
				BaseGen.symbolStack.Push("shipdebrispart_indoors_leaf", rp);
			}
		}
	}
}