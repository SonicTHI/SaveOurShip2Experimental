using RimWorld.BaseGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI.Group;
using RimWorld.Planet;
using RimWorld;

namespace SaveOurShip2
{
	class GenStep_DownedShip : GenStep_Scatterer
	{

		public override int SeedPart
		{
			get
			{
				return 694201337;
			}
		}

		protected override bool CanScatterAt(IntVec3 c, Map map)
		{
			return true;
		}

		protected override void ScatterAt(IntVec3 c, Map map, GenStepParams stepparams, int stackCount = 1)
		{
			List<Building> cores = new List<Building>();
			int rarity = Rand.RangeInclusive(1, 2);
			//limited to 100x100 due to unsettable map size, no fleets
			SpaceShipDef ship = DefDatabase<SpaceShipDef>.AllDefs.Where(def => def.ships.NullOrEmpty() && !def.neverRandom && !def.spaceSite && !def.neverWreck && def.rarityLevel <= rarity && def.sizeX < 100 && def.sizeZ < 100).RandomElement();
			ShipInteriorMod2.GenerateShip(ship, map, null, Faction.OfAncients, null, out cores, false, true, 4, (map.Size.x - ship.sizeX) / 2, (map.Size.z - ship.sizeZ) / 2);
		}
	}
}
