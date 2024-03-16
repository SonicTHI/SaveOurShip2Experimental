using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using SaveOurShip2;
using Verse.Noise;

namespace RimWorld
{
    public class Projectile_ExplosiveShipDebris : Projectile_ExplosiveShipCombat
    {
        //public Vector2 drawSize;
        public int index;
        protected override void Explode()
        {
            Map map = Map;
            base.Explode();
            if (Rand.Chance(0.3f))
            {
                PawnGenerationRequest req;
                Thing thing = null;
                if (index < 4) //debris
                {
                    if (index == 3 && Rand.Chance(0.2f)) //pod mech
                    {
                        if (Rand.Chance(0.3f))
                            req = new PawnGenerationRequest(DefDatabase<PawnKindDef>.GetNamed("Mech_Lancer"), Faction.OfMechanoids);
                        else if (Rand.Chance(0.1f))
                            req = new PawnGenerationRequest(DefDatabase<PawnKindDef>.GetNamed("SpaceCrewEVA"), Faction.OfAncients);
                        else
                            req = new PawnGenerationRequest(DefDatabase<PawnKindDef>.GetNamed("Mech_Scyther"), Faction.OfMechanoids);
                        thing = PawnGenerator.GeneratePawn(req);
                    }
                    else if (Rand.Chance(0.2f))
                        thing = ThingMaker.MakeThing(ThingDefOf.Plasteel);
                    else
                        thing = ThingMaker.MakeThing(ThingDefOf.ChunkSlagSteel);
                }
                else //asteroids
                {
                    if (index == 4 && Rand.Chance(0.1f)) //large possible critter
                    {
                        if (Rand.Chance(0.3f))
                            req = new PawnGenerationRequest(DefDatabase<PawnKindDef>.GetNamed("Cosmopillar"), Faction.OfInsects);
                        else
                            req = new PawnGenerationRequest(DefDatabase<PawnKindDef>.GetNamed("Stellapede"), Faction.OfInsects);
                        thing = PawnGenerator.GeneratePawn(req);
                    }
                    else if (Rand.Chance(0.2f))
                        thing = ThingMaker.MakeThing(ThingDefOf.Steel);
                    else
                        thing = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("ChunkGranite"));
                }
                if (thing != null)
                {
                    GenSpawn.Spawn(thing, Position, map);
                    if (!(thing is Pawn) && thing.def.stackLimit > 1)
                    {
                        thing.stackCount = Math.Min(Rand.RangeInclusive(3, 10), thing.def.stackLimit);
                    }
                }
            }
        }
        /*public override void Draw()
        {
            float num = this.ArcHeightFactor * GenMath.InverseParabola(this.DistanceCoveredFraction);
            Vector3 position = DrawPos + new Vector3(0f, 0f, 1f) * num;
            Graphics.DrawMesh(MeshPool.GridPlane(drawSize), position, this.ExactRotation, this.DrawMat, 0);
            base.Comps_PostDraw();
        }*/
    }
}
