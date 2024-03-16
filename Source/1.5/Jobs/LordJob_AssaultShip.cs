using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RimWorld
{
	// Token: 0x02000887 RID: 2183
	public class LordJob_AssaultShip : LordJob
	{
		private Faction assaulterFaction;
		private bool canKidnap = true;
		private bool canTimeoutOrFlee = true;
		private bool sappers;
		private bool useAvoidGridSmart;
		private bool canSteal = true;
		private bool breachers;
		private bool canPickUpOpportunisticWeapons;
		private static readonly IntRange AssaultTimeBeforeGiveUp = new IntRange(26000, 38000);
		private static readonly IntRange SapTimeBeforeGiveUp = new IntRange(33000, 38000);
		private static readonly IntRange BreachTimeBeforeGiveUp = new IntRange(33000, 38000);

		public override bool GuiltyOnDowned
		{
			get
			{
				return true;
			}
		}
		public LordJob_AssaultShip()
		{
		}
		public LordJob_AssaultShip(SpawnedPawnParams parms)
		{
			this.assaulterFaction = parms.spawnerThing.Faction;
			this.canKidnap = false;
			this.canTimeoutOrFlee = false;
			this.canSteal = false;
		}
		public LordJob_AssaultShip(Faction assaulterFaction, bool canKidnap = true, bool canTimeoutOrFlee = true, bool sappers = false, bool useAvoidGridSmart = false, bool canSteal = true, bool breachers = false, bool canPickUpOpportunisticWeapons = false)
		{
			this.assaulterFaction = assaulterFaction;
			this.canKidnap = canKidnap;
			this.canTimeoutOrFlee = canTimeoutOrFlee;
			this.sappers = sappers;
			this.useAvoidGridSmart = useAvoidGridSmart;
			this.canSteal = canSteal;
			this.breachers = breachers;
			this.canPickUpOpportunisticWeapons = canPickUpOpportunisticWeapons;
		}
		public override StateGraph CreateGraph()
		{
			StateGraph stateGraph = new StateGraph();
			List<LordToil> list = new List<LordToil>();
			LordToil lordToil = null;
			if (this.sappers)
			{
				lordToil = new LordToil_AssaultColonySappers();
				if (this.useAvoidGridSmart)
				{
					lordToil.useAvoidGrid = true;
				}
				stateGraph.AddToil(lordToil);
				list.Add(lordToil);
				Transition transition = new Transition(lordToil, lordToil, true, true);
				transition.AddTrigger(new Trigger_PawnLost(PawnLostCondition.Undefined, null));
				stateGraph.AddTransition(transition, false);
			}
			if (this.breachers)
			{
				LordToil lordToil2 = new LordToil_AssaultColonyBreaching();
				if (this.useAvoidGridSmart)
				{
					lordToil2.useAvoidGrid = this.useAvoidGridSmart;
				}
				stateGraph.AddToil(lordToil2);
				list.Add(lordToil2);
			}
			LordToil lordToil3 = new LordToil_AssaultShip(false, this.canPickUpOpportunisticWeapons);
			if (this.useAvoidGridSmart)
			{
				lordToil3.useAvoidGrid = true;
			}
			stateGraph.AddToil(lordToil3);
			/*LordToil_ExitMap lordToil_ExitMap = new LordToil_ExitMap(LocomotionUrgency.Jog, false, true);
			lordToil_ExitMap.useAvoidGrid = true;
			stateGraph.AddToil(lordToil_ExitMap);
			if (this.sappers)
			{
				Transition transition2 = new Transition(lordToil, lordToil3, false, true);
				transition2.AddTrigger(new Trigger_NoFightingSappers());
				stateGraph.AddTransition(transition2, false);
			}
			if (this.assaulterFaction != null && this.assaulterFaction.def.humanlikeFaction)
			{
				if (this.canTimeoutOrFlee)
				{
					Transition transition3 = new Transition(lordToil3, lordToil_ExitMap, false, true);
					transition3.AddSources(list);
					IntRange intRange;
					if (this.sappers)
					{
						intRange = LordJob_AssaultShip.SapTimeBeforeGiveUp;
					}
					else if (this.breachers)
					{
						intRange = LordJob_AssaultShip.BreachTimeBeforeGiveUp;
					}
					else
					{
						intRange = LordJob_AssaultShip.AssaultTimeBeforeGiveUp;
					}
					transition3.AddTrigger(new Trigger_TicksPassed(intRange.RandomInRange));
					transition3.AddPreAction(new TransitionAction_Message("MessageRaidersGivenUpLeaving".Translate(this.assaulterFaction.def.pawnsPlural.CapitalizeFirst(), this.assaulterFaction.Name), null, 1f));
					stateGraph.AddTransition(transition3, false);
					Transition transition4 = new Transition(lordToil3, lordToil_ExitMap, false, true);
					transition4.AddSources(list);
					FloatRange floatRange = new FloatRange(0.25f, 0.35f);
					float randomInRange = floatRange.RandomInRange;
					transition4.AddTrigger(new Trigger_FractionColonyDamageTaken(randomInRange, 900f));
					transition4.AddPreAction(new TransitionAction_Message("MessageRaidersSatisfiedLeaving".Translate(this.assaulterFaction.def.pawnsPlural.CapitalizeFirst(), this.assaulterFaction.Name), null, 1f));
					stateGraph.AddTransition(transition4, false);
				}
				if (this.canKidnap)
				{
					LordToil startingToil = stateGraph.AttachSubgraph(new LordJob_Kidnap().CreateGraph()).StartingToil;
					Transition transition5 = new Transition(lordToil3, startingToil, false, true);
					transition5.AddSources(list);
					transition5.AddPreAction(new TransitionAction_Message("MessageRaidersKidnapping".Translate(this.assaulterFaction.def.pawnsPlural.CapitalizeFirst(), this.assaulterFaction.Name), null, 1f));
					transition5.AddTrigger(new Trigger_KidnapVictimPresent());
					stateGraph.AddTransition(transition5, false);
				}
				if (this.canSteal)
				{
					LordToil startingToil2 = stateGraph.AttachSubgraph(new LordJob_Steal().CreateGraph()).StartingToil;
					Transition transition6 = new Transition(lordToil3, startingToil2, false, true);
					transition6.AddSources(list);
					transition6.AddPreAction(new TransitionAction_Message("MessageRaidersStealing".Translate(this.assaulterFaction.def.pawnsPlural.CapitalizeFirst(), this.assaulterFaction.Name), null, 1f));
					transition6.AddTrigger(new Trigger_HighValueThingsAround());
					stateGraph.AddTransition(transition6, false);
				}
			}
			if (this.assaulterFaction != null)
			{
				Transition transition7 = new Transition(lordToil3, lordToil_ExitMap, false, true);
				transition7.AddSources(list);
				transition7.AddTrigger(new Trigger_BecameNonHostileToPlayer());
				transition7.AddPreAction(new TransitionAction_Message("MessageRaidersLeaving".Translate(this.assaulterFaction.def.pawnsPlural.CapitalizeFirst(), this.assaulterFaction.Name), null, 1f));
				stateGraph.AddTransition(transition7, false);
			}*/
			return stateGraph;
		}
		public override void ExposeData()
		{
			Scribe_References.Look<Faction>(ref this.assaulterFaction, "assaulterFaction", false);
			Scribe_Values.Look<bool>(ref this.canKidnap, "canKidnap", true, false);
			Scribe_Values.Look<bool>(ref this.canTimeoutOrFlee, "canTimeoutOrFlee", true, false);
			Scribe_Values.Look<bool>(ref this.sappers, "sappers", false, false);
			Scribe_Values.Look<bool>(ref this.useAvoidGridSmart, "useAvoidGridSmart", false, false);
			Scribe_Values.Look<bool>(ref this.canSteal, "canSteal", true, false);
			Scribe_Values.Look<bool>(ref this.breachers, "breaching", false, false);
			Scribe_Values.Look<bool>(ref this.canPickUpOpportunisticWeapons, "canPickUpOpportunisticWeapons", false, false);
		}
	}
}
