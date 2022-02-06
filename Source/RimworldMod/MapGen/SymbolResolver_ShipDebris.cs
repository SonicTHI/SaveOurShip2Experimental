using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI.Group;

namespace RimWorld.BaseGen
{
    class SymbolResolver_ShipDebris : SymbolResolver
    {
        public static readonly FloatRange DefaultPawnsPoints = new FloatRange(1150f, 1600f);

        public override void Resolve(ResolveParams rp)
        {
            Map map = BaseGen.globalSettings.map;
            Faction faction = Faction.OfAncientsHostile;
            int num = 0;
            int? edgeDefenseWidth = rp.edgeDefenseWidth;
            if (edgeDefenseWidth.HasValue)
            {
                num = rp.edgeDefenseWidth.Value;
            }
            else if (rp.rect.Width >= 20 && rp.rect.Height >= 20)
            {
                num = ((!Rand.Bool) ? 4 : 2);
            }
            float num2 = (float)rp.rect.Area / 144f * 0.17f;
            BaseGen.globalSettings.minEmptyNodes = ((num2 >= 1f) ? GenMath.RoundRandom(num2) : 0);
            Lord singlePawnLord = LordMaker.MakeNewLord(faction, new LordJob_DefendBase(faction, rp.rect.CenterCell), map, null);
            TraverseParms traverseParms = TraverseParms.For(TraverseMode.PassDoors, Danger.Some, false);
            ResolveParams resolveParams = rp;
            resolveParams.rect = rp.rect;
            resolveParams.faction = null;
            resolveParams.singlePawnLord = singlePawnLord;
            int num3 = (!Rand.Chance(0.75f)) ? 0 : GenMath.RoundRandom((float)rp.rect.Area / 400f);
                for (int i = 0; i < num3; i++)
                {
                    ResolveParams resolveParams2 = rp;
                    resolveParams2.faction = null;
                    BaseGen.symbolStack.Push("firefoamPopper", resolveParams2);
                }
            if (num > 0)
            {
                ResolveParams resolveParams3 = rp;
                resolveParams3.faction = null;
                resolveParams3.edgeDefenseWidth = new int?(num);
                BaseGen.symbolStack.Push("edgeDebris", resolveParams3);
            }
            ResolveParams resolveParams4 = rp;
            resolveParams4.rect = rp.rect.ContractedBy(num);
            resolveParams4.faction = null;
            BaseGen.symbolStack.Push("shipensureCanReachMapEdge", resolveParams4);
            ResolveParams resolveParams5 = rp;
            resolveParams5.rect = rp.rect.ContractedBy(num);
            resolveParams5.faction = null;
            BaseGen.symbolStack.Push("shipdebrispart_outdoors", resolveParams5);
        }
    }
}
