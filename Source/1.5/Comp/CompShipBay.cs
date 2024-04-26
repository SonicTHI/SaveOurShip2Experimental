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
		public CompProps_ShipBay Props
		{
			get
			{
				return (CompProps_ShipBay)props;
			}
		}
		public bool CanLaunchShuttle(VehiclePawn vehicle)
		{
			foreach (IntVec3 v in vehicle.OccupiedRect())
			{
				if (!parent.OccupiedRect().Contains(v))
					return false;
			}
			return true;
		}
		public bool CanLandShuttle(VehiclePawn vehicle)
		{
			if (CanFitShuttleAt(vehicle.Position, vehicle.OccupiedRect()))
			{
				return true;
			}
			return false;
		}
		public bool CanFitShuttleAt(IntVec3 pos, CellRect occArea) //we only have square shuttles so simplified, no rot
		{
			//if too big
			if (occArea.Width > Props.maxShuttleSize || occArea.Height > Props.maxShuttleSize)
				return false;
			//if 1x1
			if (occArea.Width == 1 && occArea.Height == 1 && parent.OccupiedRect().Contains(pos) && pos.Impassable(parent.Map))
				return true;
			//if not in area
			IntVec2 halfSize = new IntVec2(occArea.Width / 2 + 1, occArea.Height / 2 + 1);
			IntVec3 halfSizeBay = new IntVec3(parent.def.Size.x / 2 + 1, 0, parent.def.Size.z / 2 + 1);
			if (pos.x - halfSize.x < parent.Position.x - halfSizeBay.x)
				return false;
			if (pos.z - halfSize.z < parent.Position.z - halfSizeBay.z)
				return false;
			if (pos.x + halfSize.x > parent.Position.x + halfSizeBay.x)
				return false;
			if (pos.x - halfSize.z > parent.Position.z + halfSizeBay.z)
				return false;
			//if occupied
			foreach (IntVec3 vec in occArea)
			{
				if (!vec.Impassable(parent.Map))
					return false;
			}
			return false;
		}
		public bool CanFitShuttle(CellRect occArea) //we only have square shuttles so simplified, no rot
		{
			//if too big
			if (occArea.Width > Props.maxShuttleSize || occArea.Height > Props.maxShuttleSize)
				return false;
			//if 1x1
			if (occArea.Width == 1 && occArea.Height == 1 && parent.OccupiedRect().Any(p => p.Impassable(parent.Map)))
				return true;
			//if not in area
			IntVec2 halfSize = new IntVec2(occArea.Width / 2 + 1, occArea.Height / 2 + 1);
			IntVec3 halfSizeBay = new IntVec3(parent.def.Size.x / 2 + 1, 0, parent.def.Size.z / 2 + 1);

			List<IntVec3> validPos = new List<IntVec3>();
			foreach (IntVec3 pos in occArea) //find a viable positions for shuttle
			{
				if (pos.x - halfSize.x < parent.Position.x - halfSizeBay.x)
					continue;
				if (pos.z - halfSize.z < parent.Position.z - halfSizeBay.z)
					continue;
				if (pos.x + halfSize.x > parent.Position.x + halfSizeBay.x)
					continue;
				if (pos.x - halfSize.z > parent.Position.z + halfSizeBay.z)
					continue;
				validPos.Add(pos);
			}
			//check all possible if occupied
			foreach (IntVec3 vec in validPos)
			{
				CellRect area = new CellRect(vec.x - halfSize.x, vec.z - halfSize.z, vec.x + halfSize.x, vec.z + halfSize.z);
				bool fits = true;
				foreach (IntVec3 v in area)
				{
					if (!v.Impassable(parent.Map))
					{
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
		}
		public override void PostDeSpawn(Map map)
		{
			mapComp.Bays.Remove(this);
			base.PostDeSpawn(map);
		}
	}
}