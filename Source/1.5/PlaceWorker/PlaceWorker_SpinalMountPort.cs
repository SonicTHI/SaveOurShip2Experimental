using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld
{
    [StaticConstructorOnStartup]
    public class PlaceWorker_SpinalMountPort : PlaceWorker
    {
        private static readonly Material FuelingPortCellMaterial = MaterialPool.MatFrom("UI/Overlays/FuelingPort", ShaderDatabase.Transparent);

        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            DrawFuelingPortCell(center, rot, def);
        }

        public static void DrawFuelingPortCell(IntVec3 center, Rot4 rot, ThingDef def)
        {
            Map currentMap = Find.CurrentMap;
            IntVec3 pos;

            if (rot.AsByte == 0)
                pos = center + new IntVec3(0, 0, 1 + (def.size.z / 2));
            else if (rot.AsByte == 1)
                pos = center + new IntVec3(1 + (def.size.z / 2), 0, 0);
            else if (rot.AsByte == 2)
                pos = center + new IntVec3(0, 0, -1 - (def.size.z / 2));
            else
                pos = center + new IntVec3(-1 - (def.size.z / 2), 0, 0);
            if (pos.Standable(currentMap))
            {
                Vector3 position = pos.ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays);
                Graphics.DrawMesh(MeshPool.plane10, position, Quaternion.identity, FuelingPortCellMaterial, 0);
            }
        }
    }
}
