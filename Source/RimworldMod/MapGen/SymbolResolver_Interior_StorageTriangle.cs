using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld.BaseGen
{
    public class SymbolResolver_Interior_StorageTriangle : SymbolResolver
    {
        private List<IntVec3> cells = new List<IntVec3>();

        private const float FreeCellsFraction = 0.45f;

        public override void Resolve(ResolveParams rp)
        {
            Map map = BaseGen.globalSettings.map;

            if (rp.disableHives.HasValue && rp.disableHives.Value)
                this.CalculateFreeCells(new CellRect(rp.rect.minX,rp.rect.minZ-rp.rect.Height,rp.rect.Width,rp.rect.Height), 0.45f);
            else
                this.CalculateFreeCells(rp.rect, 0.45f);
                ThingSetMakerDef thingSetMakerDef = rp.thingSetMakerDef ?? ThingSetMakerDefOf.MapGen_DefaultStockpile;
                ThingSetMakerParams? thingSetMakerParams = rp.thingSetMakerParams;
                ThingSetMakerParams value;
                if (thingSetMakerParams.HasValue)
                {
                    value = rp.thingSetMakerParams.Value;
                }
                else
                {
                    value = default(ThingSetMakerParams);
                    value.techLevel = new TechLevel?((rp.faction == null) ? TechLevel.Undefined : rp.faction.def.techLevel);
                    value.validator = ((ThingDef x) => rp.faction == null || x.techLevel >= rp.faction.def.techLevel || !x.IsWeapon || x.GetStatValueAbstract(StatDefOf.MarketValue, GenStuff.DefaultStuffFor(x)) >= 100f);
                    float? stockpileMarketValue = rp.stockpileMarketValue;
                    float num2 = (!stockpileMarketValue.HasValue) ? Mathf.Min((float)this.cells.Count * 130f, 2500f) : stockpileMarketValue.Value;
                    value.totalMarketValueRange = new FloatRange?(new FloatRange(num2, num2));
                }
                IntRange? countRange = value.countRange;
                if (!countRange.HasValue)
                {
                    value.countRange = new IntRange?(new IntRange(this.cells.Count, this.cells.Count));
                }
            List<Thing> list = thingSetMakerDef.root.Generate(value);
            for (int i = 0; i < list.Count; i++)
            {
                Thing thing = list[i];
                GenSpawn.Spawn(thing, cells.RandomElement(), map);
            }
        }

        private void CalculateFreeCells(CellRect rect, float freeCellsFraction)
        {
            Map map = BaseGen.globalSettings.map;
            this.cells.Clear();
            foreach (IntVec3 current in rect)
            {
                if (current.Standable(map) && current.GetFirstItem(map) == null && current.GetThingList(map).Any(thing => thing.def.defName.Equals("ShipHullTileWrecked")))
                {
                    this.cells.Add(current);
                }
            }
            int num = (int)(freeCellsFraction * (float)this.cells.Count);
            for (int i = 0; i < num; i++)
            {
                this.cells.RemoveAt(Rand.Range(0, this.cells.Count));
            }
            this.cells.Shuffle<IntVec3>();
        }
    }
}
