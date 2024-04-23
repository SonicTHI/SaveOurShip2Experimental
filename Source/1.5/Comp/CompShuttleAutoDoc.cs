using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vehicles;
using Verse;

namespace SaveOurShip2
{
    class CompShuttleAutoDoc : ThingComp
    {
        CompProps_ShuttleAutoDoc Props => (CompProps_ShuttleAutoDoc)props;

        public override void CompTickRare()
        {
            base.CompTickRare();
            if(parent is VehiclePawn vehicle)
            {
                foreach(Pawn pawn in vehicle.AllPawnsAboard)
                {
                    Hediff bleed = HealthUtility.FindMostBleedingHediff(pawn, new HediffDef[] { });
                    if (bleed != null)
                        bleed.Tended(Props.tendQualityRange.RandomInRange, Props.tendQualityRange.TrueMax);
                }
            }
        }
    }
}
