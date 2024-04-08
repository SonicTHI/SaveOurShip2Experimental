using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using SaveOurShip2;

namespace RimWorld
{
	[StaticConstructorOnStartup]
	public class CompArcholife : ThingComp
	{
		Color currentColor = new Color(0.5f, 0.5f, 0.5f);

		public override void CompTick()
		{
			base.CompTick();
			if (parent.IsHashIntervalTick(60))
			{
				Pawn myPawn = parent as Pawn;
				if (!ShipInteriorMod2.WorldComp.Unlocks.Contains("ArchotechSpore")) //no archo before spore
				{
					myPawn.Destroy(DestroyMode.Vanish);
					Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.ArchoAnimalSuddenDeath"), parent, MessageTypeDefOf.NeutralEvent);
					return;
				}
				if (myPawn.needs.food.CurLevel < myPawn.needs.food.MaxLevel)
					myPawn.needs.food.CurLevel += 0.01f;

				if (parent.IsHashIntervalTick(600)) //no archo without spore, purr
				{
					if (parent.Faction == Faction.OfPlayer && ModSettings_SoS.archoRemove)
					{
						bool hasSpore = false;
						foreach (Map m in Find.Maps)
						{
							if (m.listerBuildings.allBuildingsColonist.Any(b => b.def == ResourceBank.ThingDefOf.ShipArchotechSpore))
							{
								hasSpore = true;
								break;
							}
						}
						if (!hasSpore)
						{
							myPawn.Kill(new DamageInfo(DamageDefOf.Deterioration, 100f));
							if (Messages.liveMessages.Count == 0)
								Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.ArchoAnimalSporeDeath", parent), parent, MessageTypeDefOf.NegativeEvent);
							return;
						}
					}

					if (parent.Spawned && ((CompProperties_Archolife)props).purr)
					{
						foreach (Pawn p in parent.Map.mapPawns.FreeColonistsAndPrisonersSpawned)
						{
							if (p.Position.DistanceTo(parent.Position) < 15)
								p.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDef.Named("PsychicPurr"));
						}
					}
				}
				if (parent.IsHashIntervalTick(60000))
				{
					if (myPawn.health.hediffSet.hediffs.Where((Hediff hd) => hd.IsPermanent() || hd.def.chronic || hd is Hediff_MissingPart).TryRandomElement(out Hediff result))
					{
						HealthUtility.Cure(result);
						if (PawnUtility.ShouldSendNotificationAbout(myPawn))
						{
							Messages.Message("MessagePermanentWoundHealed".Translate(parent.LabelCap, myPawn.LabelShort, result.Label, myPawn.Named("PAWN")), myPawn, MessageTypeDefOf.PositiveEvent);
						}
					}
				}
			}

			if (ShieldState == ShieldState.Resetting)
			{
				ticksToReset--;
				if (ticksToReset <= 0)
				{
					Reset();
				}
			}
			else if (ShieldState == ShieldState.Active)
			{
				energy += EnergyGainPerTick;
				if (energy > EnergyMax)
				{
					energy = EnergyMax;
				}
			}

			//Disabled for now - was rapidly filling up the material pool.
			/*
			if(((CompProperties_Archolife)props).scintillate)
			{
				currentColor = new Color(currentColor.r + Rand.Range(-0.1f, 0.1f), currentColor.g + Rand.Range(-0.1f, 0.1f), currentColor.b + Rand.Range(-0.1f, 0.1f));
				((Pawn)parent).Drawer.renderer.graphics.nakedGraphic=((Pawn)parent).Drawer.renderer.graphics.nakedGraphic.GetColoredVersion(ShaderDatabase.CutoutComplex,currentColor,Color.white);
				typeof(PawnGraphicSet).GetField("cachedMatsBodyBaseHash", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(((Pawn)parent).Drawer.renderer.graphics, -69);
			}
			*/
		}

		private float energy = 0f;

		private int ticksToReset = -1;

		private int lastKeepDisplayTick = -9999;

		private Vector3 impactAngleVect;

		private int lastAbsorbDamageTick = -9999;

		private int StartingTicksToReset = 3200;

		private float EnergyOnReset = 0.2f;

		private float EnergyLossPerDamage = 0.033f;

		private int KeepDisplayingTicks = 1000;

		private static readonly Material BubbleMat = MaterialPool.MatFrom("Other/ShieldBubble", ShaderDatabase.Transparent);

		private float EnergyMax => ((CompProperties_Archolife)props).shield;

		private float EnergyGainPerTick => 0.0002f;

		public float Energy => energy;

		public ShieldState ShieldState
		{
			get
			{
				if (ticksToReset > 0)
				{
					return ShieldState.Resetting;
				}
				return ShieldState.Active;
			}
		}

		private bool ShouldDisplay
		{
			get
			{
				Pawn wearer = parent as Pawn;
				if (!wearer.Spawned || wearer.Dead || wearer.Downed)
				{
					return false;
				}
				if (wearer.InAggroMentalState)
				{
					return true;
				}
				if (wearer.Drafted)
				{
					return true;
				}
				if (wearer.Faction.HostileTo(Faction.OfPlayer) && !wearer.IsPrisoner)
				{
					return true;
				}
				if (Find.TickManager.TicksGame < lastKeepDisplayTick + KeepDisplayingTicks)
				{
					return true;
				}
				if(wearer.playerSettings!=null && wearer.playerSettings.RespectedMaster != null && wearer.playerSettings.followDrafted && wearer.playerSettings.RespectedMaster.Drafted)
				{
					return true;
				}
				return false;
			}
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref energy, "energy", 0f);
			Scribe_Values.Look(ref ticksToReset, "ticksToReset", -1);
			Scribe_Values.Look(ref lastKeepDisplayTick, "lastKeepDisplayTick", 0);
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			foreach (Gizmo wornGizmo in base.CompGetGizmosExtra())
			{
				yield return wornGizmo;
			}
			if (Find.Selector.SingleSelectedThing == parent)
			{
				Gizmo_EnergyShieldStatusArcholife gizmo_EnergyShieldStatus = new Gizmo_EnergyShieldStatusArcholife();
				gizmo_EnergyShieldStatus.shield = this;
				yield return gizmo_EnergyShieldStatus;
			}
		}

		public override void PostPreApplyDamage(DamageInfo dinfo, out bool absorbed)
		{
			if (ShieldState != 0)
			{
				absorbed = false;
				return;
			}
			if (dinfo.Def == DamageDefOf.EMP)
			{
				energy = 0f;
				Break();
				absorbed = false;
				return;
			}
			if (dinfo.Def.isRanged || dinfo.Def.isExplosive)
			{
				energy -= dinfo.Amount * EnergyLossPerDamage;
				if (energy < 0f)
				{
					Break();
				}
				else
				{
					AbsorbedDamage(dinfo);
				}
				absorbed = true;
				return;
			}
			absorbed = false;
			return;
		}

		public void KeepDisplaying()
		{
			lastKeepDisplayTick = Find.TickManager.TicksGame;
		}

		private void AbsorbedDamage(DamageInfo dinfo)
		{
			SoundDefOf.EnergyShield_AbsorbDamage.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
			impactAngleVect = Vector3Utility.HorizontalVectorFromAngle(dinfo.Angle);
			Vector3 loc = parent.TrueCenter() + impactAngleVect.RotatedBy(180f) * 0.5f;
			float num = Mathf.Min(10f, 2f + dinfo.Amount / 10f);
			FleckMaker.Static(loc, parent.Map, FleckDefOf.ExplosionFlash, num);
			int num2 = (int)num;
			for (int i = 0; i < num2; i++)
			{
				FleckMaker.ThrowDustPuff(loc, parent.Map, Rand.Range(0.8f, 1.2f));
			}
			lastAbsorbDamageTick = Find.TickManager.TicksGame;
			KeepDisplaying();
		}

		private void Break()
		{
			SoundDefOf.EnergyShield_Broken.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
			FleckMaker.Static(parent.TrueCenter(), parent.Map, FleckDefOf.ExplosionFlash, 12f);
			for (int i = 0; i < 6; i++)
			{
				FleckMaker.ThrowDustPuff(parent.TrueCenter() + Vector3Utility.HorizontalVectorFromAngle(Rand.Range(0, 360)) * Rand.Range(0.3f, 0.6f), parent.Map, Rand.Range(0.8f, 1.2f));
			}
			energy = 0f;
			ticksToReset = StartingTicksToReset;
		}

		private void Reset()
		{
			if (parent.Spawned)
			{
				SoundDefOf.EnergyShield_Reset.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
				FleckMaker.ThrowLightningGlow(parent.TrueCenter(), parent.Map, 3f);
			}
			ticksToReset = -1;
			energy = EnergyOnReset;
		}

		public override void PostDraw()
		{
			base.PostDraw();
			if (ShieldState == ShieldState.Active && ShouldDisplay)
			{
				float num = Mathf.Lerp(1.2f, 1.55f, energy);
				Vector3 drawPos = parent.DrawPos;
				drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
				int num2 = Find.TickManager.TicksGame - lastAbsorbDamageTick;
				if (num2 < 8)
				{
					float num3 = (float)(8 - num2) / 8f * 0.05f;
					drawPos += impactAngleVect * num3;
					num -= num3;
				}
				float angle = Rand.Range(0, 360);
				Vector3 s = new Vector3(num, 1f, num);
				Matrix4x4 matrix = default(Matrix4x4);
				matrix.SetTRS(drawPos, Quaternion.AngleAxis(angle, Vector3.up), s);
				Graphics.DrawMesh(MeshPool.plane10, matrix, BubbleMat, 0);
			}
		}
	}
}