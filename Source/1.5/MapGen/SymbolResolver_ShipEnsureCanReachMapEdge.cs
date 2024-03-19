using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld.BaseGen
{
	public class SymbolResolver_ShipEnsureCanReachMapEdge : SymbolResolver
	{
		private static HashSet<District> visited = new HashSet<District>();

		private static List<IntVec3> path = new List<IntVec3>();

		private static List<IntVec3> cellsInRandomOrder = new List<IntVec3>();

		public override void Resolve(ResolveParams rp)
		{
			SymbolResolver_ShipEnsureCanReachMapEdge.cellsInRandomOrder.Clear();
			foreach (var item in rp.rect)
			{
				SymbolResolver_ShipEnsureCanReachMapEdge.cellsInRandomOrder.Add(item);
			}
			SymbolResolver_ShipEnsureCanReachMapEdge.cellsInRandomOrder.Shuffle<IntVec3>();
			this.TryMakeAllCellsReachable(false, rp);
			this.TryMakeAllCellsReachable(true, rp);
		}

		private void TryMakeAllCellsReachable(bool canPathThroughNonStandable, ResolveParams rp)
		{
			Map map = BaseGen.globalSettings.map;
			SymbolResolver_ShipEnsureCanReachMapEdge.visited.Clear();
			for (int i = 0; i < SymbolResolver_ShipEnsureCanReachMapEdge.cellsInRandomOrder.Count; i++)
			{
				IntVec3 intVec = SymbolResolver_ShipEnsureCanReachMapEdge.cellsInRandomOrder[i];
				if (this.CanTraverse(intVec, canPathThroughNonStandable))
				{
					District room = intVec.GetDistrict(map);
					if (room != null && !SymbolResolver_ShipEnsureCanReachMapEdge.visited.Contains(room))
					{
						SymbolResolver_ShipEnsureCanReachMapEdge.visited.Add(room);
						TraverseParms traverseParms = TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false);
						if (!map.reachability.CanReachMapEdge(intVec, traverseParms))
						{
							bool found = false;
							IntVec3 foundDest = IntVec3.Invalid;
							map.floodFiller.FloodFill(intVec, (IntVec3 x) => !found && this.CanTraverse(x, canPathThroughNonStandable), delegate (IntVec3 x)
							{
								if (found)
								{
									return;
								}
								if (map.reachability.CanReachMapEdge(x, traverseParms))
								{
									found = true;
									foundDest = x;
								}
							}, 2147483647, true, null);
							if (found)
							{
								this.ReconstructPathAndDestroyWalls(foundDest, room, rp);
							}
						}
					}
				}
			}
			SymbolResolver_ShipEnsureCanReachMapEdge.visited.Clear();
		}

		private void ReconstructPathAndDestroyWalls(IntVec3 foundDest, District room, ResolveParams rp)
		{
			Map map = BaseGen.globalSettings.map;
			map.floodFiller.ReconstructLastFloodFillPath(foundDest, SymbolResolver_ShipEnsureCanReachMapEdge.path);
			while (SymbolResolver_ShipEnsureCanReachMapEdge.path.Count >= 2 && SymbolResolver_ShipEnsureCanReachMapEdge.path[0].AdjacentToCardinal(room) && SymbolResolver_ShipEnsureCanReachMapEdge.path[1].AdjacentToCardinal(room))
			{
				SymbolResolver_ShipEnsureCanReachMapEdge.path.RemoveAt(0);
			}
			IntVec3 intVec = IntVec3.Invalid;
			ThingDef thingDef = null;
			IntVec3 intVec2 = IntVec3.Invalid;
			ThingDef thingDef2 = null;
			for (int i = 0; i < SymbolResolver_ShipEnsureCanReachMapEdge.path.Count; i++)
			{
				Building edifice = SymbolResolver_ShipEnsureCanReachMapEdge.path[i].GetEdifice(map);
				if (this.IsWallOrRock(edifice))
				{
					if (!intVec.IsValid)
					{
						intVec = SymbolResolver_ShipEnsureCanReachMapEdge.path[i];
						thingDef = edifice.Stuff;
					}
					intVec2 = SymbolResolver_ShipEnsureCanReachMapEdge.path[i];
					thingDef2 = edifice.Stuff;
					edifice.Destroy(DestroyMode.Vanish);
				}
			}
			if (intVec.IsValid)
			{
				Thing thing = ThingMaker.MakeThing(ThingDef.Named("ShipAirlockWrecked"));
				thing.SetFaction(rp.faction, null);
				GenSpawn.Spawn(thing, intVec, map, WipeMode.Vanish);
			}
			if (intVec2.IsValid && intVec2 != intVec && !intVec2.AdjacentToCardinal(intVec))
			{
				Thing thing2 = ThingMaker.MakeThing(ThingDef.Named("ShipAirlockWrecked"));
				thing2.SetFaction(rp.faction, null);
				GenSpawn.Spawn(thing2, intVec2, map, WipeMode.Vanish);
			}
		}

		private bool CanTraverse(IntVec3 c, bool canPathThroughNonStandable)
		{
			Map map = BaseGen.globalSettings.map;
			Building edifice = c.GetEdifice(map);
			return this.IsWallOrRock(edifice) || ((canPathThroughNonStandable || c.Standable(map)) && !c.Impassable(map));
		}

		private bool IsWallOrRock(Building b)
		{
			return b != null && (b.def == ThingDefOf.Wall || b.def.building.isNaturalRock || b.def == ThingDefOf.Ship_Beam || b.def == ResourceBank.ThingDefOf.Ship_Beam_Wrecked);
		}
	}
}