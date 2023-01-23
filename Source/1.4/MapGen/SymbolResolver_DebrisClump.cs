using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld.BaseGen
{
    public class SymbolResolver_DebrisClump : SymbolResolver
    {
        private static List<bool> street = new List<bool>();

        public override void Resolve(ResolveParams rp)
        {
            ThingDef floorDef = ThingDef.Named("ShipHullTileWrecked");
            this.SpawnFloor(rp.rect, floorDef);
        }


        private void SpawnFloor(CellRect rect, ThingDef floorDef)
        {
            Map map = BaseGen.globalSettings.map;
            TerrainGrid terrainGrid = map.terrainGrid;
            foreach (var item in rect)
            {
                IntVec3 current = item;
                    if (Rand.Chance(0.6f))
                        GenSpawn.Spawn(ThingMaker.MakeThing(floorDef), current, map, WipeMode.Vanish);
                    else if (Rand.Chance(0.2f))
                    {
                        Thing thing = ThingMaker.MakeThing(ThingDefOf.ChunkSlagSteel, null);
                        GenSpawn.Spawn(thing, current, map, WipeMode.Vanish);
                    }
            }
        }
    }
}