using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimWorld
{
    public class ChoiceLetter_SpacePirates : ChoiceLetter
    {
        public Map map;
        public bool parely;
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

        //parley hail in comms
        /*ChoiceLetter_SpacePirates choiceLetter_SpacePirates = (ChoiceLetter_SpacePirates)LetterMaker.MakeLetter(def.letterLabel, def.letterText, def.letterDef, null, null);
        choiceLetter_SpacePirates.map = map;
        if (ShipInteriorMod2.WorldComp.PlayerFactionBounty > 40)
        {
            choiceLetter_SpacePirates.parely = true;
        }
        choiceLetter_SpacePirates.StartTimeout(6000);
        Find.LetterStack.ReceiveLetter(choiceLetter_SpacePirates, null);*/
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
                        //show trade menu with negative trade value player needs to fill, on close remove if enough

                        //if parely spawn pirate trader, normal trade window, on close remove trader

                        //Find.WindowStack.Add(new Dialog_Trade());
                        Find.LetterStack.RemoveLetter(this);
                    };
                    diaOption.resolveTree = true;
                    optionReject.action = delegate ()
                    {
                        if (!parely)
                        {
                            var mapComp = map.GetComponent<ShipHeatMapComp>();
                            mapComp.StartShipEncounter(mapComp.MapRootListAll.FirstOrDefault(), null, null, Faction.OfPirates);
                        }
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

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Map>(ref map, "map");
            Scribe_Values.Look<bool>(ref parely, "parely");
            Scribe_Values.Look<string>(ref this.signalAccept, "signalAccept", null, false);
            Scribe_Values.Look<string>(ref this.signalReject, "signalReject", null, false);
        }

        public string signalAccept;

        public string signalReject;
    }
}
