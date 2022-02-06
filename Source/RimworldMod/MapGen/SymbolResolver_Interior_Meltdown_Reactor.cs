using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld.BaseGen
{
    class SymbolResolver_Interior_Meltdown_Reactor : SymbolResolver
    {
        public override void Resolve(ResolveParams rp)
        {
            Map map = BaseGen.globalSettings.map;
            for (int i=0;i<rp.rect.Width;i++)
            {
                GenSpawn.Spawn(ThingDefOf.PowerConduit, new IntVec3(rp.rect.minX + i, 0, rp.rect.minZ + rp.rect.Height / 2), map);
            }
            GenSpawn.Spawn(ThingDef.Named("Ship_DamagedReactor"), new IntVec3(rp.rect.minX + rp.rect.Width / 2, 0, rp.rect.minZ + rp.rect.Height / 2), map);
            GenSpawn.Spawn(ThingDef.Named("ShipCapacitor"), new IntVec3(rp.rect.minX + 1, 0, rp.rect.minZ + 2), map);
        }

        public override bool CanResolve(ResolveParams rp)
        {
            return true;
        }
    }
}
