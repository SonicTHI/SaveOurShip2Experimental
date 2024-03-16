using System;

namespace RimWorld
{
    /*class CompSoSSchedule : CompSchedule
    {
        CompSoShipLight partInt;

        CompSoShipLight Part
        {
            get
            {
                if (partInt == null)
                    partInt = parent.GetComp<CompShipLight>().lightComp;
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
    }*/
}
