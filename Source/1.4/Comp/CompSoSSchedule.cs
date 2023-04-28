using System;

namespace RimWorld
{
    class CompSoSSchedule : CompSchedule
    {
        CompSoShipPart partInt;

        CompSoShipPart Part
        {
            get
            {
                if (partInt == null)
                    partInt = parent.GetComp<CompShipLight>().shipComp;
                return partInt;
            }
        }

        public override void CompTickRare()
        {
            if (Part.sunLight)
                base.CompTickRare();
            else if (!intAllowed)
            {
                intAllowed = true;
                parent.BroadcastCompSignal("ScheduledOn");
            }
        }
    }
}
