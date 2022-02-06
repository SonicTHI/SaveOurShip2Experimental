using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld
{
    class AttackableShip : PassingShip
    {
        public EnemyShipDef enemyShip;

        public AttackableShip() : base()
        {
            loadID = Find.UniqueIDsManager.GetNextPassingShipID();
        }

        protected override AcceptanceReport CanCommunicateWith(Pawn negotiator)
        {
            return "This ship refuses your hails";
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref enemyShip, "EnemyShip");
        }

        public override string FullTitle => enemyShip!=null ? enemyShip.label : "Glitched ship";

        public override string GetCallLabel()
        {
            return enemyShip != null ? enemyShip.label : "Glitched ship";
        }
    }
}
