using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SaveOurShip2
{
	[StaticConstructorOnStartup]
	public class CompEngineTrail : ThingComp
	{
		private static Graphic trailGraphic = GraphicDatabase.Get(typeof(Graphic_Multi), "Things/Building/Ship/Ship_Engine_Trail_Double", ShaderDatabase.MoteGlow, new Vector2(7, 16.5f), Color.white, Color.white);
		private static Graphic trailGraphicSingle = GraphicDatabase.Get(typeof(Graphic_Multi), "Things/Building/Ship/Ship_Engine_Trail_Single", ShaderDatabase.MoteGlow, new Vector2(7, 16.5f), Color.white, Color.white);
		private static Graphic trailGraphicLarge = GraphicDatabase.Get(typeof(Graphic_Multi), "Things/Building/Ship/NuclearEngineTrail", ShaderDatabase.MoteGlow, new Vector2(7, 26.5f), Color.white, Color.white);
		private static Graphic trailGraphicEnergy = GraphicDatabase.Get(typeof(Graphic_Multi), "Things/Building/Ship/Ship_Engine_Trail_Energy_Double", ShaderDatabase.MoteGlow, new Vector2(7, 16.5f), Color.white, Color.white);
		private static Graphic trailGraphicEnergyLarge = GraphicDatabase.Get(typeof(Graphic_Multi), "Things/Building/Ship/Ship_Engine_Trail_Energy_Large", ShaderDatabase.MoteGlow, new Vector2(11, 26.5f), Color.white, Color.white);
		private static Vector3[] offset = { new Vector3(0, 0, -5.5f), new Vector3(-5.5f, 0, 0), new Vector3(0, 0, 5.5f), new Vector3(5.5f, 0, 0) };
		private static Vector3[] offsetL = { new Vector3(0, 0, -11f), new Vector3(-11f, 0, 0), new Vector3(0, 0, 11f), new Vector3(11f, 0, 0) };
		private static Vector3[] offsetE = { new Vector3(0, 0, -9.2f), new Vector3(-9.2f, 0, 0), new Vector3(0, 0, 9.2f), new Vector3(9.2f, 0, 0) };
		public static IntVec2[] killOffset = { new IntVec2(0, -6), new IntVec2(-6, 0), new IntVec2(0, 4), new IntVec2(4, 0) };
		public static IntVec2[] killOffsetL = { new IntVec2(0, -13), new IntVec2(-13, 0), new IntVec2(0, 7), new IntVec2(7, 0) };
		public virtual CompProps_EngineTrail Props
		{
			get { return props as CompProps_EngineTrail; }
		}
		public virtual int FuelUse
		{
            get
            {
                return Props.fuelUse;
            }
        }
		public virtual int Thrust
		{
			get
			{
				return Props.thrust;
			}
		}
		public bool PodFueled => refuelComp.Props.fuelFilter.AllowedThingDefs.Contains(ResourceBank.ThingDefOf.ShuttleFuelPods);
		public bool active = false;
		int size;
		public HashSet<IntVec3> ExhaustArea = new HashSet<IntVec3>();
		public ShipMapComp mapComp;
		public CompFlickable flickComp;
		public CompRefuelable refuelComp;
		public CompPowerTrader powerComp;
		Sustainer sustainer;

		public bool CanFire()
		{
			if (flickComp.SwitchIsOn && powerComp.PowerOn)
			{
				if (Props.reactionless)
				{
					if (powerComp.PowerOn)
						return true;
				}
				else if (Props.energy)
				{
					return true;
				}
				else if (refuelComp.Fuel > FuelUse)
				{
					return true;
				}
			}
			return false;
		}
		public bool CanFire(Rot4 rot)
		{
			if (flickComp.SwitchIsOn && powerComp.PowerOn)
			{
				if (Props.reactionless)
				{
					if (powerComp.PowerOn)
						return true;
				}
				else if (rot == parent.Rotation)
				{
					if (Props.energy)
					{
						return true;
					}
					else if (refuelComp.Fuel > FuelUse)
					{
						return true;
					}
				}
			}
			return false;
		}
		public void On()
		{
			if (Props.energy)
			{
				powerComp.PowerOutput = -2000 * Thrust;
			}
			active = true;
		}
		public bool OnOld()
		{
			if (Props.reactionless)
			{
				active = true;
				return true;
			}
			if (Props.energy)
			{
				powerComp.PowerOutput = -2000 * Thrust;
				active = true;
				return true;
			}
			if (refuelComp.Fuel > FuelUse)
			{
				active = true;
				return true;
			}
			return false;
		}
		public void Off()
		{
			if (Props.energy)
			{
				powerComp.PowerOutput = -200 * Thrust;
			}
			active = false;
			/*if (sustainer != null && !sustainer.Ended)
			{
				sustainer.End();
			}*/
		}
		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			flickComp = parent.TryGetComp<CompFlickable>();
			refuelComp = parent.TryGetComp<CompRefuelable>();
			powerComp = parent.TryGetComp<CompPowerTrader>();
			mapComp = parent.Map.GetComponent<ShipMapComp>();
			size = parent.def.size.x;
			if (Props.reactionless)
				return;
			ExhaustArea.Clear();
			CellRect rectToKill;
			if (size > 3)
				rectToKill = parent.OccupiedRect().MovedBy(killOffsetL[parent.Rotation.AsInt]).ExpandedBy(2);
			else
				rectToKill = parent.OccupiedRect().MovedBy(killOffset[parent.Rotation.AsInt]).ExpandedBy(1);
			if (parent.Rotation.IsHorizontal)
				rectToKill.Width = rectToKill.Width * 2 - 3;
			else
				rectToKill.Height = rectToKill.Height * 2 - 3;
			foreach (IntVec3 v in rectToKill.Where(v => v.InBounds(parent.Map)))
			{
				ExhaustArea.Add(v);
			}
		}
		public override void PostDeSpawn(Map map)
		{
			var mapComp = map.GetComponent<ShipMapComp>();
			if (mapComp.ShipsOnMap.Values.Any(s => !s.IsWreck && s.Engines.Any()))
				mapComp.EngineRot = -1;
			Off();
			//sustainer = null;
			base.PostDeSpawn(map);
		}
		public override void PostDraw()
		{
			base.PostDraw();
			if (active && !Props.reactionless)
			{
				if (Props.energy)
				{
					if (size > 3)
					{
						trailGraphicEnergyLarge.drawSize = new Vector2(11, 26.5f + 0.5f * Mathf.Cos(Find.TickManager.TicksGame / 4));
						trailGraphicEnergyLarge.Draw(new Vector3(parent.DrawPos.x + offsetE[parent.Rotation.AsInt].x, parent.DrawPos.y + 1f, parent.DrawPos.z + offsetE[parent.Rotation.AsInt].z), parent.Rotation, parent);
					}
					else
					{
						trailGraphicEnergy.drawSize = new Vector2(7, 15.5f + 0.5f * Mathf.Cos(Find.TickManager.TicksGame / 4));
						trailGraphicEnergy.Draw(new Vector3(parent.DrawPos.x + offset[parent.Rotation.AsInt].x, parent.DrawPos.y + 1f, parent.DrawPos.z + offset[parent.Rotation.AsInt].z), parent.Rotation, parent);
					}
				}
				else
				{
					if (size > 3)
					{
						trailGraphicLarge.drawSize = new Vector2(7, 26.5f + 0.5f * Mathf.Cos(Find.TickManager.TicksGame / 4));
						trailGraphicLarge.Draw(new Vector3(parent.DrawPos.x + offsetL[parent.Rotation.AsInt].x, parent.DrawPos.y + 1f, parent.DrawPos.z + offsetL[parent.Rotation.AsInt].z), parent.Rotation, parent);
					}
					else
					{
						Vector2 drawSize = new Vector2(7, 15.5f + 0.5f * Mathf.Cos(Find.TickManager.TicksGame / 4));
						if (size == 3)
						{
							trailGraphic.drawSize = drawSize;
							trailGraphic.Draw(new Vector3(parent.DrawPos.x + offset[parent.Rotation.AsInt].x, parent.DrawPos.y + 1f, parent.DrawPos.z + offset[parent.Rotation.AsInt].z), parent.Rotation, parent);
						}
						else
						{
							trailGraphicSingle.drawSize = drawSize;
							trailGraphicSingle.Draw(new Vector3(parent.DrawPos.x + offset[parent.Rotation.AsInt].x, parent.DrawPos.y + 1f, parent.DrawPos.z + offset[parent.Rotation.AsInt].z), parent.Rotation, parent);
						}
					}
				}
			}
		}
		public override void CompTick()
		{
			base.CompTick();
			if (active)
			{
				if (Find.TickManager.TicksGame % 60 == 0)
				{
					if (!flickComp.SwitchIsOn || !powerComp.PowerOn)
					{
						Off();
						return;
					}
					if (refuelComp != null)
					{
						if (refuelComp.Fuel < FuelUse)
						{
							Off();
							return;
						}
						refuelComp.ConsumeFuel(FuelUse);
					}
					/*if (!Props.soundWorking.NullOrUndefined())
					{
						if (sustainer == null || sustainer.Ended)
						{
							sustainer = Props.soundWorking.TrySpawnSustainer(SoundInfo.InMap(parent, MaintenanceType.None));
						}
						sustainer.Maintain();
					}*/
				}
				if (Props.reactionless)
					return;
				//destroy stuff in plume
				HashSet<Thing> toBurn = new HashSet<Thing>();
				foreach (IntVec3 cell in ExhaustArea)
				{
					foreach (Thing t in cell.GetThingList(parent.Map))
					{
						if ((t.def.useHitPoints || t is Pawn) && t.def.altitudeLayer != AltitudeLayer.Terrain)
							toBurn.Add(t);
					}
				}
				foreach (Thing t in toBurn)
				{
					t.TakeDamage(new DamageInfo(DamageDefOf.Bomb, 100));
				}
			}
		}
	}
}
