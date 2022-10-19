using RimWorld.Planet;
using RimWorld.QuestGen;
using RimworldMod;
using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimworldMod.VacuumIsNotFun;

namespace RimWorld
{
    public class CompLongRangeMineralScannerSpace : ThingComp
    {
        public bool scanShips = true;
        public bool scanSites = true;
        public float findRate = 60000f;
        protected float daysWorkingSinceLastMinerals;
        public ShipHeatMapComp mapComp;
        public CompPowerTrader powerComp;

        public CompProperties_LongRangeMineralScannerSpace Props
        {
            get
            {
                return (CompProperties_LongRangeMineralScannerSpace)this.props;
            }
        }

        public bool CanUseNow
        {
            get
            {
                return this.parent.Spawned && (this.powerComp == null || this.powerComp.PowerOn) && this.parent.Faction == Faction.OfPlayer && this.parent.Map.IsSpace() && (scanShips || scanSites);
            }
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look<float>(ref this.daysWorkingSinceLastMinerals, "daysWorkingSinceLastMinerals", 0f, false);
            Scribe_Values.Look<bool>(ref this.scanShips, "scanShips", true);
            Scribe_Values.Look<bool>(ref this.scanSites, "scanSites", true);			
        }
        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            this.powerComp = this.parent.GetComp<CompPowerTrader>();
            this.mapComp = this.parent.Map.GetComponent<ShipHeatMapComp>();
        }

        public void Used(Pawn worker)
        {
            if (!this.CanUseNow)
            {
                Log.Error("Used while CanUseNow is false.");
            }
            float statValue = worker.GetStatValue(StatDefOf.ResearchSpeed, true);
            float rate = findRate;
            if (mapComp.Cloaks.Any(c => c.active))
                rate *= 5;
            this.daysWorkingSinceLastMinerals += statValue / rate;
            if (Find.TickManager.TicksGame % 59 == 0)
            {
                float mtb = this.Props.mtbDays / statValue;
                if (this.daysWorkingSinceLastMinerals >= this.Props.guaranteedToFindLumpAfterDaysWorking || Rand.MTBEventOccurs(mtb, 40000f, 59f))
                {
                    this.FoundMinerals(worker);
                }
            }
        }

        protected void FoundMinerals(Pawn worker)
        {
            this.daysWorkingSinceLastMinerals = 0f;
            bool foundSite = Rand.Bool;

            if ((foundSite && scanSites && scanShips) || (scanSites && !scanShips))
            {
                Slate slate = new Slate();
                slate.Set<Map>("map", this.parent.Map, false);
                slate.Set<Pawn>("worker", worker, false);
                int fuelCost = Rand.RangeInclusive((int)Props.minShuttleFuelPercent, (int)Props.maxShuttleFuelPercent);
                slate.Set<int>("fuelCost", fuelCost, false);
                slate.Set<float>("radius", Rand.Range(120f, 180f), false);
                slate.Set<float>("theta", Rand.Range(((WorldObjectOrbitingShip)this.parent.Map.Parent).theta - 0.25f, ((WorldObjectOrbitingShip)this.parent.Map.Parent).theta + 0.25f), false);
                slate.Set<float>("phi", Rand.Range(-1f, 1f), false);
                for (int i = 0; i < Find.World.grid.TilesCount; i++)
                {
                    if (!Find.World.worldObjects.AnyWorldObjectAt(i))
                    {
                        slate.Set<int>("siteTile", i, false);
                        break;
                    }
                }
                Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(DefDatabase<QuestScriptDef>.GetNamed("SpaceSiteQuest"), slate);
                Find.LetterStack.ReceiveLetter(quest.name, quest.description, LetterDefOf.PositiveEvent, null, null, quest, null, null);
            }
            else if (scanShips)
            {
                int chance = Rand.RangeInclusive(1,10);
                if (chance <= 2)//tradeship
                {
                    IncidentParms parms = new IncidentParms();
                    parms.target = parent.Map;
                    parms.forced = true;
                    bool tradeShip=Find.Storyteller.TryFire(new FiringIncident(IncidentDefOf.OrbitalTraderArrival, null, parms));
                    if(tradeShip)
                    {
                        if (worker != null)
                            Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("SoSTraderScan"), TranslatorFormattedStringExtensions.Translate("SoSTraderScanDesc",worker), LetterDefOf.PositiveEvent);
                        else
                            Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("SoSTraderScan"), TranslatorFormattedStringExtensions.Translate("SoSTraderScanDesc","its AI"), LetterDefOf.PositiveEvent);
                    }
                }
                else if (chance <5)//derelict
                {
                    DerelictShip ship = new DerelictShip();
                    ship.derelictShip = DefDatabase<EnemyShipDef>.AllDefs.Where(def => def.spaceSite).RandomElement();
                    parent.Map.passingShipManager.AddShip(ship);
                    if (worker != null)
                        Find.LetterStack.ReceiveLetter("SoSDerelictScan".Translate(), "SoSDerelictScanDesc".Translate(worker, ship.derelictShip), LetterDefOf.PositiveEvent);
                    else
                        Find.LetterStack.ReceiveLetter("SoSDerelictScan".Translate(), "SoSDerelictScanDesc".Translate("its AI", ship.derelictShip), LetterDefOf.PositiveEvent);
                }
                else//randomship
                {
                    AttackableShip ship = new AttackableShip();
                    ship.enemyShip = DefDatabase<EnemyShipDef>.AllDefs.Where(def => !def.neverRandom && !def.tradeShip && !def.spaceSite).RandomElement();
                    parent.Map.passingShipManager.AddShip(ship);
                    if (worker != null)
                        Find.LetterStack.ReceiveLetter("SoSEnemyScan".Translate(), "SoSEnemyScanDesc".Translate(worker, ship.enemyShip), LetterDefOf.PositiveEvent);
                    else
                        Find.LetterStack.ReceiveLetter("SoSEnemyScan".Translate(), "SoSEnemyScanDesc".Translate("its AI", ship.enemyShip), LetterDefOf.PositiveEvent);
                }
            }
        }

        [DebuggerHidden]
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (this.parent.Map.IsSpace())
            {
                Command_Toggle scanSitesCommand = new Command_Toggle
                {
                    toggleAction = delegate
                    {
                        scanSites = !scanSites;
                    },
                    defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipInsideToggleScanSites"),
                    defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipInsideToggleScanSitesDesc"),
                    icon = ContentFinder<Texture2D>.Get("UI/shipChunk", true),
                    isActive = () => scanSites
                };
                yield return scanSitesCommand;
                Command_Toggle scanShipsCommand = new Command_Toggle
                {
                    toggleAction = delegate
                    {
                        scanShips = !scanShips;
                    },
                    defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipInsideToggleScanShips"),
                    defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipInsideToggleScanShipsDesc"),
                    icon = ContentFinder<Texture2D>.Get("UI/Map_Icon_Enemy", true),
                    isActive = () => scanShips
                };
                yield return scanShipsCommand;
                if (Prefs.DevMode)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "Dev: Find site now",
                        action = delegate
                        {
                            this.FoundMinerals(PawnsFinder.AllMaps_FreeColonists.FirstOrDefault<Pawn>());
                        }
                    };
                }
            }
        }

        public override string CompInspectStringExtra()
        {
            string t = "";
            /*if (lastScanTick > (float)(Find.TickManager.TicksGame - 30))
            {
                t += TranslatorFormattedStringExtensions.Translate("UserScanAbility") + ": " + lastUserSpeed.ToStringPercent() + "\n" + TranslatorFormattedStringExtensions.Translate("ScanAverageInterval") + ": " + TranslatorFormattedStringExtensions.Translate("PeriodDays",(Props.scanFindMtbDays / lastUserSpeed).ToString("F1")) + "\n";
            }*/
            return t + TranslatorFormattedStringExtensions.Translate("ScanningProgressToGuaranteedFind") + ": " + (daysWorkingSinceLastMinerals / Props.guaranteedToFindLumpAfterDaysWorking).ToStringPercent();
        }
    }
}