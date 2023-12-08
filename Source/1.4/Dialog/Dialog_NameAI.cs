using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
    public class Dialog_NameAI : Dialog_Rename
    {
        private CompBuildingConsciousness AI;

        public Dialog_NameAI(CompBuildingConsciousness AI)
        {
            this.AI = AI;
            curName = AI.AIName;
        }

        protected override void SetName(string name)
        {
            if (name == AI.AIName || string.IsNullOrEmpty(name))
                return;

            AI.AIName = name;
            if (AI.Consciousness != null)
                AI.Consciousness.Name = new NameTriple("", name, "");
        }
    }
}
