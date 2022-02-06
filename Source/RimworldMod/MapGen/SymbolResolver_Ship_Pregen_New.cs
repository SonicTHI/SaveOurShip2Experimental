using System;
using UnityEngine;
using Verse;
using SaveOurShip2;

namespace RimWorld.BaseGen
{
    public class SymbolResolver_Ship_Pregen_New : SymbolResolver
    {
        private struct SpawnDescriptor
        {
            public IntVec3 offset;

            public ThingDef def;

            public Rot4 rot;
        }

        public override void Resolve(ResolveParams rp)
        {
            Building core = null;
            try { ShipInteriorMod2.GenerateShip(DefDatabase<EnemyShipDef>.GetNamed("CharlonWhitestone"), BaseGen.globalSettings.map, null, Faction.OfPlayer, null, out core, false, true); } catch (Exception e) { Log.Error(e.ToString()); }
            foreach(Thing thing in BaseGen.globalSettings.map.listerThings.ThingsInGroup(ThingRequestGroup.Refuelable))
            {
                ((ThingWithComps)thing).TryGetComp<CompRefuelable>().Refuel(9999);
            }
            core.TryGetComp<CompBuildingConsciousness>().AIName = "Charlon Whitestone";
            /*SymbolResolver_Ship_Pregen_New.SpawnDescriptor[] array = new SymbolResolver_Ship_Pregen_New.SpawnDescriptor[]
            {
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(3, 0, 3),
                    def = ThingDefOf.Ship_Reactor,
                    rot = Rot4.North
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(3, 0, 8),
                    def = ThingDef.Named("ShipCapacitor"),
                    rot = Rot4.East
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(0, 0, 6),
                    def = ThingDef.Named("ShipAirlock"),
                    rot = Rot4.East
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(6, 0, 6),
                    def = ThingDef.Named("ShipAirlock"),
                    rot = Rot4.East
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(14, 0, 6),
                    def = ThingDef.Named("ShipAirlock"),
                    rot = Rot4.East
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(20, 0, 6),
                    def = ThingDef.Named("ShipAirlock"),
                    rot = Rot4.East
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(0, 0, 6),
                    def = ThingDef.Named("ShipAirlock"),
                    rot = Rot4.East
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(10, 0, 19),
                    def = ThingDef.Named("ShipAirlock"),
                    rot = Rot4.East
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(17, 0, 3),
                    def = ThingDef.Named("CargoShuttle"),
                    rot = Rot4.North
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(17, 0, 3),
                    def = ThingDef.Named("ShipShuttleBay"),
                    rot = Rot4.North
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(17, 0, -2),
                    def = ThingDef.Named("Ship_Engine"),
                    rot = Rot4.North
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(3, 0, -2),
                    def = ThingDef.Named("Ship_Engine"),
                    rot = Rot4.North
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(8, 0, 17),
                    def = ThingDef.Named("Ship_ComputerCore"),
                    rot = Rot4.North
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(12, 0, 17),
                    def = ThingDef.Named("Ship_LifeSupport"),
                    rot = Rot4.North
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(10, 0, 21),
                    def = ThingDef.Named("ShipPilotSeat"),
                    rot = Rot4.North
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(10, 0, 24),
                    def = ThingDef.Named("Ship_SensorCluster"),
                    rot = Rot4.North
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(0, 0, 4),
                    def = ThingDef.Named("ShipInside_SolarGenerator"),
                    rot = Rot4.East
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(20, 0, 4),
                    def = ThingDef.Named("ShipInside_SolarGenerator"),
                    rot = Rot4.West
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(6, 0, 12),
                    def = ThingDef.Named("ShipInside_SolarGenerator"),
                    rot = Rot4.East
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(6, 0, 14),
                    def = ThingDef.Named("ShipInside_SolarGenerator"),
                    rot = Rot4.East
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(14, 0, 12),
                    def = ThingDef.Named("ShipInside_SolarGenerator"),
                    rot = Rot4.West
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(14, 0, 14),
                    def = ThingDef.Named("ShipInside_SolarGenerator"),
                    rot = Rot4.West
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(0, 0, 8),
                    def = ThingDef.Named("ShipInside_PassiveCooler"),
                    rot = Rot4.East
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(20, 0, 8),
                    def = ThingDef.Named("ShipInside_PassiveCooler"),
                    rot = Rot4.West
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(10, 0, 5),
                    def = ThingDef.Named("ShipInside_PassiveCooler"),
                    rot = Rot4.North
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(9, 0, 20),
                    def = ThingDefOf.StandingLamp,
                    rot = Rot4.North
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(11, 0, 20),
                    def = ThingDefOf.Heater,
                    rot = Rot4.North
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(9, 0, 7),
                    def = ThingDefOf.StandingLamp,
                    rot = Rot4.North
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(11, 0, 7),
                    def = ThingDefOf.Heater,
                    rot = Rot4.North
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(15, 0, 9),
                    def = ThingDefOf.StandingLamp,
                    rot = Rot4.North
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(19, 0, 9),
                    def = ThingDefOf.Heater,
                    rot = Rot4.North
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(7, 0, 7),
                    def = ThingDefOf.CryptosleepCasket,
                    rot = Rot4.East
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(7, 0, 8),
                    def = ThingDefOf.CryptosleepCasket,
                    rot = Rot4.East
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(7, 0, 9),
                    def = ThingDefOf.CryptosleepCasket,
                    rot = Rot4.East
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(7, 0, 10),
                    def = ThingDefOf.CryptosleepCasket,
                    rot = Rot4.East
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(7, 0, 11),
                    def = ThingDefOf.CryptosleepCasket,
                    rot = Rot4.East
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(7, 0, 12),
                    def = ThingDefOf.CryptosleepCasket,
                    rot = Rot4.East
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(7, 0, 13),
                    def = ThingDefOf.CryptosleepCasket,
                    rot = Rot4.East
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(7, 0, 14),
                    def = ThingDefOf.CryptosleepCasket,
                    rot = Rot4.East
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(7, 0, 15),
                    def = ThingDefOf.CryptosleepCasket,
                    rot = Rot4.East
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(12, 0, 7),
                    def = ThingDefOf.CryptosleepCasket,
                    rot = Rot4.East
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(12, 0, 8),
                    def = ThingDefOf.CryptosleepCasket,
                    rot = Rot4.East
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(12, 0, 9),
                    def = ThingDefOf.CryptosleepCasket,
                    rot = Rot4.East
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(12, 0, 10),
                    def = ThingDefOf.CryptosleepCasket,
                    rot = Rot4.East
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(12, 0, 11),
                    def = ThingDefOf.CryptosleepCasket,
                    rot = Rot4.East
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(12, 0, 12),
                    def = ThingDefOf.CryptosleepCasket,
                    rot = Rot4.East
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(12, 0, 13),
                    def = ThingDefOf.CryptosleepCasket,
                    rot = Rot4.East
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(12, 0, 14),
                    def = ThingDefOf.CryptosleepCasket,
                    rot = Rot4.East
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(12, 0, 15),
                    def = ThingDefOf.CryptosleepCasket,
                    rot = Rot4.East
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(9, 0, 9),
                    def = ThingDefOf.CryptosleepCasket,
                    rot = Rot4.South
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(9, 0, 11),
                    def = ThingDefOf.CryptosleepCasket,
                    rot = Rot4.South
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(9, 0, 13),
                    def = ThingDefOf.CryptosleepCasket,
                    rot = Rot4.South
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(9, 0, 15),
                    def = ThingDefOf.CryptosleepCasket,
                    rot = Rot4.South
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(11, 0, 8),
                    def = ThingDefOf.CryptosleepCasket,
                    rot = Rot4.North
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(11, 0, 10),
                    def = ThingDefOf.CryptosleepCasket,
                    rot = Rot4.North
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(11, 0, 12),
                    def = ThingDefOf.CryptosleepCasket,
                    rot = Rot4.North
                },
                new SymbolResolver_Ship_Pregen_New.SpawnDescriptor
                {
                    offset = new IntVec3(11, 0, 14),
                    def = ThingDefOf.CryptosleepCasket,
                    rot = Rot4.North
                },
            };
            BaseGen.symbolStack.Push("refuel", rp);
            BaseGen.symbolStack.Push("chargeBatteries", rp);

            CellRect baseRect = rp.rect;
            for (int i = 0; i < array.Length; i++)
            {
                rp.rect = new CellRect(baseRect.minX + array[i].offset.x, baseRect.minZ + array[i].offset.z, 1, 1);
                rp.thingRot = array[i].rot;
                rp.singleThingDef = array[i].def;
                BaseGen.symbolStack.Push("thing", rp);
            }

            ResolveParams parms = rp;
            parms.rect = new CellRect(baseRect.minX, baseRect.minZ, 7, 11);
            parms.disableSinglePawn = true; //Using another kludge to pass a bool into the next symbol!
            BaseGen.symbolStack.Push("shipemptyRoom", parms);
            ResolveParams parms2 = rp;
            parms2.rect = new CellRect(baseRect.minX + 14, baseRect.minZ, 7, 11);
            parms2.disableSinglePawn = true; //Using another kludge to pass a bool into the next symbol!
            BaseGen.symbolStack.Push("shipemptyRoom", parms2);
            ResolveParams parms3 = rp;
            parms3.rect = new CellRect(baseRect.minX + 6, baseRect.minZ + 5, 9, 15);
            parms3.disableSinglePawn = true; //Using another kludge to pass a bool into the next symbol!
            BaseGen.symbolStack.Push("shipemptyRoom", parms3);
            ResolveParams parms4 = rp;
            parms4.rect = new CellRect(baseRect.minX + 7, baseRect.minZ + 19, 7, 5);
            parms4.disableSinglePawn = true; //Using another kludge to pass a bool into the next symbol!
            BaseGen.symbolStack.Push("shipemptyRoom", parms4);
            */
        }
    }
}
