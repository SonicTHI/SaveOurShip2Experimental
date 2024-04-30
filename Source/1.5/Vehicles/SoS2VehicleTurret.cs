using RimWorld;
using SmashTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Vehicles;
using Verse;

namespace SaveOurShip2.Vehicles
{
    class SoS2VehicleTurret : VehicleTurret
    {
        public bool isTorpedo;
        public int hardpoint;

        public SoS2VehicleTurret() : base()
        {

        }

        public SoS2VehicleTurret(VehiclePawn vehicle) : base(vehicle)
        {

        }

        public SoS2VehicleTurret(VehiclePawn vehicle, VehicleTurret reference) : base(vehicle, reference)
        {

        }

        public override void FireTurret()
        {
            if (!isTorpedo && vehicle.compFuel.fuel >= 1)
            {
                vehicle.compFuel.ConsumeFuel(2f/turretDef.magazineCapacity);
                base.FireTurret();
            }
            else if (isTorpedo)
                base.FireTurret();
        }

        public override IEnumerable<SubGizmo> SubGizmos
        {
            get
            {
                if (isTorpedo)
                    return base.SubGizmos;
                return new List<SubGizmo>();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref hardpoint, "hardpoint");
        }

        public override void DrawAt(Vector3 drawPos, Rot8 rot)
        {
            if (!vehicle.Spawned)
            {
                VehicleGraphics.DrawTurret(this, drawPos, Rot8.East);
            }
            else
                base.DrawAt(drawPos, rot);
        }
    }
}
