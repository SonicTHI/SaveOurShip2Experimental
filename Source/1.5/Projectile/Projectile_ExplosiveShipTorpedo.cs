using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SaveOurShip2
{
	class Projectile_ExplosiveShipTorpedo : Projectile_ExplosiveShip
	{
		public override Vector3 ExactPosition
		{
			get
			{
				Vector3 b = (destination - origin) * Mathf.Clamp01(1f - ((float)ticksToImpact + 15) / StartingTicksToImpact); //Proximity fuze!
				return origin + b + Vector3.up * def.Altitude;
			}
		}
	}
}
