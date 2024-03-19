using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld.BaseGen
{
	public class SymbolResolver_ShipEdgeWallsTriangle1 : SymbolResolver
	{
		public override void Resolve(ResolveParams rp)
		{
			bool topHalf = (rp.disableHives).HasValue ? rp.disableHives.Value : false; //A kludge to utilize one of the existing boolean values
			IntVec3 lineStart;
			if (topHalf)
				lineStart = new IntVec3(rp.rect.minX, 0, rp.rect.minZ + 1);
			else
				lineStart = new IntVec3(rp.rect.minX, 0, rp.rect.minZ + rp.rect.Height - 1);
			int ecks = 0;
			int zee = 0;
			while(ecks<rp.rect.Width-1)
			{
				this.TrySpawnWall(new IntVec3(lineStart.x + ecks, 0, lineStart.z + zee),rp);
				ecks++;
				this.TrySpawnWall(new IntVec3(lineStart.x + ecks, 0, lineStart.z + zee), rp);
				if (topHalf)
					zee++;
				else
					zee--;
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
			Thing thing = ThingMaker.MakeThing(ThingDef.Named("Ship_Beam_Wrecked"));
			thing.SetFaction(rp.faction, null);
			return GenSpawn.Spawn(thing, c, map, WipeMode.Vanish);
		}
	}
}