using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Verse;

namespace RimWorld
{
	public class CompSpinalMount : ThingComp
	{

        public CompProperties_SpinalMount Props
        {
            get
            {
                return (CompProperties_SpinalMount)props;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (Props.receives && !Props.emits)
                SetColor(Props.color);
        }

        public void SetColor(Color color)
        {
            if (Props.receives)
            {
                IntVec3 vec;
                if (parent.Rotation.AsByte == 0)
                {
                    vec = new IntVec3(0, 0, -1);
                }
                else if (parent.Rotation.AsByte == 1)
                {
                    vec = new IntVec3(-1, 0, 0);
                }
                else if (parent.Rotation.AsByte == 2)
                {
                    vec = new IntVec3(0, 0, 1);
                }
                else
                {
                    vec = new IntVec3(1, 0, 0);
                }
                IntVec3 previousThingPos = parent.Position + vec;
                if (!Props.emits)
                    previousThingPos = parent.Position + vec;
                Thing amp = previousThingPos.GetFirstThingWithComp<CompSpinalMount>(parent.Map);
                if (amp != null && amp.Rotation == parent.Rotation && (amp.Position == previousThingPos || (amp.Position == previousThingPos + vec && amp.TryGetComp<CompSpinalMount>().Props.stackEnd)))
                    amp.TryGetComp<CompSpinalMount>().SetColor(color);
            }
            parent.DrawColor = color;
            parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlagDefOf.Buildings | MapMeshFlagDefOf.Things);
        }
    }
}