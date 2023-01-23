using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimWorld
{
    class ThingSetMaker_ArchotechGift : ThingSetMaker_MarketValue
    {
        public override IEnumerable<ThingDef> AllowedThingDefs(ThingSetMakerParams parms)
        {
            List<ThingDef> defs = new List<ThingDef>();
            foreach(ArchotechGiftDef def in DefDatabase<ArchotechGiftDef>.AllDefs)
            {
                if(def.research.IsFinished)
                {
                    defs.Add(def.thing);
                }
            }
            return defs;
        }
    }
}
