using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using RimworldMod;
using RimworldMod.VacuumIsNotFun;
using Verse;

namespace RimWorld
{
    class CompLongRangeMineralScannerSpaceAI : CompLongRangeMineralScannerSpace
    {
        new public bool CanUseNow
        {
            get
            {
                return false;
            }
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            if (this.parent.Map.IsSpace() || !this.powerComp.PowerOn)
                return;
            this.daysWorkingSinceLastMinerals += 0.004f;
            float mtb = this.Props.mtbDays / 20;
            if (this.daysWorkingSinceLastMinerals >= this.Props.guaranteedToFindLumpAfterDaysWorking || Rand.MTBEventOccurs(mtb, 60000f, 59f))
            {
                this.FoundMinerals(null);
            }
        }
    }
}
