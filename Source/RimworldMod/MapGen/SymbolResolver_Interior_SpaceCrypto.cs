using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld.BaseGen
{
    public class SymbolResolver_Interior_SpaceCrypto : SymbolResolver
    {
        public override void Resolve(ResolveParams rp)
        {
            Map map = BaseGen.globalSettings.map;

            if(rp.disableHives.HasValue && rp.disableHives.Value)
            {
                foreach (IntVec3 current in rp.rect)
                {
                    if (Rand.Chance(0.1f))
                    {
                        Thing thing = ThingMaker.MakeThing(ThingDef.Named("Filth_SpaceBones"));
                        GenSpawn.Spawn(thing, current, map);
                    }
                    else if(Rand.Chance(0.3f))
                    {
                        Thing thing = ThingMaker.MakeThing(ThingDefOf.Filth_Blood);
                        GenSpawn.Spawn(thing, current, map);
                    }
                }
            }
            ThingDef thingDef = ThingDefOf.AncientCryptosleepCasket;
            bool @bool = Rand.Bool;
            foreach (IntVec3 current in rp.rect)
            {
                if (@bool)
                {
                    if (current.x % 3 != 0 || current.z % 2 != 0)
                    {
                        continue;
                    }
                }
                else if (current.x % 2 != 0 || current.z % 3 != 0)
                {
                    continue;
                }
                Rot4 rot = (!@bool) ? Rot4.North : Rot4.West;
                if (!GenSpawn.WouldWipeAnythingWith(current, rot, thingDef, map, delegate { return true; }))
                {
                    if (!BaseGenUtility.AnyDoorAdjacentCardinalTo(GenAdj.OccupiedRect(current, rot, thingDef.Size), map))
                    {
                        ResolveParams resolveParams = rp;
                        resolveParams.rect = GenAdj.OccupiedRect(current, rot, thingDef.size);
                        resolveParams.singleThingDef = thingDef;
                        resolveParams.thingRot = new Rot4?(rot);
                        resolveParams.ancientCryptosleepCasketGroupID = 1;
                        if(resolveParams.disableHives.HasValue && resolveParams.disableHives.Value)
                        {
                            if(Rand.Chance(0.1f))
                                resolveParams.podContentsType = PodContentsType.AncientIncapped;
                            else
                                resolveParams.podContentsType = PodContentsType.Empty;
                        }
                        BaseGen.symbolStack.Push("ancientCryptosleepCasket", resolveParams);
                    }
                }
            }
            ResolveParams resolveParams2 = rp;
            resolveParams2.singleThingDef = ThingDefOf.Battery;
            BaseGen.symbolStack.Push("edgeThing", resolveParams2);

            ResolveParams resolveParams3 = rp;
            resolveParams3.singleThingDef = ThingDef.Named("Ship_LifeSupport");
            BaseGen.symbolStack.Push("edgeThing", resolveParams3);
        }
    }
}