using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld.BaseGen
{
	class SymbolResolver_Interior_Lab_Triangle : SymbolResolver
	{
		public override void Resolve(ResolveParams rp)
		{
			Map map = BaseGen.globalSettings.map;
			ThingDef benchDef = ThingDef.Named("HiTechResearchBench");
			if(rp.disableHives.HasValue && rp.disableHives.Value)
			{
				Thing bench = ThingMaker.MakeThing(benchDef);
				GenSpawn.Spawn(bench, new IntVec3(rp.rect.minX + 1, 0, rp.rect.maxZ - 1), map, Rot4.West);
				bench = ThingMaker.MakeThing(benchDef);
				GenSpawn.Spawn(bench, new IntVec3(rp.rect.minX + 1, 0, rp.rect.maxZ - 8), map, Rot4.West);
				GenSpawn.Spawn(ThingDef.Named("MultiAnalyzer"), new IntVec3(rp.rect.minX, 0, rp.rect.maxZ - 5), map);
				GenSpawn.Spawn(ThingDef.Named("Ship_LabConsole"), new IntVec3(rp.rect.minX, 0, rp.rect.maxZ - 12), map);
				GenSpawn.Spawn(ThingDefOf.Heater, new IntVec3(rp.rect.minX + 5, 0, rp.rect.maxZ - 6), map);
				GenSpawn.Spawn(ThingDefOf.StandingLamp, new IntVec3(rp.rect.minX + 5, 0, rp.rect.maxZ - 7), map);
			}
			else
			{
				Thing bench = ThingMaker.MakeThing(benchDef);
				GenSpawn.Spawn(bench, new IntVec3(rp.rect.minX + 1, 0, rp.rect.minZ + 2), map, Rot4.West);
				bench = ThingMaker.MakeThing(benchDef);
				GenSpawn.Spawn(bench, new IntVec3(rp.rect.minX + 1, 0, rp.rect.minZ + 9), map, Rot4.West);
				GenSpawn.Spawn(ThingDef.Named("MultiAnalyzer"), new IntVec3(rp.rect.minX, 0, rp.rect.minZ + 5), map);
				GenSpawn.Spawn(ThingDef.Named("Ship_LabConsole"), new IntVec3(rp.rect.minX, 0, rp.rect.minZ + 13), map);
				GenSpawn.Spawn(ThingDefOf.Heater, new IntVec3(rp.rect.minX + 5, 0, rp.rect.minZ + 7), map);
				GenSpawn.Spawn(ThingDefOf.StandingLamp, new IntVec3(rp.rect.minX + 5, 0, rp.rect.minZ + 8), map);
			}
		}

		public override bool CanResolve(ResolveParams rp)
		{
			return true;
		}
	}
}
