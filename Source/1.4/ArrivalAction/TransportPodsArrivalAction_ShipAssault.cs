using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using SaveOurShip2;

namespace RimWorld.Planet
{
	public class TransportPodsArrivalAction_ShipAssault : TransportPodsArrivalAction
	{
		private MapParent mapParent;
		private List<IntVec3> cells;
		public TransportPodsArrivalAction_ShipAssault()
		{
		}
		public TransportPodsArrivalAction_ShipAssault(MapParent mapParent)
		{
			this.mapParent = mapParent;
		}
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_References.Look<MapParent>(ref this.mapParent, "mapParent", false);
			Scribe_Collections.Look<IntVec3>(ref this.cells, "cells");
		}
		public override FloatMenuAcceptanceReport StillValid(IEnumerable<IThingHolder> pods, int destinationTile)
		{
			FloatMenuAcceptanceReport floatMenuAcceptanceReport = base.StillValid(pods, destinationTile);
			if (!floatMenuAcceptanceReport)
			{
				return floatMenuAcceptanceReport;
			}
			if (this.mapParent != null && this.mapParent.Tile != destinationTile)
			{
				return false;
			}
			return TransportPodsArrivalAction_LandInSpecificCell.CanLandInSpecificCell(pods, this.mapParent);
		}
		public override void Arrived(List<ActiveDropPodInfo> pods, int tile)
		{
			if (ModSettings_SoS.easyMode)
				cells = mapParent.Map.AllCells.Where(c => DropCellFinder.CanPhysicallyDropInto(c, mapParent.Map, true, true) && c.Standable(mapParent.Map)).ToList();
			else
				cells = FindTargetsForPods(mapParent.Map, pods.Count);
			Thing lookTarget = TransportPodsArrivalActionUtility.GetLookTarget(pods);
			for (int i = 0; i < pods.Count; i++)
			{
				IntVec3 c;
				DropCellFinder.TryFindDropSpotNear(this.cells[i], this.mapParent.Map, out c, false, true, true, null, true);
				DropPodUtility.MakeDropPodAt(c, this.mapParent.Map, pods[i]);
				//DropTravelingTransportPods(pods, i, this.cells[i], this.mapParent.Map);
			}
			TransportPodsArrivalActionUtility.RemovePawnsFromWorldPawns(pods);
			if (!pods.NullOrEmpty())
				Messages.Message("SoSPodsArrived".Translate(), lookTarget, MessageTypeDefOf.TaskCompletion, true);
		}
		public static bool CanLandInSpecificCell(IEnumerable<IThingHolder> pods, MapParent mapParent)
		{
			return mapParent != null && mapParent.Spawned && mapParent.HasMap && (!mapParent.EnterCooldownBlocksEntering() || FloatMenuAcceptanceReport.WithFailMessage("MessageEnterCooldownBlocksEntering".Translate(mapParent.EnterCooldownTicksLeft().ToStringTicksToPeriod(true, false, true, true))));
		}
		public List<IntVec3> FindTargetsForPods(Map map, int num)
		{
			//targets outer cells, then finds rooms near, a room search that finds outer rooms would be better
			Room outdoors = new IntVec3(0, 0, 0).GetRoom(map);
			List<IntVec3> targetCells = new List<IntVec3>();
			List<IntVec3> validCells = new List<IntVec3>();
			if (num == 0)
				return targetCells;
			foreach (IntVec3 cell in outdoors.BorderCells.Where(c => c.InBounds(map)))
				validCells.Add(cell);
			if (validCells.Any())
			{
				validCells.Shuffle();
				int i = 0;
				while (i < num + 1)
				{
					//find cell in cluster
					foreach (IntVec3 intVec in GenAdj.CellsAdjacent8Way(validCells[i], Rot4.North, new IntVec2(7, 7)))
					{
						Room room = intVec.GetRoom(map);
						if (intVec.InBounds(map) && intVec.Standable(map) && room != null && !room.TouchesMapEdge && !room.IsDoorway && !ShipInteriorMod2.AnyBridgeIn(room))
						{
							bool prevent = false;
							List<Thing> thingList = intVec.GetThingList(map);
							for (int j = 0; j < thingList.Count; j++)
							{
								Thing thing = thingList[j];
								if (thing.def.preventSkyfallersLandingOn)
								{
									prevent = true;
									break;
								}
							}
							if (!prevent)
							{
								targetCells.Add(intVec);
								break;
							}
						}
					}
					i++;
					if (i > validCells.Count || i > 30)
						break;
				}
				if (!targetCells.NullOrEmpty())
				{
					//Log.Message("Initial pod target cells: " + targetCells.Count);
					if (targetCells.Count  == num)
						return targetCells;
                    else
                    {
						//fill or remove to match
						if (targetCells.Count < num)
						{
							int k = 0;
							while (targetCells.Count < num)
							{
								targetCells.Add(targetCells[k]);
								k++;
							}
						}
						else if (targetCells.Count > num)
                        {
							while (targetCells.Count - 1 > num)
							{
								targetCells.RemoveLast();
							}
						}
						//Log.Message("Final pod target cells: " + targetCells.Count);
						return targetCells;
					}
				}
			}
			//fallbacks
			Log.Message("Found no pod target cells, fallback to random room loc");
			targetCells = map.AllCells.Where(c => c.Roofed(map) && DropCellFinder.CanPhysicallyDropInto(c, map, true, true) && c.Standable(map)).ToList();
			if (targetCells.NullOrEmpty())
				targetCells = map.AllCells.Where(c => DropCellFinder.CanPhysicallyDropInto(c, map, true, true) && c.Standable(map)).ToList();
			return targetCells;
		}
	}
}
