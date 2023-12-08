using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld
{
    class DetachedShipPart : Thing
    {
        public static byte[,] drawWreckage;
        public static IntVec3 drawMinVector;
        static bool ForceLoadedGraphic = false;
        static bool ForceLoadedGraphic2 = false;

        GraphicData graphicWall = ThingDef.Named("Ship_Beam_Wrecked_Fake").graphicData;
        GraphicData graphicFloor = ThingDef.Named("ShipHullTileWreckedFake").graphicData;

        public ShipHeatMapComp mapComp;
        public List<int> wreckageList=new List<int>();
        public byte[,] wreckage;
        public int xSize;
        public int zSize;
        public Vector3 velocity = new Vector3(0, 0, 0);

        public Vector3 drawOffset = new Vector3(0, 0, 0);

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            mapComp = Map.GetComponent<ShipHeatMapComp>();
        }

        public override void Tick() //slow steady drift, gain accel when ship engines on
        {
            float driftx;
            if (mapComp.MapEnginePower == 0)
            {
                driftx = Rand.Range(-0.1f, 0.1f);
                velocity += new Vector3(driftx, 0, Rand.Range(0.001f, 0.01f)).RotatedBy(mapComp.EngineRot * 270f);
                //Vector3 adj = new Vector3(0, 0, mapComp.MapEnginePower).RotatedBy(mapComp.EngineRot * 90f);
                //drawOffset += (DrawPos - adj).normalized * 0.005f * (int)Find.TickManager.CurTimeSpeed;
            }
            else
            {
                driftx = Rand.Range(-0.1f, 0.1f);
                velocity += new Vector3(driftx, 0, mapComp.MapEnginePower).RotatedBy(mapComp.EngineRot * 270f);
            }
            drawOffset += velocity * 0.01f * (int)Find.TickManager.CurTimeSpeed;

            if (drawOffset.x > Map.Size.x || drawOffset.x * -1 > Map.Size.x || drawOffset.z > Map.Size.z || drawOffset.z * -1 > Map.Size.z)
                Destroy();

            if (Find.TickManager.TicksGame % 32 == 0)
                EmitSmokeAndFlame();
        }

        void EmitSmokeAndFlame()
        {
            for (int i = 0; i < Math.Sqrt(wreckage.GetLength(0) * wreckage.GetLength(1))/4; i++)
            {
                int x = Rand.RangeInclusive(0, wreckage.GetLength(0) - 1);
                int z = Rand.RangeInclusive(0, wreckage.GetLength(1) - 1);
                if (wreckage[x, z]!=0)
                {
                    FleckMaker.ThrowSmoke(new Vector3(Position.x+x,0,Position.z+z)+drawOffset, Map, 1);
                    FleckMaker.ThrowMicroSparks(new Vector3(Position.x + x, 0, Position.z + z)+drawOffset, Map);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref xSize, "xSize");
            Scribe_Values.Look<int>(ref zSize, "zSize");
            if(Scribe.mode == LoadSaveMode.Saving)
                wreckageList = ListFromWreckage();
            Scribe_Collections.Look<int>(ref wreckageList, "wreckage");
            wreckage = WreckageFromList();
        }

        List<int> ListFromWreckage()
        {
            List<int> output = new List<int>();
            for(int x=0;x<wreckage.GetLength(0);x++)
            {
                for(int z=0; z<wreckage.GetLength(1);z++)
                {
                    output.Add(wreckage[x, z]);
                }
            }
            return output;
        }

        byte[,] WreckageFromList()
        {
            int index = 0;
            byte[,] output = new byte[xSize, zSize];
            for(int x=0;x<xSize;x++)
            {
                for(int z=0;z<zSize;z++)
                {
                    output[x, z] = (byte)wreckageList[index];
                    index++;
                }
            }
            return output;
        }

        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            drawLoc = drawLoc + drawOffset;
            drawWreckage = this.wreckage;
            drawMinVector = this.Position;
            if(!ForceLoadedGraphic)
            {
                ShaderTypeDef cutout = graphicWall.shaderType;
                if (cutout == null)
                {
                    cutout = ShaderTypeDefOf.Cutout;
                }
                Shader shader = cutout.Shader;
                //Log.Message("Force-loading graphic");
                typeof(GraphicData).GetField("cachedGraphic", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(graphicWall, GraphicDatabase.Get(graphicWall.graphicClass, graphicWall.texPath, shader, graphicWall.drawSize, graphicWall.color, graphicWall.colorTwo, graphicWall, graphicWall.shaderParameters));
                typeof(GraphicData).GetField("cachedGraphic", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(graphicWall, new Graphic_Linked_Fake((Graphic)typeof(GraphicData).GetField("cachedGraphic", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(graphicWall)));
                //Log.Message("Force-loaded graphic " + typeof(GraphicData).GetField("cachedGraphic", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(graphicWall));
                ForceLoadedGraphic = true;
            }
            if (!ForceLoadedGraphic2)
            {
                ShaderTypeDef cutout = graphicFloor.shaderType;
                if (cutout == null)
                {
                    cutout = ShaderTypeDefOf.Cutout;
                }
                Shader shader = cutout.Shader;
                //Log.Message("Force-loading graphic");
                typeof(GraphicData).GetField("cachedGraphic", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(graphicFloor, GraphicDatabase.Get(graphicFloor.graphicClass, graphicFloor.texPath, shader, graphicFloor.drawSize, graphicFloor.color, graphicFloor.colorTwo, graphicFloor, graphicFloor.shaderParameters));
                typeof(GraphicData).GetField("cachedGraphic", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(graphicFloor, new Graphic_256_Wreckage((Graphic)typeof(GraphicData).GetField("cachedGraphic", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(graphicFloor)));
                //Log.Message("Force-loaded graphic " + typeof(GraphicData).GetField("cachedGraphic", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(graphicFloor));
                ForceLoadedGraphic2 = true;
            }
            for (int x=0;x<wreckage.GetLength(0);x++)
            {
                for(int z=0;z<wreckage.GetLength(1);z++)
                {
                    if(wreckage[x,z]==1)
                        ((Graphic_Linked_Fake)graphicWall.Graphic).Draw(drawLoc + new Vector3(x, 0, z), this.Rotation, this);
                    else if (wreckage[x, z] == 2)
                        graphicFloor.Graphic.Draw(drawLoc + new Vector3(x, 0, z), this.Rotation, this);
                }
            }
        }
    }
}
