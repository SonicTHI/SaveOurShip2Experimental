using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	class Projectile_ExplosiveShipLaser : Projectile_ExplosiveShip
	{
		protected override void Impact(Thing hitThing, bool blockedByShield = false)
		{
			CompShipHeat heat = base.Launcher.TryGetComp<CompShipHeat>();
			base.Impact(hitThing);
			ShipCombatLaserMote obj = (ShipCombatLaserMote)(object)ThingMaker.MakeThing(ResourceBank.ThingDefOf.ShipCombatLaserMote);
			obj.origin = this.origin;
			if (hitThing != null)
				obj.destination = hitThing.DrawPos;
			else
				obj.destination = this.DrawPos;
			if (heat != null)
				obj.color = heat.Props.laserColor;
			else
				obj.color = Color.red;
			if(this.weaponDamageMultiplier>1.0f)
				obj.large = true;
			obj.Attach(hitThing);
			if (hitThing != null)
				GenSpawn.Spawn(obj, hitThing.Position, hitThing.Map, 0);
			else if (this.Map != null)
				GenSpawn.Spawn(obj, this.Position, this.Map, 0);
		}

		protected override void Explode()
		{
			if(this.weaponDamageMultiplier == 1.0f)
				base.Explode();
			else
			{
				GenExplosion.DoExplosion(base.Position, base.Map, base.def.projectile.explosionRadius * Mathf.Sqrt(this.weaponDamageMultiplier), base.def.projectile.damageDef, base.launcher, base.DamageAmount, base.ArmorPenetration, base.def.projectile.soundExplode, base.equipmentDef, base.def, intendedTarget.Thing, base.def.projectile.postExplosionSpawnThingDef, base.def.projectile.postExplosionSpawnChance, base.def.projectile.postExplosionSpawnThingCount, preExplosionSpawnThingDef: base.def.projectile.preExplosionSpawnThingDef, preExplosionSpawnChance: base.def.projectile.preExplosionSpawnChance, preExplosionSpawnThingCount: base.def.projectile.preExplosionSpawnThingCount, applyDamageToExplosionCellsNeighbors: base.def.projectile.applyDamageToExplosionCellsNeighbors, chanceToStartFire: base.def.projectile.explosionChanceToStartFire, damageFalloff: base.def.projectile.explosionDamageFalloff, direction: origin.AngleToFlat(destination));
				Destroy();
			}
		}
	}

	[StaticConstructorOnStartup]
	public class ShipCombatLaserMote : MoteDualAttached
	{
		private static readonly Material BeamMat = MaterialPool.MatFrom("Other/OrbitalBeam", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);
		public Vector3 origin;
		public Vector3 destination;
		public Color color;
		public bool large = false;
		public bool tiny = false;

		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			origin.y = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays);
			destination.y = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays);
			float alpha = ((Mote)this).Alpha;
			if (alpha <= 0f)
			{
				return;
			}
			Color instanceColor = color;
			instanceColor.a *= alpha;
			Material material = BeamMat;
			if (instanceColor != material.color)
			{
				material = MaterialPool.MatFrom((Texture2D)material.mainTexture, ShaderDatabase.MoteGlow, instanceColor);
			}
			if (!(Mathf.Abs(origin.x - destination.x) < 0.01f) || !(Mathf.Abs(origin.z - destination.z) < 0.01f))
			{
				Vector3 pos = (origin + destination) / 2f;
				if (!(origin == destination))
				{
					float z = GenGeo.MagnitudeHorizontal(origin - destination);
					Quaternion q = Quaternion.LookRotation(origin - destination);
					Vector3 s = new Vector3(large? 5f : tiny ? 0.25f : 1f, 1f, z);
					Matrix4x4 matrix = default(Matrix4x4);
					matrix.SetTRS(pos, q, s);
					Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
				}
			}
		}
	}
}
