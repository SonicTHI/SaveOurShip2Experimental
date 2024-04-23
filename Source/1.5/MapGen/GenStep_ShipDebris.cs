using RimWorld.BaseGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SaveOurShip2
{
	class GenStep_ShipDebris : GenStep_Scatterer
	{
		private static readonly IntRange SettlementSizeRange = new IntRange(50, 69);

		public override int SeedPart
		{
			get
			{
				return Rand.Range(1337, 69420);
			}
		}

		protected override bool CanScatterAt(IntVec3 c, Map map)
		{
			if (!base.CanScatterAt(c, map))
			{
				return false;
			}
			if (!c.Standable(map))
			{
				return false;
			}
			if (c.Roofed(map))
			{
				return false;
			}
			if (!map.reachability.CanReachMapEdge(c, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)))
			{
				return false;
			}
			int min = SettlementSizeRange.min;
			CellRect cellRect = new CellRect(c.x - min / 2, c.z - min / 2, min, min);
			return cellRect.FullyContainedWithin(new CellRect(0, 0, map.Size.x, map.Size.z));
		}

		protected override void ScatterAt(IntVec3 c, Map map, GenStepParams stepparams, int stackCount = 1)
		{
			int randomInRange = SettlementSizeRange.RandomInRange;
			int randomInRange2 = SettlementSizeRange.RandomInRange;
			CellRect rect = new CellRect(c.x - randomInRange / 2, c.z - randomInRange2 / 2, randomInRange, randomInRange2);
			rect.ClipInsideMap(map);
			ResolveParams resolveParams = default(ResolveParams);
			resolveParams.rect = rect;
			resolveParams.faction = null;
			BaseGen.globalSettings.map = map;
			BaseGen.globalSettings.minBuildings = 1;
			BaseGen.symbolStack.Push("shipdebris", resolveParams);
			BaseGen.Generate();
		}
	}
}
