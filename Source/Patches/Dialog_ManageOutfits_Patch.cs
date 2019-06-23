using System;
using System.Collections.Generic;
using System.Linq;

using Harmony;
using Multiplayer.API;

using RimWorld;
using UnityEngine;
using Verse;

namespace Outfitted
{
    [HarmonyPatch(typeof(Dialog_ManageOutfits), nameof(Dialog_ManageOutfits.DoWindowContents))]
    static class Dialog_ManageOutfits_DoWindowContents_Patch
    {
        const float marginVertical = 10f;

        const float marginLeft = 320f;
        const float marginRight = 10f;
        const float marginTop = 10f;
        const float marginBottom = 55f;

        const float MaxValue = 2.5f;

        static readonly FloatRange MinMaxTemperatureRange = new FloatRange(-100, 100);

        static Vector2 scrollPosition = Vector2.zero;

        static void Postfix(Rect inRect, Outfit ___selOutfitInt)
        {
            var selectedOutfit = ___selOutfitInt as ExtendedOutfit;
            if (selectedOutfit == null) {
                return;
            }

            Rect canvas = new Rect(
                marginLeft,
                Dialog_ManageOutfits.TopAreaHeight + marginTop,
                inRect.xMax - marginLeft - marginRight,
                inRect.yMax - Dialog_ManageOutfits.TopAreaHeight - marginTop - marginBottom
            );

            GUI.BeginGroup(canvas);
            Vector2 cur = Vector2.zero;

            if (MP.IsInMultiplayer) {
                MP.WatchBegin ();

                ExtendedOutfitProxy.Watch (ref selectedOutfit);
            }
            DrawDeadmanToogle (selectedOutfit, ref cur, canvas);
            DrawAutoWorkPrioritiesToggle( selectedOutfit, ref cur, canvas );
            DrawTemperatureStats(selectedOutfit, ref cur, canvas);
            cur.y += marginVertical;
            DrawApparelStats(selectedOutfit, cur, canvas);

            if (MP.IsInMultiplayer) {
                MP.WatchEnd ();
            } else if (GUI.changed) {
                var affected = Find.CurrentMap.mapPawns.FreeColonists
                                   .Where(i => i.outfits.CurrentOutfit == selectedOutfit);
                foreach (var pawn in affected) {
                    pawn.mindState?.Notify_OutfitChanged();
                }
            }

            GUI.EndGroup();

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        static void DrawDeadmanToogle(ExtendedOutfit selectedOutfit, ref Vector2 cur, Rect canvas)
        {
            Rect rect = new Rect(cur.x, cur.y, canvas.width, 30f);
            Widgets.CheckboxLabeled( rect, ResourceBank.Strings.PenaltyWornByCorpse,
                                     ref selectedOutfit.PenaltyWornByCorpse );
            TooltipHandler.TipRegion( rect, ResourceBank.Strings.PenaltyWornByCorpseTooltip);
            cur.y += rect.height;
        }

        static void DrawAutoWorkPrioritiesToggle( ExtendedOutfit outfit, ref Vector2 pos, Rect canvas )
        {
            Rect rect = new Rect( pos.x, pos.y, canvas.width, 30f );
            Widgets.CheckboxLabeled( rect, ResourceBank.Strings.AutoWorkPriorities,
                                     ref outfit.AutoWorkPriorities );
            TooltipHandler.TipRegion( rect, ResourceBank.Strings.AutoWorkPrioritiesTooltip );
            pos.y += rect.height;
        }

        static void DrawTemperatureStats(ExtendedOutfit selectedOutfit, ref Vector2 cur, Rect canvas)
        {
            // header
            Rect tempHeaderRect = new Rect(cur.x, cur.y, canvas.width, 30f);
            cur.y += 30f;
            Text.Anchor = TextAnchor.LowerLeft;
            Widgets.Label(tempHeaderRect, ResourceBank.Strings.PreferedTemperature);
            Text.Anchor = TextAnchor.UpperLeft;

            // line
            GUI.color = Color.grey;
            Widgets.DrawLineHorizontal(cur.x, cur.y, canvas.width);
            GUI.color = Color.white;

            // some padding
            cur.y += marginVertical;

            // temperature slider
            Rect sliderRect = new Rect(cur.x, cur.y, canvas.width - 20f, 40f);
            Rect tempResetRect = new Rect(sliderRect.xMax + 4f, cur.y + marginVertical, 16f, 16f);
            cur.y += 40f; // includes padding

            FloatRange targetTemps;
            if (selectedOutfit.targetTemperaturesOverride) {
                targetTemps = selectedOutfit.targetTemperatures;
                GUI.color = Color.white;
            } else {
                targetTemps = MinMaxTemperatureRange;
                GUI.color = Color.grey;
            }
            FloatRange minMaxTemps = MinMaxTemperatureRange;
            Widgets_FloatRange.FloatRange(sliderRect, 123123123, ref targetTemps, minMaxTemps, ToStringStyle.Temperature);
            GUI.color = Color.white;

            if (Math.Abs(targetTemps.min - selectedOutfit.targetTemperatures.min) > 1e-4
                || Math.Abs(targetTemps.max - selectedOutfit.targetTemperatures.max) > 1e-4)
            {
                selectedOutfit.targetTemperatures = targetTemps;
                selectedOutfit.targetTemperaturesOverride = true;
            }

            if (selectedOutfit.targetTemperaturesOverride)
            {
                if (Widgets.ButtonImage(tempResetRect, ResourceBank.Textures.ResetButton))
                {
                    selectedOutfit.targetTemperaturesOverride = false;
                    selectedOutfit.targetTemperatures = MinMaxTemperatureRange;
                }

                TooltipHandler.TipRegion(tempResetRect, ResourceBank.Strings.TemperatureRangeReset);
            }
        }

        static void DrawApparelStats(ExtendedOutfit selectedOutfit, Vector2 cur, Rect canvas)
        {
            // header
            Rect statsHeaderRect = new Rect(cur.x, cur.y, canvas.width, 30f);
            cur.y += 30f;
            Text.Anchor = TextAnchor.LowerLeft;
            Text.Font = GameFont.Small;
            Widgets.Label(statsHeaderRect, ResourceBank.Strings.PreferedStats);
            Text.Anchor = TextAnchor.UpperLeft;

            // add button
            Rect addStatRect = new Rect(statsHeaderRect.xMax - 16f, statsHeaderRect.yMin + marginVertical, 16f, 16f);
            if (Widgets.ButtonImage(addStatRect, ResourceBank.Textures.AddButton))
            {
                var options = new List<FloatMenuOption>();
                foreach (var def in selectedOutfit.UnassignedStats.OrderBy(i => i.label).OrderBy(i => i.category.displayOrder))
                {
                    FloatMenuOption option = new FloatMenuOption(def.LabelCap, delegate
                    {
                        selectedOutfit.AddStatPriority(def, 0f);
                    });
                    options.Add(option);
                }

                Find.WindowStack.Add(new FloatMenu(options));
            }

            TooltipHandler.TipRegion(addStatRect, ResourceBank.Strings.StatPriorityAdd);

            // line
            GUI.color = Color.grey;
            Widgets.DrawLineHorizontal(cur.x, cur.y, canvas.width);
            GUI.color = Color.white;

            // some padding
            cur.y += marginVertical;

            var stats = selectedOutfit.StatPriorities.ToList();

            // main content in scrolling view
            Rect contentRect = new Rect(cur.x, cur.y, canvas.width, canvas.height - cur.y);
            Rect viewRect = new Rect(contentRect)
            {
                height = 30f * stats.Count
            };

            if (viewRect.height > contentRect.height)
            {
                viewRect.width -= 20f;
            }

            Widgets.BeginScrollView(contentRect, ref scrollPosition, viewRect);

            GUI.BeginGroup(viewRect);
            cur = Vector2.zero;

            // none label
            if (stats.Count > 0)
            {
                // legend kind of thingy.
                Rect legendRect = new Rect(cur.x + (viewRect.width - 24) / 2, cur.y, (viewRect.width - 24) / 2, 20f);
                Text.Font = GameFont.Tiny;
                GUI.color = Color.grey;
                Text.Anchor = TextAnchor.LowerLeft;
                Widgets.Label(legendRect, "-" + MaxValue.ToString("N1"));
                Text.Anchor = TextAnchor.LowerRight;
                Widgets.Label(legendRect, MaxValue.ToString("N1"));
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
                cur.y += 15f;

                // statPriority weight sliders
                foreach (var stat in stats)
                {
                    DrawStatRow(selectedOutfit, stat, ref cur, viewRect.width);
                }
            }
            else
            {
                Rect noneLabel = new Rect(cur.x, cur.y, viewRect.width, 30f);
                GUI.color = Color.grey;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(noneLabel, ResourceBank.Strings.None);
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                cur.y += 30f;
            }

            GUI.EndGroup();
            Widgets.EndScrollView();
        }

        static void DrawStatRow(ExtendedOutfit selectedOutfit, StatPriority statPriority, ref Vector2 cur, float width)
        {
            // set up rects
            Rect labelRect = new Rect(cur.x, cur.y, (width - 24) / 2f, 30f);
            Rect sliderRect = new Rect(labelRect.xMax + 4f, cur.y + 5f, labelRect.width, 25f);
            Rect buttonRect = new Rect(sliderRect.xMax + 4f, cur.y + 3f, 16f, 16f);

            // draw label
            Text.Font = Text.CalcHeight(statPriority.Stat.LabelCap, labelRect.width) > labelRect.height
                            ? GameFont.Tiny
                            : GameFont.Small;

            GUI.color = AssigmentColor(statPriority);

            Widgets.Label(labelRect, statPriority.Stat.LabelCap);
            Text.Font = GameFont.Small;

            // draw button
            // if manually added, delete the priority
            string buttonTooltip = string.Empty;
            if (statPriority.IsManual)
            {
                buttonTooltip = ResourceBank.Strings.StatPriorityDelete(statPriority.Stat.LabelCap);
                if (Widgets.ButtonImage(buttonRect, ResourceBank.Textures.DeleteButton))
                {
                    selectedOutfit.RemoveStatPriority(statPriority.Stat);
                }
            }
            // if overridden auto assignment, reset to auto
            else if (statPriority.IsOverride)
            {
                buttonTooltip = ResourceBank.Strings.StatPriorityReset(statPriority.Stat.LabelCap);
                if (Widgets.ButtonImage(buttonRect, ResourceBank.Textures.ResetButton))
                {
                    statPriority.Weight = statPriority.Default;

                    if (MP.IsInMultiplayer) {
                        ExtendedOutfitProxy.SetStatPriority (selectedOutfit.uniqueId, statPriority.Stat, statPriority.Default);
                    }
                }
            }

            // draw line behind slider
            GUI.color = new Color(.3f, .3f, .3f);
            for (int y = (int)cur.y; y < cur.y + 30; y += 5)
            {
                Widgets.DrawLineVertical((sliderRect.xMin + sliderRect.xMax) / 2f, y, 3f);
            }

            // draw slider
            GUI.color = AssigmentColor(statPriority);

            float weight = GUI.HorizontalSlider(sliderRect, statPriority.Weight, -MaxValue, MaxValue);

            if (Mathf.Abs(weight - statPriority.Weight) > 1e-4)
            {
                statPriority.Weight = weight;

                if (MP.IsInMultiplayer) {
                    ExtendedOutfitProxy.SetStatPriority (selectedOutfit.uniqueId, statPriority.Stat, weight);
                }
            }

            GUI.color = Color.white;

            // tooltips
            TooltipHandler.TipRegion(labelRect, statPriority.Stat.LabelCap + "\n\n" + statPriority.Stat.description);
            if (buttonTooltip != string.Empty)
            {
                TooltipHandler.TipRegion(buttonRect, buttonTooltip);
            }

            TooltipHandler.TipRegion(sliderRect, statPriority.Weight.ToStringByStyle(ToStringStyle.FloatTwo));

            // advance row
            cur.y += 30f;
        }

        static Color AssigmentColor(StatPriority statPriority)
        {
            if (statPriority.IsManual) {
                return Color.white;
            }

            if (statPriority.IsDefault) {
                return Color.grey;
            }

            if (statPriority.IsOverride) {
                return new Color(0.75f, 0.69f, 0.33f);
            }

            return Color.cyan;
        }
    }
}
