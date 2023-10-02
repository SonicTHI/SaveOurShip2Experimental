using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld.BaseGen
{
    public class SymbolResolver_ShipDebrisEdgeWalls : SymbolResolver
    {
        public override void Resolve(ResolveParams rp)
        {
            foreach (IntVec3 current in rp.rect.EdgeCells)
            {
                this.TrySpawnWall(current, rp);
            }
        }

        private Thing TrySpawnWall(IntVec3 c, ResolveParams rp)
        {
            Map map = BaseGen.globalSettings.map;
            List<Thing> thingList = c.GetThingList(map);
            for (int i = 0; i < thingList.Count; i++)
            {
                if (!thingList[i].def.destroyable)
                {
                    return null;
                }
                if (thingList[i] is Building_Door)
                {
                    return null;
                }
            }
            for (int j = thingList.Count - 1; j >= 0; j--)
            {
                thingList[j].Destroy(DestroyMode.Vanish);
            }
            if (Rand.Chance(0.05f))
            {
                Thing thing2 = ThingMaker.MakeThing(ResourceBank.ThingDefOf.ShipHullTileWrecked);
                return GenSpawn.Spawn(thing2, c, map, WipeMode.Vanish);
            }
            /*if(Rand.Chance(0.05f))
            {
                Thing thing3 = ThingMaker.MakeThing(ThingDef.Named("ShipInside_SolarGenerator"));
                thing3.SetFaction(rp.faction, null);
                GenSpawn.Spawn(thing3, c, map, WipeMode.Vanish);
                if (!rp.rect.Contains(c + IntVec3.North))
                    thing3.Rotation = Rot4.South;
                else if (!rp.rect.Contains(c + IntVec3.South))
                    thing3.Rotation = Rot4.North;
                else if (!rp.rect.Contains(c + IntVec3.East))
                    thing3.Rotation = Rot4.West;
                else
                    thing3.Rotation = Rot4.East;
                return thing3;
            }*/
            Thing thing = ThingMaker.MakeThing(ThingDef.Named("Ship_Beam_Wrecked"));
            //thing.SetFaction(rp.faction, null);
            return GenSpawn.Spawn(thing, c, map, WipeMode.Vanish);
        }
    }
}