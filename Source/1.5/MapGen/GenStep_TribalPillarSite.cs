using RimWorld.BaseGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI.Group;
using RimWorld;

namespace SaveOurShip2
{
	class GenStep_TribalPillarSite : GenStep_Scatterer
	{
		private static readonly IntRange SettlementSizeRange = new IntRange(50, 69);

		public override int SeedPart
		{
			get
			{
				return 666133769;
			}
		}

		protected override bool CanScatterAt(IntVec3 c, Map map)
		{
			return true;
		}

		protected override void ScatterAt(IntVec3 c, Map map, GenStepParams stepparams, int stackCount = 1)
		{
			Faction nastyTribals = Find.FactionManager.AllFactions.Where(fac => fac.def.techLevel == TechLevel.Neolithic && fac.PlayerRelationKind==FactionRelationKind.Hostile).FirstOrDefault();
			Lord defendShip = LordMaker.MakeNewLord(nastyTribals, new LordJob_DefendShip(nastyTribals, map.Center), map);
			List<Building> cores = new List<Building>();
			ShipInteriorMod2.GenerateShip(DefDatabase<SpaceShipDef>.GetNamed("TribalVillageIsNotAShip"), map, null, nastyTribals, defendShip, out cores, false, true);
		}
	}
}
