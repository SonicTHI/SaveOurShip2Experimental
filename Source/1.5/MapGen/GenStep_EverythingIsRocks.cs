using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	class GenStep_EverythingIsRocks : GenStep
	{
		public override int SeedPart => 420133769;

		public override void Generate(Map map, GenStepParams parms)
		{
			map.regionAndRoomUpdater.Enabled = false;
			foreach(IntVec3 cell in map.AllCells)
			{
				GenSpawn.Spawn(GenStep_RocksFromGrid.RockDefAt(cell), cell, map);
				map.roofGrid.SetRoof(cell, RoofDefOf.RoofRockThick);
			}
			GenStep_ScatterLumpsMineable genStep_ScatterLumpsMineable = new GenStep_ScatterLumpsMineable();
			genStep_ScatterLumpsMineable.maxValue = float.MaxValue;
			float num3 = 10f;
			genStep_ScatterLumpsMineable.countPer10kCellsRange = new FloatRange(num3, num3);
			genStep_ScatterLumpsMineable.Generate(map, parms);
			map.regionAndRoomUpdater.Enabled = true;
		}
	}
}
