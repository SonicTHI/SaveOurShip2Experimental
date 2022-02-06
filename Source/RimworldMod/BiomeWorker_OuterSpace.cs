using RimWorld.Planet;
using System;

namespace RimWorld
{
	public class BiomeWorker_OuterSpace : BiomeWorker
	{
		public override float GetScore(Tile tile, int tileID)
		{
			return -999f;
		}
	}
}
