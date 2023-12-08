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
        public Faction shipFaction;
        public SpaceNavyDef spaceNavyDef;
        public int wreckLevel;

        public DerelictShip() : base()
        {
            loadID = Find.UniqueIDsManager.GetNextPassingShipID();
            ticksUntilDeparture = Rand.RangeInclusive(40000, 80000);
        }

        protected override AcceptanceReport CanCommunicateWith(Pawn negotiator)
        {
            return "There is no response";
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref derelictShip, "EnemyShip");
            Scribe_Defs.Look(ref spaceNavyDef, "spaceNavyDef");
            Scribe_Values.Look<int>(ref wreckLevel, "wreckLevel");
            Scribe_References.Look<Faction>(ref shipFaction, "shipFaction", false);
        }

        public override string FullTitle
        {
            get
            {
                if (derelictShip != null)
                    return (loadID + ": " + derelictShip.label);
                return "Glitched ship";
            }
        }

        public override string GetCallLabel()
        {
            return derelictShip != null ? derelictShip.label : "Glitched ship";
        }
    }
}
