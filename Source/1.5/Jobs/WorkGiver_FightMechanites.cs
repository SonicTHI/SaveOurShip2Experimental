using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace SaveOurShip2
{
	class WorkGiver_FightMechanites : WorkGiver_Scanner
	{
		private const int NearbyPawnRadius = 15;

		private const int MaxReservationCheckDistance = 15;

		private const float HandledDistance = 5f;

		public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(ResourceBank.ThingDefOf.MechaniteFire);

		public override PathEndMode PathEndMode => PathEndMode.Touch;

		public override Danger MaxPathDanger(Pawn pawn)
		{
			return Danger.Deadly;
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			Fire fire = t as Fire;
			if (fire == null)
			{
				return false;
			}
			Pawn pawn2 = fire.parent as Pawn;
			if (pawn2 != null)
			{
				if (pawn2 == pawn)
				{
					return false;
				}
				if ((pawn2.Faction == pawn.Faction || pawn2.HostFaction == pawn.Faction || pawn2.HostFaction == pawn.HostFaction) && !pawn.Map.areaManager.Home[fire.Position] && IntVec3Utility.ManhattanDistanceFlat(pawn.Position, pawn2.Position) > 15)
				{
					return false;
				}
				if (!pawn.CanReach(pawn2, PathEndMode.Touch, Danger.Deadly))
				{
					return false;
				}
			}
			else
			{
				if (pawn.WorkTagIsDisabled(WorkTags.Firefighting))
				{
					return false;
				}
				if (!pawn.Map.areaManager.Home[fire.Position])
				{
					JobFailReason.Is(WorkGiver_FixBrokenDownBuilding.NotInHomeAreaTrans);
					return false;
				}
			}
			if ((pawn.Position - fire.Position).LengthHorizontalSquared > 225 && !pawn.CanReserve(fire, 1, -1, null, forced))
			{
				return false;
			}
			if (FireIsBeingHandled(fire, pawn))
			{
				return false;
			}
			return true;
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			return JobMaker.MakeJob(JobDefOf.BeatFire, t);
		}

		public static bool FireIsBeingHandled(Fire f, Pawn potentialHandler)
		{
			if (!f.Spawned)
			{
				return false;
			}
			return f.Map.reservationManager.FirstRespectedReserver(f, potentialHandler)?.Position.InHorDistOf(f.Position, 5f) ?? false;
		}
	}
}
