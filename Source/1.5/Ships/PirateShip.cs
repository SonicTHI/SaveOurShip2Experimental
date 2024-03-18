using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using SaveOurShip2;

namespace RimWorld
{
	class PirateShip : TradeShip
	{

		public Faction shipFaction;
		public SpaceNavyDef spaceNavyDef;
		public bool parleyed = false;
		public bool paidOff = false;


		public PirateShip()
		{
		}
		public PirateShip(TraderKindDef def, Faction faction = null)
		{
			ticksUntilDeparture = Rand.RangeInclusive(6000, 12000);

			this.def = def;
			things = new ThingOwner<Thing>(this);
			tmpExtantNames.Clear();
			List<Map> maps = Find.Maps;
			for (int i = 0; i < maps.Count; i++)
			{
				tmpExtantNames.AddRange(maps[i].passingShipManager.passingShips.Select((PassingShip x) => x.name));
			}

			/*name = NameGenerator.GenerateName(RulePackDefOf.NamerTraderGeneral, tmpExtantNames);
			if (faction != null)
			{
				name = string.Format("{0} {1} {2}", name, "OfLower".Translate(), faction.Name);
			}*/
			name = "Pirate ship";

			randomPriceFactorSeed = Rand.RangeInclusive(1, 10000000);
			loadID = Find.UniqueIDsManager.GetNextPassingShipID();
		}
		public override void Depart()
		{
			var mapComp = Map.GetComponent<ShipHeatMapComp>();
			int bounty = ShipInteriorMod2.WorldComp.PlayerFactionBounty;
			int roll = Rand.RangeInclusive(1, 10);
			if (bounty > 50) //player is pirate
			{
				if ((!parleyed && roll < 5) || (parleyed && roll < 2)) //betrayal
				{
					Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("ShipPirateAttacksBetray"), TranslatorFormattedStringExtensions.Translate("ShipPirateAttacksBetrayDesc"), LetterDefOf.ThreatBig);
					mapComp.StartShipEncounter(this);
					return;
				}
			}
			else //not a pirate
			{
				if (paidOff || parleyed)
				{
					if (roll < 3) //pirates want more
					{
						Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("ShipPirateAttacksPay"), TranslatorFormattedStringExtensions.Translate("ShipPirateAttacksPayDesc"), LetterDefOf.ThreatBig);
						mapComp.StartShipEncounter(this);
						return;
					}
				}
				else //didnt pay/parley
				{
					if (roll < 10)
					{
						Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("ShipPirateAttacksWait"), TranslatorFormattedStringExtensions.Translate("ShipPirateAttacksWaitDesc"), LetterDefOf.ThreatBig);
						mapComp.StartShipEncounter(this);
						return;
					}
				}
			}
			base.Depart();
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look<bool>(ref parleyed, "parleyed");
			Scribe_Values.Look<bool>(ref paidOff, "paidOff");
			Scribe_Defs.Look(ref spaceNavyDef, "spaceNavyDef");
			/*Scribe_References.Look<Faction>(ref shipFaction, "shipFaction", false);
			Scribe_Defs.Look(ref def, "def");
			Scribe_Deep.Look(ref things, "things", this);
			Scribe_Collections.Look(ref soldPrisoners, "soldPrisoners", LookMode.Reference);
			Scribe_Values.Look(ref randomPriceFactorSeed, "randomPriceFactorSeed", 0);
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				soldPrisoners.RemoveAll((Pawn x) => x == null);
			}*/
		}
		public override string FullTitle => name;

		/*
		public TraderKindDef def;

		public ThingOwner things;

		public List<Pawn> soldPrisoners = new List<Pawn>();

		public int randomPriceFactorSeed = -1;

		public static List<string> tmpExtantNames = new List<string>();


		public int Silver => CountHeldOf(ThingDefOf.Silver);

		public TradeCurrency TradeCurrency => def.tradeCurrency;

		public IThingHolder ParentHolder => base.Map;

		public TraderKindDef TraderKind => def;

		public int RandomPriceFactorSeed => randomPriceFactorSeed;

		public string TraderName => name;

		public bool CanTradeNow => !base.Departed;

		public float TradePriceImprovementOffsetForPlayer => 0f;

		public IEnumerable<Thing> Goods
		{
			get
			{
				for (int i = 0; i < things.Count; i++)
				{
					Pawn pawn = things[i] as Pawn;
					if (pawn == null || !soldPrisoners.Contains(pawn))
					{
						yield return things[i];
					}
				}
			}
		}
		public IEnumerable<Thing> ColonyThingsWillingToBuy(Pawn playerNegotiator)
		{
			foreach (Thing item in TradeUtility.AllLaunchableThingsForTrade(base.Map, this))
			{
				yield return item;
			}

			foreach (Pawn item2 in TradeUtility.AllSellableColonyPawns(base.Map, checkAcceptableTemperatureOfAnimals: false))
			{
				yield return item2;
			}
		}

		public void GenerateThings()
		{
			ThingSetMakerParams parms = default(ThingSetMakerParams);
			parms.traderDef = def;
			parms.tile = base.Map.Tile;
			things.TryAddRangeOrTransfer(ThingSetMakerDefOf.TraderStock.root.Generate(parms));
		}

		public override void PassingShipTick()
		{
			base.PassingShipTick();
			for (int num = things.Count - 1; num >= 0; num--)
			{
				Pawn pawn = things[num] as Pawn;
				if (pawn != null)
				{
					pawn.Tick();
					if (pawn.Dead)
					{
						things.Remove(pawn);
					}
				}
			}
		}

		public override void TryOpenComms(Pawn negotiator)
		{
			if (CanTradeNow)
			{
				Find.WindowStack.Add(new Dialog_Trade(negotiator, this));
				LessonAutoActivator.TeachOpportunity(ConceptDefOf.BuildOrbitalTradeBeacon, OpportunityType.Critical);
				PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter_Send(Goods.OfType<Pawn>(), "LetterRelatedPawnsTradeShip".Translate(Faction.OfPlayer.def.pawnsPlural), LetterDefOf.NeutralEvent);
				TutorUtility.DoModalDialogIfNotKnown(ConceptDefOf.TradeGoodsMustBeNearBeacon);
			}
		}

		public override void Depart()
		{
			base.Depart();
			things.ClearAndDestroyContentsOrPassToWorld();
			soldPrisoners.Clear();
		}

		public override string GetCallLabel()
		{
			return name + " (" + def.label + ")";
		}

		protected override AcceptanceReport CanCommunicateWith(Pawn negotiator)
		{
			AcceptanceReport result = base.CanCommunicateWith(negotiator);
			if (!result.Accepted)
			{
				return result;
			}

			return negotiator.CanTradeWith(base.Faction, TraderKind).Accepted;
		}

		public int CountHeldOf(ThingDef thingDef, ThingDef stuffDef = null)
		{
			return HeldThingMatching(thingDef, stuffDef)?.stackCount ?? 0;
		}

		public void GiveSoldThingToTrader(Thing toGive, int countToGive, Pawn playerNegotiator)
		{
			Thing thing = toGive.SplitOff(countToGive);
			thing.PreTraded(TradeAction.PlayerSells, playerNegotiator, this);
			Thing thing2 = TradeUtility.ThingFromStockToMergeWith(this, thing);
			if (thing2 != null)
			{
				if (!thing2.TryAbsorbStack(thing, respectStackLimit: false))
				{
					thing.Destroy();
				}

				return;
			}

			Pawn pawn = thing as Pawn;
			if (pawn != null && pawn.RaceProps.Humanlike)
			{
				soldPrisoners.Add(pawn);
			}

			things.TryAdd(thing, canMergeWithExistingStacks: false);
		}

		public void GiveSoldThingToPlayer(Thing toGive, int countToGive, Pawn playerNegotiator)
		{
			Thing thing = toGive.SplitOff(countToGive);
			thing.PreTraded(TradeAction.PlayerBuys, playerNegotiator, this);
			Pawn pawn = thing as Pawn;
			if (pawn != null)
			{
				soldPrisoners.Remove(pawn);
			}

			TradeUtility.SpawnDropPod(DropCellFinder.TradeDropSpot(base.Map), base.Map, thing);
		}

		public Thing HeldThingMatching(ThingDef thingDef, ThingDef stuffDef)
		{
			for (int i = 0; i < things.Count; i++)
			{
				if (things[i].def == thingDef && things[i].Stuff == stuffDef)
				{
					return things[i];
				}
			}

			return null;
		}

		public void ChangeCountHeldOf(ThingDef thingDef, ThingDef stuffDef, int count)
		{
			Thing thing = HeldThingMatching(thingDef, stuffDef);
			if (thing == null)
			{
				Log.Error("Changing count of thing trader doesn't have: " + thingDef);
			}

			thing.stackCount += count;
		}

		public override string ToString()
		{
			return FullTitle;
		}

		public ThingOwner GetDirectlyHeldThings()
		{
			return things;
		}

		public void GetChildHolders(List<IThingHolder> outChildren)
		{
			ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
		}*/
	}
}
