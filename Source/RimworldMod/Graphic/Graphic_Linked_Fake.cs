using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Verse
{
    public class Graphic_Linked_Fake : Graphic
    {
        protected Graphic subGraphic;

        public virtual LinkDrawerType LinkerType
        {
            get
            {
                return LinkDrawerType.Basic;
            }
        }

        public override Material MatSingle
        {
            get
            {
                return MaterialAtlasPool.SubMaterialFromAtlas(this.subGraphic.MatSingle, LinkDirections.None);
            }
        }

        public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
        {
            return new Graphic_Linked(this.subGraphic.GetColoredVersion(newShader, newColor, newColorTwo))
            {
                data = this.data
            };
        }

        public override void Print(SectionLayer layer, Thing thing, float extraRotation)
        {
            Material mat = this.LinkedDrawMatFrom(thing, thing.Position);
            Printer_Plane.PrintPlane(layer, thing.TrueCenter(), new Vector2(1f, 1f), mat, 0f, false, null, null, 0.01f, 0f);
        }

        public Material MatSingleFor(Thing thing, Vector3 loc)
        {
            return this.LinkedDrawMatFrom(thing, loc.ToIntVec3());
        }

        protected Material LinkedDrawMatFrom(Thing parent, IntVec3 cell)
        {
            int num = 0;
            int num2 = 1;
            for (int i = 0; i < 4; i++)
            {
                IntVec3 c = cell + GenAdj.CardinalDirections[i];
                if (this.ShouldLinkWith(c, parent))
                {
                    num += num2;
                }
                num2 *= 2;
            }
            LinkDirections linkSet = (LinkDirections)num;
            return MaterialAtlasPool.SubMaterialFromAtlas(this.subGraphic.MatSingleFor(parent), linkSet);
        }

        public Graphic_Linked_Fake(Graphic subGraphic)
        {
            this.subGraphic = subGraphic;
        }

        public bool ShouldLinkWith(IntVec3 c, Thing parent)
        {
            if (!(parent is DetachedShipPart))
                return false;
            c.x = c.x - Mathf.RoundToInt(((DetachedShipPart)parent).drawOffset.x);
            c.z = c.z - Mathf.RoundToInt(((DetachedShipPart)parent).drawOffset.z);
            return c.x >= DetachedShipPart.drawMinVector.x && c.z >= DetachedShipPart.drawMinVector.z && c.x-DetachedShipPart.drawMinVector.x < DetachedShipPart.drawWreckage.GetLength(0) && c.z - DetachedShipPart.drawMinVector.z < DetachedShipPart.drawWreckage.GetLength(1) && DetachedShipPart.drawWreckage[c.x-DetachedShipPart.drawMinVector.x,c.z-DetachedShipPart.drawMinVector.z]==1;
        }

        public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
        {
            Mesh arg_4D_0 = this.MeshAt(rot);
            Quaternion quaternion = this.QuatFromRot(rot);
            if (extraRotation != 0f)
            {
                quaternion *= Quaternion.Euler(Vector3.up * extraRotation);
            }
            loc += this.DrawOffset(rot);
            Material material = this.MatSingleFor(thing, loc);
            Graphics.DrawMesh(arg_4D_0, loc, quaternion, material, 0);
            if (this.ShadowGraphic != null)
            {
                this.ShadowGraphic.DrawWorker(loc, rot, thingDef, thing, extraRotation);
            }
        }
    }
}