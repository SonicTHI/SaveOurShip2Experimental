using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimWorld
{
	public class Command_TargetShipCombat : Command
	{
		public Verb verb;

		private List<Verb> groupedVerbs;
		public List<Building_ShipTurret> turrets;
		public bool drawRadius = true;

		public override Color IconDrawColor
		{
			get
			{
				if (verb.EquipmentSource != null)
				{
					return verb.EquipmentSource.DrawColor;
				}
				return base.IconDrawColor;
			}
		}

		public override void GizmoUpdateOnMouseover()
		{
			if (drawRadius)
			{
				verb.verbProps.DrawRadiusRing(verb.caster.Position);
				if (!groupedVerbs.NullOrEmpty())
				{
					foreach (Verb groupedVerb in groupedVerbs)
					{
						groupedVerb.verbProps.DrawRadiusRing(groupedVerb.caster.Position);
					}
				}
			}
		}

		public override void MergeWith(Gizmo other)
		{
			base.MergeWith(other);
			Command_TargetShipCombat command_VerbTargetShip = other as Command_TargetShipCombat;
			if (command_VerbTargetShip == null)
			{
				Log.ErrorOnce("Tried to merge Command_VerbTarget with unexpected type", 73406263);
				return;
			}
			if (groupedVerbs == null)
			{
				groupedVerbs = new List<Verb>();
			}
			groupedVerbs.Add(command_VerbTargetShip.verb);
			if (command_VerbTargetShip.groupedVerbs != null)
			{
				groupedVerbs.AddRange(command_VerbTargetShip.groupedVerbs);
			}
		}

		public override void ProcessInput(Event ev)
		{
			var mapComp = turrets.FirstOrDefault().Map.GetComponent<ShipHeatMapComp>();
			base.ProcessInput(ev);
			SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
			if (mapComp.ShipMapState != ShipMapState.inCombat)
			{
				Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.TurretNotInShipCombat"), null, MessageTypeDefOf.RejectInput, historical: false);
				return;
			}
			CameraJumper.TryJump(mapComp.ShipCombatTargetMap.Center, mapComp.ShipCombatTargetMap);
			Targeter targeter = Find.Targeter;
			TargetingParameters parms = new TargetingParameters();
			parms.canTargetPawns = true;
			parms.canTargetBuildings = true;
			parms.canTargetLocations = true;
			Find.Targeter.BeginTargeting(parms, (Action<LocalTargetInfo>)delegate (LocalTargetInfo x)
			{
				foreach (Building_ShipTurret turret in turrets)
				{
					turret.SetTarget(x);
				}
			}, (Pawn)null, delegate { CameraJumper.TryJump(turrets[0].Position, mapComp.ShipCombatOriginMap); });
		}
	}
}
