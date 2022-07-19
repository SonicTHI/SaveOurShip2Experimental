using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld
{
    [StaticConstructorOnStartup]
    public class CompEngineTrail : ThingComp
    {
        CellRect rectToKill;
        public bool active = false;
        private static Graphic trailGraphic = GraphicDatabase.Get(typeof(Graphic_Multi), "Things/Building/Ship/Ship_Engine_Trail_Double", ShaderDatabase.MoteGlow, new Vector2(7, 16.5f), Color.white, Color.white);
        private static Graphic trailGraphicSingle = GraphicDatabase.Get(typeof(Graphic_Multi), "Things/Building/Ship/Ship_Engine_Trail_Single", ShaderDatabase.MoteGlow, new Vector2(7, 16.5f), Color.white, Color.white);
        private static Graphic trailGraphicLarge = GraphicDatabase.Get(typeof(Graphic_Multi), "Things/Building/Ship/NuclearEngineTrail", ShaderDatabase.MoteGlow, new Vector2(7, 26.5f), Color.white, Color.white);
        private static Vector3[] offset = { new Vector3(0, 0, -5.5f), new Vector3(-5.5f, 0, 0), new Vector3(0, 0, 5.5f), new Vector3(5.5f, 0, 0) };
        private static Vector3[] offsetL = { new Vector3(0, 0, -11f), new Vector3(-11f, 0, 0), new Vector3(0, 0, 11f), new Vector3(11f, 0, 0) };
        public static IntVec2[] killOffset = { new IntVec2(0, -6), new IntVec2(-6, 0), new IntVec2(0, 4), new IntVec2(4, 0) };
        public static IntVec2[] killOffsetL = { new IntVec2(0, -13), new IntVec2(-13, 0), new IntVec2(0, 7), new IntVec2(7, 0) };
		public CompProperties_EngineTrail Props
        {
            get { return props as CompProperties_EngineTrail; }
        }
        public override void PostDraw()
        {
            base.PostDraw();
            if (active)
            {
                if (parent.def.size.x > 3)
                {
                    trailGraphicLarge.drawSize = new Vector2(7, 26.5f + 0.5f * Mathf.Cos(Find.TickManager.TicksGame / 4));
                    trailGraphicLarge.Draw(new Vector3(parent.DrawPos.x + offsetL[parent.Rotation.AsInt].x, parent.DrawPos.y + 1f, parent.DrawPos.z + offsetL[parent.Rotation.AsInt].z), parent.Rotation, parent);
                }
                else
                {
                    Vector2 drawSize = new Vector2(7, 15.5f + 0.5f * Mathf.Cos(Find.TickManager.TicksGame / 4));
                    trailGraphic.drawSize = drawSize;
                    trailGraphicSingle.drawSize = drawSize;
                    if (parent.def.size.x == 3)
                        trailGraphic.Draw(new Vector3(parent.DrawPos.x + offset[parent.Rotation.AsInt].x, parent.DrawPos.y + 1f, parent.DrawPos.z + offset[parent.Rotation.AsInt].z), parent.Rotation, parent);
                    else
                        trailGraphicSingle.Draw(new Vector3(parent.DrawPos.x + offset[parent.Rotation.AsInt].x, parent.DrawPos.y + 1f, parent.DrawPos.z + offset[parent.Rotation.AsInt].z), parent.Rotation, parent);
                }
            }
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (parent.def.size.x > 3)
                rectToKill = parent.OccupiedRect().MovedBy(killOffsetL[parent.Rotation.AsInt]).ExpandedBy(2);
            else
                rectToKill = parent.OccupiedRect().MovedBy(killOffset[parent.Rotation.AsInt]).ExpandedBy(1);
            if (parent.Rotation.IsHorizontal)
                rectToKill.Width = rectToKill.Width * 2 - 3;
            else
                rectToKill.Height = rectToKill.Height * 2 - 3;
        }
        public override void CompTick()
        {
            base.CompTick();
            if (active)
            {
                foreach (IntVec3 cell in rectToKill)
                {
                    List<Thing> toBurn = new List<Thing>();
                    foreach (Thing t in cell.GetThingList(parent.Map))
                    {
                        if (t.def.useHitPoints)
                            toBurn.Add(t);
                    }
                    foreach (Thing t in toBurn)
                    {
                        if (t.def.altitudeLayer != AltitudeLayer.Terrain) { }
                            t.TakeDamage(new DamageInfo(DamageDefOf.Bomb, 100));
                    }
                }
            }
        }
    }
}
