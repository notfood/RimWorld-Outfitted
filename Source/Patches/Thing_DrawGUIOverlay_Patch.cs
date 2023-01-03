using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Outfitted
{
    [HarmonyPatch(typeof(Thing), nameof(Thing.DrawGUIOverlay))]
    static class Thing_DrawGUIOverlay_Patch
    {
        static int cachedId = -1;
        static int cachedTick = -1;
        static List<float> cachedScores = new List<float>();

        static void Postfix(Thing __instance)
        {
            if (!OutfittedMod.showApparelScores) {
                return;
            }

            if (Find.CameraDriver.CurrentZoom != CameraZoomRange.Closest) {
                return;
            }

            var pawn = Find.Selector.SingleSelectedThing as Pawn;
            if (pawn == null || !pawn.IsColonistPlayerControlled) {
                return;
            }

            var apparel = __instance as Apparel;
            if (apparel == null) {
                return;
            }

            var outfit = pawn.outfits.CurrentOutfit as ExtendedOutfit;
            if (outfit == null)
            {
                return;
            }

            if (!outfit.filter.Allows(apparel))
            {
                return;
            }

            var scores = CachedScoresForPawn(pawn);

            float score = JobGiver_OptimizeApparel.ApparelScoreGain(pawn, apparel, scores);
            if (Math.Abs(score) > 0.01f)
            {
                var pos = GenMapUI.LabelDrawPosFor(apparel, 0f);
                GenMapUI.DrawThingLabel(pos, score.ToString("F1"), BeautyDrawer.BeautyColor(score, 3f));
            }
        }

        static List<float> CachedScoresForPawn(Pawn pawn)
        {
            if (cachedId != pawn.thingIDNumber || cachedTick < GenTicks.TicksGame) {
                cachedScores = ScoresForPawn(pawn);
                cachedId = pawn.thingIDNumber;
                cachedTick = GenTicks.TicksGame;
            }

            return cachedScores;
        }

        static List<float> ScoresForPawn(Pawn pawn)
        {
            var wornApparelScores = new List<float>();
            for (int i = 0; i < pawn.apparel.WornApparel.Count; i++)
            {
                wornApparelScores.Add(JobGiver_OptimizeApparel.ApparelScoreRaw(pawn, pawn.apparel.WornApparel[i]));
            }
            return wornApparelScores;
        }
    }
}
