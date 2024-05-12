using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Vehicles;
using Verse;

namespace SaveOurShip2.Vehicles
{
    class CompVehicleLoadData : VehicleComp
    {
        public bool appliedYet=false;
        public PatternDef pattern;
        public Color one, two, three;
        public float fuel, tiles;
        public List<string> upgrades = new List<string>();
        public Vector2 displacement;

        public override void CompTick()
        {
            base.CompTick();
            CompUpgradeTree tree = Vehicle.comps.Where(comp => comp is CompUpgradeTree).FirstOrDefault() as CompUpgradeTree;
            if (tree == null)
                tree = Vehicle.CompUpgradeTree;
            if (tree == null)
                return;
            if (!appliedYet && RGBMaterialPool.cache.ContainsKey(Vehicle))
            {
                Vehicle.Pattern = pattern;
                Vehicle.patternToPaint = new PatternData(one, two, three, pattern, displacement, tiles);
                Vehicle.patternData = Vehicle.patternToPaint;
                //Vehicle.SetColor();
                Vehicle.CompFueledTravel.Refuel(fuel);
                foreach (string upgrade in upgrades)
                    tree.FinishUnlock(tree.Props.def.GetNode(upgrade));
                appliedYet = true;
            }
        }
    }
}
