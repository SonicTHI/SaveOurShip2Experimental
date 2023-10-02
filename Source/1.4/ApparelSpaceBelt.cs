using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld
{
    [StaticConstructorOnStartup]
    class ApparelSpaceBelt : Apparel
    {
        private static readonly Material BubbleMat = MaterialPool.MatFrom("Other/ShieldBubble", ShaderDatabase.Transparent,Color.cyan);

        public override void TickRare()
        {
            base.TickRare();
            if (this.Wearer==null || this.Wearer.health?.hediffSet?.GetFirstHediffOfDef(ResourceBank.HediffDefOf.SpaceBeltBubbleHediff) == null)
            {
                if (Wearer != null && Wearer.apparel != null)
                {
                    Wearer.apparel.Unlock(this);
                    Wearer.apparel.Remove(this);
                }
                this.Destroy();
            }
        }

        public override void DrawWornExtras()
        {
                Vector3 drawPos = base.Wearer.Drawer.DrawPos;
                drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
                float angle = Rand.Range(0, 360);
                Vector3 s = new Vector3(1.25f, 1f, 1.25f);
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(drawPos, Quaternion.AngleAxis(angle, Vector3.up), s);
                Graphics.DrawMesh(MeshPool.plane10, matrix, BubbleMat, 0);
            if (base.Wearer.HashOffsetTicks() % 50 == 0)
                TickRare(); //Really shouldn't be ticking from here. But whatever.
        }
    }
}
