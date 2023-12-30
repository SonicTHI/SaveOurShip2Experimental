using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
using SaveOurShip2;

namespace RimWorld
{
    public class CompShipLight : ThingComp
    {
        Building shipPart;
        public CompSoShipLight lightComp;
        int lightDirections = 0;
        List<CompGlower> glowers = new List<CompGlower>();
        public bool sunLight;
        public int Rot;

        ShipHeatMapComp mapCompInt;
        CompPowerTrader powerCompInt;

        public CompPowerTrader PowerComp
        {
            get
            {
                if (powerCompInt == null)
                    powerCompInt = parent.TryGetComp<CompPowerTrader>();
                return powerCompInt;
            }
        }

        public ShipHeatMapComp MapComp
        {
            get
            {
                if (mapCompInt == null)
                    mapCompInt = parent.Map.GetComponent<ShipHeatMapComp>();
                return mapCompInt;
            }
        }

        public CompProperties_ShipLight Props
        {
            get
            {
                return (CompProperties_ShipLight)props;
            }
        }
        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            RemoveGlowers(map);
        }
        public override void PostPrintOnto(SectionLayer layer)
        {
            base.PostPrintOnto(layer);

            if (MapComp != null && MapComp.loaded) //If the region isn't dirty because it's being loaded, but because someone built something nearby
                UpdateLight(lightComp.lightColor, false, false);

            DrawLight(new Rot4(Rot), layer);
            /*if (lightDirections != 0)
            {
                if ((lightDirections & 1) == 1)
                    DrawLight(Rot4.South, layer);
                if ((lightDirections & 2) == 2)
                    DrawLight(Rot4.East, layer);
                if ((lightDirections & 4) == 4)
                    DrawLight(Rot4.West, layer);
                if ((lightDirections & 8) == 8)
                    DrawLight(Rot4.North, layer);
            }*/
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look<Building>(ref shipPart, "shipPart");
            //Scribe_Values.Look<int>(ref lightDirections, "lightDirs");
            Scribe_Values.Look<int>(ref Rot, "Rot");
            Scribe_Values.Look<bool>(ref sunLight, "sun");
        }
        void DrawLight(Rot4 rot, SectionLayer layer)
        {
            Material mat;
            if (sunLight)
                mat = Props.sunLightGraphic.Graphic.GetColoredVersion(ShaderDatabase.Cutout, lightComp.lightColor.ToColor, Color.white).MatAt(rot);
            else
                mat = Props.lightGraphic.Graphic.GetColoredVersion(ShaderDatabase.Cutout, lightComp.lightColor.ToColor, Color.white).MatAt(rot);
            Printer_Plane.PrintPlane(layer, this.parent.DrawPos + Altitudes.AltIncVect, Vector2.one*1.125f, mat);
        }
        void UpdateLight(ColorInt color, bool onLoad=false, bool dirty=true)
        {
            if (ShipInteriorMod2.AirlockBugFlag)
                return;

            if (!onLoad)
            {
                //lightDirections = 0;
                RemoveGlowers(parent.Map);
            }

            if (PowerComp == null || PowerComp.PowerOn)
            {
                IntVec3 pos = new IntVec3();
                if (Rot == 0)
                    pos = parent.Position + new IntVec3(0, 0, 1);
                else if (Rot == 1)
                    pos = parent.Position + new IntVec3(1, 0, 0);
                else if (Rot == 2)
                    pos = parent.Position + new IntVec3(0, 0, -1);
                else if (Rot == 3)
                    pos = parent.Position + new IntVec3(-1, 0, 0);
                AddGlower(pos, color, sunLight);
                /*
                Map map = parent.Map;
                IntVec3 pos = parent.Position + new IntVec3(0, 0, -1);
                if (!onLoad)
                {
                    if (CanLight(pos, map))
                    {
                        lightDirections += (byte)1;
                        AddGlower(pos, color, sunLight);
                    }
                }
                else if((lightDirections & 1) == 1)
                    AddGlower(pos, color, sunLight);
                pos += new IntVec3(1, 0, 1);
                if (!onLoad)
                {
                    if (CanLight(pos, map))
                    {
                        lightDirections += (byte)2;
                        AddGlower(pos, color, sunLight);
                    }
                }
                else if ((lightDirections & 2) == 2)
                    AddGlower(pos, color, sunLight);
                pos += new IntVec3(-2, 0, 0);
                if (!onLoad)
                {
                    if (CanLight(pos, map))
                    {
                        lightDirections += (byte)4;
                        AddGlower(pos, color, sunLight);
                    }
                }
                else if ((lightDirections & 4) == 4)
                    AddGlower(pos, color, sunLight);
                pos += new IntVec3(1, 0, 1);
                if (!onLoad)
                {
                    if (CanLight(pos, map))
                    {
                        lightDirections += (byte)8;
                        AddGlower(pos, color, sunLight);
                    }
                }
                else if ((lightDirections & 8) == 8)
                    AddGlower(pos, color, sunLight);*/
            }

            if (dirty && parent.Spawned)
                parent.DirtyMapMesh(parent.Map);
        }
        bool CanLight(IntVec3 pos, Map map)
        {
            Building edifice = pos.GetEdifice(map);
            return (edifice == null || (!(edifice is Building_Door) && edifice.def.passability != Traversability.Impassable));
        }
        public void UpdateColors(ColorInt color)
        {
            if (color != lightComp.lightColor)
            {
                lightComp.lightColor = color;
                UpdateLight(color);
            }
        }
        public ColorInt GlowerColor()
        {
            if (glowers.Count > 0)
                return glowers.First().GlowColor;
            return ColorIntUtility.AsColorInt(Color.white);

        }
        void AddGlower(IntVec3 pos, ColorInt color, bool sun)
        {
            ThingWithComps dummy = ThingMaker.MakeThing(ResourceBank.ThingDefOf.SoS2DummyObject) as ThingWithComps;
            GenSpawn.Spawn(dummy, pos, parent.Map);
            CompSoSGlower glower = new CompSoSGlower();
            glower.parent = dummy;
            CompProperties_Glower glowerProps = new CompProperties_Glower();
            glowerProps.glowColor = color;
            glowerProps.glowRadius = Props.lightRadius;
            glowerProps.colorPickerEnabled = true;
            glowerProps.darklightToggle = true;
            glowerProps.overlightRadius = sun ? Props.overlightRadius : 0;
            glower.props = glowerProps;
            glower.Initialize(glowerProps);
            glowers.Add(glower);
            if (PowerComp != null && PowerComp.PowerOn)
            {
                parent.Map.glowGrid.RegisterGlower(glower);
            }
            dummy.DeSpawn();
        }
        void RemoveGlowers(Map map)
        {
            foreach (CompGlower glower in glowers)
                map.glowGrid.DeRegisterGlower(glower);
            glowers = new List<CompGlower>();
        }
        public override void ReceiveCompSignal(string signal)
        {
            if (!ShipInteriorMod2.AirlockBugFlag && lightComp!=null && MapComp.loaded)
            {
                switch (signal)
                {
                    case "PowerTurnedOn":
                    case "PowerTurnedOff":
                    case "ScheduledOn":
                    case "ScheduledOff":
                        UpdateLight(lightComp.lightColor);
                        break;
                }
            }
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo giz in base.CompGetGizmosExtra())
                yield return giz;
            foreach (CompGlower glower in glowers)
            {
                foreach (Gizmo giz in glower.CompGetGizmosExtra())
                    yield return giz;
            }
        }
        public void SetupLighting(CompSoShipLight comp, bool sun, int rot)
        {
            lightComp = comp;
            shipPart = (Building)comp.parent;
            sunLight = sun;
            Rot = rot;
            UpdateLight(lightComp.lightColor);
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            if(respawningAfterLoad)
            {
                lightComp = shipPart.TryGetComp<CompSoShipLight>();
                UpdateLight(lightComp.lightColor, true);
            }
        }
        public override void CompTickRare()
        {
            base.CompTickRare();
            if(lightComp!=null && lightComp.discoMode)
            {
                float hue = (parent.HashOffset() + Find.TickManager.TicksGame) / 4000f;
                hue = hue - Mathf.Floor(hue);
                lightComp.lightColor.SetHueSaturation(hue, 1);
                UpdateLight(lightComp.lightColor);
            }
        }
    }
}