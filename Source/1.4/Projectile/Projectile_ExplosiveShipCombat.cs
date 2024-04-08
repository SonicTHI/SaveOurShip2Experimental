using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using SaveOurShip2;

namespace RimWorld
{
	public class Projectile_ExplosiveShipCombat : Projectile_Explosive
	{
		public override void Tick()
		{
			base.Tick();
			if (this.Spawned)
			{
				foreach (CompShipCombatShield shield in this.Map.GetComponent<ShipHeatMapComp>().Shields)
				{
					if (!shield.shutDown && Position.DistanceTo(shield.parent.Position) <= shield.radius)
					{
						shield.HitShield(this);
						break;
					}
				}
			}
			if (!(this is Projectile_ExplosiveShipCombatPsychic))
			{
				if (this.Spawned && this.ExactPosition.ToIntVec3().GetThingList(this.Map).Any(t => t is Building b && (b.TryGetComp<CompSoShipPart>()?.Props.isPlating ?? false)))
				{
					Explode();
				}
			}
		}
		/*
		public override Vector3 ExactPosition
		{
			get
			{
				Vector3 b = (destination - origin) * Mathf.Clamp01(1f - ((float)ticksToImpact + 5) / StartingTicksToImpact); //Proximity fuze!
				return origin + b + Vector3.up * def.Altitude;
			}
		}*/
	}
}
