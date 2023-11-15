using SaveOurShip2;
using Verse;

namespace RimWorld
{
    /// <summary>
    /// Undocks dockParent if destroyed, roofs and floors plating.
    /// </summary>
    public class CompSoShipDocking : ThingComp
    {
        public Building_ShipAirlock dockParent;
        public bool removedByDock;
        public IntVec3 position;
        public ShipHeatMapComp mapComp;

        public CompProperties_SoShipDocking Props
        {
            get
            {
                return (CompProperties_SoShipDocking)props;
            }
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (Props.isPlating)
            {
                mapComp = parent.Map.GetComponent<ShipHeatMapComp>();
                position = parent.Position;
                mapComp.MapExtenderCells.Add(position);
                parent.Map.roofGrid.SetRoof(position, ResourceBank.RoofDefOf.RoofShip);
                parent.Map.terrainGrid.SetTerrain(position, ResourceBank.TerrainDefOf.FakeFloorInsideShipFoam);
            }
        }
        public override void PostDeSpawn(Map map)
        {
            if (Props.isPlating)
            {
                map.roofGrid.SetRoof(position, null);
                map.terrainGrid.RemoveTopLayer(position);
                mapComp.MapExtenderCells.Remove(position);
            }
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