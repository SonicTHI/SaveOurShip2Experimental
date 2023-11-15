using System;
using Verse;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Verse.AI.Group;
using Verse.Sound;
using System.IO;
using System.Text;

namespace SaveOurShip2
{
    public static class WorldSwitchUtility
    {
        //dep
        /*
        public static bool LoadShipFlag = false; //NOTE: This is set to true in ScenPart_LoadShip.PostWorldGenerate and false in the patch to MapGenerator.GenerateMap
        public static bool StartShipFlag = false; //as above but for ScenPart_StartInSpace
        private static PastWorldUWO2 PastWorldTrackerInternal = null;
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


        public static bool FactionRelationFlag = false;
        public static bool LoadWorldFlag = false;
        public static Faction SavedPlayerFaction = null;
        public static Map SavedMap = null;
        public static UniqueIDsManager Uniques = null;
        public static WorldPawns Pawns = null;
        public static FactionManager Factions = null;
        public static IdeoManager Ideos = null;
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
        }*/
    }
}