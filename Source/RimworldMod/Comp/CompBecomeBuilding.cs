using RimWorld.Planet;
using System;
using UnityEngine;
using Verse;
using System.Collections.Generic;

namespace RimWorld
{
	[StaticConstructorOnStartup]
	public class CompBecomeBuilding : ThingComp
	{
        private static readonly Texture2D TransformCommandTex = ContentFinder<Texture2D>.Get("UI/Hover_Off_Icon");

        public CompProperties_BecomeBuilding Props
		{
			get
			{
				return (CompProperties_BecomeBuilding)this.props;
			}
		}

		public void transform()
        {
            IntVec3 myPos = this.parent.Position;
            Map myMap = this.parent.Map;
            Building transformed = (Building)ThingMaker.MakeThing(Props.buildingDef);
            transformed.Position = myPos;
            transformed.SetFaction(parent.Faction);
            if (this.parent.TryGetComp<CompRefuelable>() != null)
            {
                int fuelAmount = Mathf.CeilToInt(this.parent.GetComp<CompRefuelable>().Fuel);
                this.parent.GetComp<CompRefuelable>().ConsumeFuel(fuelAmount);
                transformed.GetComp<CompRefuelable>().Refuel(fuelAmount);
            }
            if (this.parent.ParentHolder != null && !(this.parent.ParentHolder is Map))
            {
                if (this.parent.ParentHolder is ActiveDropPodInfo)
                {
                    ((ActiveDropPodInfo)this.parent.ParentHolder).GetDirectlyHeldThings().TryAdd(MinifyUtility.MakeMinified(transformed));
                    ((ActiveDropPodInfo)this.parent.ParentHolder).GetDirectlyHeldThings().Remove(this.parent);
                }
                else if(this.parent.ParentHolder is Caravan)
                {
                    ((Caravan)this.parent.ParentHolder).RemovePawn((Pawn)this.parent);
                    ((Caravan)this.parent.ParentHolder).AddPawnOrItem(MinifyUtility.MakeMinified(transformed),true);
                }
            }
            else if(this.parent.Spawned)
            {
                ((Pawn)this.parent).inventory.DropAllNearPawn(this.parent.Position, false, true);
                transformed.SpawnSetup(myMap, false);
            }
            transformed.HitPoints = (int)(transformed.MaxHitPoints * ((Pawn)this.parent).health.summaryHealth.SummaryHealthPercent);
            this.parent.Destroy(DestroyMode.Vanish);
        }

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			/*if (((Pawn)this.parent).drafter == null) {
				((Pawn)this.parent).drafter = new Pawn_DraftController ((Pawn)this.parent);
			}
			if (((Pawn)this.parent).drafter != null)
			{
				IEnumerable<Gizmo> draftGizmos = (IEnumerable<Gizmo>)typeof(Pawn_DraftController).GetMethod ("GetGizmos").Invoke (((Pawn)this.parent).drafter,new object[0]);
				foreach (Gizmo c2 in draftGizmos)
				{
					yield return c2;
				}
			}*/
			foreach (Gizmo g in base.CompGetGizmosExtra()) {
				yield return g;
			}
			if (parent.Faction != Faction.OfPlayer)
				yield break;
			Command_Action transform = new Command_Action();
			transform.defaultLabel = TranslatorFormattedStringExtensions.Translate("CommandToggleHover");
			transform.defaultDesc = TranslatorFormattedStringExtensions.Translate("CommandHoverOffDesc");
			transform.icon = TransformCommandTex;
			transform.action = delegate
			{
				this.transform();
			};
			yield return transform;
		}

		public override void CompTick()
		{
			base.CompTick ();
			if (this.parent.TryGetComp<CompRefuelable> ().Fuel <= 0 && (parent.ParentHolder is Map || parent.ParentHolder is Caravan)) {
				Pawn meAsPawn = (Pawn)this.parent;
				if (meAsPawn.IsCaravanMember ()) {
					Caravan myCaravan = meAsPawn.GetCaravan ();
					Thing myStack;
					Pawn uselessPawn;
					ThingDef fuelType=null;
					foreach (ThingDef theDef in meAsPawn.GetComp<CompRefuelable> ().Props.fuelFilter.AllowedThingDefs) {
						fuelType = theDef;
						break;
					}
					if (CaravanInventoryUtility.TryGetThingOfDef(myCaravan, fuelType,out myStack,out uselessPawn)) {
						int fuelToTake=(int)Mathf.Clamp(myStack.stackCount,1,meAsPawn.GetComp<CompRefuelable>().TargetFuelLevel);
						CaravanInventoryUtility.TakeThings(myCaravan, delegate(Thing thing) {
							if(thing.def==fuelType)
								return fuelToTake;
							return 0;
						});
						meAsPawn.GetComp<CompRefuelable> ().Refuel (fuelToTake);
					} 
					else {
						Messages.Message ("Shuttle in caravan has run out of fuel", MessageTypeDefOf.CautionInput);
						CaravanInventoryUtility.MoveAllInventoryToSomeoneElse (meAsPawn, myCaravan.PawnsListForReading, null);
						myCaravan.RemovePawn (meAsPawn);
						CaravanInventoryUtility.GiveThing (myCaravan, MinifyUtility.MakeMinified ((Building)ThingMaker.MakeThing (Props.buildingDef)));
						meAsPawn.Destroy (DestroyMode.Vanish);
					}
				} else {
					Messages.Message ("Shuttle has run out of fuel", MessageTypeDefOf.CautionInput);
					//Find.CameraDriver.SetRootPosAndSize (meAsPawn.Position.ToVector3(), 1);
					this.transform ();
				}
			}
		}
	}
}

