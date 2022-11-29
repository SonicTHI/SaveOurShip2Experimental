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
        public static TerrainDef hullTerrain = DefDatabase<TerrainDef>.GetNamed("FakeFloorInsideShip");
        public static TerrainDef mechHullTerrain = DefDatabase<TerrainDef>.GetNamed("FakeFloorInsideShipMech");
        public static TerrainDef archotechHullTerrain = DefDatabase<TerrainDef>.GetNamed("FakeFloorInsideShipArchotech");
        public static TerrainDef wreckageTerrain = DefDatabase<TerrainDef>.GetNamed("ShipWreckageTerrain");
        public static TerrainDef hullfoamTerrain = DefDatabase<TerrainDef>.GetNamed("FakeFloorInsideShipFoam");
        public static ThingDef hullPlateDef = ThingDef.Named("ShipHullTile");
        public static ThingDef mechHullPlateDef = ThingDef.Named("ShipHullTileMech");
        public static ThingDef archoHullPlateDef = ThingDef.Named("ShipHullTileArchotech");
        public static ThingDef hullFoamDef = ThingDef.Named("ShipHullfoamTile");
        bool isTile;
        bool isMechTile;
        bool isArchoTile;
        bool isFoamTile;

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
            isTile = parent.def == hullPlateDef;
            isMechTile = parent.def == mechHullPlateDef;
            isArchoTile = parent.def == archoHullPlateDef;
            isFoamTile = parent.def == hullFoamDef;
            map = parent.Map;
            positions = new List<IntVec3>();
            foreach (IntVec3 pos in GenAdj.CellsOccupiedBy(parent))
            {
                positions.Add(pos);
            }
            if (Props.roof && !ShipInteriorMod2.AirlockBugFlag) //MoveShip copies roof
            {
                foreach (IntVec3 pos in positions)
                {
                    var oldRoof = map.roofGrid.RoofAt(pos);
                    if (!ShipInteriorMod2.IsRoofDefAirtight(oldRoof))
                        map.roofGrid.SetRoof(pos, roof);
                }
            }
            if (respawningAfterLoad)
            {
                return;
            }
            foreach (IntVec3 v in positions)
            {
                SetShipTerrain(v);
            }
        }
        public void SetShipTerrain(IntVec3 v)
        {
            if (!map.terrainGrid.TerrainAt(v).layerable)
            {
                if (Props.archotech)
                    map.terrainGrid.SetTerrain(v, archotechHullTerrain);
                else if (Props.mechanoid)
                    map.terrainGrid.SetTerrain(v, mechHullTerrain);
                else if (Props.foam)
                    map.terrainGrid.SetTerrain(v, hullfoamTerrain);
                else if (Props.wreckage)
                    map.terrainGrid.SetTerrain(v, wreckageTerrain);
                else
                    map.terrainGrid.SetTerrain(v, hullTerrain);
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
                    if (t.TryGetComp<CompRoofMe>()!=null)
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
                            //Log.Message(string.Format("Spawning wrecks {0} at {1}", thing.def.defName, pos));
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
            if (!Props.roof)
                return;
            if ((Find.PlaySettings.showRoofOverlay || parent.Position.Fogged(parent.Map)) && parent.Position.Roofed(parent.Map))
            {
                foreach (Thing t in parent.Position.GetThingList(parent.Map))
                {
                    if (t is Building_ShipTurret || (t.TryGetComp<CompShipHeatSink>() != null && t.def.altitudeLayer == AltitudeLayer.WorldClipper))
                    {
                        return;
                    }
                }
                if (isTile)
                {
                    Graphics.DrawMesh(material: roofedGraphicTile.MatSingleFor(parent), mesh: roofedGraphicTile.MeshAt(parent.Rotation), position: new UnityEngine.Vector3(parent.DrawPos.x, 0, parent.DrawPos.z), rotation: Quaternion.identity, layer: 0);
                }
                else if (isMechTile || isArchoTile)
                {
                    Graphics.DrawMesh(material: roofedGraphicTileMech.MatSingleFor(parent), mesh: roofedGraphicTileMech.MeshAt(parent.Rotation), position: new UnityEngine.Vector3(parent.DrawPos.x, 0, parent.DrawPos.z), rotation: Quaternion.identity, layer: 0);
                }
            }
        }
    }
}