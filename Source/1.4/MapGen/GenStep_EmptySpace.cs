using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimWorld
{
	public class GenStep_EmptySpace : GenStep
	{
		public override void Generate(Map map, GenStepParams parms)
		{
			List<IntVec3> list = new List<IntVec3>();
			TerrainGrid terrainGrid = map.terrainGrid;
			TerrainDef terrainDef=DefDatabase<TerrainDef>.GetNamed("EmptySpace");
			foreach (IntVec3 current in map.AllCells)
			{
				terrainGrid.SetTerrain(current, terrainDef);
			}
			MapGenFloatGrid elevation = MapGenerator.Elevation;
			foreach (IntVec3 allCell in map.AllCells)
			{
				elevation[allCell] = 0.0f;
			}
			MapGenFloatGrid fertility = MapGenerator.Fertility;
			foreach (IntVec3 allCell2 in map.AllCells)
			{
				fertility[allCell2] = 0.0f;
			}
		}

		public override int SeedPart
		{
			get
			{
				return 133742069;
			}
		}
	}
}