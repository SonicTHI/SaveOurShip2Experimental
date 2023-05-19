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
using HarmonyLib;

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
                if (!parent.Spawned || powerComp == null || !powerComp.PowerOn || parent.Faction != Faction.OfPlayer || !parent.Map.IsSpace())
                    return false;
                return scanShips || scanSites || mapComp.InCombat;
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
            if (Find.TickManager.TicksGame % 60 == 0)
            {
                float statValue = worker.GetStatValue(StatDefOf.ResearchSpeed, true);
                if (!mapComp.InCombat)
                {
                    float rate = findRate;
                    if (mapComp.Cloaks.Any(c => c.active))
                        rate /= 4;
                    this.daysWorkingSinceLastMinerals += 60 * statValue / rate;
                    float mtb = this.Props.mtbDays / statValue;
                    if (this.daysWorkingSinceLastMinerals >= this.Props.guaranteedToFindLumpAfterDaysWorking || Rand.MTBEventOccurs(mtb, 40000f, 60f))
                    {
                        this.FoundMinerals(worker);
                    }
                }
                else if (Find.TickManager.TicksGame % 300 == 0)
                {
                    if (Rand.RangeInclusive(0,21) > statValue)
                        ScannedRoom();
                }
            }
        }
        public void ScannedRoom()
        {
            if (mapComp.MasterMapComp.Scanned)
                return;
            List<Room> rooms = mapComp.ShipCombatTargetMap.regionGrid.allRooms.Where(r => !r.TouchesMapEdge && r.ProperRoom && r.Fogged).ToList();
            if (!rooms.NullOrEmpty())
            {
                //Log.Message("scanned room with " + this.parent);
                FloodFillerFog.FloodUnfog(rooms.RandomElement().Cells.FirstOrDefault(), mapComp.ShipCombatTargetMap);
            }
            else
            {
                mapComp.ShipCombatTargetMap.fogGrid.ClearAllFog();
                mapComp.MasterMapComp.Scanned = true;
            }
        }

        protected void FoundMinerals(Pawn worker)
        {
            this.daysWorkingSinceLastMinerals = 0f;

            int chance;
            if (scanSites && !scanShips)
                chance = 1;
            else if (!scanSites && scanShips)
                chance = Rand.RangeInclusive(3, 15);
            else
                chance = Rand.RangeInclusive(1, 15);

            if (chance  < 3) //legacy site
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
            else if (chance > 3 && chance < 7) //tradeship, already has faction, navy resolves in SpawnEnemyShip
            {
                IncidentParms parms = new IncidentParms();
                parms.target = parent.Map;
                parms.forced = true;
                bool tradeShip = Find.Storyteller.TryFire(new FiringIncident(IncidentDefOf.OrbitalTraderArrival, null, parms));
                if (tradeShip)
                {
                    if (worker != null)
                        Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("SoSTraderScan"), TranslatorFormattedStringExtensions.Translate("SoSTraderScanDesc", worker), LetterDefOf.PositiveEvent);
                    else
                        Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("SoSTraderScan"), TranslatorFormattedStringExtensions.Translate("SoSTraderScanDesc", "its AI"), LetterDefOf.PositiveEvent);
                }
            }
            else if (chance == 7) //premade sites, very low chance
            {
                DerelictShip ship = new DerelictShip();
                int rarity = Rand.RangeInclusive(1, 2);
                if (Rand.Chance((float)SaveOurShip2.ModSettings_SoS.navyShipChance))
                {
                    SpaceNavyDef navy = ShipInteriorMod2.ValidRandomNavy(Faction.OfPlayer);
                    if (navy != null)
                    {
                        ship.derelictShip = navy.enemyShipDefs.Where(def => def.spaceSite && def.rarityLevel <= Rand.RangeInclusive(1, 2)).RandomElement();
                        ship.shipFaction = Find.FactionManager.AllFactions.Where(f => navy.factionDefs.Contains(f.def)).RandomElement();
                        ship.spaceNavyDef = navy;
                        if (ship.derelictShip.neverWreck)
                            ship.wreckLevel = 0;
                        else
                            ship.wreckLevel = Rand.RangeInclusive(0, 3);
                    }
                }
                if (ship.derelictShip == null)
                {
                    ship.derelictShip = DefDatabase<EnemyShipDef>.AllDefs.Where(def => def.spaceSite && def.rarityLevel <= rarity).RandomElement();
                    ship.shipFaction = Faction.OfAncientsHostile;
                }
                Log.Message("Created ship with def: " + ship.derelictShip + " fac: " + ship.shipFaction + " navy: " + ship.spaceNavyDef);
                parent.Map.passingShipManager.AddShip(ship);
                if (worker != null)
                    Find.LetterStack.ReceiveLetter("SoSDerelictScan".Translate(), "SoSDerelictScanDesc".Translate(worker, ship.derelictShip), LetterDefOf.PositiveEvent);
                else
                    Find.LetterStack.ReceiveLetter("SoSDerelictScan".Translate(), "SoSDerelictScanDesc".Translate("its AI", ship.derelictShip), LetterDefOf.PositiveEvent);
            }
            else if (chance > 7 && chance < 12) //ship wreck
            {
                DerelictShip ship = new DerelictShip();
                int rarity = Rand.RangeInclusive(1, 2);
                if (chance == 11)
                    ship.wreckLevel = 2;
                else
                    ship.wreckLevel = 3;
                if (Rand.Chance((float)SaveOurShip2.ModSettings_SoS.navyShipChance))
                {
                    SpaceNavyDef navy = ShipInteriorMod2.ValidRandomNavy(Faction.OfPlayer);
                    if (navy != null)
                    {
                        ship.spaceNavyDef = navy;
                        ship.derelictShip = navy.enemyShipDefs.Where(def => !def.neverRandom && !def.spaceSite && !def.neverWreck && def.rarityLevel <= rarity).RandomElement();
                        ship.shipFaction = Find.FactionManager.AllFactions.Where(f => navy.factionDefs.Contains(f.def)).RandomElement();
                    }
                }
                if (ship.derelictShip == null)
                {
                    ship.derelictShip = DefDatabase<EnemyShipDef>.AllDefs.Where(def => !def.neverRandom && !def.spaceSite && !def.neverWreck && def.rarityLevel <= rarity && !def.navyExclusive).RandomElement();
                    ship.shipFaction = Faction.OfAncientsHostile;
                }

                Log.Message("Created ship with def: " + ship.derelictShip + " fac: " + ship.shipFaction + " navy: " + ship.spaceNavyDef);
                parent.Map.passingShipManager.AddShip(ship);
                if (worker != null)
                    Find.LetterStack.ReceiveLetter("SoSDerelictScan".Translate(), "SoSDerelictScanDesc".Translate(worker, ship.derelictShip), LetterDefOf.PositiveEvent);
                else
                    Find.LetterStack.ReceiveLetter("SoSDerelictScan".Translate(), "SoSDerelictScanDesc".Translate("its AI", ship.derelictShip), LetterDefOf.PositiveEvent);
            }
            else //random ship
            {
                AttackableShip ship = new AttackableShip();
                int rarity = Rand.RangeInclusive(1, 2);
                if (Rand.Chance((float)SaveOurShip2.ModSettings_SoS.navyShipChance))
                {
                    SpaceNavyDef navy = ShipInteriorMod2.ValidRandomNavy();
                    if (navy != null)
                    {
                        ship.spaceNavyDef = navy;
                        ship.attackableShip = navy.enemyShipDefs.Where(def => !def.neverRandom && !def.neverAttacks && def.rarityLevel <= rarity).RandomElement();
                        ship.shipFaction = Find.FactionManager.AllFactions.Where(f => navy.factionDefs.Contains(f.def)).RandomElement();
                    }
                }
                if (ship.attackableShip == null)
                {
                    ship.attackableShip = DefDatabase<EnemyShipDef>.AllDefs.Where(def => !def.neverRandom && !def.neverAttacks && !def.navyExclusive && def.rarityLevel <= rarity).RandomElement();
                    ship.shipFaction = Faction.OfAncientsHostile;
                }

                Log.Message("Created ship with def: " + ship.attackableShip + " fac: " + ship.shipFaction + " navy: " + ship.spaceNavyDef);
                parent.Map.passingShipManager.AddShip(ship);
                if (worker != null)
                    Find.LetterStack.ReceiveLetter("SoSEnemyScan".Translate(), "SoSEnemyScanDesc".Translate(worker, ship.attackableShip), LetterDefOf.PositiveEvent);
                else
                    Find.LetterStack.ReceiveLetter("SoSEnemyScan".Translate(), "SoSEnemyScanDesc".Translate("its AI", ship.attackableShip), LetterDefOf.PositiveEvent);
            }
        }

        [DebuggerHidden]
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (this.parent.Map.IsSpace() && this.parent.Faction == Faction.OfPlayer)
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
                            if (!mapComp.InCombat)
                                this.FoundMinerals(PawnsFinder.AllMaps_FreeColonists.FirstOrDefault<Pawn>());
                            else
                                ScannedRoom();
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