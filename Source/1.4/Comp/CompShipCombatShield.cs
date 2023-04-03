using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
using SaveOurShip2;

namespace RimWorld
{
    [StaticConstructorOnStartup]
    public class CompShipCombatShield : CompShipHeat
    {
        private static readonly Material ShieldMaterial = MaterialPool.MatFrom("Things/Building/Ship/ShieldBubbleSOS", ShaderDatabase.MoteGlow);
        private static readonly Material ConeMaterial = MaterialPool.MatFrom("Other/ForceFieldCone", ShaderDatabase.MoteGlow);
        private static readonly MaterialPropertyBlock PropBlock = new MaterialPropertyBlock();
        public static float HeatDamageMult = 3f;

        public float radiusSet=40;
        public float radius=40;
        public bool shutDown;
        private int lastIntercepted = -69;
        private float lastInterceptAngle;

		public CompFlickable flickComp;
        public CompPowerTrader powerComp;
        public CompBreakdownable breakComp;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            flickComp = parent.TryGetComp<CompFlickable>();
            powerComp = parent.TryGetComp<CompPowerTrader>();
            breakComp = parent.TryGetComp<CompBreakdownable>();
            parent.Map.GetComponent<ShipHeatMapComp>().Shields.Add(this);
        }
        public override void PostDeSpawn(Map map)
        {
            map.GetComponent<ShipHeatMapComp>().Shields.Remove(this);
            base.PostDeSpawn(map);
        }
        public override void CompTick()
        {
            base.CompTick();
            this.shutDown = breakComp.BrokenDown || !powerComp.PowerOn;
            if (!this.shutDown && Find.TickManager.TicksGame % 60 == 0)
            {			
                float absDiff = Math.Abs(radius - radiusSet);
			    if (absDiff > 0 && absDiff < 1)
				    radius = radiusSet;
                else if (radiusSet > radius)
                    radius+=1f;
                else if (radiusSet < radius)
                    radius-=1f;
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
			stringBuilder.Append("ShipInsideRadius".Translate() + radius + "/" + radiusSet);
            return stringBuilder.ToString();
        }

        public static Dictionary<ThingDef, float> ProjectileToMult = new Dictionary<ThingDef, float>() {
            {ThingDef.Named("Proj_ShipSpinalBeamPlasma"), 1.5f},
            {ThingDef.Named("Bullet_Torpedo_HighExplosive"), 0.33f},
            {ThingDef.Named("Bullet_Torpedo_EMP"), 10f},
            {ThingDef.Named("Bullet_Torpedo_Antimatter"), 0.33f},
        };

        public virtual float CalcHeatGenerated(Projectile_ExplosiveShipCombat proj)
        {
            float heatGenerated = proj.DamageAmount * HeatDamageMult * Props.heatMultiplier;
            heatGenerated *= ProjectileToMult.TryGetValue(proj.def, 1f);
            return heatGenerated;
        }

        public void HitShield(Projectile_ExplosiveShipCombat proj)
        {
            lastInterceptAngle = proj.DrawPos.AngleToFlat(parent.TrueCenter());
            lastIntercepted = Find.TickManager.TicksGame;

            float heatGenerated = CalcHeatGenerated(proj);
            if (proj is Projectile_ExplosiveShipCombatLaser || proj is Projectile_ExplosiveShipCombatPsychic)
            {
                ShipCombatLaserMote obj = (ShipCombatLaserMote)(object)ThingMaker.MakeThing(ThingDef.Named("ShipCombatLaserMote"));
                obj.origin = proj.origin;
                obj.destination = proj.DrawPos;
                obj.color = proj.Launcher.TryGetComp<CompShipHeat>().Props.laserColor;
                if (proj.weaponDamageMultiplier > 1f)
                    obj.large = true;
                obj.Attach(parent);
                GenSpawn.Spawn(obj, proj.DrawPos.ToIntVec3(), proj.Map, 0);
            }
            if (!AddHeatToNetwork(heatGenerated))
            {
                if (myNet != null)
                    AddHeatToNetwork(myNet.StorageCapacity - myNet.StorageUsed);
                breakComp.DoBreakdown();
                GenExplosion.DoExplosion(parent.Position, parent.Map, 1.9f, DamageDefOf.Flame, parent);
                SoundDefOf.EnergyShield_Broken.PlayOneShot(new TargetInfo(parent));
				if (parent.Faction != Faction.OfPlayer)
                    Messages.Message(TranslatorFormattedStringExtensions.Translate("ShipCombatShieldBrokenEnemy"), parent, MessageTypeDefOf.PositiveEvent);
                else
                    Messages.Message(TranslatorFormattedStringExtensions.Translate("ShipCombatShieldBroken"), parent, MessageTypeDefOf.NegativeEvent);
            }
            if (powerComp != null && powerComp.PowerNet.CurrentStoredEnergy() > heatGenerated /20)
            {
                foreach (CompPowerBattery bat in powerComp.PowerNet.batteryComps)
                {
                    bat.DrawPower(Mathf.Min((heatGenerated / 20) * bat.StoredEnergy / powerComp.PowerNet.CurrentStoredEnergy(), bat.StoredEnergy));
                }
            }

            FleckMaker.ThrowMicroSparks(parent.DrawPos, parent.Map);

            GenExplosion.DoExplosion(proj.Position, parent.Map, 3, DefDatabase<DamageDef>.GetNamed("ShieldExplosion"), null);
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
            Scribe_Values.Look(ref radius, "radius",40);
            Scribe_Values.Look(ref radiusSet, "radiusSet",40);
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
            if (this.parent.Faction != Faction.OfPlayer)
                yield break;
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            yield return new Command_Action
            {
                action = delegate ()
                {
                    this.ChangeShieldSize(-10f);
                },
                defaultLabel = "- 10",
                defaultDesc = "CommandDecShieldRadius".Translate(),
                hotKey = KeyBindingDefOf.Misc5,
                icon = ContentFinder<Texture2D>.Get("UI/Commands/TempLower", true)
            };
            yield return new Command_Action
            {
                action = delegate ()
                {
                    this.ChangeShieldSize(-1f);
                },
                defaultLabel = "- 1",
                defaultDesc = "CommandDecShieldRadius".Translate(),
                hotKey = KeyBindingDefOf.Misc4,
                icon = ContentFinder<Texture2D>.Get("UI/Commands/TempLower", true)
            };
            yield return new Command_Action
            {
                action = delegate ()
                {
                    radiusSet = 40f;
                    powerComp.PowerOutput = -1500;
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
                },
                defaultLabel = "CommandResetShieldRadius".Translate(),
                defaultDesc = "CommandResetShieldRadiusDesc".Translate(),
                hotKey = KeyBindingDefOf.Misc1,
                icon = ContentFinder<Texture2D>.Get("UI/Commands/TempReset", true)
            };
            yield return new Command_Action
            {
                action = delegate ()
                {
                    this.ChangeShieldSize(1f);
                },
                defaultLabel = "+ 1",
                defaultDesc = "CommandIncShieldRadius".Translate(),
                hotKey = KeyBindingDefOf.Misc2,
                icon = ContentFinder<Texture2D>.Get("UI/Commands/TempRaise", true)
            };
            yield return new Command_Action
            {
                action = delegate ()
                {
                    this.ChangeShieldSize(10f);
                },
                defaultLabel = "+ 10",
                defaultDesc = "CommandIncShieldRadius".Translate(),
                hotKey = KeyBindingDefOf.Misc3,
                icon = ContentFinder<Texture2D>.Get("UI/Commands/TempRaise", true)
            };
            yield break;
        }
        public void ChangeShieldSize(float radius)
        {
            SoundDefOf.DragSlider.PlayOneShotOnCamera(null);
            radiusSet += radius;
            radiusSet = Mathf.Clamp(radiusSet, 20f, 60f);
        }
    }
}
