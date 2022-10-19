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
            CellRect.CellRectIterator iterator = rp.rect.GetIterator();
            while (!iterator.Done())
            {
                Thing thing;
                if (rp.disableSinglePawn==true)
                    thing = ThingMaker.MakeThing(ShipInteriorMod2.hullPlateDef, null);
                else
                    thing = ThingMaker.MakeThing(ThingDef.Named("ShipHullTileWrecked"), null);
                GenSpawn.Spawn(thing, iterator.Current, map, WipeMode.Vanish);
                iterator.MoveNext();
            }
        }
    }
}