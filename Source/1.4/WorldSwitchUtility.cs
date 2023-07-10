using System;
using Verse;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using Verse.AI.Group;
using Verse.Sound;
using System.IO;
using System.Text;

namespace SaveOurShip2
{
    public static class WorldSwitchUtility
    {
        public static bool SaveShipFlag = false;
        public static bool LoadShipFlag = false; //NOTE: This is set to true in ScenPart_LoadShip.PostWorldGenerate and false in the patch to MapGenerator.GenerateMap
        public static bool StartShipFlag = false; //as above but for ScenPart_StartInSpace
        public static PastWorldUWO2 PastWorldTracker
        {
            get
            {
                if (PastWorldTrackerInternal == null && Find.World != null)
                    PastWorldTrackerInternal = (PastWorldUWO2)Find.World.components.First(x => x is PastWorldUWO2);
                return PastWorldTrackerInternal;
            }
        }
        public static void PurgePWT()
        {
            PastWorldTrackerInternal = null;
        }


        //recheck if still in use bellow
        public static bool FactionRelationFlag = false;
        public static bool LoadWorldFlag = false;
        public static Faction SavedPlayerFaction = null;
        public static Map SavedMap = null;
        public static UniqueIDsManager Uniques = null;
        public static WorldPawns Pawns = null;
        public static FactionManager Factions = null;
        public static IdeoManager Ideos = null;
        private static PastWorldUWO2 PastWorldTrackerInternal = null;
        public static World SoonToBeObsoleteWorld = null;
        public static bool planetkiller = false;
        public static bool NoRecache = false;
        public static List<ScenPart> CachedScenario;


        static Faction DonatedToFaction = null;
        static float DonatedAmount = 0;
        static List<WorldComponent> tmpComponents = new List<WorldComponent>();


        public static void ColonyAbandonWarning(Action action)
        {
            DiaNode theNode;
            if (Find.Scenario.AllParts.Any(p => p.def.defName.Equals("GameCondition_Planetkiller")))
            {
                theNode = new DiaNode(TranslatorFormattedStringExtensions.Translate("ShipAbandonColoniesWarningPK"));
                planetkiller = true;
            }
            else
                theNode = new DiaNode(TranslatorFormattedStringExtensions.Translate("ShipAbandonColoniesWarning"));

            DiaOption accept = new DiaOption("Accept");
            accept.resolveTree = true;
            List<Faction> alliedFactions = new List<Faction>();
            foreach (Faction f in Find.FactionManager.AllFactionsVisible)
            {
                if (f != Faction.OfPlayer && f.PlayerRelationKind == FactionRelationKind.Ally)
                    alliedFactions.Add(f);
            }
            if (alliedFactions.Any() && !planetkiller)
                accept.action = delegate { ChooseAllyToTakeColony(action, alliedFactions); };
            else
                accept.action = delegate { AbandonColony(action); };
            theNode.options.Add(accept);

            DiaOption cancel = new DiaOption("Cancel");
            cancel.resolveTree = true;
            theNode.options.Add(cancel);

            Dialog_NodeTree dialog_NodeTree = new Dialog_NodeTree(theNode, true, false, null);
            dialog_NodeTree.silenceAmbientSound = false;
            dialog_NodeTree.closeOnCancel = true;
            Find.WindowStack.Add(dialog_NodeTree);
        }

        private static void ChooseAllyToTakeColony(Action action, List<Faction> alliedFactions)
        {
            DiaNode theNode = new DiaNode(TranslatorFormattedStringExtensions.Translate("ChooseAllyToTakeColony"));

            foreach (Faction f in alliedFactions)
            {
                DiaOption option = new DiaOption(f.Name);
                option.resolveTree = true;
                option.action = delegate { AllyTakeColony(f); action.Invoke(); };
                theNode.options.Add(option);
            }

            DiaOption none = new DiaOption("None - leave the colonists to their fate");
            none.resolveTree = true;
            none.action = delegate { AbandonColony(action); };
            theNode.options.Add(none);

            DiaOption cancel = new DiaOption("Cancel");
            cancel.resolveTree = true;
            theNode.options.Add(cancel);

            Dialog_NodeTree dialog_NodeTree = new Dialog_NodeTree(theNode, true, false, null);
            dialog_NodeTree.silenceAmbientSound = false;
            dialog_NodeTree.closeOnCancel = true;
            Find.WindowStack.Add(dialog_NodeTree);
        }

        private static void AllyTakeColony(Faction f)
        {
            List<Map> colonies = new List<Map>();
            foreach (Map m in Find.Maps)
            {
                if (m.IsPlayerHome && !(m.Parent is WorldObjectOrbitingShip))
                {
                    colonies.Add(m);
                }
            }
            foreach (Map m in colonies)
            {
                m.wealthWatcher.ForceRecount();
                DonatedToFaction = f;
                DonatedAmount = m.wealthWatcher.WealthTotal;
                List<Pawn> pawnsToDonate = new List<Pawn>();
                foreach (Pawn p in m.mapPawns.FreeColonistsAndPrisonersSpawned)
                {
                    pawnsToDonate.Add(p);
                }
                foreach (Pawn p in pawnsToDonate)
                {
                    p.SetFaction(f);
                    p.DeSpawn();
                    Find.WorldPawns.PassToWorld(p);
                }
                int tile = m.Parent.Tile;
                Find.WorldObjects.Remove(m.Parent);
                WorldObject newSettlement = WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
                newSettlement.SetFaction(f);
                newSettlement.Tile = tile;
                if (m.Parent is Settlement)
                    ((Settlement)newSettlement).Name = ((Settlement)m.Parent).Name;
                Find.WorldObjects.Add(newSettlement);
            }

        }

        private static void AbandonColony(Action action)
        {
            List<Map> colonies = new List<Map>();
            foreach (Map m in Find.Maps)
            {
                if (m.IsPlayerHome && !(m.Parent is WorldObjectOrbitingShip))
                {
                    colonies.Add(m);
                }
            }
            foreach (Map m in colonies)
            {
                int tile = m.Parent.Tile;
                Find.WorldObjects.Remove(m.Parent);
                WorldObject newSettlement = WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.AbandonedSettlement);
                newSettlement.SetFaction(Faction.OfPlayer);
                newSettlement.Tile = tile;
                Find.WorldObjects.Add(newSettlement);
            }
            action.Invoke();
        }

        private static void KillAllColonistsNotInCrypto(Map shipMap, Building_ShipBridge bridge)
        {
            List<Pawn> toKill = new List<Pawn>();
            foreach (Pawn p in shipMap.mapPawns.AllPawns)
            {
                if (p.RaceProps != null && p.RaceProps.IsFlesh && (!p.InContainerEnclosed) && (!ShipInteriorMod2.IsHologram(p) || p.health.hediffSet.GetFirstHediff<HediffPawnIsHologram>().consciousnessSource.Map != shipMap))
                    toKill.Add(p);
            }
            foreach (Pawn p in toKill)
                p.Kill(null);
            foreach (Thing t in shipMap.spawnedThings)
            {
                if (t is Corpse c)
                {
                    var compRot = c.GetComp<CompRottable>();
                    if (t.GetRoom() != null && t.GetRoom().Temperature > 0 && compRot != null)
                        compRot.RotProgress = compRot.PropsRot.TicksToDessicated;
                }
            }

            Find.World.GetComponent<PastWorldUWO2>().renderedThatAlready = false;

            float EnergyCost = 100000;
            foreach (CompPowerBattery capacitor in bridge.PowerComp.PowerNet.batteryComps)
            {
                if (capacitor.StoredEnergy <= EnergyCost)
                {
                    capacitor.SetStoredEnergyPct(capacitor.StoredEnergyPct - (EnergyCost / capacitor.Props.storedEnergyMax));
                    EnergyCost = 0;
                    break;
                }
                else
                {
                    EnergyCost -= capacitor.StoredEnergy;
                    capacitor.SetStoredEnergyPct(0);
                }
            }

            //Oddly enough, interstellar flight takes a lot of time
            /*int years = Rand.RangeInclusive(ShipInteriorMod2.minTravelTime.Value, ShipInteriorMod2.maxTravelTime.Value);
            Current.Game.tickManager.DebugSetTicksGame(Current.Game.tickManager.TicksAbs + 3600000 * years);
            Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("SoSTimePassedLabel"), TranslatorFormattedStringExtensions.Translate("SoSTimePassed",years), LetterDefOf.NeutralEvent);*/
        }

        public static void SaveShip(Building_ShipBridge bridge)
        {
            KillAllColonistsNotInCrypto(bridge.Map, bridge);
            string folder = Path.Combine(GenFilePaths.SaveDataFolderPath, "SoS2");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            string filename = Path.Combine(folder, Faction.OfPlayer.Name + "_" + bridge.ShipName + ".sos2");
            ShipInteriorMod2.SaveShip(bridge, filename);
        }
    }

    //Despite what common sense might tell you, this object does more than store past worlds. It also holds SoS story progress and research unlocks, as well as ship combat data.
	public class PastWorldUWO2 : WorldComponent
    {
        private int ShipsHaveInsidesVersion;
        public int PlayerFactionBounty;
        public int LastBountyRaidTick;				   
		public List<PreviousWorld> PastWorlds=new List<PreviousWorld>();
        public List<string> Unlocks = new List<string>();
        public bool startedEndgame;
        public bool SoSWin = false;
        public bool renderedThatAlready = false;
        public Dictionary<int, byte> PawnsInSpaceCache = new Dictionary<int, byte>();
        public List<Building_ShipAdvSensor> Sensors = new List<Building_ShipAdvSensor>();

        public PastWorldUWO2(World world) : base(world)
        {

        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            if (!Find.FactionManager.AllFactions.Any(f => f.def == FactionDefOf.Mechanoid))
                Log.Error("SOS2: Mechanoid faction not found! Parts of SOS2 will likely fail to function properly!");
            if (!Find.FactionManager.AllFactions.Any(f => f.def == FactionDefOf.Pirate))
                Log.Warning("SOS2: Pirate faction not found! SOS2 gameplay experience will be affected.");
            if (!Find.FactionManager.AllFactions.Any(f => f.def == FactionDefOf.Insect))
                Log.Warning("SOS2: Insect faction not found! SOS2 gameplay experience will be affected.");
        }

		public override void ExposeData() {
			base.ExposeData ();
            Scribe_Values.Look<int>(ref ShipsHaveInsidesVersion,"SoSVersion",0);
            WorldSwitchUtility.LoadWorldFlag = true;
			Scribe_Collections.Look<PreviousWorld> (ref PastWorlds, "PastWorlds", LookMode.Deep, new object[0]);
            WorldSwitchUtility.LoadWorldFlag = false;
            Scribe_Collections.Look<string>(ref Unlocks, "Unlocks", LookMode.Value);
            Scribe_Values.Look<int>(ref PlayerFactionBounty, "PlayerFactionBounty", 0);
            Scribe_Values.Look<int>(ref LastBountyRaidTick, "LastBountyRaidTicks", 0);
            Scribe_Values.Look<bool>(ref startedEndgame, "StartedEndgame");
            Scribe_Values.Look<int>(ref IncidentWorker_ShipCombat.LastAttackTick, "LastShipBattleTick");

            if (Scribe.mode != LoadSaveMode.PostLoadInit)
            {
                WorldSwitchUtility.PurgePWT();
            }
            /*if (Scribe.mode!=LoadSaveMode.Saving)
            {
                if(Unlocks.Contains("JTDrive")) //Back-compatibility: unlock JT drive research project if you got it before techprints were a thing
                {
                    Find.ResearchManager.FinishProject(ResearchProjectDef.Named("SoSJTDrive"));
                    Unlocks.Remove("JTDrive");
                }
                if (!Unlocks.Contains("JTDriveToo")) //Legacy compatibility for back when policies were different and a certain developer's head was still outside his own ass
                {
                    if (!Unlocks.Contains("JTDriveResearchChecked") && Find.ResearchManager.GetProgress(ResearchProjectDef.Named("SoSJTDrive")) >= 4000) //Hey, if you've already finished this research, you deserve special commemmoration!
                    {
                        Unlocks.Add("JTDriveToo");
                        GiveMeEntanglementManifold();
                    }
                    else //Let's check another way!
                    {
                        foreach (FieldInfo field in typeof(ResearchManager).GetFields()) //Let's randomly look at fields inside the research manager!
                        {
                            if (field.FieldType == typeof(Dictionary<ResearchProjectDef, int>)) //Hmm, any sort of dictionary of research projects and integers must be important!
                            {
                                if (((Dictionary<ResearchProjectDef, int>)field.GetValue(Find.ResearchManager)).ContainsKey(ResearchProjectDef.Named("SoSJTDrive"))) //Hey, if the JT drive gets mentioned in such an important place, maybe it means you already found one!
                                {
                                    Unlocks.Add("JTDriveToo");
                                    GiveMeEntanglementManifold();
                                    ((Dictionary<ResearchProjectDef, int>)field.GetValue(Find.ResearchManager)).Remove(ResearchProjectDef.Named("SoSJTDrive")); //Remove the NASTY EVIL FORBIDDEN DATA!
                                }
                            }
                        }
                    }
                    if(!Unlocks.Contains("JTDriveResearchChecked"))
                        Unlocks.Add("JTDriveResearchChecked");
                }
            }
            //recover from incorrect savestates
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (!ShipCombatManager.InCombat && !ShipCombatManager.InEncounter)
                {
                    if (ShipCombatManager.EnemyShip == null
                        && (ShipCombatManager.CanSalvageEnemyShip || ShipCombatManager.ShouldSalvageEnemyShip))
                    {
                        Log.Error("Recovering from incorrect state regarding enemy ship in save file. If there was an enemy ship, it is now lost and cannot be salvaged.");
                        ShipCombatManager.CanSalvageEnemyShip = false;
                        ShipCombatManager.ShouldSalvageEnemyShip = false;
                        ShipCombatManager.ShouldSkipSalvagingEnemyShip = false;
                    }
                }
            }*/
        }
		public override void WorldComponentTick()
        {
            if (Find.TickManager.TicksGame % 600 == 0)
            {
                var check = ((MapParent)Find.WorldObjects.AllWorldObjects.Where(ob => ob.def.defName.Equals("ShipOrbiting")).FirstOrDefault());
                if (check == null)
                    return;
                Map map = check.Map;
                if (map != null && PlayerFactionBounty > 20 && Find.TickManager.TicksGame-LastBountyRaidTick > Mathf.Max(600000f / Mathf.Sqrt(PlayerFactionBounty),60000f) && !map.GetComponent<ShipHeatMapComp>().InCombat)
                {
                    LastBountyRaidTick = Find.TickManager.TicksGame;
                    Building_ShipBridge bridge = map.listerBuildings.AllBuildingsColonistOfClass<Building_ShipBridge>().FirstOrDefault();
                    if (bridge == null)
                        return;
                    map.GetComponent<ShipHeatMapComp>().StartShipEncounter(bridge, bounty : true);
                }
            }
        }

        /*private void GiveMeEntanglementManifold()
        {
            IncidentParms parms = new IncidentParms();
            parms.target = Find.World;
            parms.forced = true;
            QueuedIncident qi = new QueuedIncident(new FiringIncident(IncidentDef.Named("SoSFreeEntanglement"), null, parms),Find.TickManager.TicksGame, Find.TickManager.TicksGame+99999999);
            Find.Storyteller.incidentQueue.Add(qi);
        }*/
	}
}

