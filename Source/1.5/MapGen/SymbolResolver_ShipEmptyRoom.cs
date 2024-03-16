using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld.BaseGen
{
    public class SymbolResolver_ShipEmptyRoom : SymbolResolver
    {
        public override void Resolve(ResolveParams rp)
        {
            if (!rp.noRoof.HasValue || !rp.noRoof.Value)
            {
                BaseGen.symbolStack.Push("roof", rp);
            }
            ResolveParams resolveParams = rp;
            BaseGen.symbolStack.Push("shipedgeWalls", resolveParams);
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