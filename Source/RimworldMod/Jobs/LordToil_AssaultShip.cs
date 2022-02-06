using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RimWorld
{
    [StaticConstructorOnStartup]
    class LordToil_AssaultShip : LordToil
	{
		private bool attackDownedIfStarving;
		private bool canPickUpOpportunisticWeapons;
		static DutyDef assaultShip = DefDatabase<DutyDef>.GetNamed("SoSAssaultShip");
		public override bool ForceHighStoryDanger
		{
			get
			{
				return true;
			}
		}
		public LordToil_AssaultShip(bool attackDownedIfStarving = false, bool canPickUpOpportunisticWeapons = false)
		{
			this.attackDownedIfStarving = attackDownedIfStarving;
			this.canPickUpOpportunisticWeapons = canPickUpOpportunisticWeapons;
		}
		public override bool AllowSatisfyLongNeeds
		{
			get
			{
				return false;
			}
		}
		public override void Init()
		{
			base.Init();
			LessonAutoActivator.TeachOpportunity(ConceptDefOf.Drafting, OpportunityType.Critical);
		}
		public override void UpdateAllDuties()
		{
			for (int i = 0; i < this.lord.ownedPawns.Count; i++)
			{
				this.lord.ownedPawns[i].mindState.duty = new PawnDuty(assaultShip);
				this.lord.ownedPawns[i].mindState.duty.attackDownedIfStarving = this.attackDownedIfStarving;
				this.lord.ownedPawns[i].mindState.duty.pickupOpportunisticWeapon = this.canPickUpOpportunisticWeapons;
			}
		}
	}
}
