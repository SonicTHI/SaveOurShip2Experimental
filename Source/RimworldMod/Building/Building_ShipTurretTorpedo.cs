using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld
{
    [StaticConstructorOnStartup]
    class Building_ShipTurretTorpedo : Building_ShipTurret
    {
        public static Graphic torpedoBayDoor = GraphicDatabase.Get(typeof(Graphic_Single), "Things/Building/Ship/TorpedoTubeDoor", ShaderDatabase.Cutout, new Vector2(6, 7), Color.white, Color.white);
        public static Graphic torpedoBayDoorSm = GraphicDatabase.Get(typeof(Graphic_Single), "Things/Building/Ship/TorpedoTubeDoor_small", ShaderDatabase.Cutout, new Vector2(6, 3), Color.white, Color.white);
        public static Graphic torpedoBayDoorXS = GraphicDatabase.Get(typeof(Graphic_Single), "Things/Building/Ship/TorpedoTubeDoor_single", ShaderDatabase.Cutout, new Vector2(3, 3), Color.white, Color.white);
        public static Mesh doorOne = MeshMakerPlanes.NewPlaneMesh(new Vector2(6, 7), false, false, false);
        public static Mesh doorTwo = MeshMakerPlanes.NewPlaneMesh(new Vector2(6, 7), true, false, false);
        public static Mesh doorOneSm = MeshMakerPlanes.NewPlaneMesh(new Vector2(6, 3), false, false, false);
        public static Mesh doorTwoSm = MeshMakerPlanes.NewPlaneMesh(new Vector2(6, 3), true, false, false);
        public static Mesh doorOneXS = MeshMakerPlanes.NewPlaneMesh(new Vector2(3, 3), false, false, false);
        float ticksSinceOpen = 0;
        float TicksToOpenNow = 60;

        int timesFired = 0;
        static Vector3[] TubePos = { new Vector3(-1, 0, -1.5f), new Vector3(1, 0, -1.5f), new Vector3(-1, 0, 0), new Vector3(1, 0, 0), new Vector3(-1, 0, 1.5f), new Vector3(1, 0, 1.5f) };
        static Vector3[] TubePosTwo = { new Vector3(-1, 0, 0), new Vector3(1, 0, 0) };

        public static Dictionary<Map, List<Building_ShipTurretTorpedo>> allTubesOnMap = new Dictionary<Map, List<Building_ShipTurretTorpedo>>();
		
        public override void Draw()
        {
            base.Draw();
            float d = 0.4f * -3.5f *Mathf.Clamp01(ticksSinceOpen / TicksToOpenNow);
            for (int i = 0; i < 2; i++)
            {
                Vector3 vector;
                Mesh mesh;
                if (i == 0)
                {
                    vector = new Vector3(0f, 0f, -1f);
                    if (def.size.z > 3)
                        mesh = doorOne;
                    else if (def.size.x > 3)
                        mesh = doorOneSm;
                    else
                        mesh = doorOneXS;
                }
                else
                {
                    vector = new Vector3(0f, 0f, 1f);
                    if (def.size.z > 3)
                        mesh = doorTwo;
                    else
                        mesh = doorTwoSm;
                }
                Rot4 rotation = base.Rotation;
                rotation.Rotate(RotationDirection.Clockwise);
                vector = rotation.AsQuat * vector;
                Vector3 drawPos = DrawPos;
                drawPos.y = AltitudeLayer.MapDataOverlay.AltitudeFor();
                drawPos += vector * d;
                if (i == 0 || def.size.x > 3)
                {
                    if(def.size.z > 3)
                        Graphics.DrawMesh(mesh, drawPos, base.Rotation.AsQuat, torpedoBayDoor.MatSingle, 0);
                    else if (def.size.x > 3)
                        Graphics.DrawMesh(mesh, drawPos, base.Rotation.AsQuat, torpedoBayDoorSm.MatSingle, 0);
                    else
                        Graphics.DrawMesh(mesh, drawPos, base.Rotation.AsQuat, torpedoBayDoorXS.MatSingle, 0);
                }
            }
        }

        public override void Tick()
        {
            base.Tick();
            if(this.Map.GetComponent<ShipHeatMapComp>().InCombat)
            {
                if (ticksSinceOpen < TicksToOpenNow && this.TryGetComp<CompPowerTrader>().PowerOn)
                    ticksSinceOpen++;
            }
            else
            {
                if (ticksSinceOpen > 0)
                    ticksSinceOpen--;
            }
        }

        public Vector3 TorpedoTubePos()
        {
            Vector3 output;
            if (def.size.z > 3)
                output = TubePos[timesFired % 6];
            else if (def.size.x > 3)
                output = TubePosTwo[timesFired % 2];
            else
                output = new Vector3();
            timesFired++;
            return output;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!allTubesOnMap.ContainsKey(Map))
            {
                allTubesOnMap.Add(Map, new List<Building_ShipTurretTorpedo>());
            }
            allTubesOnMap[Map].Add(this);
        }

        public override void DeSpawn(DestroyMode mode)
        {
            allTubesOnMap[Map].Remove(this);
            base.DeSpawn(mode);
        }
    }
}
