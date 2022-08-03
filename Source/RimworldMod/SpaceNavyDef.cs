using System;
using System.Collections.Generic;
using Verse;

namespace RimWorld
{
    public class SpaceNavyDef : Def
    {
        public FactionDef factionDef;

        public string label;

        public List<EnemyShipDef> enemyShipDefs;

        public bool canOperateAfterFactionDefeated = false;

        // Crew defs
        public string crewDef;
        public string marineDef;
        public string marineHeavyDef;

        public string GetUniqueLoadID()
        {
            return "SpaceNavy_" + defName;
        }
    }
}
