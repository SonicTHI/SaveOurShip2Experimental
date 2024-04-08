using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld.BaseGen
{
	class SymbolResolver_Interior_Salvage_Triangle : SymbolResolver
	{
		private List<IntVec3> cells = new List<IntVec3>();

		private const float FreeCellsFraction = 0.45f;

		public override void Resolve(ResolveParams rp)
		{
			Map map = BaseGen.globalSettings.map;

			this.CalculateFreeCells(rp.rect, 0.45f);

			ThingDef thingy = ThingDef.Named("ShipChunkSalvage");
			for (int i = 0; i < 6; i++)
			{
				IntVec3 cell = cells.RandomElement();
				if(!GenSpawn.WouldWipeAnythingWith(cell,Rot4.North,thingy,map,delegate { return true; }))
				{
					GenSpawn.Spawn(thingy, cell, map);
				}
			}
		}

		private void CalculateFreeCells(CellRect rect, float freeCellsFraction)
		{
			Map map = BaseGen.globalSettings.map;
			this.cells.Clear();
			foreach (IntVec3 current in rect)
			{
				if (current.Standable(map) && current.GetFirstItem(map) == null && current.GetThingList(map).Any(thing => thing.def == ResourceBank.ThingDefOf.ShipHullTileWrecked))
				{
					this.cells.Add(current);
				}
			}
			int num = (int)(freeCellsFraction * (float)this.cells.Count);
			for (int i = 0; i < num; i++)
			{
				this.cells.RemoveAt(Rand.Range(0, this.cells.Count));
			}
			this.cells.Shuffle<IntVec3>();
		}
	}
}
