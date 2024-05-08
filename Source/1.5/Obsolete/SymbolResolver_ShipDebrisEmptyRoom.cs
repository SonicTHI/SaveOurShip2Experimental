using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

using RimWorld.BaseGen;

namespace SaveOurShip2
{
	public class SymbolResolver_ShipDebrisEmptyRoom : SymbolResolver
	{
		public override void Resolve(ResolveParams rp)
		{
			if (!rp.noRoof.HasValue || !rp.noRoof.Value)
			{
				BaseGen.symbolStack.Push("roof", rp);
			}
			ResolveParams resolveParams = rp;
			BaseGen.symbolStack.Push("shipdebrisedgeWalls", resolveParams);
			ResolveParams resolveParams2 = rp;
			BaseGen.symbolStack.Push("shipfloor", resolveParams2);
			BaseGen.symbolStack.Push("clear", rp);
			if (rp.addRoomCenterToRootsToUnfog.HasValue && rp.addRoomCenterToRootsToUnfog.Value && Current.ProgramState == ProgramState.MapInitializing)
			{
				MapGenerator.rootsToUnfog.Add(rp.rect.CenterCell);
			}
		}
	}
}