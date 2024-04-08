using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI.Group;

namespace RimWorld.BaseGen
{
	class SymbolResolver_Interior_Cannibal_Barracks : SymbolResolver
	{
		public override void Resolve(ResolveParams rp)
		{
			Map map = BaseGen.globalSettings.map;
			InteriorSymbolResolverUtility.PushBedroomHeatersCoolersAndLightSourcesSymbols(rp, true);
			ThingDef filth = ThingDefOf.Filth_Blood;
			Lord singlePawnLord = rp.singlePawnLord ?? LordMaker.MakeNewLord(rp.faction, new LordJob_DefendBase(rp.faction, rp.rect.CenterCell), map, null);
			ResolveParams resolveParams = rp;
			resolveParams.rect = rp.rect;
			resolveParams.singlePawnLord = singlePawnLord;
			int numPawns = Rand.Range(4, 7);
			for (int i = 0; i < numPawns; i++)
			{
				PawnGenerationRequest req = new PawnGenerationRequest(PawnKindDef.Named("SpaceCannibal"), Faction.OfAncientsHostile);
				resolveParams.singlePawnToSpawn = PawnGenerator.GeneratePawn(req);
				BaseGen.symbolStack.Push("pawn", resolveParams);
			}
			foreach (IntVec3 current in rp.rect)
			{
				if (Rand.Chance(0.4f))
				{
					Thing thing = ThingMaker.MakeThing(filth);
					GenSpawn.Spawn(thing, current, map);
				}
			}
			InteriorSymbolResolverUtility.PushBedroomHeatersCoolersAndLightSourcesSymbols(rp, true);
			BaseGen.symbolStack.Push("fillWithBeds", rp);
		}

		public override bool CanResolve(ResolveParams rp)
		{
			return true;
		}
	}
}
