using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI.Group;

namespace RimWorld.BaseGen
{
	class SymbolResolver_Interior_SpaceDanger : SymbolResolver
	{

		public override void Resolve(ResolveParams rp)
		{
			Map map = BaseGen.globalSettings.map;
			Faction faction = rp.faction;
			ThingDef filth;
			switch (Rand.RangeInclusive(0, 2))
			{
				case 0:
					faction = Faction.OfAncientsHostile;
					filth = ThingDefOf.Filth_Blood;
					Lord singlePawnLord = rp.singlePawnLord ?? LordMaker.MakeNewLord(faction, new LordJob_DefendBase(faction, rp.rect.CenterCell), map, null);
					ResolveParams resolveParams = rp;
					resolveParams.rect = rp.rect;
					resolveParams.faction = faction;
					resolveParams.singlePawnLord = singlePawnLord;
					int numPawns = Rand.Range(4, 10);
					for(int i=0; i < numPawns; i++)
					{
						PawnGenerationRequest req = new PawnGenerationRequest(PawnKindDef.Named("SpaceCannibal"), Faction.OfAncientsHostile);
						resolveParams.singlePawnToSpawn = PawnGenerator.GeneratePawn(req);
						BaseGen.symbolStack.Push("pawn", resolveParams);
					}
					break;
				case 1:
					faction = Faction.OfInsects;
					filth = ThingDefOf.Filth_Slime;
					ResolveParams resolveParams3 = rp;
					int? hivesCount = rp.hivesCount;
					resolveParams3.hivesCount = new int?((!hivesCount.HasValue) ? Rand.Range(2, 4) : hivesCount.Value);
					resolveParams3.faction = faction;
					BaseGen.symbolStack.Push("hives", resolveParams3);
					break;
				default:
					faction = Faction.OfMechanoids;
					filth = ThingDefOf.Filth_Fuel;
					ResolveParams resolveParams2 = rp;
					int? mechanoidsCount = rp.mechanoidsCount;
					resolveParams2.mechanoidsCount = new int?((!mechanoidsCount.HasValue) ? Rand.Range(4,7) : mechanoidsCount.Value);
					resolveParams2.faction = faction;
					BaseGen.symbolStack.Push("randomMechanoidGroup", resolveParams2);
					break;
			}
			foreach (IntVec3 current in rp.rect)
			{
				if (Rand.Chance(0.6f))
				{
					Thing thing = ThingMaker.MakeThing(filth);
					GenSpawn.Spawn(thing, current, map);
				}
			}
		}
	}
}