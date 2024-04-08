using RimWorld.Planet;
using System;
using UnityEngine;
using Verse;
using System.Collections.Generic;

namespace RimWorld
{
	[StaticConstructorOnStartup]
	public class CompBecomePawn : ThingComp
	{
		private static readonly Texture2D TransformCommandTex = ContentFinder<Texture2D>.Get("UI/Hover_On_Icon");

		public CompProperties_BecomePawn Props
		{
			get
			{
				return (CompProperties_BecomePawn)this.props;
			}
		}

		public void transform()
		{
			int fuelAmount =  Mathf.CeilToInt(this.parent.GetComp<CompRefuelable> ().Fuel);
			this.parent.GetComp<CompRefuelable> ().ConsumeFuel (fuelAmount);
			IntVec3 myPos = this.parent.Position;
			Map myMap = this.parent.Map;
			this.parent.Destroy (DestroyMode.Vanish);
			Pawn transformed = myPawn (this.parent, myPos, fuelAmount);
			transformed.SpawnSetup (myMap, false);
			if(transformed.TryGetComp<CompShuttleCosmetics>()!=null)
				CompShuttleCosmetics.ChangeShipGraphics(transformed, transformed.TryGetComp<CompShuttleCosmetics>().Props);
		}

		public static Pawn myPawn(Thing meAsABuilding, IntVec3 myPos, int fuelAmount)
		{
			PawnKindDef theDef = meAsABuilding.TryGetComp<CompBecomePawn> ().Props.pawnDef;
			Pawn transformed = PawnGenerator.GeneratePawn (theDef,Faction.OfPlayer);
			transformed.Position = myPos;
			transformed.GetComp<CompRefuelable> ().Refuel (fuelAmount);
			transformed.SetFactionDirect (Faction.OfPlayer);
			transformed.relations = new Pawn_RelationsTracker (transformed);
			transformed.psychicEntropy = new Pawn_PsychicEntropyTracker(transformed);
			transformed.apparel = new Pawn_ApparelTracker(transformed);
			float healthPercent = (float)meAsABuilding.HitPoints / (float)meAsABuilding.MaxHitPoints;
			if (healthPercent < 0.99f)
			{
				Hediff injury = HediffMaker.MakeHediff(HediffDefOf.Scratch, transformed, transformed.RaceProps.body.corePart);
				injury.Severity = transformed.RaceProps.body.corePart.def.GetMaxHealth(transformed) * 0.375f * (1 - healthPercent);
				transformed.health.AddHediff(injury);
			}
			if (meAsABuilding.TryGetComp<CompShuttleCosmetics>() != null && transformed.TryGetComp<CompShuttleCosmetics>() != null)
			{
				int whichVersion = meAsABuilding.TryGetComp<CompShuttleCosmetics>().whichVersion;
				transformed.TryGetComp<CompShuttleCosmetics>().whichVersion = whichVersion;
			}
			return transformed;
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			if (parent.Faction != Faction.OfPlayer)
				yield break;
			foreach (Gizmo g in base.CompGetGizmosExtra()) {
				yield return g;
			}
			Command_Action transform = new Command_Action();
			transform.defaultLabel = TranslatorFormattedStringExtensions.Translate("CommandToggleHover");
			transform.defaultDesc = TranslatorFormattedStringExtensions.Translate("CommandHoverOnDesc");
			transform.icon = TransformCommandTex;
			transform.action = delegate
			{
				this.transform();
			};
			if(this.parent.GetComp<CompRefuelable>().Fuel>0)
				yield return transform;
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			if (parent.TryGetComp<CompShuttleCosmetics>() != null)
				CompShuttleCosmetics.ChangeShipGraphics(parent, parent.TryGetComp<CompShuttleCosmetics>().Props);
		}
	}
}

