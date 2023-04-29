using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Verse
{
    public class Graphic_SingleOnOff : Graphic
    {
        protected Material mat;
        protected Material matOff;

        public override Material MatSingle
        {
            get
            {
                return this.mat;
            }
        }

        public Material MatSingleOff
        {
            get
            {
                return this.matOff;
            }
        }

        public override Material MatWest
        {
            get
            {
                return this.mat;
            }
        }

        public override Material MatSouth
        {
            get
            {
                return this.mat;
            }
        }

        public override Material MatEast
        {
            get
            {
                return this.mat;
            }
        }

        public override Material MatNorth
        {
            get
            {
                return this.mat;
            }
        }

        public override bool ShouldDrawRotated
        {
            get
            {
                return this.data == null || this.data.drawRotated;
            }
        }

        public override void Init(GraphicRequest req)
        {
            this.data = req.graphicData;
            this.path = req.path;
            this.color = req.color;
            this.colorTwo = req.colorTwo;
            this.drawSize = req.drawSize;
            MaterialRequest req2 = default(MaterialRequest);
            req2.mainTex = ContentFinder<Texture2D>.Get(req.path, true);
            req2.shader = req.shader;
            req2.color = this.color;
            req2.colorTwo = this.colorTwo;
            req2.renderQueue = req.renderQueue;
            req2.shaderParameters = req.shaderParameters;
            this.mat = MaterialPool.MatFrom(req2);

            MaterialRequest req3 = default(MaterialRequest);
            req3.mainTex = ContentFinder<Texture2D>.Get(req.path+"_off", true);
            req3.shader = req.shader;
            req3.color = this.color;
            req3.colorTwo = this.colorTwo;
            req3.renderQueue = req.renderQueue;
            req3.shaderParameters = req.shaderParameters;
            this.matOff = MaterialPool.MatFrom(req3);
        }

        public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
        {
            return GraphicDatabase.Get<Graphic_SingleOnOff>(this.path, newShader, this.drawSize, newColor, newColorTwo, this.data);
        }

        public override Material MatAt(Rot4 rot, Thing thing = null)
        {
            if (thing != null && thing is Building_SpaceCrib crib)
            {
                if (crib.iAmClosed)
                    return this.mat;
                return this.matOff;
            }
            if (thing.TryGetComp<CompPowerTrader>() == null || (thing.TryGetComp<CompPowerTrader>().PowerNet != null && thing.TryGetComp<CompPowerTrader>().PowerOn && (thing.TryGetComp<CompRefuelable>()==null || thing.TryGetComp<CompRefuelable>().Fuel>0)))
            {
                return this.mat;
            }
            return this.matOff;
        }

        public override string ToString()
        {
            return string.Concat(new object[]
            {
                "Single(path=",
                this.path,
                ", color=",
                this.color,
                ", colorTwo=",
                this.colorTwo,
                ")"
            });
        }
    }
}
