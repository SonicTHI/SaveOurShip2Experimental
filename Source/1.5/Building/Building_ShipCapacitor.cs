using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld
{
    [StaticConstructorOnStartup]
    class Building_ShipCapacitor : Building
    {
        private static Graphic barGraphic = GraphicDatabase.Get(typeof(Graphic_Multi), "Things/Building/Ship/CapacitorBar", ShaderDatabase.Cutout, new Vector2(3, 5), Color.white, Color.white);
        private static Graphic barGraphicMini = GraphicDatabase.Get(typeof(Graphic_Multi), "Things/Building/Ship/CapacitorSmallBar", ShaderDatabase.Cutout, new Vector2(3, 3), Color.white, Color.white);

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
            Color barColor;
            if (this.TryGetComp<CompPowerBattery>().StoredEnergyPct < 0.25f)
                barColor = new Color(0.25f+this.TryGetComp<CompPowerBattery>().StoredEnergyPct * 3, 0, 0);
            else
            {
                float angle = (this.TryGetComp<CompPowerBattery>().StoredEnergyPct - 0.25f) * 2 * Mathf.PI / 3;
                barColor = new Color(Mathf.Cos(angle), Mathf.Sin(angle), 0);
            }
            if(def.size.x>1)
                barGraphic.GetColoredVersion(ShaderDatabase.Cutout, barColor, barColor).Draw(new Vector3(drawLoc.x, drawLoc.y + 1f, drawLoc.z), Rotation, this);
            else
                barGraphicMini.GetColoredVersion(ShaderDatabase.Cutout, barColor, barColor).Draw(new Vector3(drawLoc.x, drawLoc.y + 1f, drawLoc.z), Rotation, this);
        }
    }
}
