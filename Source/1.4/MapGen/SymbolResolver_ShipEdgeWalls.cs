using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld.BaseGen
{
    public class SymbolResolver_ShipEdgeWalls : SymbolResolver
    {
        public override void Resolve(ResolveParams rp)
        {
            foreach (IntVec3 current in rp.rect.EdgeCells)
            {
                this.TrySpawnWall(current, rp);
            }
            int numHorizPillars = 0;
            if(rp.rect.Height >= 13)
            {
                numHorizPillars = rp.rect.Height / 6 - 1;
            }
            int numVertPillars = 0;
            if(rp.rect.Width >= 13)
            {
                numVertPillars = rp.rect.Width / 6 - 1;
            }
            for(int i=1; i<=numHorizPillars; i++)
            {
                for(int j=1; j<=numVertPillars; j++)
                {
                    this.TrySpawnWall(new IntVec3(rp.rect.minX + (6 * i) - 1, 0, rp.rect.minZ + (6 * j) - 1),rp);
                }
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
            Thing thing;
            if(rp.disableSinglePawn==true)
                thing = ThingMaker.MakeThing(ThingDef.Named("Ship_Beam"));
            else
                thing = ThingMaker.MakeThing(ThingDef.Named("Ship_Beam_Wrecked"));
            thing.SetFaction(rp.faction, null);
            return GenSpawn.Spawn(thing, c, map, WipeMode.Vanish);
        }
    }
}