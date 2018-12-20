using System;
using System.Linq;
using Harmony;
using RimWorld;
using UnityEngine;
using Verse;

namespace Outfitted
{
    [StaticConstructorOnStartup]
    public static class OutfittedMod
    {
        internal static bool showApparelScores;

        static OutfittedMod()
        {
            HarmonyInstance.Create("rimworld.outfitted").PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
        }

        static readonly SimpleCurve HitPointsPercentScoreFactorCurve = new SimpleCurve {
            {
                new CurvePoint (0f, 0f),
                true
            },
            {
                new CurvePoint (0.2f, 0.2f),
                true
            },
            {
                new CurvePoint (0.22f, 0.6f),
                true
            },
            {
                new CurvePoint (0.5f, 0.6f),
                true
            },
            {
                new CurvePoint (0.52f, 1f),
                true
            }
        };

        static readonly SimpleCurve InsulationColdScoreFactorCurve_NeedWarm = new SimpleCurve {
            {
                new CurvePoint (0f, 1f),
                true
            },
            {
                new CurvePoint (30f, 8f),
                true
            }
        };

        static readonly SimpleCurve InsulationFactorCurve = new SimpleCurve {
            new CurvePoint(-5f, 0.1f),
            new CurvePoint(0f, 1f),
            new CurvePoint(100f, 4f)
        };

        public static float ApparelScoreRaw(Pawn pawn, Apparel apparel, NeededWarmth neededWarmth = NeededWarmth.Any)
        {
            var outfit = pawn.outfits.CurrentOutfit as ExtendedOutfit;
            if (outfit == null)
            {
                Log.ErrorOnce("Outfitted :: Not an ExtendedOutfit, something went wrong.", 399441);
                return 0f;
            }

            float score = 0.1f + ApparelScoreRawPriorities(pawn, apparel, outfit);

            if (apparel.def.useHitPoints)
            {
                float x = (float)apparel.HitPoints / apparel.MaxHitPoints;
                score *= HitPointsPercentScoreFactorCurve.Evaluate(x);
            }
            score += apparel.GetSpecialApparelScoreOffset();

            score *= ApparelScoreRawInsulation(pawn, apparel, outfit, neededWarmth);

            if (apparel.WornByCorpse && (pawn == null || ThoughtUtility.CanGetThought(pawn, ThoughtDefOf.DeadMansApparel)))
            {
                score -= 0.5f;
                if (score > 0f)
                {
                    score *= 0.1f;
                }
            }
            if (apparel.Stuff == ThingDefOf.Human.race.leatherDef)
            {
                if (pawn == null || ThoughtUtility.CanGetThought(pawn, ThoughtDefOf.HumanLeatherApparelSad))
                {
                    score -= 0.5f;
                    if (score > 0f)
                    {
                        score *= 0.1f;
                    }
                }
                if (pawn != null && ThoughtUtility.CanGetThought(pawn, ThoughtDefOf.HumanLeatherApparelHappy))
                {
                    score += 0.12f;
                }
            }

            return score;
        }

        static float ApparelScoreRawPriorities(Pawn pawn, Apparel apparel, ExtendedOutfit outfit)
        {
            if (outfit.StatPriorities.Count() == 0) {
                return 0f;
            }

            return outfit.StatPriorities
                         .Select(sp => new
                         {
                             weight = sp.Weight,
                             value = apparel.GetStatValue(sp.Stat, true),
                             def = sp.Stat.defaultBaseValue,
                         })
                         .Average(sp => (Math.Abs(sp.def) < 0.001f ? sp.value : (sp.value - sp.def)/sp.def) * Mathf.Pow(sp.weight, 3));
        }

        static float ApparelScoreRawInsulation(Pawn pawn, Apparel apparel, ExtendedOutfit outfit, NeededWarmth neededWarmth)
        {
            float insulation = 1f;

            if (outfit.targetTemperaturesOverride)
            {
                var comfortableRange = pawn.ComfortableTemperatureRange();
                var targetRange = outfit.targetTemperatures;
                var insulationRange = GetInsulationStats(apparel);

                if (pawn.apparel.WornApparel.Contains(apparel))
                {
                    comfortableRange.min -= insulationRange.min;
                    comfortableRange.max -= insulationRange.max;
                }
                else
                {
                    foreach (var otherApparel in pawn.apparel.WornApparel)
                    {
                        if (!ApparelUtility.CanWearTogether(apparel.def, otherApparel.def, pawn.RaceProps.body))
                        {
                            var otherInsulationRange = GetInsulationStats(otherApparel);

                            comfortableRange.min -= otherInsulationRange.min;
                            comfortableRange.max -= otherInsulationRange.max;
                        }
                    }
                }

                FloatRange temperatureScoreOffset = new FloatRange(0f, 0f);

                // cold values are negative
                float neededInsulationCold = targetRange.min - comfortableRange.min;
                if (neededInsulationCold < 0)
                {
                    // too cold
                    if (neededInsulationCold > insulationRange.min)
                    {
                        temperatureScoreOffset.min += neededInsulationCold;
                    }
                    else
                    {
                        temperatureScoreOffset.min += insulationRange.min;
                    }
                }
                else
                {
                    // warm enough
                    if (insulationRange.min > neededInsulationCold)
                    {
                        temperatureScoreOffset.min += insulationRange.min - neededInsulationCold;
                    }
                }

                // hot values are positive
                float neededInsulationWarmth = targetRange.max - comfortableRange.max;
                if (neededInsulationWarmth > 0)
                {
                    // too hot
                    if (neededInsulationWarmth < insulationRange.max)
                    {
                        temperatureScoreOffset.max += neededInsulationWarmth;
                    }
                    else
                    {
                        temperatureScoreOffset.max += insulationRange.max;
                    }
                }
                else
                {
                    // cool enough
                    if (insulationRange.max < neededInsulationWarmth)
                    {
                        temperatureScoreOffset.max += insulationRange.max - neededInsulationWarmth;
                    }
                }

                // invert for scoring
                temperatureScoreOffset.min *= -1;

                temperatureScoreOffset.min = InsulationFactorCurve.Evaluate(temperatureScoreOffset.min);
                temperatureScoreOffset.max = InsulationFactorCurve.Evaluate(temperatureScoreOffset.max);

                insulation = temperatureScoreOffset.min * temperatureScoreOffset.max;
            }
            else
            {
                if (neededWarmth == NeededWarmth.Warm)
                {
                    float statValue = apparel.GetStatValue(StatDefOf.Insulation_Cold, true);
                    insulation *= InsulationColdScoreFactorCurve_NeedWarm.Evaluate(statValue);
                }
            }
            return insulation;
        }

        static FloatRange GetInsulationStats(Apparel apparel)
        {
            var insulationCold = apparel.GetStatValue(StatDefOf.Insulation_Cold);
            var insulationHeat = apparel.GetStatValue(StatDefOf.Insulation_Heat);

            return new FloatRange(insulationCold, insulationHeat);
        }
    }
}
