using RimWorld;
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
    class CompHoverMode : VehicleComp
    {
        public CompProperties_HoverMode Props => props as CompProperties_HoverMode;

        public override void CompTick()
        {
            base.CompTick();
            if (Vehicle.Spawned && Vehicle.ignition.Drafted)
                LaunchProtocol.ThrowFleck(ResourceBank.FleckDefOf.SoS2Exhaust_Short, parent.DrawPos, parent.Map, 0.8f, 1, Rand.Range(0,360), 15, 0);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Command_ActionHighlighter
            {
                defaultLabel = Translator.Translate("SoS.HoverJump"),
                defaultDesc = Translator.Translate("SoS.HoverJump.Desc"),
                icon = ContentFinder<Texture2D>.Get("UI/Hover_On_Icon"),
                action = delegate()
                {
                    Vehicle.CompFueledTravel?.ConsumeFuel(5);
                    LandingTargeter.Instance.BeginTargeting(Vehicle, Vehicle.Map, delegate(LocalTargetInfo target, Rot4 rot)
                    {
                        Vehicle.CompVehicleLauncher.TryLaunch(Vehicle.Map.Tile, new AerialVehicleArrivalAction_LandSpecificCell(Vehicle, Vehicle.Map.Parent, Vehicle.Map.Tile, target.Cell, rot));
                    }, (LocalTargetInfo targetInfo) => !Ext_Vehicles.IsRoofRestricted(Vehicle.VehicleDef, targetInfo.Cell, Vehicle.Map), null, null, true);
                },
                disabled = Ext_Vehicles.IsRoofRestricted(Vehicle.VehicleDef, Vehicle.Position, Vehicle.Map),
                disabledReason = "Cannot launch under roof"
			};
		}
    }
}
