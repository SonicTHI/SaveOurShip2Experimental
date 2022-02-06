using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld.BaseGen
{
    public class SymbolResolver_ShipFloorTriangle2 : SymbolResolver
    {
        public override void Resolve(ResolveParams rp)
        {
            Map map = BaseGen.globalSettings.map;
            bool topHalf = (rp.disableHives).HasValue ? rp.disableHives.Value : false; //A kludge to utilize one of the existing boolean values
            for(int ecks = 0; ecks < rp.rect.Width; ecks++)
            {
                for (int zee = 0; zee < rp.rect.Height; zee++)
                {
                    if (zee < ecks / 2)
                    {
                        Thing thing = ThingMaker.MakeThing(ThingDef.Named("ShipHullTileWrecked"), null);
                        if(topHalf)
                            GenSpawn.Spawn(thing, new IntVec3(rp.rect.minX+ecks,0,rp.rect.minZ-zee), map, WipeMode.Vanish);
                        else
                            GenSpawn.Spawn(thing, new IntVec3(rp.rect.minX + ecks, 0, rp.rect.minZ + zee), map, WipeMode.Vanish);
                    }
                }
            }
        }
    }
}