using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld
{
    public class CompProperties_BlackBoxConsole : CompProperties
    {

        public CompProperties_BlackBoxConsole()
        {
            this.compClass = typeof(CompBlackBoxConsole);
        }
    }
}