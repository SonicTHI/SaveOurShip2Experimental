using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimWorld
{
    public class ArchotechGiftDef : Def, ILoadReferenceable
    {
        public ResearchProjectDef research;
        public ThingDef thing;

        public string GetUniqueLoadID()
        {
            return "ArchoGift_" + defName;
        }
    }
}
