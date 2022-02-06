using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Verse;

namespace RimWorld
{
    [StaticConstructorOnStartup]
    public class CompRoofMe : ThingComp
    {
        public static GraphicData roofedData = new GraphicData();
        public static GraphicData roofedDataMech = new GraphicData();
        public static Graphic roofedGraphicTile;
        public static Graphic roofedGraphicTileMech;
        public static RoofDef roof = DefDatabase<RoofDef>.GetNamed("RoofShip");
        bool isTile;
        bool isMechTile;
        bool isArchoTile;
        bool isFoamTile;
        public static TerrainDef hullTerrain = DefDatabase<TerrainDef>.GetNamed("FakeFloorInsideShip");
        public static TerrainDef mechHullTerrain = DefDatabase<TerrainDef>.GetNamed("FakeFloorInsideShipMech");
        public static TerrainDef archotechHullTerrain = DefDatabase<TerrainDef>.GetNamed("FakeFloorInsideShipArchotech");
        public static TerrainDef wreckageTerrain = DefDatabase<TerrainDef>.GetNamed("ShipWreckageTerrain");
        public static TerrainDef hullfoamTerrain = DefDatabase<TerrainDef>.GetNamed("FakeFloorInsideShipFoam");

        static CompRoofMe()
        {
            roofedData.texPath = "Things/Building/Ship/Ship_Roof";
            roofedData.graphicClass = typeof(Graphic_Single);
            roofedData.shaderType = ShaderTypeDefOf.MetaOverlay;
            roofedGraphicTile = new Graphic_256(roofedData.Graphic);
            roofedDataMech.texPath = "Things/Building/Ship/Ship_RoofMech";
            roofedDataMech.graphicClass = typeof(Graphic_Single);
            roofedDataMech.shaderType = ShaderTypeDefOf.MetaOverlay;
            roofedGraphicTileMech = new Graphic_256(roofedDataMech.Graphic);
        }

        public CompProperties_RoofMe Props
        {
            get
            {
                return (CompProperties_RoofMe)this.props;
            }
        }

        List<IntVec3> positions = new List<IntVec3>();
        Map map;
		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
            isTile = parent.def == ShipInteriorMod2.hullPlateDef;
            isMechTile = parent.def == ShipInteriorMod2.mechHullPlateDef;
            isArchoTile = parent.def == ShipInteriorMod2.archoHullPlateDef;
            isFoamTile = parent.def.defName.Equals("ShipHullfoamTile");
            map = parent.Map;
            positions = new List<IntVec3>();
            foreach (IntVec3 pos in GenAdj.CellsOccupiedBy(parent))
            {
                positions.Add(pos);
            }
            foreach (IntVec3 pos in positions)
            {
                if (Props.roof && map.roofGrid.Roofed(pos) && map.roofGrid.RoofAt(pos)!=roof)
                    map.roofGrid.SetRoof(pos, roof);
            }
            if (respawningAfterLoad)
            {
                return;
            }
            foreach (IntVec3 pos in positions)
            {
                if(Props.roof)
                    map.roofGrid.SetRoof(pos, roof);
                if (!map.terrainGrid.TerrainAt(pos).layerable)
                {
                    if (!Props.wreckage && !Props.mechanoid && !isArchoTile && !isFoamTile && map.terrainGrid.TerrainAt(pos) != hullTerrain)
                        map.terrainGrid.SetTerrain(pos, hullTerrain);
                    else if (!Props.wreckage && !isArchoTile && !isFoamTile && map.terrainGrid.TerrainAt(pos) != mechHullTerrain)
                        map.terrainGrid.SetTerrain(pos, mechHullTerrain);
                    else if (!Props.wreckage && !isFoamTile && map.terrainGrid.TerrainAt(pos) != archotechHullTerrain)
                        map.terrainGrid.SetTerrain(pos, archotechHullTerrain);
                    else if (!Props.wreckage && isFoamTile && map.terrainGrid.TerrainAt(pos) != hullfoamTerrain)
                        map.terrainGrid.SetTerrain(pos, hullfoamTerrain);
                    else
                        map.terrainGrid.SetTerrain(pos, wreckageTerrain);
                }
            }
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            foreach (IntVec3 pos in positions)
            {
                bool stillHasTile = false;
                foreach(Thing t in map.thingGrid.ThingsAt(pos))
                {
                    if(t.TryGetComp<CompRoofMe>()!=null)
                    {
                        stillHasTile = true;
                        break;
                    }
                }
                if (!stillHasTile)
                {
                    TerrainDef manualTerrain = map.terrainGrid.TerrainAt(pos); //RimWorld freaks out about regions if we don't do the leavings manually
                    map.terrainGrid.RemoveTopLayer(pos, false);
                    List<ThingDefCountClass> list = manualTerrain.CostListAdjusted(null);
                    for (int i = 0; i < list.Count; i++)
                    {
                        ThingDefCountClass thingDefCountClass = list[i];
                        int num = GenMath.RoundRandom((float)thingDefCountClass.count * manualTerrain.resourcesFractionWhenDeconstructed);
                        if (num > 0)
                        {
                            Thing thing = ThingMaker.MakeThing(thingDefCountClass.thingDef);
                            thing.stackCount = num;
                            GenPlace.TryPlaceThing(thing, pos, map, ThingPlaceMode.Near);
                        }
                    }
                    if (Props.roof)
                        map.roofGrid.SetRoof(pos, null);
                }
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();
            if((Find.PlaySettings.showRoofOverlay || parent.Map.fogGrid.fogGrid[parent.Map.cellIndices.CellToIndex(parent.Position)]) && parent.Position.Roofed(parent.Map) && parent.Position.GetFirstThing<Building_ShipTurret>(parent.Map)==null)
            {
                if (isTile)
                {
                    Graphics.DrawMesh(material: roofedGraphicTile.MatSingleFor(parent), mesh: roofedGraphicTile.MeshAt(parent.Rotation), position: new UnityEngine.Vector3(parent.DrawPos.x,0,parent.DrawPos.z), rotation: Quaternion.identity, layer: 0);
                }
                else if (isMechTile || isArchoTile)
                {
                    Graphics.DrawMesh(material: roofedGraphicTileMech.MatSingleFor(parent), mesh: roofedGraphicTileMech.MeshAt(parent.Rotation), position: new UnityEngine.Vector3(parent.DrawPos.x, 0, parent.DrawPos.z), rotation: Quaternion.identity, layer: 0);
                }
            }
        }
    }
}