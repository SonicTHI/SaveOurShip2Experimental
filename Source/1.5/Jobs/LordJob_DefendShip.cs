using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI.Group;
using RimWorld;

namespace SaveOurShip2
{
	class LordJob_DefendShip : LordJob
	{
		private Faction faction;
		public IntVec3 baseCenter;

		public LordJob_DefendShip()
		{

		}

		public LordJob_DefendShip(Faction faction, IntVec3 baseCenter)
		{
			this.faction = faction;
			this.baseCenter = baseCenter;
		}

		public override StateGraph CreateGraph()
		{
			return new StateGraph
			{
				StartingToil = new LordToil_DefendShip(baseCenter)
			};
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_References.Look(ref faction, "faction");
			Scribe_Values.Look(ref baseCenter, "baseCenter");
		}
	}
}
