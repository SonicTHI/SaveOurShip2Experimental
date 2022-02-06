using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace SaveOurShip2
{
    [HarmonyPatch(typeof(MapDrawer), "DrawMapMesh", null)]
    [StaticConstructorOnStartup]
    public class RenderPlanetBehindMap
    {
        static RenderTexture target = new RenderTexture(textureSize, textureSize, 16);
        static Texture2D virtualPhoto = new Texture2D(textureSize, textureSize, TextureFormat.RGB24, false);
        public static Material PlanetMaterial = MaterialPool.MatFrom(virtualPhoto);

        const int textureSize = 2048;
        const float altitude = 1100f;

        public static bool renderedThatAlready = false;
        static BiomeDef outerSpaceBiome = DefDatabase<BiomeDef>.GetNamed("OuterSpaceBiome");

        [HarmonyPrefix]
        public static void PreDraw()
        {
            Map map = Find.CurrentMap;

            // if we aren't in space, abort!
            if((renderedThatAlready && !ShipInteriorMod2.renderPlanet) || map.Biome!=outerSpaceBiome)
            {
                return;
            }
            //TODO replace this when interplanetary travel is ready
            //Find.PlaySettings.showWorldFeatures = false;
            RenderTexture oldTexture = Find.WorldCamera.targetTexture;
            RenderTexture oldSkyboxTexture = RimWorld.Planet.WorldCameraManager.WorldSkyboxCamera.targetTexture;

            Find.World.renderer.wantedMode = RimWorld.Planet.WorldRenderMode.Planet;
            Find.WorldCameraDriver.JumpTo(Find.CurrentMap.Tile);
            Find.WorldCameraDriver.altitude = altitude;
            Find.WorldCameraDriver.GetType()
                .GetField("desiredAltitude", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(Find.WorldCameraDriver, altitude);

            float num = (float)UI.screenWidth / (float)UI.screenHeight;

            Find.WorldCameraDriver.Update();
            Find.World.renderer.CheckActivateWorldCamera();
            Find.World.renderer.DrawWorldLayers();
            WorldRendererUtility.UpdateWorldShadersParams();
            //TODO replace this when interplanetary travel is ready
            /*List<WorldLayer> layers = (List<WorldLayer>)typeof(WorldRenderer).GetField("layers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(Find.World.renderer);
            foreach(WorldLayer layer in layers)
            {
                if (layer is WorldLayer_Stars)
                    layer.Render();
            }
            Find.PlaySettings.showWorldFeatures = false;*/
            RimWorld.Planet.WorldCameraManager.WorldSkyboxCamera.targetTexture = target;
            RimWorld.Planet.WorldCameraManager.WorldSkyboxCamera.aspect = num;
            RimWorld.Planet.WorldCameraManager.WorldSkyboxCamera.Render();

            Find.WorldCamera.targetTexture = target;
            Find.WorldCamera.aspect = num;
            Find.WorldCamera.Render();

            RenderTexture.active = target;
            virtualPhoto.ReadPixels(new Rect(0, 0, textureSize, textureSize), 0, 0);
            virtualPhoto.Apply();
            RenderTexture.active = null;

            Find.WorldCamera.targetTexture = oldTexture;
            RimWorld.Planet.WorldCameraManager.WorldSkyboxCamera.targetTexture = oldSkyboxTexture;
            Find.World.renderer.wantedMode = RimWorld.Planet.WorldRenderMode.None;
            Find.World.renderer.CheckActivateWorldCamera();

            if(!((List<WorldLayer>)typeof(WorldRenderer).GetField("layers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(Find.World.renderer)).FirstOrFallback().ShouldRegenerate)
                renderedThatAlready = true;
        }
    }
}
