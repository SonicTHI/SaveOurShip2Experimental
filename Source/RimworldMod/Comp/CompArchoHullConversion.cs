using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimWorld
{
	[StaticConstructorOnStartup]
	class CompArchoHullConversion : ThingComp
    {
		private int age;

		private int ticksToConversion;

		protected CompProperties_ArchoHullConversion Props => (CompProperties_ArchoHullConversion)props;

		public float AgeDays => (float)age / 60000f;

		public float CurrentRadius => Props.radiusPerDayCurve.Evaluate(AgeDays);

		Dictionary<ThingDef, Tuple<ThingDef, bool, bool>> Conversions = new Dictionary<ThingDef, Tuple<ThingDef,bool, bool>>();

		ResearchProjectDef OptimizationProject = ResearchProjectDef.Named("ArchotechHullConversion");

		public bool OptimizeMatter = true;

		public override void PostExposeData()
		{
			Scribe_Values.Look(ref age, "age", 0);
			Scribe_Values.Look(ref ticksToConversion, "ticksToConversion", 0);
			Scribe_Values.Look(ref OptimizeMatter, "optimize", true);
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostPostMake();
			Conversions.Add(ThingDef.Named("Ship_Beam_Unpowered"), new Tuple<ThingDef, bool, bool>(ThingDef.Named("Ship_BeamArchotech_Unpowered"),false,false));
			Conversions.Add(ThingDef.Named("Ship_Beam"), new Tuple<ThingDef, bool, bool>(ThingDef.Named("Ship_BeamArchotech"),false, false));
			Conversions.Add(ThingDef.Named("Ship_Corner_OneOne"), new Tuple<ThingDef, bool, bool>(ThingDef.Named("Ship_Corner_Archo_OneOne"), false, false));
			Conversions.Add(ThingDef.Named("Ship_Corner_OneOneFlip"), new Tuple<ThingDef, bool, bool>(ThingDef.Named("Ship_Corner_Archo_OneOneFlip"), false, false));
			Conversions.Add(ThingDef.Named("Ship_Corner_OneTwo"), new Tuple<ThingDef, bool, bool>(ThingDef.Named("Ship_Corner_Archo_OneTwo"), false, false));
			Conversions.Add(ThingDef.Named("Ship_Corner_OneTwoFlip"), new Tuple<ThingDef, bool, bool>(ThingDef.Named("Ship_Corner_Archo_OneTwoFlip"), true, true));
			Conversions.Add(ThingDef.Named("Ship_Corner_OneThree"), new Tuple<ThingDef, bool, bool>(ThingDef.Named("Ship_Corner_Archo_OneThree"), false, false));
			Conversions.Add(ThingDef.Named("Ship_Corner_OneThreeFlip"), new Tuple<ThingDef, bool, bool>(ThingDef.Named("Ship_Corner_Archo_OneThreeFlip"), true, false));
			Conversions.Add(ThingDef.Named("ShipInside_SolarGenerator"), new Tuple<ThingDef, bool, bool>(ThingDef.Named("ShipInside_SolarGeneratorArchotech"), false, false));
			Conversions.Add(ThingDef.Named("ShipInside_PassiveCooler"), new Tuple<ThingDef, bool, bool>(ThingDef.Named("ShipInside_PassiveCoolerArchotech"), false, false));
			Conversions.Add(ThingDef.Named("ShipInside_PassiveCoolerAdvanced"), new Tuple<ThingDef, bool, bool>(ThingDef.Named("ShipInside_PassiveCoolerArchotech"), false, false));
			Conversions.Add(ThingDef.Named("ShipInside_PassiveVent"), new Tuple<ThingDef, bool, bool>(ThingDef.Named("ShipInside_PassiveVentArchotech"), false, false));
			Conversions.Add(ThingDef.Named("ShipAirlock"), new Tuple<ThingDef, bool, bool>(ThingDef.Named("ShipAirlockArchotech"), false, false));
			Conversions.Add(ThingDef.Named("ShipHullTile"), new Tuple<ThingDef, bool, bool>(ThingDef.Named("ShipHullTileArchotech"), false, false));
		}

		public override void CompTick()
		{
			if (this.parent.Map.GetComponent<ShipHeatMapComp>().InCombat || !OptimizeMatter || !parent.Spawned || !OptimizationProject.IsFinished || this.parent.Map.mapPawns.AllPawns.Where(p => p.Faction != Faction.OfPlayer && p.Faction.PlayerRelationKind == FactionRelationKind.Hostile && !p.Downed && !p.Dead && !p.IsPrisoner && !p.IsSlave).Any())
			{
				return;
			}
			age++;
			ticksToConversion--;
			if (ticksToConversion <= 0)
			{
				float currentRadius = CurrentRadius;
				float num = (float)Math.PI * currentRadius * currentRadius * 0.01f;
				float num2 = 60f / num;
				int num3;
				if (num2 >= 1f)
				{
					ticksToConversion = GenMath.RoundRandom(num2);
					num3 = 1;
				}
				else
				{
					ticksToConversion = 1;
					num3 = GenMath.RoundRandom(1f / num2);
				}
				for (int i = 0; i < num3; i++)
				{
					ConvertHullTile(currentRadius);
				}
			}
		}

		private void ConvertHullTile(float radius)
        {
			IntVec3 c = parent.Position + (Rand.InsideUnitCircleVec3 * radius).ToIntVec3();
			if (!c.InBounds(parent.Map))
			{
				return;
			}
			List<Thing> toDestroy = new List<Thing>();
			List<Thing> toSpawn = new List<Thing>();
			foreach(Thing t in c.GetThingList(parent.Map))
            {
				if(Conversions.ContainsKey(t.def))
                {
					Thing replacement = ThingMaker.MakeThing(Conversions[t.def].Item1);
					replacement.Rotation = Conversions[t.def].Item2 ? t.Rotation.Opposite : t.Rotation;
					replacement.Position = t.Position + (Conversions[t.def].Item3 ? IntVec3.South.RotatedBy(replacement.Rotation) : IntVec3.Zero);
					replacement.SetFaction(Faction.OfPlayer);
					toDestroy.Add(t);
					toSpawn.Add(replacement);
                }
            }
			if (toDestroy.Count > 0)
			{
				TerrainDef terrain = parent.Map.terrainGrid.TerrainAt(c);
				parent.Map.terrainGrid.RemoveTopLayer(c, false);
				foreach (Thing t in toDestroy)
					t.Destroy();
				foreach (Thing replacement in toSpawn)
				{
					replacement.SpawnSetup(parent.Map, false);
					FleckMaker.ThrowSmoke(replacement.DrawPos, parent.Map, 2);
				}
				if (terrain != CompRoofMe.hullTerrain)
					parent.Map.terrainGrid.SetTerrain(c, terrain);
			}
		}

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
			List<Gizmo> newList = new List<Gizmo>();
			newList.AddRange(base.CompGetGizmosExtra());
			if (OptimizationProject.IsFinished)
			{
				newList.Add(new Command_Toggle
				{
					toggleAction = delegate
					{
						OptimizeMatter = !OptimizeMatter;
					},
					isActive = () => OptimizeMatter,
					defaultLabel = TranslatorFormattedStringExtensions.Translate("ArchotechOptimizeMatter"),
					defaultDesc = TranslatorFormattedStringExtensions.Translate("ArchotechOptimizeMatterDesc"),
					icon = ContentFinder<Texture2D>.Get("UI/ArchotechCommandOptimize")
				});
			}
			return newList;
        }
    }
}
