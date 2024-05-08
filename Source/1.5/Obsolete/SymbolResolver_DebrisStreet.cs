using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

using RimWorld.BaseGen;

namespace SaveOurShip2
{
	public class SymbolResolver_DebrisStreet : SymbolResolver
	{
		private static List<bool> street = new List<bool>();

		public override void Resolve(ResolveParams rp)
		{
			bool? streetHorizontal = rp.streetHorizontal;
			bool flag = (!streetHorizontal.HasValue) ? (rp.rect.Width >= rp.rect.Height) : streetHorizontal.Value;
			int width = (!flag) ? rp.rect.Width : rp.rect.Height;
			ThingDef floorDef = ResourceBank.ThingDefOf.ShipHullTileWrecked;
			this.CalculateStreet(rp.rect, flag, floorDef);
			this.FillStreetGaps(flag, width);
			//this.RemoveShortStreetParts(flag, width);
			this.SpawnFloor(rp.rect, flag, floorDef);
		}

		private void CalculateStreet(CellRect rect, bool horizontal, ThingDef floorDef)
		{
			SymbolResolver_DebrisStreet.street.Clear();
			int num = (!horizontal) ? rect.Height : rect.Width;
			for (int i = 0; i < num; i++)
			{
				if (horizontal)
				{
					SymbolResolver_DebrisStreet.street.Add(this.CausesStreet(new IntVec3(rect.minX + i, 0, rect.minZ - 1), floorDef) && this.CausesStreet(new IntVec3(rect.minX + i, 0, rect.maxZ + 1), floorDef));
				}
				else
				{
					SymbolResolver_DebrisStreet.street.Add(this.CausesStreet(new IntVec3(rect.minX - 1, 0, rect.minZ + i), floorDef) && this.CausesStreet(new IntVec3(rect.maxX + 1, 0, rect.minZ + i), floorDef));
				}
			}
		}

		private void FillStreetGaps(bool horizontal, int width)
		{
			int num = -1;
			for (int i = 0; i < SymbolResolver_DebrisStreet.street.Count; i++)
			{
				if (SymbolResolver_DebrisStreet.street[i])
				{
					num = i;
				}
				else if (num != -1 && i - num <= width)
				{
					for (int j = i + 1; j < i + width + 1; j++)
					{
						if (j >= SymbolResolver_DebrisStreet.street.Count)
						{
							break;
						}
						if (SymbolResolver_DebrisStreet.street[j])
						{
							SymbolResolver_DebrisStreet.street[i] = true;
							break;
						}
					}
				}
			}
		}

		private void RemoveShortStreetParts(bool horizontal, int width)
		{
			for (int i = 0; i < SymbolResolver_DebrisStreet.street.Count; i++)
			{
				if (SymbolResolver_DebrisStreet.street[i])
				{
					int num = 0;
					for (int j = i; j < SymbolResolver_DebrisStreet.street.Count; j++)
					{
						if (!SymbolResolver_DebrisStreet.street[j])
						{
							break;
						}
						num++;
					}
					int num2 = 0;
					for (int k = i; k >= 0; k--)
					{
						if (!SymbolResolver_DebrisStreet.street[k])
						{
							break;
						}
						num2++;
					}
					int num3 = num2 + num - 1;
					if (num3 < width)
					{
						SymbolResolver_DebrisStreet.street[i] = false;
					}
				}
			}
		}

		private void SpawnFloor(CellRect rect, bool horizontal, ThingDef floorDef)
		{
			Map map = BaseGen.globalSettings.map;
			TerrainGrid terrainGrid = map.terrainGrid;
			foreach (var item in rect)
			{
				IntVec3 current = item;
				if ((horizontal && SymbolResolver_DebrisStreet.street[current.x - rect.minX]) || (!horizontal && SymbolResolver_DebrisStreet.street[current.z - rect.minZ]))
				{
					if (Rand.Chance(0.6f))
						GenSpawn.Spawn(ThingMaker.MakeThing(floorDef), current, map, WipeMode.Vanish);
					else if (Rand.Chance(0.2f))
					{
						Thing thing = ThingMaker.MakeThing(ThingDefOf.ChunkSlagSteel, null);
						GenSpawn.Spawn(thing, current, map, WipeMode.Vanish);
					}
				}
			}
		}

		private bool CausesStreet(IntVec3 c, ThingDef floorDef)
		{
			Map map = BaseGen.globalSettings.map;
			if (!c.InBounds(map))
			{
				return false;
			}
			Building edifice = c.GetEdifice(map);
			return (edifice != null && (edifice.def == ThingDefOf.Ship_Beam || edifice.def == ResourceBank.ThingDefOf.Ship_Beam_Wrecked)) || c.GetThingList(map).Any(t => t.def == ResourceBank.ThingDefOf.ShipHullTile || t.def == ResourceBank.ThingDefOf.ShipHullTileWrecked || t.def==ThingDefOf.ChunkSlagSteel) || c.GetDoor(map) != null;
		}
	}
}