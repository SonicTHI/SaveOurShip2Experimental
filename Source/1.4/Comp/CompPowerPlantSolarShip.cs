using RimworldMod;
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
        private static float FullSunPower = 300;
        private const float NightPower = 0.0f;
        private CompProperties_PowerPlantSolarShip PropsSolar
        {
            get { return props as CompProperties_PowerPlantSolarShip; }
        }
        UnfoldComponent compUnfold;
        public List<IntVec3> unfoldTo;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            compUnfold = parent.TryGetComp<UnfoldComponent>();
            IntVec3 v = IntVec3.South.RotatedBy(parent.Rotation);
            unfoldTo = new List<IntVec3>
            {
                parent.Position + v,
                parent.Position + v * 2,
                parent.Position + v * 3
            };
            if (parent.Map.IsSpace())
                FullSunPower = 600;
            else
                FullSunPower = 300;
        }
        protected override float DesiredPowerOutput
        {
            get
            {
                float desire = Mathf.Lerp(NightPower * PropsSolar.bonusPower, FullSunPower * PropsSolar.bonusPower, parent.Map.skyManager.CurSkyGlow) * RoofedPowerOutputFactor;

                if (unfoldTo.Any(s => s.Impassable(parent.Map) || (s.GetRoom(parent.Map)?.IsDoorway ?? false)))
                {
                    desire = 0.0f;
                }

                if (compUnfold != null)
                {
                    if (Mathf.Approximately(desire, 0.0f))
                    {
                        compUnfold.Target = 0.0f;
                    }
                    else
                    {
                        compUnfold.Target = 1.0f;
                        if (!compUnfold.IsAtTarget)
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
                foreach (IntVec3 c in unfoldTo)
                {
                    num1++;
                    if (parent.Map.roofGrid.Roofed(c))
                        num2++;
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
