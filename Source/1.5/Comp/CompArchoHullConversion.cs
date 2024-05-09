using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;


namespace SaveOurShip2
{
	class CompArchoHullConversion : ThingComp
	{
		public bool OptimizeMatter = true;
		private int ticksToConversion;
		private int age;
		public float AgeDays => (float)age / 60000f;

		public ShipMapComp mapComp;
		ResearchProjectDef OptimizationProject = ResearchProjectDef.Named("ArchotechHullConversion");
		protected CompProps_ArchoHullConversion Props => (CompProps_ArchoHullConversion)props;
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
			mapComp = parent?.Map?.GetComponent<ShipMapComp>();
		}

		public override void CompTick()
		{
			if (!OptimizeMatter || !parent.Spawned || !OptimizationProject.IsFinished || parent.Map.IsSpace() && parent.Map.GetComponent<ShipMapComp>().ShipMapState != ShipMapState.nominal || parent.Map.mapPawns.AllPawns.Where(p => p.HostileTo(Faction.OfPlayer)).Any())
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
			List<Thing> toDestroy = new List<Thing>();
			List<Thing> toSpawn = new List<Thing>();
			foreach (Thing t in c.GetThingList(parent.Map))
			{
				if (ShipInteriorMod2.archoConversions.ContainsKey(t.def))
				{
					toDestroy.Add(t);
					Thing replacement = ThingMaker.MakeThing(ShipInteriorMod2.archoConversions[t.def]);
					replacement.Rotation = t.Rotation;
					replacement.Position = t.Position;
					replacement.SetFaction(Faction.OfPlayer);
					/*var attachComp = t.TryGetComp<CompAttachBase>();
					if (attachComp != null)
					{
						foreach (AttachableThing attach in attachComp.attachments)
						{
							attach.parent = replacement;
						}
						replacement.TryGetComp<CompAttachBase>().attachments.AddRange(new List<AttachableThing>(attachComp.attachments));
						t.TryGetComp<CompAttachBase>().attachments.Clear();
					}*/
					int shipIndex = mapComp.ShipIndexOnVec(parent.Position);
					if (shipIndex > 0)
					{
						mapComp.ShipsOnMap[shipIndex].RemoveFromCache(t as Building, DestroyMode.Vanish);
						mapComp.ShipsOnMap[shipIndex].AddToCache(replacement as Building);
					}
					toSpawn.Add(replacement);
				}
			}
			if (toDestroy.Count > 0)
			{
				ShipInteriorMod2.MoveShipFlag = true;
				foreach (Thing t in toDestroy)
				{
					t.Destroy();
				}
				foreach (Thing replacement in toSpawn)
				{
					replacement.SpawnSetup(parent.Map, false);
					FleckMaker.ThrowSmoke(replacement.DrawPos, parent.Map, 2);
				}
				parent.Map.roofGrid.SetRoof(c, ResourceBank.RoofDefOf.RoofShip);
				ShipInteriorMod2.MoveShipFlag = false;
				/*TerrainDef terrain = parent.Map.terrainGrid.TerrainAt(c);
				parent.Map.terrainGrid.RemoveTopLayer(c, false);

				if (terrain != ResourceBank.TerrainDefOf.FakeFloorInsideShip && terrain != ResourceBank.TerrainDefOf.FakeFloorInsideShip && terrain != ResourceBank.TerrainDefOf.FakeFloorInsideShipMech && terrain != ResourceBank.TerrainDefOf.ShipWreckageTerrain && terrain != ResourceBank.TerrainDefOf.FakeFloorInsideShipFoam)
					parent.Map.terrainGrid.SetTerrain(c, terrain);*/
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
					defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.ArchotechOptimizeMatter"),
					defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.ArchotechOptimizeMatterDesc"),
					icon = ContentFinder<Texture2D>.Get("UI/ArchotechCommandOptimize")
				});
			}
			return newList;
		}
	}
}
