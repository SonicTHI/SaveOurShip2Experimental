using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using Vehicles;

namespace SaveOurShip2
{
	public class CompShipBay : ThingComp
	{
		private ShipMapComp mapComp;
		CellRect occupiedRect;
		public CompProps_ShipBay Props
		{
			get
			{
				return (CompProps_ShipBay)props;
			}
		}
		public bool CanLaunchShuttle(VehiclePawn vehicle)
		{
			if (vehicle.def.Size.x > Props.maxShuttleSize || vehicle.def.Size.z > Props.maxShuttleSize)
				return false;
			foreach (IntVec3 v in vehicle.OccupiedRect())
			{
				if (!occupiedRect.Contains(v))
					return false;
			}
			return true;
		}
		public bool CanFitShuttleAt(CellRect occArea)
		{
			if (occArea.Width > Props.maxShuttleSize || occArea.Height > Props.maxShuttleSize)
				return false;
			foreach (IntVec3 v in occArea)
			{
				if (!occupiedRect.Contains(v) || v.Impassable(parent.Map))
					return false;
			}
			Log.Message("CanFitShuttleAt");
			return true;
		}
		public bool CanFitShuttleSize(int x, int z) //we only have square shuttles so simplified, no rot
		{
			//if too big
			if (x > Props.maxShuttleSize || z > Props.maxShuttleSize)
				return false;
			//if 1x1
			if (x == 1 && z == 1 && occupiedRect.Any(p => p.Impassable(parent.Map)))
				return true;
			//if not in area
			IntVec2 halfSize = new IntVec2(x / 2, z / 2);
			//find a viable positions for shuttle
			List<IntVec3> validPos = new List<IntVec3>();
			foreach (IntVec3 pos in occupiedRect.Where(v => v.x >= occupiedRect.minX + halfSize.x && v.z >= occupiedRect.minZ +  halfSize.z && v.x <= occupiedRect.maxX - halfSize.x && v.z <= occupiedRect.maxZ - halfSize.z))
			{
				validPos.Add(pos);
			}
			//check all viable rects if occupied
			List<IntVec3> invalidPos = new List<IntVec3>();
			foreach (IntVec3 vec in validPos)
			{
				CellRect area = new CellRect(vec.x - halfSize.x, vec.z - halfSize.z, x, z);
				bool fits = true;
				foreach (IntVec3 v in area)
				{
					if (invalidPos.Contains(v) || v.Impassable(parent.Map))
					{
						invalidPos.Add(v);
						fits = false;
						break;
					}
				}
				if (fits)
					return true;
			}
			return false;
		}
		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			mapComp = parent.Map.GetComponent<ShipMapComp>();
			mapComp.Bays.Add(this);
			occupiedRect = parent.OccupiedRect();
		}
		public override void PostDeSpawn(Map map)
		{
			mapComp.Bays.Remove(this);
			base.PostDeSpawn(map);
		}
	}
}