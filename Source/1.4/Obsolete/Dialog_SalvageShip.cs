using RimWorld.Planet;
using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RimWorld
{
    class Dialog_SalvageShip : Window
    {
        private enum Tab
        {
            Pawns,
            Items
        }

        private List<ThingOwner> transporters;

        private List<TransferableOneWay> transferables;

        private TransferableOneWayWidget pawnsTransfer;

        private TransferableOneWayWidget itemsTransfer;

        private Tab tab;

        private float lastMassFlashTime = -9999f;

        private bool massUsageDirty = true;

        private float cachedMassUsage;

        private const float TitleRectHeight = 35f;

        private const float BottomAreaHeight = 55f;

        private readonly Vector2 BottomButtonSize = new Vector2(160f, 40f);

        private static List<TabRecord> tabsList = new List<TabRecord>();

        private static List<List<TransferableOneWay>> tmpLeftToLoadCopy = new List<List<TransferableOneWay>>();

        private static Dictionary<TransferableOneWay, int> tmpLeftCountToTransfer = new Dictionary<TransferableOneWay, int>();

        public bool CanChangeAssignedThingsAfterStarting => false;

        public bool LoadingInProgressOrReadyToLaunch => true;

        public override Vector2 InitialSize => new Vector2(1024f, UI.screenHeight);

        protected override float Margin => 0f;

        private int numSalvageBays;

        private Map map;

        private static int MASS_PER_BAY = 1000;

        private float MassCapacity
        {
            get
            {
                return numSalvageBays * MASS_PER_BAY;
            }
        }

        private float MassUsage
        {
            get
            {
                if (massUsageDirty)
                {
                    massUsageDirty = false;
                    cachedMassUsage = CollectionsMassCalculator.MassUsageTransferables(transferables, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, includePawnsMass: true);
                }
                return cachedMassUsage;
            }
        }

        public Dialog_SalvageShip(int numSalvageBays, Map map)
        {
            this.numSalvageBays = numSalvageBays;
            this.map = map;
            transporters = new List<ThingOwner>();
            for(int i=0;i<numSalvageBays;i++)
            {
                transporters.Add(new ThingOwner<Thing>());
            }
            forcePause = true;
            absorbInputAroundWindow = true;
        }

        public override void PostOpen()
        {
            base.PostOpen();
            CalculateAndRecacheTransferables();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect rect = new Rect(0f, 0f, inRect.width, 35f);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, TranslatorFormattedStringExtensions.Translate("SalvageShip"));
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            CaravanUIUtility.DrawCaravanInfo(new CaravanUIUtility.CaravanInfo(MassUsage, MassCapacity, "", 0, "", new Pair<float, float>(0,0), new Pair<ThingDef, float>(null,0), "", 0, ""), null, map.Tile, null, lastMassFlashTime, new Rect(12f, 35f, inRect.width - 24f, 40f), lerpMassColor: true, ((TaggedString)null) );
            tabsList.Clear();
            tabsList.Add(new TabRecord(TranslatorFormattedStringExtensions.Translate("PawnsTab"), delegate
            {
                tab = Tab.Pawns;
            }, tab == Tab.Pawns));
            tabsList.Add(new TabRecord(TranslatorFormattedStringExtensions.Translate("ItemsTab"), delegate
            {
                tab = Tab.Items;
            }, tab == Tab.Items));
            inRect.yMin += 119f;
            Widgets.DrawMenuSection(inRect);
            TabDrawer.DrawTabs(inRect, tabsList);
            inRect = inRect.ContractedBy(17f);
            GUI.BeginGroup(inRect);
            Rect rect2 = inRect.AtZero();
            DoBottomButtons(rect2);
            Rect inRect2 = rect2;
            inRect2.yMax -= 59f;
            bool anythingChanged = false;
            switch (tab)
            {
                case Tab.Pawns:
                    pawnsTransfer.OnGUI(inRect2, out anythingChanged);
                    break;
                case Tab.Items:
                    itemsTransfer.OnGUI(inRect2, out anythingChanged);
                    break;
            }
            if (anythingChanged)
            {
                CountToTransferChanged();
            }
            GUI.EndGroup();
        }

        public override bool CausesMessageBackground()
        {
            return true;
        }

        private void AddToTransferables(Thing t)
        {
            if (t == null || (t is Corpse && ((Corpse)t).InnerPawn == null)|| t.stackCount==0)
                return;
            try
            {
                TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatching(t, transferables, TransferAsOneMode.PodsOrCaravanPacking);
                if (transferableOneWay == null)
                {
                    transferableOneWay = new TransferableOneWay();
                    transferables.Add(transferableOneWay);
                }
                if (transferableOneWay.things.Contains(t))
                {
                    Log.Error("Tried to add the same thing twice to TransferableOneWay: " + t);
                }
                else
                {
                    transferableOneWay.things.Add(t);
                }
            }
            catch(Exception e)
            {
                Log.Error(e.Message);
            }
        }

        private void DoBottomButtons(Rect rect)
        {
            Rect rect2 = new Rect(rect.width / 2f - BottomButtonSize.x / 2f, rect.height - 55f, BottomButtonSize.x, BottomButtonSize.y);
            if (Widgets.ButtonText(rect2, TranslatorFormattedStringExtensions.Translate("AcceptButton")))
            {
                if (TryAccept())
                {
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    Close(doCloseSound: false);
                }
            }
            if (Widgets.ButtonText(new Rect(rect2.x - 10f - BottomButtonSize.x, rect2.y, BottomButtonSize.x, BottomButtonSize.y), TranslatorFormattedStringExtensions.Translate("ResetButton")))
            {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                CalculateAndRecacheTransferables();
            }
            if (Widgets.ButtonText(new Rect(rect2.x - 20f - BottomButtonSize.x * 2, rect2.y, BottomButtonSize.x, BottomButtonSize.y), TranslatorFormattedStringExtensions.Translate("SelectEverything")))
            {
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
                SetToLoadEverything();
            }
        }

        private void CalculateAndRecacheTransferables()
        {
            transferables = new List<TransferableOneWay>();
            AddItemsToTransferables();
            //Log.Message("Transferables list has " + transferables.Count + " entries."); //TODO remove
            pawnsTransfer = new TransferableOneWayWidget(null, null, null, TranslatorFormattedStringExtensions.Translate("FormCaravanColonyThingCountTip"), drawMass: true, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, includePawnsMassInMassUsage: true, () => MassCapacity - MassUsage, 0f, ignoreSpawnedCorpseGearAndInventoryMass: false, map.Tile, drawMarketValue: true, drawEquippedWeapon: true, drawNutritionEatenPerDay: false, drawItemNutrition: false, drawForagedFoodPerDay: false);
            CaravanUIUtility.AddPawnsSections(pawnsTransfer, transferables);
            itemsTransfer = new TransferableOneWayWidget(transferables.Where((TransferableOneWay x) => x.ThingDef.category != ThingCategory.Pawn), null, null, TranslatorFormattedStringExtensions.Translate("FormCaravanColonyThingCountTip"), drawMass: true, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, includePawnsMassInMassUsage: true, () => MassCapacity - MassUsage, 0f, ignoreSpawnedCorpseGearAndInventoryMass: false, map.Tile, drawMarketValue: true, drawEquippedWeapon: false, drawNutritionEatenPerDay: false, drawItemNutrition: true, drawForagedFoodPerDay: false, drawDaysUntilRot: true);
            CountToTransferChanged();
        }

        private bool TryAccept()
        {
            List<Pawn> pawnsFromTransferables = TransferableUtility.GetPawnsFromTransferables(transferables);
            if (!CheckForErrors(pawnsFromTransferables))
            {
                return false;
            }
            if (LoadingInProgressOrReadyToLaunch)
            {
                AssignTransferablesToRandomTransporters();
            }
            return true;
        }

        private void AssignTransferablesToRandomTransporters()
        {
            foreach(TransferableOneWay tr in transferables)
            {
                TransferableUtility.Transfer(tr.things, tr.CountToTransfer, delegate (Thing splitPiece, IThingHolder originalHolder)
                {
                    int which = Rand.RangeInclusive(0, transporters.Count() - 1);
                    transporters[which].TryAddOrTransfer(splitPiece);
                });
            }
        }

        private bool CheckForErrors(List<Pawn> pawns)
        {
            if (MassUsage > MassCapacity)
            {
                FlashMass();
                Messages.Message(TranslatorFormattedStringExtensions.Translate("TooBigTransportersMassUsage"), MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }
            return true;
        }

        private void AddItemsToTransferables()
        {
            foreach (Thing item in ShipCombatManager.Salvage)
                AddToTransferables(item);
            foreach(ThingDef def in ShipCombatManager.SalvageGeneric.Keys)
            {
                for(int i=0;i<ShipCombatManager.SalvageGeneric[def]/def.stackLimit;i++)
                {
                    Thing stack = ThingMaker.MakeThing(def);
                    stack.stackCount = def.stackLimit;
                    AddToTransferables(stack);
                }
                if(ShipCombatManager.SalvageGeneric[def]%def.stackLimit > 0)
                {
                    Thing stack = ThingMaker.MakeThing(def);
                    stack.stackCount = ShipCombatManager.SalvageGeneric[def] % def.stackLimit;
                    AddToTransferables(stack);
                }
            }
        }

        private void FlashMass()
        {
            lastMassFlashTime = Time.time;
        }

        private void SetToLoadEverything()
        {
            for (int i = 0; i < transferables.Count; i++)
            {
                transferables[i].AdjustTo(transferables[i].GetMaximumToTransfer());
            }
            CountToTransferChanged();
        }

        private void CountToTransferChanged()
        {
            massUsageDirty = true;
        }

        public override void PostClose()
        {
            base.PostClose();
            Thing[] bays = map.spawnedThings.Where(t => t.def == ResourceBank.ThingDefOf.ShipSalvageBay).ToArray();
            for (int i=0; i<numSalvageBays; i++)
            {
                ActiveDropPodInfo activeDropPodInfo = new ActiveDropPodInfo();
                activeDropPodInfo.innerContainer = transporters[i];
                activeDropPodInfo.leaveSlag = false;
                DropPodUtility.MakeDropPodAt(bays[i].Position, map, activeDropPodInfo);
            }
            ShipCombatManager.Salvage.Clear();
            ShipCombatManager.SalvageGeneric.Clear();
        }
    }
}
