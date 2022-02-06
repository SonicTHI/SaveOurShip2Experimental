using System;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Threading.Tasks;
using RimworldMod;
using UnityEngine;
using Verse;
using RimworldMod.VacuumIsNotFun;

namespace SaveOurShip2
{

    [HarmonyPatch(typeof(SkyManager), "SkyManagerUpdate")]
    public class FixLightingColors
    {

        public static void Postfix()
        {
            if (!MapChangeHelper.MapIsSpace) return;

            MatBases.LightOverlay.color = new Color(1.0f, 1.0f, 1.0f);
        }

    }

    [HarmonyPatch(typeof(Section), MethodType.Constructor, typeof(IntVec3), typeof(Map))]
    [StaticConstructorOnStartup]
    public class SectionConstructorPatch
    {

        private static Type SunShadowsType;
        private static Type TerrainType;

        static SectionConstructorPatch()
        {
            SunShadowsType = AccessTools.TypeByName("SectionLayer_SunShadows");
            TerrainType = AccessTools.TypeByName("SectionLayer_Terrain");
        }

        public static void Postfix(Map map, Section __instance, List<SectionLayer> ___layers)
        {
            if (!map.IsSpace()) return;

            // Kill shadows
            ___layers.RemoveAll(layer => SunShadowsType.IsInstanceOfType(layer));

            // Get and store terrain layer for recalculation
            var terrain = ___layers.Find(layer => TerrainType.IsInstanceOfType(layer));
            SectionThreadManager.AddSection(map, __instance, terrain);
        }

    }

    // Since this targets an internal class, it's manually patched in the ShipInteriorMod2 constructor
    public class SectionRegenerateHelper
    {

        public static void Postfix(SectionLayer __instance, Section ___section)
        {
            if (!___section.map.IsSpace()) return;

            MeshRecalculateHelper.RecalculatePlanetLayer(__instance);
        }

    }

    // This helper class contains everything related to recalculating planet meshes
    public class MeshRecalculateHelper
    {

        public static List<Task> Tasks = new List<Task>();
        public static List<SectionLayer> LayersToDraw = new List<SectionLayer>();

        public static void RecalculatePlanetLayer(SectionLayer instance)
        {
            var mesh = instance.GetSubMesh(RenderPlanetBehindMap.PlanetMaterial);
            Tasks.Add(Task.Factory.StartNew(() => RecalculateMesh(mesh)));
            LayersToDraw.Add(instance);
        }

        private static void RecalculateMesh(object info)
        {
            if (!(info is LayerSubMesh mesh))
            {
                Log.Error("Save Our Ship tried to start a calculate thread with an incorrect info object type");
                return;
            }

            lock (mesh)
            {
                mesh.finalized = false;
                mesh.Clear(MeshParts.UVs);
                for (var i = 0; i < mesh.verts.Count; i++)
                {
                    var xdiff = mesh.verts[i].x - SectionThreadManager.Center.x;
                    var xfromEdge = xdiff + SectionThreadManager.CellsWide / 2f;
                    var zdiff = mesh.verts[i].z - SectionThreadManager.Center.z;
                    var zfromEdge = zdiff + SectionThreadManager.CellsHigh / 2f;

                    mesh.uvs.Add(new Vector3(xfromEdge / SectionThreadManager.CellsWide,
                        zfromEdge / SectionThreadManager.CellsHigh, 0.0f));
                }

                mesh.FinalizeMesh(MeshParts.UVs);
            }
        }
    }

    [HarmonyPatch(typeof(MapInterface), "Notify_SwitchedMap")]
    public class MapChangeHelper
    {

        public static bool MapIsSpace;

        public static void Postfix()
        {
            // Make sure we're on a map and not loading (causes issues if we are)
            if (Find.CurrentMap == null || Scribe.mode != LoadSaveMode.Inactive) return;

            MapIsSpace = Find.CurrentMap.IsSpace();
        }

    }

    [HarmonyPatch(typeof(Game), "LoadGame")]
    public class GameLoadHelper
    {
        public static void Postfix()
        {
            // We need to execute the change notification exactly once on load after the game is fully loaded, which is
            // done here, after all loading is completed
            MapChangeHelper.Postfix();
        }

    }

    [HarmonyPatch(typeof(Game), "FinalizeInit")]
    public class FinalizeInitHelper
    {

        public static void Postfix()
        {
            // Update the camera driver and camera on init - faster than using the game's methods by far, and much
            // faster than using Unity GetComponents every frame
            SectionThreadManager.Driver = Find.CameraDriver;
            SectionThreadManager.GameCamera = Find.CameraDriver.GetComponent<Camera>();

        }

    }

    [HarmonyPatch(typeof(Game), "UpdatePlay")]
    public class SectionThreadManager
    {

        public static CameraDriver Driver;
        public static Camera GameCamera;
        public static Vector3 Center;
        public static float CellsHigh;
        public static float CellsWide;

        public static Dictionary<Map, Dictionary<Section, SectionLayer>> MapSections =
            new Dictionary<Map, Dictionary<Section, SectionLayer>>();
        private static Vector3 lastCameraPosition = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);

        public static void AddSection(Map map, Section section, SectionLayer layer)
        {
            Dictionary<Section, SectionLayer> sections;
            if (!MapSections.TryGetValue(map, out sections))
            {
                sections = new Dictionary<Section, SectionLayer>();
                MapSections.Add(map, sections);
            }

            sections.Add(section, layer);
        }

        // Thread spawner
        public static void Prefix()
        {
            if (!MapChangeHelper.MapIsSpace || !MapSections.ContainsKey(Find.CurrentMap)) return;

            // Calculate all the various fields we're going to be using this call before we start making threads
            Center = GameCamera.transform.position;
            var ratio = (float)UI.screenWidth / UI.screenHeight;
            CellsHigh = UI.screenHeight / Find.CameraDriver.CellSizePixels;
            CellsWide = CellsHigh * ratio;

            // Camera hasn't moved, no need to update
            if ((lastCameraPosition - Center).magnitude < 1e-4) return;
            lastCameraPosition = Center;
            var sections = MapSections[Find.CurrentMap];

            var visibleRect = Driver.CurrentViewRect;
            foreach (var entry in sections)
            {
                if (!visibleRect.Overlaps(entry.Key.CellRect)) continue;

                MeshRecalculateHelper.RecalculatePlanetLayer(entry.Value);
            }
        }

        // The real thread waiter
        public static void Postfix()
        {
            if (!MeshRecalculateHelper.Tasks.Any()) return;

            // Wait on threads to complete
            Task.WaitAll(MeshRecalculateHelper.Tasks.ToArray());
            MeshRecalculateHelper.Tasks.Clear();

            // Draw the layers since we stopped it previously - must be done on main thread to prevent crashes
            foreach (var layer in MeshRecalculateHelper.LayersToDraw)
            {
                var mesh = layer.GetSubMesh(RenderPlanetBehindMap.PlanetMaterial);
                if (!mesh.finalized || mesh.disabled) continue;

                Graphics.DrawMesh(mesh.mesh, Vector3.zero, Quaternion.identity, mesh.material, 0);
            }
            MeshRecalculateHelper.LayersToDraw.Clear();
        }
    }
}
