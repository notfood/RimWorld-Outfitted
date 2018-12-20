using Harmony;
using RimWorld;
using Verse;

namespace Outfitted
{
    [HarmonyPatch (typeof(JobGiver_OptimizeApparel), nameof(JobGiver_OptimizeApparel.ApparelScoreRaw))]
    static class JobGiver_OptimizeApparel_ApparelScoreRaw_Patch
    {
        static bool Prefix(Pawn pawn, Apparel ap, out float __result, NeededWarmth ___neededWarmth) {

            __result = OutfittedMod.ApparelScoreRaw(pawn, ap, ___neededWarmth);

            return false;
        }
    }
}
