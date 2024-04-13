using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using RimWorld.BaseGen;

namespace SaveOurShip2
{
	public class SymbolResolver_Interior_SpaceEmpty : SymbolResolver
	{

		public override void Resolve(ResolveParams rp)
		{
			Map map = BaseGen.globalSettings.map;
			ThingDef filth;
			switch(Rand.RangeInclusive(0,4))
			{
				case 0:
					filth = ThingDefOf.Filth_Blood;
					break;
				case 1:
					filth = ThingDefOf.Filth_CorpseBile;
					break;
				case 2:
					filth = ThingDefOf.Filth_RubbleBuilding;
					break;
				case 3:
					filth = ThingDefOf.Filth_Trash;
					break;
				default:
					filth = ThingDefOf.Filth_Ash;
					break;
			}
			foreach(IntVec3 current in rp.rect)
			{
				if(Rand.Chance(0.4f))
				{
					Thing thing = ThingMaker.MakeThing(filth);
					GenSpawn.Spawn(thing, current, map);
				}
			}
		}
	}
}