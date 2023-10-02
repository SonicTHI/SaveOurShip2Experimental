using RimWorld.Planet;
using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace RimWorld
{
    [StaticConstructorOnStartup]
    class ShipCombatManager
    {
        //obsolete
        public static int SalvageChunkProgress;
        public static int SlagChunkProgress;
        public static List<Thing> Salvage = new List<Thing>();
        public static Dictionary<ThingDef, int> SalvageGeneric = new Dictionary<ThingDef, int>();
        public static void SalvageEverything(Map map)
        {
            //Log.Message("Beginning salvage code");
            foreach (Thing t in map.spawnedThings)
            {
                SalvageThing(t);
            }
            for(int i=0; i<SalvageChunkProgress/10; i++)
            {
                Thing chunk = ThingMaker.MakeThing(ThingDefOf.MinifiedThing);
                Thing inner = ThingMaker.MakeThing(ThingDef.Named("ShipChunkSalvage"));
                ((MinifiedThing)chunk).InnerThing = inner;
                Salvage.Add(chunk);
            }
            for(int j=0;j<SlagChunkProgress/20;j++)
            {
                if (SalvageGeneric.ContainsKey(ThingDefOf.ChunkSlagSteel))
                    SalvageGeneric[ThingDefOf.ChunkSlagSteel] += 1;
                else
                    SalvageGeneric.Add(ThingDefOf.ChunkSlagSteel, 1);
            }
            //Find.WindowStack.Add(new Dialog_SalvageShip(PlayerShip.spawnedThings.Where(t=>t.def == ResourceBank.ThingDefOf.ShipSalvageBay).Count(), PlayerShip));
        }
        public static void SalvageThing(Thing thing)
        {
            if(thing is Building)
            {
                if (thing.def.minifiedDef != null)
                {
                    try
                    {
                        MinifiedThing mini = thing.MakeMinified();
                        Salvage.Add(mini);
                    }
                    catch(Exception)
                    {

                    }
                }
                else
                {
                    foreach (ThingDefCountClass t in thing.CostListAdjusted())
                    {
                        float salvageEfficiency = 0.5f;
                        if (t.thingDef != ThingDefOf.ComponentSpacer && t.thingDef != ThingDefOf.Steel)
                        {
                            if (t.thingDef == ThingDefOf.Plasteel)
                                salvageEfficiency = 0.25f;
                            //if (t.thingDef == ThingDefOf.Silver && (myDef == null || !myDef.tradeShip))
                                //salvageEfficiency = 0.1f;
                            if (SalvageGeneric.ContainsKey(t.thingDef))
                            {
                                SalvageGeneric[t.thingDef] += (int)(t.count * salvageEfficiency);
                            }
                            else
                            {
                                SalvageGeneric.Add(t.thingDef, (int)(t.count * salvageEfficiency));
                            }
                        }
                        else if (t.thingDef == ThingDefOf.Steel)
                            SlagChunkProgress += (int)(t.count * salvageEfficiency);
                        else
                            SalvageChunkProgress += t.count;
                    }
                }
            }
            else if(thing is Pawn)
            {
                if (ShipInteriorMod2.EVAlevel((Pawn)thing)<3)
                    HealthUtility.DamageUntilDead((Pawn)thing);
                else
                    HealthUtility.DamageUntilDowned((Pawn)thing);
                Salvage.Add(thing);
            }
            else if((thing.def.category == ThingCategory.Item || thing is Corpse) && thing.TryGetComp<CompExplosive>()==null)
            {
                thing.TakeDamage(new DamageInfo(DamageDefOf.Crush,Rand.Range(1,100)));
                if(!thing.Destroyed)
                    Salvage.Add(thing);
            }
        }
    }
}
