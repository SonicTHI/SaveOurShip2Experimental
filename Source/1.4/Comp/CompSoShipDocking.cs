using System.Collections.Generic;
using Verse;

namespace RimWorld
{
    /// <summary>
    /// Undocks dockParent if destroyed.
    /// </summary>
    public class CompSoShipDocking : ThingComp
    {
        public Building_ShipAirlock dockParent;
        public bool removedByDock;
        public CompProperties_SoShipDocking Props
        {
            get
            {
                return (CompProperties_SoShipDocking)props;
            }
        }
        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            if (!removedByDock) //if any part is destroyed, destroy entire assembly + one extender
            {
                dockParent?.DeSpawnDock();
                if (!Props.extender) //if not the extender, destroy one
                {
                    if (Rand.Bool)
                        dockParent?.First.Destroy();
                    else
                        dockParent?.Second.Destroy();
                }
                dockParent?.ResetDock();
            }
        }
    }
}