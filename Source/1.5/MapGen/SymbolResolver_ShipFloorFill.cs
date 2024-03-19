using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using SaveOurShip2;

namespace RimWorld.BaseGen
{
	public class SymbolResolver_ShipFloorFill : SymbolResolver
	{
		public override void Resolve(ResolveParams rp)
		{
			Map map = BaseGen.globalSettings.map;
			TerrainGrid terrainGrid = map.terrainGrid;
			foreach (var item in rp.rect)
			{
				Thing thing;
				if (rp.disableSinglePawn==true)
					thing = ThingMaker.MakeThing(ResourceBank.ThingDefOf.ShipHullTile, null);
				else
					thing = ThingMaker.MakeThing(ResourceBank.ThingDefOf.ShipHullTileWrecked, null);
				GenSpawn.Spawn(thing, item, map, WipeMode.Vanish);
			}
		}
	}
}