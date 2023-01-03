using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Outfitted
{
    [HarmonyPatch (typeof(JobGiver_OptimizeApparel), nameof(JobGiver_OptimizeApparel.ApparelScoreRaw))]
    static class JobGiver_OptimizeApparel_ApparelScoreRaw_Patch
    {
        /*
         * Inserts ApparelScoreExtra before testing Thing.Stuff, ignores scoring behind.
         * Outfitted replaces Stats, Temperature, useHitPoints and WornByCorpse
         */
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var add = AccessTools.Method(typeof(JobGiver_OptimizeApparel_ApparelScoreRaw_Patch), nameof(ApparelScoreExtra));
            var find = AccessTools.PropertyGetter(typeof(Thing), nameof(Thing.Stuff));
            var fld = AccessTools.Field(typeof(JobGiver_OptimizeApparel), "neededWarmth");

            foreach (var ins in instructions)
            {
                if (ins.opcode == OpCodes.Callvirt && find.Equals(ins.operand))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldsfld, fld);
                    yield return new CodeInstruction(OpCodes.Call, add);
                    yield return new CodeInstruction(OpCodes.Stloc_0);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                }

                yield return ins;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float ApparelScoreExtra(Apparel ap, Pawn pawn, NeededWarmth neededWarmth)
        {
            return OutfittedMod.ApparelScoreExtra(pawn, ap, neededWarmth);
        }
    }
}
