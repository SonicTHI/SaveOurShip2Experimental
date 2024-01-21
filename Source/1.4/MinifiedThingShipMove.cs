using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using SaveOurShip2;

namespace RimWorld
{
    class MinifiedThingShipMove : MinifiedThing
    {
        public Building shipRoot;
        public IntVec3 bottomLeftPos;
        public byte shipRotNum;
        public bool includeRock = false;
        public Map targetMap = null;
        public Faction fac = null;

        public override void Tick()
        {
            base.Tick();
            if (Find.Selector.SelectedObjects.Count > 1 || !Find.Selector.SelectedObjects.Contains(this))
            {
                if (InstallBlueprintUtility.ExistingBlueprintFor(this) != null)
                    ShipInteriorMod2.MoveShip(shipRoot, targetMap, InstallBlueprintUtility.ExistingBlueprintFor(this).Position - bottomLeftPos, fac, shipRotNum, includeRock);
                if (!Destroyed)
                    Destroy(DestroyMode.Vanish);
            }
        }

        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (Graphic is Graphic_Single)
            {
                Graphic.Draw(drawLoc, Rot4.North, this, 0f);
                return;
            }
            Graphic.Draw(drawLoc, Rot4.South, this, 0f);
        }

        public override string GetInspectString()
        {
            return TranslatorFormattedStringExtensions.Translate("ShipMoveDesc");
        }
    }
}
