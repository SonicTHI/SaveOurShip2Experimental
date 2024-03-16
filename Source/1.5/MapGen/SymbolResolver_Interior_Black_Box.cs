using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld.BaseGen
{
    class SymbolResolver_Interior_Black_Box : SymbolResolver
    {
        public override void Resolve(ResolveParams rp)
        {
            Map map = BaseGen.globalSettings.map;
            ThingSetMakerParams value = default(ThingSetMakerParams);
            float num2 = 8000f;
            value.totalMarketValueRange = new FloatRange?(new FloatRange(num2, num2));
            List<Thing> list = DefDatabase<ThingSetMakerDef>.GetNamed("SpaceEpicLoot").root.Generate(value);
            for (int i = 0; i < list.Count; i++)
            {
                Thing thing = list[i];
                GenSpawn.Spawn(thing, rp.rect.Cells.RandomElement(), map);
            }
            GenSpawn.Spawn(ThingDef.Named("BlackBoxAI"), new IntVec3(rp.rect.minX + 11, 0, rp.rect.minZ + rp.rect.Height / 2), map);
        }

        public override bool CanResolve(ResolveParams rp)
        {
            return true;
        }
    }
}
