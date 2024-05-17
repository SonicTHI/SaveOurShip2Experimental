using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;
using RimWorld;

namespace SaveOurShip2
{
	/*public class Building_ShipForceField : Building
	{
		public CompPowerTrader powerComp;
		public bool active = false;
		public bool activate = false;
		public override bool BlocksPawn(Pawn p)
		{
			if (active && powerComp != null && powerComp.PowerOn)
				return false;
			else
				return true;
		}
		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			powerComp = GetComp<CompPowerTrader>();
		}
		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			base.DrawAt(drawLoc, flip);
			//td draw powered forcefield wall
			if (active)
			{

			}
		}
		public override void Tick()
		{
			base.Tick();
			if (Find.TickManager.TicksGame % 60 == 0)
			{
				if (powerComp.PowerOn && activate)
				{
					powerComp.PowerOutput = powerComp.Props.basePowerConsumption * 10;
					active = true;
				}
				else
				{
					powerComp.PowerOutput = powerComp.Props.basePowerConsumption;
					active = false;
				}
			}
		}
		public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (Gizmo c in base.GetGizmos())
			{
				yield return c;
			}
			Command_Toggle toggleField = new Command_Toggle
			{
				toggleAction = delegate
				{
					//find all attached fields and set them to !this
					ToggleAdjacent(!activate);
				},
				defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.ToggleField"),
				defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.ToggleFieldDesc"),
				icon = ContentFinder<Texture2D>.Get("UI/Commands/DesirePower"),
				isActive = () => activate
			};
			yield return toggleField;
		}
		void ToggleAdjacent(bool state)
		{
			activate = state;
			foreach (IntVec3 v in GenAdj.CellsAdjacentCardinal(this))
			{
				foreach (Thing t in v.GetThingList(Map))
				{
					if (t is Building_ShipForceField f && f.activate != state)
					{
						f.ToggleAdjacent(state);
					}
				}
			}
		}
	}*/
}
