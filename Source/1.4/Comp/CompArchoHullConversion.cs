using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using RimworldMod;

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

		Dictionary<ThingDef, ThingDef> Conversions = new Dictionary<ThingDef, ThingDef>();

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
			Conversions.Add(ThingDef.Named("Ship_Beam_Unpowered"), ThingDef.Named("Ship_BeamArchotech_Unpowered"));
			Conversions.Add(ThingDef.Named("Ship_Beam"), ThingDef.Named("Ship_BeamArchotech"));
			Conversions.Add(ThingDef.Named("Ship_Corner_OneOne"), ThingDef.Named("Ship_Corner_Archo_OneOne"));
			Conversions.Add(ThingDef.Named("Ship_Corner_OneOneFlip"), ThingDef.Named("Ship_Corner_Archo_OneOneFlip"));
			Conversions.Add(ThingDef.Named("Ship_Corner_OneTwo"), ThingDef.Named("Ship_Corner_Archo_OneTwo"));
			Conversions.Add(ThingDef.Named("Ship_Corner_OneTwoFlip"), ThingDef.Named("Ship_Corner_Archo_OneTwoFlip"));
			Conversions.Add(ThingDef.Named("Ship_Corner_OneThree"), ThingDef.Named("Ship_Corner_Archo_OneThree"));
			Conversions.Add(ThingDef.Named("Ship_Corner_OneThreeFlip"),ThingDef.Named("Ship_Corner_Archo_OneThreeFlip"));
			Conversions.Add(ThingDef.Named("ShipInside_SolarGenerator"), ThingDef.Named("ShipInside_SolarGeneratorArchotech"));
			Conversions.Add(ThingDef.Named("ShipInside_PassiveVent"), ThingDef.Named("ShipInside_PassiveVentArchotech"));
			Conversions.Add(ThingDef.Named("ShipAirlock"), ThingDef.Named("ShipAirlockArchotech"));
			Conversions.Add(ThingDef.Named("ShipHullTile"), ThingDef.Named("ShipHullTileArchotech"));
			Conversions.Add(ThingDef.Named("Ship_BeamMech_Unpowered"), ThingDef.Named("Ship_BeamArchotech_Unpowered"));
			Conversions.Add(ThingDef.Named("Ship_BeamMech"), ThingDef.Named("Ship_BeamArchotech"));
			Conversions.Add(ThingDef.Named("Ship_Corner_OneOne_Mech"), ThingDef.Named("Ship_Corner_Archo_OneOne"));
			Conversions.Add(ThingDef.Named("Ship_Corner_OneOne_MechFlip"), ThingDef.Named("Ship_Corner_Archo_OneOneFlip"));
			Conversions.Add(ThingDef.Named("Ship_Corner_OneTwo_Mech"), ThingDef.Named("Ship_Corner_Archo_OneTwo"));
			Conversions.Add(ThingDef.Named("Ship_Corner_OneTwoFlip_Mech"), ThingDef.Named("Ship_Corner_Archo_OneTwoFlip"));
			Conversions.Add(ThingDef.Named("Ship_Corner_OneThree_Mech"), ThingDef.Named("Ship_Corner_Archo_OneThree"));
			Conversions.Add(ThingDef.Named("Ship_Corner_OneThreeFlip_Mech"), ThingDef.Named("Ship_Corner_Archo_OneThreeFlip"));
			Conversions.Add(ThingDef.Named("ShipInside_SolarGeneratorMech"), ThingDef.Named("ShipInside_SolarGeneratorArchotech"));
			Conversions.Add(ThingDef.Named("ShipInside_PassiveVentMechanoid"), ThingDef.Named("ShipInside_PassiveVentArchotech"));
			Conversions.Add(ThingDef.Named("ShipAirlockMech"), ThingDef.Named("ShipAirlockArchotech"));
			Conversions.Add(ThingDef.Named("ShipHullTileMech"), ThingDef.Named("ShipHullTileArchotech"));
		}

		public override void CompTick()
		{
			if (!OptimizeMatter || !parent.Spawned || !OptimizationProject.IsFinished || this.parent.Map.IsSpace() && this.parent.Map.GetComponent<ShipHeatMapComp>().InCombat || this.parent.Map.mapPawns.AllPawns.Where(p => p.HostileTo(Faction.OfPlayer)).Any())
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
					Thing replacement = ThingMaker.MakeThing(Conversions[t.def]);
					replacement.Rotation = t.Rotation;
					replacement.Position = t.Position;
					replacement.SetFaction(Faction.OfPlayer);
					toDestroy.Add(t);
					toSpawn.Add(replacement);
                }
            }
			if (toDestroy.Count > 0)
			{
				TerrainDef terrain = parent.Map.terrainGrid.TerrainAt(c);
				parent.Map.terrainGrid.RemoveTopLayer(c, false);
				ShipInteriorMod2.AirlockBugFlag = true; //prevent wall light destruction
				foreach (Thing t in toDestroy)
					t.Destroy();
				ShipInteriorMod2.AirlockBugFlag = false;
				foreach (Thing replacement in toSpawn)
				{
					replacement.SpawnSetup(parent.Map, false);
					FleckMaker.ThrowSmoke(replacement.DrawPos, parent.Map, 2);
				}
				if (terrain != ResourceBank.TerrainDefOf.FakeFloorInsideShip && terrain!= ResourceBank.TerrainDefOf.FakeFloorInsideShip && terrain!= ResourceBank.TerrainDefOf.FakeFloorInsideShipMech && terrain!= ResourceBank.TerrainDefOf.ShipWreckageTerrain && terrain!= ResourceBank.TerrainDefOf.FakeFloorInsideShipFoam)
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
