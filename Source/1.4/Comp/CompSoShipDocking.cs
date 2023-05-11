using Verse;

namespace RimWorld
{
    /// <summary>
    /// Undocks dockParent if destroyed.
    /// </summary>
    public class CompSoShipDocking : ThingComp
    {
        public Building_ShipAirlock dockParent;
        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            dockParent?.UnDock();
        }
    }
}