using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.Noise;

namespace RimWorld
{
    class GenStep_InsectPillarCaves : GenStep_Caves
    {
        public override void Generate(Map map, GenStepParams parms)
        {
            List<IntVec3> cells = map.AllCells.ToList();
            foreach(IntVec3 cell in cells)
            {
                MapGenerator.Caves[cell] = 1;
            }
            List<Thing> things;
            foreach(IntVec3 cell in cells.Where(c => c.DistanceToEdge(map) < 2))
            {
                things = cell.GetThingList(map).ListFullCopy();
                foreach (Thing thing in things)
                    thing.Destroy();
                MapGenerator.Caves[cell] = 0;
            }
			for (int i = 0; i < 5; i++)
			{
				Dig(new IntVec3(0, 0, Rand.Range(5, map.Size.z - 5)), 90, 20 , cells, map, false);
				Dig(new IntVec3(map.Size.x - 1, 0, Rand.Range(5, map.Size.z - 5)), 270, 20, cells, map, false);
				Dig(new IntVec3(Rand.Range(5, map.Size.x - 5), 0, 0), 0, 20, cells, map, false);
				Dig(new IntVec3(Rand.Range(5, map.Size.x - 5), 0, map.Size.z - 1), 180, 20, cells, map, false);
			}
			bool foundPillarSpot = false;
			IEnumerable<IntVec3> centerArea = cells.Where(c => c.x > map.Size.x / 4 && c.x < map.Size.x - map.Size.x / 4 && c.z > map.Size.z / 4 && c.z < map.Size.z - map.Size.z / 4);
			do {
				IntVec3 cell = centerArea.RandomElement();
				if (cell.GetThingList(map).Count == 0)
				{
					foundPillarSpot = true;
					GenSpawn.Spawn(ThingDef.Named("ShipArchotechPillarD"), cell, map);
					Lord theLord = LordMaker.MakeNewLord(Faction.OfInsects, new LordJob_DefendBase(Faction.OfInsects,cell), map);
					for (int i = 0; i < 6; i++)
					{
						Pawn spider = (Pawn)GenSpawn.Spawn(PawnGenerator.GeneratePawn(PawnKindDef.Named("Archospider"), Faction.OfInsects), cell, map);
						theLord.AddPawn(spider);
					}
				}
			} while (!foundPillarSpot);

			foreach(Pawn p in map.mapPawns.AllPawnsSpawned.Where(p=>p.Faction!=Faction.OfPlayer))
            {
				p.needs.food.CurLevel = 0.1f;
            }
        }

		private new void Dig(IntVec3 start, float dir, float width, List<IntVec3> group, Map map, bool closed, HashSet<IntVec3> visited = null)
		{
			HashSet<IntVec3> tmpGroupSet = new HashSet<IntVec3>();
			FloatRange BranchedTunnelWidthOffset = new FloatRange(0.2f, 0.4f);
			Perlin directionNoise = new Perlin(0.0020500000100582838, 2.0, 0.5, 4, Rand.Int, QualityMode.Medium);

			Vector3 vect = start.ToVector3Shifted();
			IntVec3 intVec = start;
			float num = 0f;
			MapGenFloatGrid caves = MapGenerator.Caves;
			if (visited == null)
			{
				visited = new HashSet<IntVec3>();
			}
			tmpGroupSet.Clear();
			tmpGroupSet.AddRange(group);
			int num2 = 0;
			while (true)
			{
				SetCaveAround(intVec, width, map, visited, out bool hitAnotherTunnel);
				while (vect.ToIntVec3() == intVec)
				{
					vect += Vector3Utility.FromAngleFlat(dir) * 0.5f;
					num += 0.5f;
				}
				IntVec3 intVec3 = new IntVec3(intVec.x, 0, vect.ToIntVec3().z);
				if (intVec3.InBounds(map))
				{
					caves[intVec3] = Mathf.Max(caves[intVec3], width);
					visited.Add(intVec3);
				}
				intVec = vect.ToIntVec3();
				dir += (float)directionNoise.GetValue(num * 60f, (float)start.x * 200f, (float)start.z * 200f) * 8f;
				width -= 0.034f;
				if (!(width < 1.4f))
				{
					num2++;
					continue;
				}
				break;
			}
			List<Thing> things;
			foreach(IntVec3 cell in visited)
            {
				if (cell.InBounds(map))
				{
					things = cell.GetThingList(map).ListFullCopy();
					foreach (Thing thing in things)
						if(thing.def.mineable)
							thing.Destroy(DestroyMode.Vanish);
				}
            }
			foreach(IntVec3 cell in visited)
            {
				if (cell.InBounds(map) && Rand.Chance(0.001f))
				{
					Hive hive = (Hive)GenSpawn.Spawn(ThingDefOf.Hive, cell, map);
					hive.PawnSpawner.SpawnPawnsUntilPoints(300);
					hive.PawnSpawner.canSpawnPawns = false;
					hive.GetComp<CompSpawnerHives>().canSpawnHives = false;
				}
			}
		}

		private new void SetCaveAround(IntVec3 around, float tunnelWidth, Map map, HashSet<IntVec3> visited, out bool hitAnotherTunnel)
		{
			hitAnotherTunnel = false;
			int num = GenRadial.NumCellsInRadius(tunnelWidth / 2f);
			MapGenFloatGrid caves = MapGenerator.Caves;
			for (int i = 0; i < num; i++)
			{
				IntVec3 intVec = around + GenRadial.RadialPattern[i];
				if (intVec.InBounds(map))
				{
					if (caves[intVec] > 0f && !visited.Contains(intVec))
					{
						hitAnotherTunnel = true;
					}
					caves[intVec] = Mathf.Max(caves[intVec], tunnelWidth);
					visited.Add(intVec);
				}
			}
		}
	}
}
