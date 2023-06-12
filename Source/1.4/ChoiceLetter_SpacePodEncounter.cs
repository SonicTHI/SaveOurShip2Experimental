using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimWorld
{
    //not used, not finished - user choice letter
    /*public class ChoiceLetter_SpacePodEncounter : ChoiceLetter
    {
        public Map map;
        public int fee;

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                if (base.ArchivedOnly)
                {
                    yield return base.Option_Close;
                    yield break;
                }
                DiaOption diaOption = new DiaOption("LetterSpaceDebrisAccept".Translate());
                diaOption.action = delegate ()
                {
                    List<Building> engines = new List<Building>();
                    float fuel = FuelOnMap(engines);
                    foreach (Building b in engines)
                    {
                        var fuelComp = b.TryGetComp<CompRefuelable>();
                        fuelComp.ConsumeFuel(fee * fuelComp.Fuel / fuel);
                    }
                    Find.LetterStack.RemoveLetter(this);
                };
                diaOption.resolveTree = true;
                float cap;
                if (HasFuel((float)fee, out cap))
                {
                    diaOption.Disable("LetterSpaceDebrisFail".Translate(this.fee.ToString(), cap));
                }
                yield return diaOption;
                yield return base.Option_Reject;
                yield return base.Option_Postpone;
                yield break;
            }
        }
        private float FuelOnMap(List<Building> engines)
        {
            float fuel = 0;
            foreach (Building b in map.listerBuildings.allBuildingsColonist.Where(e => e.TryGetComp<CompEngineTrail>() != null && !e.TryGetComp<CompEngineTrail>().Props.reactionless))
            {
                engines.Add(b);
                var fuelComp = b.TryGetComp<CompRefuelable>();
                fuel += fuelComp.Fuel;
            }
            return fuel;
        }
        private bool HasFuel(float fee, out float cap)
        {
            cap = 0;
            float fuel = 0;
            foreach (Building b in map.listerBuildings.allBuildingsColonist.Where(e => e.TryGetComp<CompEngineTrail>() != null))
            {
                var engineComp = b.TryGetComp<CompEngineTrail>();
                if (engineComp != null && !engineComp.Props.reactionless)
                {
                    var fuelComp = b.TryGetComp<CompRefuelable>();
                    fuel += fuelComp.Fuel;
                    cap += fuelComp.Props.fuelCapacity;
                }
            }
            if (fuel > fee)
                return true;
            return false;
        }

        public override bool CanShowInLetterStack
        {
            get
            {
                return base.CanShowInLetterStack && Find.Maps.Contains(this.map);// && this.faction.kidnapped.KidnappedPawnsListForReading.Contains(this.kidnapped);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Map>(ref this.map, "map", false);
            Scribe_Values.Look<int>(ref this.fee, "fee", 0, false);
        }
    }*/
}
