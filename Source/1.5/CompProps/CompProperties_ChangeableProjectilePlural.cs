using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld
{
    public class CompProperties_ChangeableProjectilePlural : CompProperties
    {
        public int maxTorpedoes;
        public int tubes;

        public CompProperties_ChangeableProjectilePlural()
        {
            compClass = typeof(CompChangeableProjectilePlural);
        }
    }
}
