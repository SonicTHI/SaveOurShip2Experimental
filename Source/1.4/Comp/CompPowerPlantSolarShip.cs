using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimWorld
{
    [StaticConstructorOnStartup]
    public class CompPowerPlantSolarShip : CompPowerPlant
    {
        private static readonly Vector2 BarSize = new Vector2(0.3f, 0.07f);
        private static readonly Material PowerPlantSolarBarFilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.5f, 0.475f, 0.1f), false);
        private static readonly Material PowerPlantSolarBarUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.15f, 0.15f, 0.15f), false);
        private static float FullSunPower
        {
            get { return 800f; }
        }
        private const float NightPower = 0.0f;
        private CompProperties_PowerPlantSolarShip PropsSolar
        {
            get { return props as CompProperties_PowerPlantSolarShip; }
        }

        public override float DesiredPowerOutput
        {
            get
            {
                float desire = Mathf.Lerp(NightPower * PropsSolar.bonusPower, FullSunPower * PropsSolar.bonusPower, this.parent.Map.skyManager.CurSkyGlow) * this.RoofedPowerOutputFactor;
                IntVec3 intVec3_2 = this.parent.Position + IntVec3.South.RotatedBy(this.parent.Rotation);
                IntVec3 intVec3_3 = this.parent.Position + (IntVec3.South.RotatedBy(this.parent.Rotation) * 2);
                IntVec3 intVec3_4 = this.parent.Position + (IntVec3.South.RotatedBy(this.parent.Rotation) * 3);

                if (intVec3_4.Impassable(this.parent.Map) || intVec3_3.Impassable(this.parent.Map) || intVec3_2.Impassable(this.parent.Map))
                {
                    desire = 0.0f;
                }

                UnfoldComponent comp = this.parent.GetComp<UnfoldComponent>();
                if(comp != null)
                {
                    if (Mathf.Approximately(desire, 0.0f))
                    {
                        comp.Target = 0.0f;
                    }
                    else
                    {
                        comp.Target = 1.0f;
                        if (!comp.IsAtTarget)
                        {
                            desire = 0.0f;
                        }
                    }
                }

                return desire;
            }
        }

        private float RoofedPowerOutputFactor
        {
            get
            {
                int num1 = 0;
                int num2 = 0;
                List<IntVec3> rects = new List<IntVec3>();
                rects.Add(this.parent.Position + IntVec3.South.RotatedBy(this.parent.Rotation));
                rects.Add(this.parent.Position + (IntVec3.South.RotatedBy(this.parent.Rotation) * 2));
                rects.Add(this.parent.Position + (IntVec3.South.RotatedBy(this.parent.Rotation) * 3));
                foreach (IntVec3 c in rects)
                {
                    ++num1;
                    if (this.parent.Map.roofGrid.Roofed(c))
                        ++num2;
                }
                return (float)(num1 - num2) / (float)num1;
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();
            GenDraw.FillableBarRequest r = new GenDraw.FillableBarRequest();
            r.center = this.parent.DrawPos + Vector3.up * 0.1f;
            r.size = CompPowerPlantSolarShip.BarSize;
            r.fillPercent = this.PowerOutput / this.PropsSolar.bonusPower / FullSunPower;
            r.filledMat = CompPowerPlantSolarShip.PowerPlantSolarBarFilledMat;
            r.unfilledMat = CompPowerPlantSolarShip.PowerPlantSolarBarUnfilledMat;
            r.margin = 0.15f;
            Rot4 rotation = this.parent.Rotation;
            rotation.Rotate(RotationDirection.Clockwise);
            r.rotation = rotation;
            GenDraw.DrawFillableBar(r);
        }
    }
}
