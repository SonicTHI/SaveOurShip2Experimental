using RimworldMod;
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
    class Building_SpaceCrib : Building_Bed
    {
        static Graphic overlayGraphic = GraphicDatabase.Get(typeof(Graphic_Single), "Things/Building/Ship/SpaceCrib", ShaderDatabase.Cutout, new Vector2(1, 1), Color.white, Color.white);

        public ShipHeatMapComp mapComp;
        public bool iAmClosed = false;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.mapComp = this.Map.GetComponent<ShipHeatMapComp>();
        }
        public override void TickRare()
        {
            base.TickRare();
            bool closed = false;
            if (!Map.IsSpace())
                return;

            if (ShipInteriorMod2.ExposedToOutside(Position.GetRoom(Map)))
                closed = true;
            else if (!mapComp.LifeSupports.Where(s => s.active).Any())
                closed = true;

            UpdateState(closed);
        }

        public void UpdateState(bool closed)
        {
            if (iAmClosed != closed)
            {
                iAmClosed = closed;
            }
        }

        public override void Draw()
        {
            base.Draw();
            if (iAmClosed)
                overlayGraphic.Draw(DrawPos+new Vector3(0, 6, 0), Rot4.South, this);
        }
    }
}
