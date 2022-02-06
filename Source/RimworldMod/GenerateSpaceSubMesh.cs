using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace SaveOurShip2
{
    [HarmonyPatch(typeof(SectionLayer), "FinalizeMesh", null)]
    [StaticConstructorOnStartup]
    public static class GenerateSpaceSubMesh
    {
        public static TerrainDef spaceTerrain = TerrainDef.Named("EmptySpace");
        [HarmonyPrefix]
        public static bool GenerateMesh(SectionLayer __instance, Section ___section)
        {
            if (__instance.GetType().Name != "SectionLayer_Terrain")
                return true;

            bool foundSpace = false;
            foreach (IntVec3 cell in ___section.CellRect.Cells)
            {
                TerrainDef terrain1 = ___section.map.terrainGrid.TerrainAt(cell);
                if (terrain1 == spaceTerrain)
                {
                    foundSpace = true;
                    Printer_Mesh.PrintMesh(__instance, Matrix4x4.TRS(cell.ToVector3() + new Vector3(0.5f, 0f, 0.5f),Quaternion.identity,Vector3.one), MeshMakerPlanes.NewPlaneMesh(1f), RenderPlanetBehindMap.PlanetMaterial);
                }
            }
            if (!foundSpace)
            {
                for (int i = 0; i < __instance.subMeshes.Count; i++)
                {
                    if (__instance.subMeshes[i].material == RenderPlanetBehindMap.PlanetMaterial)
                    {
                        __instance.subMeshes.RemoveAt(i);
                    }
                }
            }
            return true;
        }
    }
}
