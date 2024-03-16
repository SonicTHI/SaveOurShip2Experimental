using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using SaveOurShip2;

namespace RimWorld
{
    class LordToil_AssaultShip : LordToil
	{
		private bool attackDownedIfStarving;
		private bool canPickUpOpportunisticWeapons;
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
				this.lord.ownedPawns[i].mindState.duty = new PawnDuty(ResourceBank.DutyDefOf.SoSAssaultShip);
				this.lord.ownedPawns[i].mindState.duty.attackDownedIfStarving = this.attackDownedIfStarving;
				this.lord.ownedPawns[i].mindState.duty.pickupOpportunisticWeapon = this.canPickUpOpportunisticWeapons;
			}
		}
	}
}
