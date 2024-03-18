using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld.BaseGen
{
	public class SymbolResolver_ShipEmptyRoomTriangle2 : SymbolResolver
	{
		public override void Resolve(ResolveParams rp)
		{
			ResolveParams resolveParams = rp;
			BaseGen.symbolStack.Push("shipedgeWallsTriangle2", resolveParams);
			ResolveParams resolveParams2 = rp;
			BaseGen.symbolStack.Push("shipfloorTriangle2", resolveParams2);
		}
	}
}