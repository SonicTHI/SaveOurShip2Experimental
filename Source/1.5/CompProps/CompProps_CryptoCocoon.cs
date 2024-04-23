using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
    class CompProps_CryptoCocoon : CompProperties_AbilityEffect
    {
        public CompProps_CryptoCocoon()
        {
            this.compClass = typeof(CompCryptoCocoon);
        }
    }
}
