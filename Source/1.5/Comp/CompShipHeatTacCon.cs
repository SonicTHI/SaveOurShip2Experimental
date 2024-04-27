using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace SaveOurShip2
{
	public class CompShipHeatTacCon : CompShipHeat
	{
		public bool PointDefenseMode = false;
		public bool HoldFire = false;

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look<bool>(ref PointDefenseMode, "PointDefenseMode", false);
			Scribe_Values.Look<bool>(ref HoldFire, "HoldFire", false);
		}
		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			foreach (Gizmo gizmo in base.CompGetGizmosExtra())
			{
				yield return gizmo;
			}
			if (myNet == null || this.parent.Faction != Faction.OfPlayer)
				yield break;
			HashSet<Def> defs = new HashSet<Def>();
			foreach (var t in myNet.Turrets)
			{
				defs.Add(t.parent.def);
			}
			if (myNet.Cloaks.Any())
			{
				bool anyCloakOn = myNet.AnyCloakOn();
				Command_Toggle toggleCloak = new Command_Toggle
				{
					toggleAction = delegate
					{
						foreach (CompShipHeatSource h in myNet.Cloaks)
						{
							h.parent.TryGetComp<CompFlickable>().SwitchIsOn = !anyCloakOn;
						}
					},
					defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.ToggleCloak"),
					defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.ToggleCloakDesc"),
					isActive = () => anyCloakOn
				};
				if (anyCloakOn)
					toggleCloak.icon = ContentFinder<Texture2D>.Get("UI/CloakingDeviceOn");
				else
					toggleCloak.icon = ContentFinder<Texture2D>.Get("UI/CloakingDeviceOff");
				yield return toggleCloak;
			}
			if (myNet.Shields.Any())
			{
				bool anyShieldOn = myNet.AnyShieldOn();
				Command_Toggle toggleShields = new Command_Toggle
				{
					toggleAction = delegate
					{
						foreach (var b in myNet.Shields)
						{
							b.flickComp.SwitchIsOn = !anyShieldOn;
						}
					},
					defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.ToggleShields"),
					defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.ToggleShieldsDesc"),
					icon = ContentFinder<Texture2D>.Get("UI/Shield_On"),
					isActive = () => anyShieldOn
				};
				yield return toggleShields;
			}
			var mapComp = parent.Map.GetComponent<ShipMapComp>();
			/*if (!mapComp.InCombat && mapComp.HasTarget)
			{
				Command_Action endTarget = new Command_Action
				{
					action = delegate
					{
						mapComp.EndTarget();
					},
					defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipTargetEnd"),
					defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipTargetEndDesc"),
					icon = ContentFinder<Texture2D>.Get("UI/EndBattle_Icon")
				};
				yield return endTarget;
			}*/
			if (!myNet.Turrets.Any())
				yield break;
			if (myNet.Turrets.Any(t => ((Building_ShipTurret)t.parent).holdFire == false))
				HoldFire = false;
			Command_Action selectWeapons = new Command_Action
			{
				action = delegate
				{
					Find.Selector.Deselect(this);
					foreach (CompShipHeat h in myNet.Turrets)
					{
						Find.Selector.Deselect(this.parent);
						Find.Selector.Select(h.parent);
					}
				},
				defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.SelectWeapons"),
				defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.SelectWeaponsDesc"),
				icon = ContentFinder<Texture2D>.Get("UI/Select_All_Weapons_Icon")
			};
			yield return selectWeapons;
			Command_Toggle command_Toggle = new Command_Toggle
			{
				defaultLabel = TranslatorFormattedStringExtensions.Translate("CommandHoldFire"),
				defaultDesc = TranslatorFormattedStringExtensions.Translate("CommandHoldFireDesc"),
				icon = ContentFinder<Texture2D>.Get("UI/Commands/HoldFire"),
				hotKey = KeyBindingDefOf.Misc6,
				toggleAction = delegate
				{
					HoldFire = !HoldFire;
					foreach (CompShipHeat h in myNet.Turrets)
					{
						((Building_ShipTurret)h.parent).holdFire = HoldFire;
						if (HoldFire)
							((Building_ShipTurret)h.parent).ResetForcedTarget();
					}
				},
				isActive = (() => HoldFire)
			};
			yield return command_Toggle;
			Command_Action ceaseFire = new Command_Action
			{
				action = delegate
				{
					Find.Selector.Deselect(this);
					foreach (CompShipHeat h in myNet.Turrets)
					{
						((Building_ShipTurret)h.parent).ResetForcedTarget();
					}
				},
				defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.HoldFire"),
				defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.HoldFireDesc"),
				icon = ContentFinder<Texture2D>.Get("UI/Commands/HoldFire")
			};
			yield return ceaseFire;
			if (myNet.Turrets.Any(t => t.Props.pointDefense))
			{
				if (myNet.Turrets.Any(t => t.Props.pointDefense && ((Building_ShipTurret)t.parent).PointDefenseMode == false))
					PointDefenseMode = false;
				Command_Toggle togglePD = new Command_Toggle
				{
					defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.TogglePointDefense"),
					defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.TogglePointDefenseDesc"),
					icon = ContentFinder<Texture2D>.Get("UI/PointDefenseMode"),
					toggleAction = delegate
					{
						PointDefenseMode = !PointDefenseMode;
						foreach (var t in myNet.Turrets.Where(b => b.Props.pointDefense))
						{
							((Building_ShipTurret)t.parent).PointDefenseMode = PointDefenseMode;
						}
					},
					isActive = (() => PointDefenseMode)
				};
				yield return togglePD;
			}
			foreach (Def def in defs)
			{
				Command_Action select = new Command_Action
				{
					action = delegate
					{
						Find.Selector.Deselect(this);
						foreach (CompShipHeat h in myNet.Turrets.Where(t => t.parent.def == def))
						{
							Find.Selector.Deselect(this.parent);
							Find.Selector.Select(h.parent);
						}
					},
					defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.SelectWeapons"),
					defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.SelectWeaponsDesc"),
					icon = ((ThingDef)def).uiIcon
				};
				yield return select;
			}
		}
	}
}
