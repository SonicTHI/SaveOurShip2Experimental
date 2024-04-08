using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace RimWorld
{
	public class WorkGiver_LoadTorpedoTube : WorkGiver_Scanner
	{
		public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);

		public override PathEndMode PathEndMode => (PathEndMode)2;

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			Building_ShipTurretTorpedo building_Tube = t as Building_ShipTurretTorpedo;
			if (building_Tube == null || building_Tube.torpComp == null)
			{
				return false;
			}
			if (ForbidUtility.IsForbidden(t, pawn) || !ReservationUtility.CanReserveAndReach(pawn, t, (PathEndMode)2, DangerUtility.NormalMaxDanger(pawn), 1, -1, (ReservationLayerDef)null, false))
			{
				return false;
			}
			if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
			{
				return false;
			}
			if (building_Tube.torpComp.FullyLoaded)
			{
				JobFailReason.Is(TranslatorFormattedStringExtensions.Translate("SoS.TorpedoFullyLoaded"), (string)null);
				return false;
			}
			if (FireUtility.IsBurning(t))
			{
				return false;
			}
			if (FindAmmo(pawn, building_Tube) == null)
			{
				JobFailReason.Is(TranslatorFormattedStringExtensions.Translate("SoS.TorpedoNone"), (string)null);
				return false;
			}
			return true;
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			Building_ShipTurretTorpedo building_Tube = t as Building_ShipTurretTorpedo;
			if (building_Tube == null)
			{
				return null;
			}
			Thing val = FindAmmo(pawn, building_Tube);
			if (val == null)
			{
				return null;
			}
			Job val2 = new Job(DefDatabase<JobDef>.GetNamed("LoadTorpedoTube"), t, val);
			val2.count = 1;
			return (Job)(object)val2;
		}

		private Thing FindAmmo(Pawn pawn, Building_ShipTurretTorpedo tube)
		{
			StorageSettings allowedShellsSettings = ThingCompUtility.TryGetComp<CompChangeableProjectilePlural>(tube.gun).allowedShellsSettings;
			ThingRequest val = ThingRequest.ForGroup(ThingRequestGroup.Shell);
			return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, val, (PathEndMode)3, TraverseParms.For(pawn, (Danger)3, (TraverseMode)0, false), 9999f, (Predicate<Thing>)Predicate, (IEnumerable<Thing>)null, 0, -1, false, RegionType.Set_Passable, false);
			bool Predicate(Thing x)
			{
				if (!ForbidUtility.IsForbidden(x, pawn) && ReservationUtility.CanReserve(pawn, x, 1, -1, (ReservationLayerDef)null, false))
				{
					if (allowedShellsSettings != null)
					{
						return allowedShellsSettings.AllowedToAccept(x);
					}
					return true;
				}
				return false;
			}
		}

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
		{
			if (pawn.Map.GetComponent<ShipHeatMapComp>().TorpedoTubes.Any())
				return pawn.Map.GetComponent<ShipHeatMapComp>().TorpedoTubes;
			return new List<Thing>();
		}
	}
}
