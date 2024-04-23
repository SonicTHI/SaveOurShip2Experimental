using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	class DerelictShip : PassingShip
	{
		public SpaceShipDef derelictShip;
		public Faction shipFaction;
		public SpaceNavyDef spaceNavyDef;
		public int wreckLevel;
		//wrecklevel
		//1 (light damage - starting ships): outer explo few
		//2: outer explo more, destroy some buildings, some dead crew, chance for more invaders
		//3: wreck all hull, outer explo lots, chance to split, destroy most buildings, most crew dead, chance for invaders
		//4: planetside wreck - no invaders

		public DerelictShip() : base()
		{
			loadID = Find.UniqueIDsManager.GetNextPassingShipID();
			ticksUntilDeparture = Rand.RangeInclusive(40000, 80000);
		}

		protected override AcceptanceReport CanCommunicateWith(Pawn negotiator)
		{
			return "There is no response";
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Defs.Look(ref derelictShip, "EnemyShip");
			Scribe_Defs.Look(ref spaceNavyDef, "spaceNavyDef");
			Scribe_Values.Look<int>(ref wreckLevel, "wreckLevel");
			Scribe_References.Look<Faction>(ref shipFaction, "shipFaction", false);
		}

		public override string FullTitle
		{
			get
			{
				if (derelictShip != null)
					return (loadID + ": " + derelictShip.label);
				return "Glitched ship";
			}
		}

		public override string GetCallLabel()
		{
			return derelictShip != null ? derelictShip.label : "Glitched ship";
		}
	}
}
