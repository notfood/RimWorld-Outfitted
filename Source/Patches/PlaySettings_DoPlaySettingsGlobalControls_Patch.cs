using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Outfitted
{
    [HarmonyPatch(typeof(PlaySettings), nameof(PlaySettings.DoPlaySettingsGlobalControls))]
    static class PlaySettings_DoPlaySettingsGlobalControls_Patch
    {
        static void Postfix(WidgetRow row, bool worldView)
        {
            if (worldView)
            {
                return;
            }

            row.ToggleableIcon(ref OutfittedMod.showApparelScores, ResourceBank.Textures.ShirtBasic, ResourceBank.Strings.OutfitShow, SoundDefOf.Mouseover_ButtonToggle, null);
        }
    }
}
