using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using SmashTools;

namespace SaveOurShip2
{
	//[StaticConstructorOnStartup]
	public class CompShipBay : ThingComp
	{
		/*private static Graphic roofedGraphic = GraphicDatabase.Get(typeof(Graphic_Single), "Things/Building/Ship/Shuttle_Bay_Roof", ShaderDatabase.Cutout, new Vector2(5f, 5f), Color.white, Color.white);

		public static GraphicData roofedData = new GraphicData();
		public static Graphic roofedGraphicTile;
		static CompShipBay()
		{
			roofedData.texPath = "Things/Building/Ship/Shuttle_Bay_Roof";
			roofedData.graphicClass = typeof(Graphic_Single);
			roofedData.shaderType = ShaderTypeDefOf.Cutout;
			roofedGraphicTile = new Graphic_256(roofedData.Graphic);
		}*/
		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Collections.Look<IntVec3>(ref reservedArea, "reservedArea", LookMode.Reference);
			if (reservedArea == null)
				reservedArea = new HashSet<IntVec3>();
		}

		public ShipMapComp mapComp;
		public CellRect bayRect;
		Matrix4x4 matrix = new Matrix4x4();
		public HashSet<IntVec3> reservedArea = new HashSet<IntVec3>();
		HashSet<CompFueledTravel> dockedShuttles = new HashSet<CompFueledTravel>();
		CompRefuelable compRefuelable;
		CompPowerTrader compPowerTrader;

		public CompProps_ShipBay Props
		{
			get
			{
				return (CompProps_ShipBay)props;
			}
		}
		public void ReserveArea(IntVec3 pos, VehiclePawn vehicle) //we only have square shuttles so simplified, no rot
		{
			CellRect rect = new CellRect(pos .x- vehicle.def.size.x / 2, pos.z - vehicle.def.Size.z / 2, vehicle.def.size.x, vehicle.def.size.z);
			foreach (IntVec3 v in rect)
			{
				reservedArea.Add(v);
			}
		}
		public void UnReserveArea(IntVec3 pos, VehiclePawn vehicle) //we only have square shuttles so simplified, no rot
		{
			CellRect rect = new CellRect(pos.x - vehicle.def.size.x / 2, pos.z - vehicle.def.Size.z / 2, vehicle.def.size.x, vehicle.def.size.z);
			foreach (IntVec3 v in rect)
			{
				reservedArea.Remove(v);
			}
		}
		public bool CanLaunchShuttle(VehiclePawn vehicle)
		{
			if (vehicle.def.Size.x > Props.maxShuttleSize || vehicle.def.Size.z > Props.maxShuttleSize)
				return false;
			foreach (IntVec3 v in vehicle.OccupiedRect())
			{
				if (!bayRect.Contains(v))
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
				if (!bayRect.Contains(v) || v.Impassable(parent.Map) || reservedArea.Contains(v))
				{
					return false;
				}
			}
			return true;
		}
		public IntVec3 CanFitShuttleSize(VehiclePawn vehicle) //we only have square shuttles so simplified, no rot
		{
			int x = vehicle.def.size.x;
			int z = vehicle.def.size.z;
			//if too big
			if (x > Props.maxShuttleSize || z > Props.maxShuttleSize)
				return IntVec3.Zero;
			//if 1x1
			if (x == 1 && z == 1)
			{
				foreach (IntVec3 vec in bayRect)
				{
					if (!vec.Impassable(parent.Map) && !reservedArea.Contains(vec))
					{
						return vec;
					}
				}
				return IntVec3.Zero;
			}
			//if not in area
			IntVec2 halfSize = new IntVec2(x / 2, z / 2);
			//find a viable positions for shuttle
			List<IntVec3> validPos = new List<IntVec3>();
			foreach (IntVec3 pos in bayRect.Where(v => v.x >= bayRect.minX + halfSize.x && v.z >= bayRect.minZ +  halfSize.z && v.x <= bayRect.maxX - halfSize.x && v.z <= bayRect.maxZ - halfSize.z))
			{
				validPos.Add(pos);
			}
			//check all viable rects if occupied
			HashSet<IntVec3> invalidPos = new HashSet<IntVec3>();
			foreach (IntVec3 vec in validPos)
			{
				CellRect area = new CellRect(vec.x - halfSize.x, vec.z - halfSize.z, x, z);
				bool fits = true;
				foreach (IntVec3 v in area)
				{
					if (invalidPos.Contains(v) || v.Impassable(parent.Map) || v.GetThingList(parent.Map).Any(t => t is VehiclePawn) || reservedArea.Contains(vec))
					{
						invalidPos.Add(v);
						fits = false;
						break;
					}
				}
				if (fits)
				{
					return vec;
				}
			}
			return IntVec3.Zero;
		}
		public override void PostDraw()
		{
			base.PostDraw();
			if ((Find.PlaySettings.showRoofOverlay || parent.GetRoom().Cells.Any(c => c.Fogged(parent.Map))) && parent.Position.Roofed(parent.Map))
			{
				//roofedGraphic.Draw(new Vector3(parent.DrawPos.x, Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead), parent.DrawPos.z), parent.Rotation, parent);

				Graphics.DrawMesh(MeshPool.plane10, matrix, Props.roofGraphic, 0);

				//roofedGraphic.Draw(new Vector3(parent.DrawPos.x, parent.DrawPos.y + 10f, parent.DrawPos.z), parent.Rotation, parent);

				//Graphics.DrawMesh(roofedGraphicTile.MeshAt(parent.Rotation), new Vector3(parent.DrawPos.x + occupiedRect.Width / 2f, parent.DrawPos.y + 10f, parent.DrawPos.z + occupiedRect.Height / 2f), Quaternion.identity, roofedGraphicTile.MatSingleFor(parent), 0);
			}
		}
		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			mapComp = parent.Map.GetComponent<ShipMapComp>();
			mapComp.Bays.Add(this);
			bayRect = parent.OccupiedRect().ContractedBy(Props.borderSize);
			matrix.SetTRS(new Vector3(parent.DrawPos.x, Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead), parent.DrawPos.z), parent.Rotation.AsQuat, new Vector3(parent.DrawSize.x, 1f, parent.DrawSize.y));
			ReCacheDockedShuttles();
			compRefuelable = parent.GetComp<CompRefuelable>();
			compPowerTrader = parent.GetComp<CompPowerTrader>();
		}
		public override void PostDeSpawn(Map map)
		{
			mapComp.Bays.Remove(this);
			base.PostDeSpawn(map);
		}

        public override void CompTick()
        {
            base.CompTick();
			int tick = Find.TickManager.TicksGame;
			if (tick % 8 == 0 && parent.Spawned && compRefuelable != null && compPowerTrader != null && compPowerTrader.PowerOn) //ie, is powered and is not a salvage bay
			{
				if (tick % 256 == 0)
					ReCacheDockedShuttles();
				foreach (CompFueledTravel comp in dockedShuttles)
				{
					if (compRefuelable.fuel > 0 && comp.FuelPercentOfTarget < 1)
					{
						comp.Refuel(1);
						compRefuelable.ConsumeFuel(1);
					}
					if (Props.autoRepair>0 && comp.Vehicle.statHandler.NeedsRepairs)
					{
						VehicleComponent component = GenCollection.FirstOrDefault<VehicleComponent>(comp.Vehicle.statHandler.ComponentsPrioritized,(VehicleComponent c) => c.HealthPercent < Props.repairUpTo);
						if (component != null)
						{
							component.HealComponent(Props.autoRepair);
							comp.Vehicle.CrashLanded = false;
							FleckMaker.ThrowMicroSparks(GenAdj.CellsOccupiedBy(comp.Vehicle).RandomElement().ToVector3Shifted(), parent.Map);
						}
					}
				}
			}
        }

		void ReCacheDockedShuttles()
        {
			dockedShuttles = new HashSet<CompFueledTravel>();
			foreach(IntVec3 cell in GenAdj.CellsOccupiedBy(parent))
            {
				VehiclePawn shuttle = cell.GetThingList(parent.Map).OfType<VehiclePawn>().FirstOrDefault();
				if(shuttle!=null)
                {
					CompFueledTravel fueledTravel = shuttle.GetComp<CompFueledTravel>();
					if (fueledTravel != null && fueledTravel.Props.fuelType == ResourceBank.ThingDefOf.ShuttleFuelPods)
						dockedShuttles.Add(fueledTravel);
                }
            }
        }
    }
}