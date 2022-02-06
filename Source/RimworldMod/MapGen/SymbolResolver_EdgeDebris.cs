using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace RimWorld.BaseGen
{
    public class SymbolResolver_EdgeDebris : SymbolResolver
    {

        public override void Resolve(ResolveParams rp)
        {
            Map map = BaseGen.globalSettings.map;
            Faction faction = rp.faction;
            int width = 4;
            width = Mathf.Clamp(width, 1, Mathf.Min(rp.rect.Width, rp.rect.Height) / 2);
            CellRect rect = rp.rect;
            for (int j = 0; j < width; j++)
            {
                if (j % 2 == 0)
                {
                    ResolveParams rp3 = rp;
                    rp3.faction = faction;
                    rp3.rect = rect;
                    BaseGen.symbolStack.Push("edgeSlag", rp3);
                }
                rect = rect.ContractedBy(1);
            }
        }
    }
}
