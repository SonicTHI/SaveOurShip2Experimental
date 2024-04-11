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
	class CompArchoHullConversion : ThingComp
	{
		public bool OptimizeMatter = true;
		private int ticksToConversion;
		private int age;
		public float AgeDays => (float)age / 60000f;

		public ShipHeatMapComp mapComp;
		ResearchProjectDef OptimizationProject = ResearchProjectDef.Named("ArchotechHullConversion");
		protected CompProperties_ArchoHullConversion Props => (CompProperties_ArchoHullConversion)props;
		public float CurrentRadius => Props.radiusPerDayCurve.Evaluate(AgeDays);

		public override void PostExposeData()
		{
			Scribe_Values.Look(ref age, "age", 0);
			Scribe_Values.Look(ref ticksToConversion, "ticksToConversion", 0);
			Scribe_Values.Look(ref OptimizeMatter, "optimize", true);
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostPostMake();
			mapComp = parent?.Map?.GetComponent<ShipHeatMapComp>();
		}

		public override void CompTick()
		{
			if (!OptimizeMatter || !parent.Spawned || !OptimizationProject.IsFinished || parent.Map.IsSpace() && parent.Map.GetComponent<ShipHeatMapComp>().ShipMapState != ShipMapState.nominal || parent.Map.mapPawns.AllPawns.Where(p => p.HostileTo(Faction.OfPlayer)).Any())
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
			if (!c.InBounds(parent.Map) || mapComp.ShipIndexOnVec(parent.Position) != mapComp.ShipIndexOnVec(c))
			{
				return;
			}
			//List<Thing> toDestroy = new List<Thing>();
			//List<Thing> toSpawn = new List<Thing>();
			List<Thing> toConvert = new List<Thing>();
			foreach (Thing t in c.GetThingList(parent.Map))
			{
				if (ShipInteriorMod2.archoConversions.ContainsKey(t.def))
				{
					t.TryGetComp<CompSoShipPart>().ArchoConvert = true;
					toConvert.Add(t);
				}
			}
			if (toConvert.Count > 0)
			{
				foreach (Thing t in toConvert)
				{
					t.Destroy();
				}
			}
			/*if (toDestroy.Count > 0)
			{
				TerrainDef terrain = parent.Map.terrainGrid.TerrainAt(c);
				parent.Map.terrainGrid.RemoveTopLayer(c, false);

				if (terrain != ResourceBank.TerrainDefOf.FakeFloorInsideShip && terrain!= ResourceBank.TerrainDefOf.FakeFloorInsideShip && terrain!= ResourceBank.TerrainDefOf.FakeFloorInsideShipMech && terrain!= ResourceBank.TerrainDefOf.ShipWreckageTerrain && terrain!= ResourceBank.TerrainDefOf.FakeFloorInsideShipFoam)
					parent.Map.terrainGrid.SetTerrain(c, terrain);
			}*/
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
					defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.ArchotechOptimizeMatter"),
					defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.ArchotechOptimizeMatterDesc"),
					icon = ContentFinder<Texture2D>.Get("UI/ArchotechCommandOptimize")
				});
			}
			return newList;
		}
	}
}
