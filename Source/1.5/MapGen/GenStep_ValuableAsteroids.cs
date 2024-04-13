using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI.Group;
using RimWorld;

namespace SaveOurShip2
{
	class GenStep_ValuableAsteroids : GenStep
	{
		public static List<Thing> SpawnList;

		public override int SeedPart
		{
			get
			{
				return Rand.Range(1337,69420);
			}
		}

		public override void Generate(Map map, GenStepParams parms)
		{
			SpawnList = new List<Thing>();
			map.regionAndRoomUpdater.Enabled = false;
			int LittleAssTeroids = Rand.RangeInclusive(4, 12);
			int BigAssTeroids = Rand.RangeInclusive(1, 2);
			for (int i = 0; i < BigAssTeroids; i++)
			{
				GenerateBigAsteroid(map, parms);
			}
			for (int i=0;i<LittleAssTeroids;i++)
			{
				GenerateSmallAsteroid(map, parms);
			}
			map.regionAndRoomUpdater.Enabled = true;
			foreach(Thing t in SpawnList)
			{
				t.Position = CellFinder.FindNoWipeSpawnLocNear(t.Position, map, ThingDef.Named("SpaceHive"), Rot4.East, 10);
				GenSpawn.Spawn(t, t.Position, map);
			}
		}

		private void GenerateSmallAsteroid(Map map, GenStepParams parms)
		{
			IntVec3 center = map.Center + new IntVec3(Rand.RangeInclusive(map.Size.x / -3, map.Size.x / 3), 0, Rand.RangeInclusive(map.Size.z / -3, map.Size.z / 3));
			int numLumps = Rand.RangeInclusive(1, 3);
			int radius = Rand.RangeInclusive(3, 7);
			ThingDef rock = DefDatabase<ThingDef>.AllDefs.Where(def => def.building != null && def.building.isNaturalRock && !def.building.isResourceRock).RandomElement();
			foreach (IntVec3 current in FastLump(center, Rand.RangeInclusive(radius * radius, (int)(radius * radius * 1.5f))))
			{
				GenSpawn.Spawn(rock, current, map);
			}
			for (int i = 0; i < numLumps; i++)
			{
				ThingDef mineral = DefDatabase<ThingDef>.AllDefs.Where(def => def.building != null && def.building.isNaturalRock && def.building.isResourceRock).RandomElement();
				int pricePerCell = (int)(mineral.building.mineableYield * mineral.building.mineableThing.BaseMarketValue);
				int numCells = Rand.RangeInclusive(250, 500) / pricePerCell;
				if (numCells <= 4)
					numCells = 4;
				IntVec3 lumpCenter = center + new IntVec3(Rand.Range(-1 * radius / 2, radius / 2), 0, Rand.Range(-1 * radius / 2, radius / 2));
				foreach (IntVec3 current in FastLump(lumpCenter, numCells))
				{
					GenSpawn.Spawn(mineral, current, map);
				}
			}
		}

		private void GenerateBigAsteroid(Map map, GenStepParams parms)
		{
			IntVec3 center = map.Center + new IntVec3(Rand.RangeInclusive(map.Size.x / -3, map.Size.x / 3), 0, Rand.RangeInclusive(map.Size.z / -3, map.Size.z / 3));
			int radius = Rand.RangeInclusive(20, 30);
			ThingDef rock = DefDatabase<ThingDef>.AllDefs.Where(def => def.building != null && def.building.isNaturalRock && !def.building.isResourceRock).RandomElement();
			foreach (IntVec3 current in FastLump(center, Rand.RangeInclusive(radius * radius, (int)(radius * radius * 1.5f))))
			{
				GenSpawn.Spawn(rock, current, map);
			}
			int numLumps = Rand.RangeInclusive(4, 7);
			for (int i = 0; i < numLumps; i++)
			{
				ThingDef mineral = DefDatabase<ThingDef>.AllDefs.Where(def => def.building != null && def.building.isNaturalRock && def.building.isResourceRock).RandomElement();
				int pricePerCell = (int)(mineral.building.mineableYield * mineral.building.mineableThing.BaseMarketValue);
				int numCells = Rand.RangeInclusive(250, 750) / pricePerCell;
				if (numCells <= 6)
					numCells = 6;
				IntVec3 lumpCenter = center + new IntVec3(Rand.Range(-1 * radius / 2, radius), 0, Rand.Range(-1 * radius / 2, radius));
				foreach (IntVec3 current in FastLump(lumpCenter, numCells))
				{
					GenSpawn.Spawn(mineral, current, map);
				}
			}
			if(Rand.Chance(0.75f))
			{
				IntVec3 centerOffset = center + new IntVec3(Rand.RangeInclusive(-3, 3), 0, Rand.RangeInclusive(-3, 3));
				bool insects = Rand.Chance(0.65f);
				foreach(IntVec3 current in FastLump(centerOffset,Rand.RangeInclusive(radius * radius / 4, radius * radius / 3)))
				{
					if (current.GetFirstMineable(map) != null&& current.GetFirstMineable(map).def.building.naturalTerrain!=null)
					{
						map.terrainGrid.SetTerrain(current, current.GetFirstMineable(map).def.building.naturalTerrain);
						current.GetFirstMineable(map).Destroy(DestroyMode.Vanish);
					}
					else
					{
						map.terrainGrid.SetTerrain(current, ThingDefOf.Granite.building.naturalTerrain);
					}
					if(Rand.Chance(0.2f))
					{
						if (insects)
							GenSpawn.Spawn(ThingDefOf.Filth_Slime, current, map);
						else if (Rand.Chance(0.2f))
							GenSpawn.Spawn(ThingDefOf.ChunkSlagSteel, current, map);
						else
							GenSpawn.Spawn(ThingDefOf.Filth_Fuel, current, map);
					}
				}
				if(insects)
				{
					int numHives = Rand.RangeInclusive(4, 6);
					for (int i=0; i < numHives; i++)
					{
						IntVec3 centerOffsetOffset = centerOffset + new IntVec3(Rand.RangeInclusive(radius / -8, radius / 8), 0, Rand.RangeInclusive(radius / -8, radius / 8));
						Thing t = ThingMaker.MakeThing(ThingDef.Named("SpaceHive"));
						t.Position = centerOffsetOffset;
						SpawnList.Add(t);
					}
				}
				else
				{
					int totalMechPoints = (int)StorytellerUtility.DefaultSiteThreatPointsNow();
					Lord MechLord = LordMaker.MakeNewLord(Faction.OfMechanoids, new LordJob_DefendBase(Faction.OfMechanoids, centerOffset), map, null);
					while (totalMechPoints > 0)
					{
						PawnKindDef pawnKindDef = (from kind in DefDatabase<PawnKindDef>.AllDefsListForReading
												   where kind.RaceProps.IsMechanoid
												   select kind).RandomElementByWeight((PawnKindDef kind) => 1f / kind.combatPower);
						Pawn mech = PawnGenerator.GeneratePawn(pawnKindDef);
						mech.SetFaction(Faction.OfMechanoids);
						IntVec3 centerOffsetOffset = centerOffset + new IntVec3(Rand.RangeInclusive(radius / -8, radius / 8), 0, Rand.RangeInclusive(radius / -8, radius / 8));
						mech.Position = centerOffsetOffset;
						MechLord.AddPawn(mech);
						SpawnList.Add(mech);
						totalMechPoints -= (int)pawnKindDef.combatPower;
					}
				}
			}
		}

		private List<IntVec3> FastLump(IntVec3 center, int radius)
		{
			List<IntVec3> lump = new List<IntVec3>();
			int xradA = (int)(Rand.RangeInclusive(1, 10) * 0.1f * radius);
			int xradB = (int)(Rand.RangeInclusive(1, 10) * 0.1f * radius);
			int zradA = (int)(Rand.RangeInclusive(1, 10) * 0.1f * radius);
			int zradB = (int)(Rand.RangeInclusive(1, 10) * 0.1f * radius);
			for (int x = -xradA; x < 0; x++)
			{
				for (int z = -zradA; z < 0; z++)
				{
					float ecks = 2f * x * x / xradA;
					float zee = 2f * z * z / zradA;
					if (ecks * ecks + zee * zee < 1)
						lump.Add(new IntVec3(x, 0, z) + center);
				}
			}
			for (int x = -xradA; x <= 0; x++)
			{
				for (int z = 0; z <= zradB; z++)
				{
					float ecks = 2f * x * x / xradA;
					float zee = 2f * z * z / zradB;
					if (ecks * ecks + zee * zee < 1)
						lump.Add(new IntVec3(x, 0, z) + center);
				}
			}
			for (int x = 0; x <= xradB; x++)
			{
				for (int z = -zradA; z <= 0; z++)
				{
					float ecks = 2f * x * x / xradB;
					float zee = 2f * z * z / zradA;
					if (ecks * ecks + zee * zee < 1)
						lump.Add(new IntVec3(x, 0, z) + center);
				}
			}
			for (int x = 0; x <= xradB; x++)
			{
				for (int z = 0; z <= zradB; z++)
				{
					float ecks = 2f * x * x / xradB;
					float zee = 2f * z * z / zradB;
					if (ecks * ecks + zee * zee < 1)
						lump.Add(new IntVec3(x, 0, z) + center);
				}
			}
			return lump;
		}
	}
}
