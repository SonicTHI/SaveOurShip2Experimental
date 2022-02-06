using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using SaveOurShip2;
using Verse;
using Verse.AI;

namespace RimworldMod.VacuumIsNotFun {

    [HarmonyPatch(typeof(PathFinder), "FindPath", typeof(IntVec3), typeof(LocalTargetInfo), typeof(TraverseParms),
        typeof(PathEndMode), typeof(PathFinderCostTuning))]
    public static class H_Vacuum_PathFinder {
        private const int SpaceTileCostUnsuited = 10000;
        private const int SpaceTileCostSuited = 100;

        // The purpose of this transpiler is to add the pathfinding costs for space into the pathfinding code
        // We're looking for a line at the end of the calculation of the cost of a tile that looks like:
        //     int num15 = num14 + PathFinder.calcGrid[index3].knownCost;
        // We want to patch our pathfinding cost right above that line
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            var patched = false;
            var gotIndex = false;
            var gotCost = false;

            var indexOperand = new object();
            var costOperand = new object();

            CodeInstruction lastCode = null;

            var blueprintField = AccessTools.Field(typeof(PathFinder), "blueprintGrid");
            var signalField = AccessTools.Field(typeof(PathFinder), "calcGrid");

            foreach (var code in instructions) {
                // Need to get some operands - specifically, the operands for index5 (cell location) and
                // num14 (cell cost)

                // Retrieve num14 (cell cost) operand from a const addition above our injection point
                if (!gotCost && lastCode?.opcode == OpCodes.Ldloc_S && code.LoadsConstant(600)) {
                    costOperand = lastCode.operand;
                    gotCost = true;
                }

                // Retrieve index5 (cell location) operand from blueprint grid just above injection point
                if (!gotIndex && code.opcode == OpCodes.Ldloc_S && lastCode.LoadsField(blueprintField)) {
                    indexOperand = code.operand;
                    gotIndex = true;
                }

                // Our injection point is the first access to PathFinder.calcGrid directly after num14 is loaded
                // Note that the total cell cost (num14) is already loaded onto the stack by now, which is fine because
                // we need to add to it anyway
                if (!patched && lastCode?.opcode == OpCodes.Ldloc_S && (lastCode?.OperandIs(costOperand) ?? false) &&
                    code.LoadsField(signalField)) {
                    yield return new CodeInstruction(OpCodes.Ldarg_0); // Load this
                    var mapField = AccessTools.Field(typeof(PathFinder), "map");
                    yield return new CodeInstruction(OpCodes.Ldfld, mapField); // Load map
                    yield return new CodeInstruction(OpCodes.Ldarg_3); // Load TraverseParms
                    yield return new CodeInstruction(OpCodes.Ldloc_S, indexOperand); // Load tile index
                    var costMethod = AccessTools.Method(typeof(H_Vacuum_PathFinder), nameof(AdditionalPathCost));
                    yield return new CodeInstruction(OpCodes.Call, costMethod); // Call method to get tile cost
                    yield return new CodeInstruction(OpCodes.Add); // Add num14 and our cost
                    yield return new CodeInstruction(OpCodes.Stloc_S, costOperand); // Store updated tile cost
                    yield return new CodeInstruction(OpCodes.Ldloc_S, costOperand); // Load cost to replace one we took

                    patched = true;
                }

                lastCode = code;
                yield return code;
            }
        }

        // Generate additional pathfinding costs for tiles that are in space
        public static int AdditionalPathCost(Map map, TraverseParms parms, int index) {
            // Only run in space, and if pawn doesn't have a space suit
            if (!map.IsSpace() || (!ShipInteriorMod2.useVacuumPathfinding && parms.pawn.Faction.IsPlayer)) return 0;

            // Find tile room
            var room = map.cellIndices.IndexToCell(index).GetRoom(map);

            // If room isn't space, zero extra cost
            if (!room?.IsSpace() ?? true) return 0;

            // If room is space, cost depending on whether pawn is suited or not
            return ShipInteriorMod2.EVAlevel(parms.pawn) > 6 ? SpaceTileCostSuited : SpaceTileCostUnsuited;
        }
    }

    [HarmonyPatch(typeof(Region), "DangerFor")]
    public static class H_Vacuum_Region_Danger {

        // The purpose of this transpiler is to increase the danger of vacuum regions
        // We're looking for a line right before the danger is cached and returned that looks like:
        //     if (Current.ProgramState == ProgramState.Playing)
        // We want to patch our additional danger into that if statement
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            var patched = false;

            CodeInstruction lastLastCode = null;
            CodeInstruction lastCode = null;

            var signalMethod = AccessTools.Method(typeof(Current), "get_ProgramState");

            foreach (var code in instructions) {
                // Our injection point is after the call to program state right after danger (local variable 1) is
                // stored (essentially, in the middle of an if statement, but need to dodge labels)
                if (!patched && (lastLastCode?.opcode == OpCodes.Stloc_1) && (lastCode?.Calls(signalMethod) ?? false)) {
                    yield return new CodeInstruction(OpCodes.Ldloc_1); // Load danger
                    yield return new CodeInstruction(OpCodes.Ldarg_0); // Load this
                    var roomProperty = AccessTools.Method(typeof(Region), "get_Room");
                    yield return new CodeInstruction(OpCodes.Call, roomProperty); // Load room
                    yield return new CodeInstruction(OpCodes.Ldarg_1); // Load pawn
                    yield return new CodeInstruction(OpCodes.Ldarg_0); // Load this
                    var mapProperty = AccessTools.Method(typeof(Region), "get_Map");
                    yield return new CodeInstruction(OpCodes.Call, mapProperty); // Load map
                    var addDangerMethod = AccessTools.Method(typeof(VacuumExtensions),
                        nameof(VacuumExtensions.ExtraDangerFor));
                    yield return new CodeInstruction(OpCodes.Call, addDangerMethod); // Call method to get danger
                    yield return new CodeInstruction(OpCodes.Stloc_1); // Store updated danger

                    patched = true;
                }

                lastLastCode = lastCode;
                lastCode = code;
                yield return code;
            }
        }
    }

    public static class VacuumExtensions {

        public static Danger ExtraDangerFor(Danger original, Room room, Pawn p, Map map) {
            // Always pass through deadly, if tile or map isn't space, return normal danger
            if (original == Danger.Deadly || !map.IsSpace() || (!ShipInteriorMod2.useVacuumPathfinding && p.Faction.IsPlayer) || (!room?.IsSpace() ?? true)) return original;

            return ShipInteriorMod2.EVAlevel(p) > 3 ? Danger.Some : Danger.Deadly;
        }

        public static bool IsSpace(this Room room) {
            return room.FirstRegion.type != RegionType.Portal && (room.OpenRoofCount > 0 || room.TouchesMapEdge);
        }
    }
}
