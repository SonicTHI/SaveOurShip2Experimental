using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Verse
{
        public class Graphic_MultiOnOff : Graphic
        {
            private Material[] mats = new Material[8];

            private bool westFlipped;

            private bool eastFlipped;

            private float drawRotatedExtraAngleOffset;

            public string GraphicPath
            {
                get
                {
                    return this.path;
                }
            }

            public override Material MatSingle
            {
                get
                {
                    return this.MatSouth;
                }
            }

            public override Material MatWest
            {
                get
                {
                    return this.mats[3];
                }
            }

            public override Material MatSouth
            {
                get
                {
                    return this.mats[2];
                }
            }

            public override Material MatEast
            {
                get
                {
                    return this.mats[1];
                }
            }

            public override Material MatNorth
            {
                get
                {
                    return this.mats[0];
                }
            }

            public Material MatWestOff
            {
                get
                {
                    return this.mats[7];
                }
            }

            public Material MatSouthOff
            {
                get
                {
                    return this.mats[6];
                }
            }

            public Material MatEastOff
            {
                get
                {
                    return this.mats[5];
                }
            }

            public Material MatNorthOff
            {
                get
                {
                    return this.mats[4];
                }
            }

            public override bool WestFlipped
            {
                get
                {
                    return this.westFlipped;
                }
            }

            public override bool EastFlipped
            {
                get
                {
                    return this.eastFlipped;
                }
            }

            public override bool ShouldDrawRotated
            {
                get
                {
                    return (this.data == null || this.data.drawRotated) && (this.MatEast == this.MatNorth || this.MatWest == this.MatNorth);
                }
            }

            public override float DrawRotatedExtraAngleOffset
            {
                get
                {
                    return this.drawRotatedExtraAngleOffset;
                }
            }

            public override void Init(GraphicRequest req)
            {
                this.data = req.graphicData;
                this.path = req.path;
                this.color = req.color;
                this.colorTwo = req.colorTwo;
                this.drawSize = req.drawSize;
                Texture2D[] array = new Texture2D[this.mats.Length];
                array[0] = ContentFinder<Texture2D>.Get(req.path + "_north", false);
                array[1] = ContentFinder<Texture2D>.Get(req.path + "_east", false);
                array[2] = ContentFinder<Texture2D>.Get(req.path + "_south", false);
                array[3] = ContentFinder<Texture2D>.Get(req.path + "_west", false);
                array[4] = ContentFinder<Texture2D>.Get(req.path + "_northoff", false);
                array[5] = ContentFinder<Texture2D>.Get(req.path + "_eastoff", false);
                array[6] = ContentFinder<Texture2D>.Get(req.path + "_southoff", false);
                array[7] = ContentFinder<Texture2D>.Get(req.path + "_westoff", false);
                if (array[0] == null)
                {
                    if (array[2] != null)
                    {
                        array[0] = array[2];
                        this.drawRotatedExtraAngleOffset = 180f;
                    }
                    else if (array[1] != null)
                    {
                        array[0] = array[1];
                        this.drawRotatedExtraAngleOffset = -90f;
                    }
                    else
                    {
                        if (!(array[3] != null))
                        {
                            Log.Error("Failed to find any texture while constructing " + this.ToStringSafe<Graphic_MultiOnOff>() + ". Filenames have changed; if you are converting an old mod, recommend renaming textures from *_back to *_north, *_side to *_east, and *_front to *_south.", false);
                            return;
                        }
                        array[0] = array[3];
                        this.drawRotatedExtraAngleOffset = 90f;
                    }
                }
                if (array[2] == null)
                {
                    array[2] = array[0];
                }
                if (array[1] == null)
                {
                    if (array[3] != null)
                    {
                        array[1] = array[3];
                        this.eastFlipped = base.DataAllowsFlip;
                    }
                    else
                    {
                        array[1] = array[0];
                    }
                }
                if (array[3] == null)
                {
                    if (array[1] != null)
                    {
                        array[3] = array[1];
                        this.westFlipped = base.DataAllowsFlip;
                    }
                    else
                    {
                        array[3] = array[0];
                    }
                }
                if (array[5] == null)
                {
                    if (array[7] != null)
                    {
                        array[5] = array[7];
                        this.eastFlipped = base.DataAllowsFlip;
                    }
                    else
                    {
                        array[5] = array[4];
                    }
                }
                if (array[7] == null)
                {
                    if (array[5] != null)
                    {
                        array[7] = array[5];
                        this.westFlipped = base.DataAllowsFlip;
                    }
                    else
                    {
                        array[7] = array[4];
                    }
                }
                for (int i = 0; i < this.mats.Length; i++)
                {
                    MaterialRequest req2 = default(MaterialRequest);
                    req2.mainTex = array[i];
                    req2.shader = req.shader;
                    req2.color = this.color;
                    req2.colorTwo = this.colorTwo;
                    req2.shaderParameters = req.shaderParameters;
                    this.mats[i] = MaterialPool.MatFrom(req2);
                }
            }

            public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
            {
                return GraphicDatabase.Get<Graphic_Multi>(this.path, newShader, this.drawSize, newColor, newColorTwo, this.data);
            }

            public override string ToString()
            {
                return string.Concat(new object[]
                {
                "Multi(initPath=",
                this.path,
                ", color=",
                this.color,
                ", colorTwo=",
                this.colorTwo,
                ")"
                });
            }

            public override int GetHashCode()
            {
                int seed = 0;
                seed = Gen.HashCombine<string>(seed, this.path);
                seed = Gen.HashCombineStruct<Color>(seed, this.color);
                return Gen.HashCombineStruct<Color>(seed, this.colorTwo);
            }

            public override Material MatAt(Rot4 rot, Thing thing = null)
            {
                if ((thing is Building_CryptosleepCasket && ((ThingOwner)typeof(Building_CryptosleepCasket).GetField("innerContainer", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(thing)).Count > 0) || (thing.TryGetComp<CompPowerTrader>() != null && thing.TryGetComp<CompPowerTrader>().PowerNet!=null && thing.TryGetComp<CompPowerTrader>().PowerOn))
                {
                    switch (rot.AsInt)
                    {
                        case 0:
                            return this.MatNorth;
                        case 1:
                            return this.MatEast;
                        case 2:
                            return this.MatSouth;
                        case 3:
                            return this.MatWest;
                        default:
                            return BaseContent.BadMat;
                    }
                }
                else
                {
                    switch (rot.AsInt)
                    {
                        case 0:
                            return this.MatNorthOff;
                        case 1:
                            return this.MatEastOff;
                        case 2:
                            return this.MatSouthOff;
                        case 3:
                            return this.MatWestOff;
                        default:
                            return BaseContent.BadMat;
                    }
                }
            }
        }
    }
