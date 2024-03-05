using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimWorld
{
    //dep
    /*class Bullet_HologramLaser : Bullet
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            base.Impact(hitThing);
            ShipCombatLaserMote obj = (ShipCombatLaserMote)(object)ThingMaker.MakeThing(ThingDef.Named("ShipCombatLaserMote"));
            obj.origin = this.origin;
            if (hitThing != null)
                obj.destination = hitThing.DrawPos;
            else
                obj.destination = this.DrawPos;
            obj.color = ((Pawn)this.launcher).health.hediffSet.GetFirstHediff<HediffPawnIsHologram>().consciousnessSource.TryGetComp<CompBuildingConsciousness>().HologramColor;
            obj.tiny = true;
            obj.Attach(hitThing);
            if (hitThing != null)
                GenSpawn.Spawn(obj, hitThing.Position, this.launcher.Map, 0);
            else
                GenSpawn.Spawn(obj, this.Position, this.launcher.Map, 0);
        }
    }*/
}
