using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Outfitted
{
    [HarmonyPatch(typeof(Thing), nameof(Thing.DrawGUIOverlay))]
    static class Thing_DrawGUIOverlay_Patch
    {
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

            float score = JobGiver_OptimizeApparel.ApparelScoreGain( pawn, apparel );
            if (Math.Abs(score) > 0.01f)
            {
                var pos = GenMapUI.LabelDrawPosFor(apparel, 0f);
                GenMapUI.DrawThingLabel(pos, score.ToString("F1"), BeautyDrawer.BeautyColor(score, 3f));
            }
        }
    }
}
