using RimWorld.BaseGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld
{
    class GenStep_ShipEngineImpactSite : GenStep_Scatterer
    {
        private static readonly IntRange SettlementSizeRange = new IntRange(50, 69);

        public override int SeedPart
        {
            get
            {
                return 666133769;
            }
        }

        protected override bool CanScatterAt(IntVec3 c, Map map)
        {
            return true;
        }

        protected override void ScatterAt(IntVec3 c, Map map, GenStepParams stepparams, int stackCount = 1)
        {
            ThingDef chunk = ThingDefOf.ShipChunk;
            for(int i=0;i<13;i++)
            {
                GenSpawn.Spawn(chunk, new IntVec3(c.x + Rand.RangeInclusive(-20, 20), 0, c.z + Rand.RangeInclusive(-20, 20)),map);
            }
            ThingDef JTDrive = ThingDef.Named("JTDriveSalvage");
            GenSpawn.Spawn(JTDrive, c, map);
        }
    }
}
