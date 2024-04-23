using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

using RimWorld.BaseGen;

namespace SaveOurShip2
{
	public class SymbolResolver_ExtraShipDoor : SymbolResolver
	{
		public override void Resolve(ResolveParams rp)
		{
			Map map = BaseGen.globalSettings.map;
			IntVec3 intVec = IntVec3.Invalid;
			int num = -1;
			for (int i = 0; i < 4; i++)
			{
				if (!this.WallHasDoor(rp.rect, new Rot4(i)))
				{
					for (int j = 0; j < 2; j++)
					{
						IntVec3 intVec2;
						if (this.TryFindRandomDoorSpawnCell(rp.rect, new Rot4(i), out intVec2))
						{
							int distanceToExistingDoors = this.GetDistanceToExistingDoors(intVec2, rp.rect);
							if (!intVec.IsValid || distanceToExistingDoors > num)
							{
								intVec = intVec2;
								num = distanceToExistingDoors;
								if (num == 2147483647)
								{
									break;
								}
							}
						}
					}
				}
			}
			if (intVec.IsValid)
			{
				Thing thing = ThingMaker.MakeThing(ResourceBank.ThingDefOf.ShipAirlockWrecked);
				thing.SetFaction(rp.faction, null);
				GenSpawn.Spawn(thing, intVec, BaseGen.globalSettings.map, WipeMode.Vanish);
			}
		}

		private bool WallHasDoor(CellRect rect, Rot4 dir)
		{
			Map map = BaseGen.globalSettings.map;
			foreach (IntVec3 current in rect.GetEdgeCells(dir))
			{
				if (current.GetDoor(map) != null)
				{
					return true;
				}
			}
			return false;
		}

		private bool TryFindRandomDoorSpawnCell(CellRect rect, Rot4 dir, out IntVec3 found)
		{
			Map map = BaseGen.globalSettings.map;
			if (dir == Rot4.North)
			{
				if (rect.Width <= 2)
				{
					found = IntVec3.Invalid;
					return false;
				}
				int newX;
				if (!Rand.TryRangeInclusiveWhere(rect.minX + 1, rect.maxX - 1, delegate (int x)
				{
					IntVec3 c = new IntVec3(x, 0, rect.maxZ + 1);
					IntVec3 c2 = new IntVec3(x, 0, rect.maxZ - 1);
					return c.InBounds(map) && c.Standable(map) && c2.InBounds(map) && c2.Standable(map);
				}, out newX))
				{
					found = IntVec3.Invalid;
					return false;
				}
				found = new IntVec3(newX, 0, rect.maxZ);
				return true;
			}
			else if (dir == Rot4.South)
			{
				if (rect.Width <= 2)
				{
					found = IntVec3.Invalid;
					return false;
				}
				int newX2;
				if (!Rand.TryRangeInclusiveWhere(rect.minX + 1, rect.maxX - 1, delegate (int x)
				{
					IntVec3 c = new IntVec3(x, 0, rect.minZ - 1);
					IntVec3 c2 = new IntVec3(x, 0, rect.minZ + 1);
					return c.InBounds(map) && c.Standable(map) && c2.InBounds(map) && c2.Standable(map);
				}, out newX2))
				{
					found = IntVec3.Invalid;
					return false;
				}
				found = new IntVec3(newX2, 0, rect.minZ);
				return true;
			}
			else if (dir == Rot4.West)
			{
				if (rect.Height <= 2)
				{
					found = IntVec3.Invalid;
					return false;
				}
				int newZ;
				if (!Rand.TryRangeInclusiveWhere(rect.minZ + 1, rect.maxZ - 1, delegate (int z)
				{
					IntVec3 c = new IntVec3(rect.minX - 1, 0, z);
					IntVec3 c2 = new IntVec3(rect.minX + 1, 0, z);
					return c.InBounds(map) && c.Standable(map) && c2.InBounds(map) && c2.Standable(map);
				}, out newZ))
				{
					found = IntVec3.Invalid;
					return false;
				}
				found = new IntVec3(rect.minX, 0, newZ);
				return true;
			}
			else
			{
				if (rect.Height <= 2)
				{
					found = IntVec3.Invalid;
					return false;
				}
				int newZ2;
				if (!Rand.TryRangeInclusiveWhere(rect.minZ + 1, rect.maxZ - 1, delegate (int z)
				{
					IntVec3 c = new IntVec3(rect.maxX + 1, 0, z);
					IntVec3 c2 = new IntVec3(rect.maxX - 1, 0, z);
					return c.InBounds(map) && c.Standable(map) && c2.InBounds(map) && c2.Standable(map);
				}, out newZ2))
				{
					found = IntVec3.Invalid;
					return false;
				}
				found = new IntVec3(rect.maxX, 0, newZ2);
				return true;
			}
		}

		private int GetDistanceToExistingDoors(IntVec3 cell, CellRect rect)
		{
			Map map = BaseGen.globalSettings.map;
			int num = 2147483647;
			foreach (IntVec3 current in rect.EdgeCells)
			{
				if (current.GetDoor(map) != null)
				{
					num = Mathf.Min(num, Mathf.Abs(cell.x - current.x) + Mathf.Abs(cell.z - current.z));
				}
			}
			return num;
		}
	}
}