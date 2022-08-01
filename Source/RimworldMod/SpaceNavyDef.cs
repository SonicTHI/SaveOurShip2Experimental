using System;
using System.Collections.Generic;
using Verse;

namespace RimWorld
{
    class SpaceNavyDef : Def
    {
        public FactionDef factionDef;

        public string label;

        public List<EnemyShipDef> enemyShipDefs;

        public List<string> crewDefs;

        public string GetUniqueLoadID()
        {
            return "SpaceNavy_" + defName;
        }
    }
}
