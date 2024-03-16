using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld
{
    public class OverlayComponent : ThingComp
    {

        public CompProperties_Overlay Props
        {
            get
            {
                return (CompProperties_Overlay)this.props;
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();
            Matrix4x4 matrix = new Matrix4x4();
            matrix.SetTRS(this.parent.DrawPos + Altitudes.AltIncVect, this.parent.Rotation.AsQuat, Props.size);
            Graphics.DrawMesh(MeshPool.plane10, matrix, Props.overlayGraphic, 0);
        }
    }
}
