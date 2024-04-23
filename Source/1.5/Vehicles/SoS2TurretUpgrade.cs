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
    class SoS2TurretUpgrade : TurretUpgrade
    {
        public int turretSlot; //Used to hide upgrade nodes on shuttles which don't have enough hardpoints
        public bool useShuttleFuel;

        public override bool UnlockOnLoad => false;

        public override void Unlock(VehiclePawn vehicle, bool unlockingAfterLoad)
        {
            CompVehicleTurrets compTurrets = vehicle.GetComp<CompVehicleTurrets>(); //Needed because the standard cached version keeps nullref'ing
            SoS2VehicleTurret newTurret = (SoS2VehicleTurret)Activator.CreateInstance(typeof(SoS2VehicleTurret), vehicle, turrets[0]);
            newTurret.isTorpedo = !useShuttleFuel;
            newTurret.hardpoint = turretSlot;
            if(useShuttleFuel)
                newTurret.shellCount = newTurret.turretDef.magazineCapacity;

            newTurret.SetTarget(LocalTargetInfo.Invalid);
            newTurret.ResetAngle();
            newTurret.FillEvents_Def();
            compTurrets.turrets.Add(newTurret);
            compTurrets.RevalidateTurrets();
            compTurrets.CheckDuplicateKeys();
        }
    }
}
