using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimWorld
{
	public class ChoiceLetter_SpacePod : ChoiceLetter
	{
		public Map map;
		public override bool CanDismissWithRightClick
		{
			get
			{
				return false;
			}
		}

		public override bool CanShowInLetterStack
		{
			get
			{
				return base.CanShowInLetterStack;
			}
		}

		public override IEnumerable<DiaOption> Choices
		{
			get
			{
				if (base.ArchivedOnly)
				{
					yield return base.Option_Close;
				}
				else
				{
					DiaOption diaOption = new DiaOption("AcceptButton".Translate());
					DiaOption optionReject = new DiaOption("RejectLetter".Translate());
					diaOption.action = delegate ()
					{
						SpawnPod();
						//Find.SignalManager.SendSignal(new Signal(this.signalAccept));
						Find.LetterStack.RemoveLetter(this);
					};
					diaOption.resolveTree = true;
					optionReject.action = delegate ()
					{
						//Find.SignalManager.SendSignal(new Signal(this.signalReject));
						Find.LetterStack.RemoveLetter(this);
					};
					optionReject.resolveTree = true;
					yield return diaOption;
					yield return optionReject;
					if (this.lookTargets.IsValid())
					{
						yield return base.Option_JumpToLocationAndPostpone;
					}
					yield return base.Option_Postpone;
					optionReject = null;
				}
				yield break;
			}
		}
		void SpawnPod() //find salv bay, spawn pod - on land spawn rand contents
		{
			//td merge with Projectile_ExplosiveShipDebris?
			Thing thing = null;
			int i = Rand.RangeInclusive(1, 20);
			if (i < 6) //pawn
			{
				PawnGenerationRequest req;
				if (i == 1)
					req = new PawnGenerationRequest(DefDatabase<PawnKindDef>.GetNamed("Mech_Lancer"), Faction.OfMechanoids);
				else if (i == 2)
					req = new PawnGenerationRequest(DefDatabase<PawnKindDef>.GetNamed("Mech_Scyther"), Faction.OfMechanoids);
				else if (i == 3)
					req = new PawnGenerationRequest(DefDatabase<PawnKindDef>.GetNamed("Stellapede"), Faction.OfInsects);
				else if (i == 4)
					req = new PawnGenerationRequest(DefDatabase<PawnKindDef>.GetNamed("SpaceCrewMarine"), Faction.OfAncientsHostile);
				else
					req = new PawnGenerationRequest(DefDatabase<PawnKindDef>.GetNamed("SpaceCrewEVA"), Faction.OfAncients);
				thing = PawnGenerator.GeneratePawn(req);
			}
			else
			{
				ThingDef thingDef = ThingSetMaker_ResourcePod.RandomPodContentsDef(false);
				ThingDef stuff = GenStuff.RandomStuffByCommonalityFor(thingDef, TechLevel.Undefined);
				thing = ThingMaker.MakeThing(thingDef, stuff);
				thing.stackCount = Math.Min(Rand.RangeInclusive(10, 40), thing.def.stackLimit);
			}
			ActiveDropPodInfo activeDropPodInfo = new ActiveDropPodInfo();
			activeDropPodInfo.innerContainer.TryAdd(thing);
			activeDropPodInfo.leaveSlag = true;
			IntVec3 intVec = DropCellFinder.TradeDropSpot(map);
			DropPodUtility.MakeDropPodAt(intVec, map, activeDropPodInfo, null);
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_References.Look<Map>(ref map, "map");
			Scribe_Values.Look<string>(ref this.signalAccept, "signalAccept", null, false);
			Scribe_Values.Look<string>(ref this.signalReject, "signalReject", null, false);
		}

		public string signalAccept;

		public string signalReject;
	}
}
