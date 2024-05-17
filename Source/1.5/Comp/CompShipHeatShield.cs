﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using Vehicles;

namespace SaveOurShip2
{
	[StaticConstructorOnStartup]
	public class CompShipHeatShield : CompShipHeat
	{
		private static readonly Material ShieldMaterial = MaterialPool.MatFrom("Things/Building/Ship/ShieldBubbleSOS", ShaderDatabase.MoteGlow);
		private static readonly Material ConeMaterial = MaterialPool.MatFrom("Other/ForceFieldCone", ShaderDatabase.MoteGlow);
		private static readonly MaterialPropertyBlock PropBlock = new MaterialPropertyBlock();
		public static float HeatDamageMult = 3.5f;

		public float radiusSet = -1;
		public float radius = -1;
		public bool shutDown;
		bool vehicleWantsShutDown = false;
		private int lastIntercepted = -69;
		private float lastInterceptAngle;

		public CompFlickable flickComp;
		public CompPowerTrader powerComp;
		public CompBreakdownable breakComp;
		VehiclePawn parentVehicle;

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			if (radiusSet == -1)
				radiusSet = radius = Props.shieldDefault;
			if (parent.Spawned)
			{
				ShipMapComp mapComp = parent.Map.GetComponent<ShipMapComp>();
				if (mapComp != null)
					mapComp.Shields.Add(this);
			}
			parentVehicle = parent as VehiclePawn;
			if (parentVehicle != null)
				return;
			flickComp = parent.TryGetComp<CompFlickable>();
			powerComp = parent.TryGetComp<CompPowerTrader>();
			breakComp = parent.TryGetComp<CompBreakdownable>();
		}
		public override void PostDeSpawn(Map map)
		{
			map.GetComponent<ShipMapComp>().Shields.Remove(this);
			base.PostDeSpawn(map);
		}
		public override void CompTick()
		{
			base.CompTick();
			if (parentVehicle != null)
			{
				if (!parentVehicle.Spawned || parentVehicle.statHandler == null)
					shutDown = true;
				else
					shutDown = parentVehicle.statHandler.GetComponentHealth("shieldGenerator") <= 10 || vehicleWantsShutDown;
			}
			else if (breakComp != null && powerComp != null)
				shutDown = breakComp.BrokenDown || !powerComp.PowerOn || Venting;
			else
				shutDown = true;

			if (!this.shutDown && Find.TickManager.TicksGame % 60 == 0)
			{			
				float absDiff = Math.Abs(radius - radiusSet);
				if (absDiff > 0 && absDiff < 1)
					radius = radiusSet;
				else if (radiusSet > radius)
					radius+=1f;
				else if (radiusSet < radius)
					radius-=1f;
				if(powerComp != null)
					powerComp.PowerOutput = radius * -50;
			}
		}
		public override string CompInspectStringExtra()
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (shutDown)
			{
				if (stringBuilder.Length != 0)
				{
					stringBuilder.AppendLine();
				}
				stringBuilder.Append(TranslatorFormattedStringExtensions.Translate("ShutDown"));
			}
			stringBuilder.Append(TranslatorFormattedStringExtensions.Translate("SoS.ShieldRadius", radius, radiusSet));
			return stringBuilder.ToString();
		}

		public static Dictionary<ThingDef, float> ProjectileToMult = new Dictionary<ThingDef, float>() {
			{ThingDef.Named("Proj_ShipSpinalBeamPlasma"), 1.5f},
			{ThingDef.Named("Bullet_Torpedo_HighExplosive"), 0.33f},
			{ThingDef.Named("Bullet_Torpedo_EMP"), 10f},
			{ThingDef.Named("Bullet_Torpedo_Antimatter"), 0.33f},
		};

		public virtual float CalcHeatGenerated(Projectile proj)
		{
			float heatGenerated = proj.DamageAmount * HeatDamageMult * Props.heatMultiplier;
			heatGenerated *= ProjectileToMult.TryGetValue(proj.def, 1f);
			if (proj is Projectile_ExplosiveShipDebris)
				heatGenerated *= 10;
			return heatGenerated;
		}

		public void HitShield(Projectile proj)
		{
			lastInterceptAngle = proj.DrawPos.AngleToFlat(parent.TrueCenter());
			lastIntercepted = Find.TickManager.TicksGame;

			float heatGenerated = CalcHeatGenerated(proj);
			if (parent.Spawned && (proj is Projectile_ExplosiveShipLaser || proj is Projectile_ExplosiveShipPsychic))
			{
				ShipCombatLaserMote obj = (ShipCombatLaserMote)(object)ThingMaker.MakeThing(ResourceBank.ThingDefOf.ShipCombatLaserMote);
				obj.origin = proj.origin;
				obj.destination = proj.DrawPos;
				if (proj.Launcher != null && proj.Launcher.TryGetComp<CompShipHeat>()!=null)
				{
					obj.color = proj.Launcher.TryGetComp<CompShipHeat>().Props.laserColor;
					if (proj.weaponDamageMultiplier > 1f)
						obj.large = true;
				}
				else
					obj.color = Color.red;
				obj.Attach(parent);
				GenSpawn.Spawn(obj, proj.DrawPos.ToIntVec3(), proj.Map, 0);
			}
			if (!AddHeatToNetwork(heatGenerated))
			{
				if (myNet != null)
					AddHeatToNetwork(myNet.StorageCapacity - myNet.StorageUsed);
				if (breakComp != null)
					breakComp.DoBreakdown();
				else
				{
					parentVehicle.statHandler.SetComponentHealth("shieldGenerator", 0);
					if(parentVehicle.Spawned)
						parentVehicle.Map.GetComponent<ListerVehiclesRepairable>().Notify_VehicleTookDamage(parentVehicle);
				}
				if(parent.Spawned)
					GenExplosion.DoExplosion(parent.Position, parent.Map, 1.9f, DamageDefOf.Flame, parent);
				SoundDef.Named("EnergyShield_Broken").PlayOneShot(new TargetInfo(parent));
				if (parent.Faction != Faction.OfPlayer)
					Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.CombatShieldBrokenEnemy"), parent, MessageTypeDefOf.PositiveEvent);
				else
					Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.CombatShieldBroken"), parent, MessageTypeDefOf.NegativeEvent);
			}
			if (powerComp != null && powerComp.PowerNet.CurrentStoredEnergy() > heatGenerated / 20)
			{
				foreach (CompPowerBattery bat in powerComp.PowerNet.batteryComps)
				{
					bat.DrawPower(Mathf.Min((heatGenerated / 20) * bat.StoredEnergy / powerComp.PowerNet.CurrentStoredEnergy(), bat.StoredEnergy));
				}
			}

			if (parent.Spawned)
			{
				FleckMaker.ThrowMicroSparks(parent.DrawPos, parent.Map);
				GenExplosion.DoExplosion(proj.Position, parent.Map, 3, DefDatabase<DamageDef>.GetNamed("ShieldExplosion"), null, screenShakeFactor: (proj is Projectile_ExplosiveShip ? 1 : 0));
			}
			proj.Destroy();
		}

		public float Alpha()
		{
			if (shutDown)
			{
				if(!Find.Selector.IsSelected(parent))
					return 0f;
				return 0.5f;
			}
			float baseAlpha = 0;
			if(Find.Selector.IsSelected(parent))
				baseAlpha = Mathf.Lerp(0.5f, 1, (Mathf.Sin((float)(Gen.HashCombineInt(parent.thingIDNumber, 42069) % 100) + Time.realtimeSinceStartup * 2f) + 1f) / 2f);
			else
				baseAlpha = Mathf.Lerp(0.25f, 0.75f, (Mathf.Sin((float)(Gen.HashCombineInt(parent.thingIDNumber, 69420) % 100) + Time.realtimeSinceStartup * 0.7f) + 1f) / 2f);
			int num = Find.TickManager.TicksGame - lastIntercepted;
			float interceptAlpha = Mathf.Clamp01(1f - (float)num / 120f) * 0.99f;
			return Mathf.Max(baseAlpha, interceptAlpha);
		}
		private float HitConeAlpha()
		{
			int num = Find.TickManager.TicksGame - lastIntercepted;
			return Mathf.Clamp01(1f - (float)num / 42f) * 0.99f;
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref lastIntercepted, "lastIntercepted");
			Scribe_Values.Look(ref shutDown, "shutDown");
			Scribe_Values.Look(ref radius, "radius", Props.shieldDefault);
			Scribe_Values.Look(ref radiusSet, "radiusSet", Props.shieldDefault);
			Scribe_Values.Look(ref vehicleWantsShutDown, "wantsShutDown");
		}
		public override void PostDraw()
		{
			base.PostDraw();
			Vector3 pos = parent.Position.ToVector3Shifted();
			pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
			float alpha = Alpha();
			if (alpha > 0f)
			{
				Color value = Props.color;
				value.a *= alpha;
				PropBlock.SetColor(ShaderPropertyIDs.Color, value);
				Matrix4x4 matrix = default(Matrix4x4);
				matrix.SetTRS(pos, Quaternion.identity, new Vector3(radius * 2.1f, 1f, radius * 2.1f));
				Graphics.DrawMesh(MeshPool.plane10, matrix, ShieldMaterial, 0, null, 0, PropBlock);
			}
			float coneAlpha = HitConeAlpha();
			if (coneAlpha > 0f)
			{
				Color color = Color.white;
				color.a *= coneAlpha;
				PropBlock.SetColor(ShaderPropertyIDs.Color, color);
				Matrix4x4 matrix2 = default(Matrix4x4);
				matrix2.SetTRS(pos, Quaternion.Euler(0f, lastInterceptAngle - 90f, 0f), new Vector3(radius * 2.3f, 1f, radius * 2.3f));
				Graphics.DrawMesh(MeshPool.plane10, matrix2, ConeMaterial, 0, null, 0, PropBlock);
			}
		}
		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			if (parent.Faction != Faction.OfPlayer)
				yield break;

			foreach (Gizmo gizmo in base.CompGetGizmosExtra())
			{
				yield return gizmo;
			}
			yield return new Command_Action
			{
				action = delegate ()
				{
					ChangeShieldSize(-10f);
				},
				defaultLabel = "- 10",
				defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.ShieldRadiusDec"),
				hotKey = KeyBindingDefOf.Misc5,
				icon = ContentFinder<Texture2D>.Get("UI/Commands/TempLower", true)
			};
			yield return new Command_Action
			{
				action = delegate ()
				{
					ChangeShieldSize(-1f);
				},
				defaultLabel = "- 1",
				defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.ShieldRadiusDec"),
				hotKey = KeyBindingDefOf.Misc4,
				icon = ContentFinder<Texture2D>.Get("UI/Commands/TempLower", true)
			};
			yield return new Command_Action
			{
				action = delegate ()
				{
					radiusSet = Props.shieldDefault;
					if(powerComp != null)
						powerComp.PowerOutput = -powerComp.Props.basePowerConsumption;
					SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
				},
				defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.ShieldRadiusReset"),
				defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.ShieldRadiusResetDesc"),
				hotKey = KeyBindingDefOf.Misc1,
				icon = ContentFinder<Texture2D>.Get("UI/Commands/TempReset", true)
			};
			yield return new Command_Action
			{
				action = delegate ()
				{
					ChangeShieldSize(1f);
				},
				defaultLabel = "+ 1",
				defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.ShieldRadiusInc"),
				hotKey = KeyBindingDefOf.Misc2,
				icon = ContentFinder<Texture2D>.Get("UI/Commands/TempRaise", true)
			};
			yield return new Command_Action
			{
				action = delegate ()
				{
					ChangeShieldSize(10f);
				},
				defaultLabel = "+ 10",
				defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.ShieldRadiusInc"),
				hotKey = KeyBindingDefOf.Misc3,
				icon = ContentFinder<Texture2D>.Get("UI/Commands/TempRaise", true)
			};
			if (parent is VehiclePawn)
			{
				yield return new Command_Toggle
				{
					toggleAction = delegate ()
					{
						vehicleWantsShutDown = !vehicleWantsShutDown;
					},
					isActive = delegate () { return !vehicleWantsShutDown; },
					defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.ShuttleToggleShield"),
					defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.ShuttleToggleShieldDesc"),
					icon = ContentFinder<Texture2D>.Get("UI/Shield_On")
				};
            }
			yield break;
		}
		public void ChangeShieldSize(float radius)
		{
			SoundDefOf.DragSlider.PlayOneShotOnCamera(null);
			radiusSet += radius;
			radiusSet = Mathf.Clamp(radiusSet, Props.shieldMin, Props.shieldMax);
		}
	}
}
