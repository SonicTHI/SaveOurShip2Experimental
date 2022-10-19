using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld
{
    class DerelictShip : PassingShip
    {
        public EnemyShipDef derelictShip;

        public DerelictShip() : base()
        {
            loadID = Find.UniqueIDsManager.GetNextPassingShipID();
        }

        protected override AcceptanceReport CanCommunicateWith(Pawn negotiator)
        {
            return "There is no response";
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref derelictShip, "EnemyShip");
        }

        public override string FullTitle => derelictShip != null ? derelictShip.label : "Glitched ship";

        public override string GetCallLabel()
        {
            return derelictShip != null ? derelictShip.label : "Glitched ship";
        }
    }
}
