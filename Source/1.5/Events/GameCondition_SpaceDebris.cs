using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld
{
	[StaticConstructorOnStartup]
	public class GameCondition_SpaceDebris : GameCondition
	{

		private IntRange initialStrikeDelay = new IntRange(1000, 1500);
		private IntRange TicksBetweenStrikes;
		private int nextLaunchProjTicks;
		ThingDef spawnProjectile;
		public int angle = 0;
		public int EventType = 0;
		public bool asteroids;
		static readonly List<ThingDef> projectiles = new List<ThingDef> //meh but true random size/damage/etc. d req a lot more work
		{
			ResourceBank.ThingDefOf.Proj_ShipDebrisA, ResourceBank.ThingDefOf.Proj_ShipDebrisB, ResourceBank.ThingDefOf.Proj_ShipDebrisC, ResourceBank.ThingDefOf.Proj_ShipDebrisD, ResourceBank.ThingDefOf.Proj_ShipRockA, ResourceBank.ThingDefOf.Proj_ShipRockB, ResourceBank.ThingDefOf.Proj_ShipRockC
		};
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look<IntRange>(ref this.initialStrikeDelay, "initialStrikeDelay", default(IntRange), false);
			Scribe_Values.Look<IntRange>(ref this.TicksBetweenStrikes, "ticksBetweenStrikes", default(IntRange), false);
			Scribe_Values.Look<int>(ref this.nextLaunchProjTicks, "nextLaunchProjTicks", 0, false);
			Scribe_Values.Look<int>(ref this.angle, "angle", 0, false);
			Scribe_Values.Look<bool>(ref this.asteroids, "asteroids", false, false);
		}
		public override void Init()
		{
			this.nextLaunchProjTicks = Find.TickManager.TicksGame + this.initialStrikeDelay.RandomInRange;
			//interval
			TicksBetweenStrikes = new IntRange(60, 200);
			//debris or rocks - light, fast / mixed / slow, large
			//small rocks, debris
			//large debris (spawns mats)
			//rocks (spawns rock)
			//pods (spawns critter/mech)
		}
		public override void GameConditionTick()
		{
			//on random timer
			if (Find.TickManager.TicksGame > this.nextLaunchProjTicks)
			{
				//determine origin and target //td change to angle based target +-15, origin clip -+ 10
				IntVec3 spawnCell;
				IntVec3 targetCell;
				if (angle == 0) //N-S
				{
					spawnCell = new IntVec3(Rand.RangeInclusive(0, SingleMap.Size.x - 1), 0, SingleMap.Size.z - 1);
					targetCell = new IntVec3(Rand.RangeInclusive(0, SingleMap.Size.x - 1), 0, -1);
				}
				else if (angle == 1) //E-W
				{
					spawnCell = new IntVec3(SingleMap.Size.x - 1, 0, Rand.RangeInclusive(0, SingleMap.Size.z - 1));
					targetCell = new IntVec3(-1, 0, Rand.RangeInclusive(0, SingleMap.Size.z - 1));
				}
				else if (angle == 2) //S-N
				{
					spawnCell = new IntVec3(Rand.RangeInclusive(0, SingleMap.Size.x - 1), 0, 0);
					targetCell = new IntVec3(Rand.RangeInclusive(0, SingleMap.Size.x - 1), 0, SingleMap.Size.z);
				}
				else //W-E
				{
					spawnCell = new IntVec3(0, 0, Rand.RangeInclusive(0, SingleMap.Size.z - 1));
					targetCell = new IntVec3(SingleMap.Size.x, 0, Rand.RangeInclusive(0, SingleMap.Size.z - 1));
				}
				//type
				int index;
				if (asteroids)
					index = Rand.RangeInclusive(4, 6);
				else
					index = Rand.RangeInclusive(0, 3);
				spawnProjectile = projectiles[index];
				//int size = Rand.RangeInclusive(1, 5);
				//spawnProjectile.projectile.speed = (50 / size) + 50;
				//spawnProjectile.projectile.explosionRadius = size;
				Projectile projectile = (Projectile)GenSpawn.Spawn(spawnProjectile, spawnCell, SingleMap);
				((Projectile_ExplosiveShipDebris)projectile).index = index;
				//((Projectile_ExplosiveShipDebris)projectile).drawSize = new Vector2 (size, size);
				projectile.Launch(null, spawnCell.ToVector3Shifted(), targetCell, targetCell, ProjectileHitFlags.All, equipment: null);
				this.nextLaunchProjTicks = Find.TickManager.TicksGame + TicksBetweenStrikes.RandomInRange;
			}
		}
		public override void End()
		{
			var mapComp = SingleMap.GetComponent<ShipHeatMapComp>();
			mapComp.ShipMapState = ShipMapState.nominal;
			mapComp.BurnTimer = 0;
			mapComp.MapFullStop();
			base.End();
		}
	}
}
