using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Harmony;
using RimWorld;
using Verse;

namespace Outfitted.Database
{
    static class Priority
    {
        public const float Unwanted = -2f;
        public const float Undesired = -1f;
        public const float Neutral = 0f;
        public const float Desired = 1f;
        public const float Wanted = 2f;
    }

    [HarmonyPatch(typeof(OutfitDatabase), nameof(OutfitDatabase.MakeNewOutfit))]
    static class OutfitDatabase_MakeNewOutfit_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var oldConstructor = AccessTools.Constructor(typeof(Outfit), new[] { typeof(int), typeof(string) });
            var newConstructor = AccessTools.Constructor(typeof(ExtendedOutfit), new[] { typeof(int), typeof(string) });
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Newobj && oldConstructor.Equals(instruction.operand))
                {
                    instruction.operand = newConstructor;
                }

                yield return instruction;
            }
        }
    }

    [HarmonyPatch(typeof(OutfitDatabase), nameof(OutfitDatabase.ExposeData))]
    static class OutfitDatabase_ExposeData_Patch
    {
        static void Postfix(OutfitDatabase __instance, List<Outfit> ___outfits) {
            if (Scribe.mode != LoadSaveMode.LoadingVars) {
                return;
            }

            if (___outfits.Any(i => i is ExtendedOutfit)) {
                return;
            }

            foreach (var outfit in ___outfits.ToList()) {
                ___outfits.Remove(outfit);

                ___outfits.Add(ReplaceKnownVanillaOutfits(outfit));
            }

            OutfitDatabase_GenerateStartingOutfits_Patch.GenerateStartingOutfits(__instance, false);
        }

        static Outfit ReplaceKnownVanillaOutfits(Outfit outfit) {
            var newOutfit = new ExtendedOutfit(outfit);
            switch (newOutfit.label)
            {
                default:
                    newOutfit.AddRange(new List<StatPriority>
                    {
                        new StatPriority(StatDefOf.MoveSpeed, Priority.Desired),
                        new StatPriority(StatDefOf.WorkSpeedGlobal, Priority.Wanted),
                        new StatPriority(StatDefOf.ArmorRating_Blunt, Priority.Desired),
                        new StatPriority(StatDefOf.ArmorRating_Sharp, Priority.Desired),
                    });
                    break;
                case "Worker":
                    newOutfit.AddRange(new List<StatPriority>
                    {
                        new StatPriority(StatDefOf.MoveSpeed, Priority.Neutral),
                        new StatPriority(StatDefOf.WorkSpeedGlobal, Priority.Desired),
                    });
                    break;
                case "Soldier":
                    newOutfit.AddRange(new List<StatPriority>
                    {
                        new StatPriority(StatDefOf.ShootingAccuracyPawn, Priority.Wanted),
                        new StatPriority(StatDefOf.AccuracyShort, Priority.Desired),
                        new StatPriority(StatDefOf.AccuracyMedium, Priority.Desired),
                        new StatPriority(StatDefOf.AccuracyLong, Priority.Desired),
                        new StatPriority(StatDefOf.MoveSpeed, Priority.Desired),
                        new StatPriority(StatDefOf.ArmorRating_Blunt, Priority.Neutral),
                        new StatPriority(StatDefOf.ArmorRating_Sharp, Priority.Desired),
                        new StatPriority(StatDefOf.MeleeDodgeChance, Priority.Neutral),
                        new StatPriority(StatDefOf.AimingDelayFactor, Priority.Unwanted),
                        new StatPriority(StatDefOf.RangedWeapon_Cooldown, Priority.Unwanted),
                        new StatPriority(StatDefOf.PainShockThreshold, Priority.Wanted),
                    });
                    break;
                case "Nudist":
                    newOutfit.AddRange(new List<StatPriority>
                    {
                        new StatPriority(StatDefOf.MoveSpeed, Priority.Desired),
                        new StatPriority(StatDefOf.WorkSpeedGlobal, Priority.Wanted),
                    });
                    break;
            }

            return newOutfit;
        }
    }

    [HarmonyPatch(typeof(OutfitDatabase), "GenerateStartingOutfits")]
    public static class OutfitDatabase_GenerateStartingOutfits_Patch
    {
        static bool Prefix(OutfitDatabase __instance) {
            try {
                GenerateStartingOutfits(__instance);
            } catch (Exception e) {
                Log.Error("Can't generate outfits: " + e);
            }

            return false;
        }

        internal static void GenerateStartingOutfits(OutfitDatabase db, bool vanilla = true) {
            if (vanilla) {
                ConfigureWorkerOutfit(MakeOutfit(db, "Anything"), new Dictionary<StatDef, float> {
                    {StatDefOf.MoveSpeed, Priority.Desired},
                    {StatDefOf.WorkSpeedGlobal, Priority.Wanted},
                    {StatDefOf.ArmorRating_Blunt, Priority.Desired},
                    {StatDefOf.ArmorRating_Sharp, Priority.Desired},
                });

                ConfigureWorkerOutfit(MakeOutfit(db, "Worker"), new Dictionary<StatDef, float> {
                    {StatDefOf.MoveSpeed, Priority.Neutral},
                    {StatDefOf.WorkSpeedGlobal, Priority.Desired},
                });
            }

            ConfigureWorkerOutfit(MakeOutfit(db, "Doctor"), new Dictionary<StatDef, float> {
                {StatDefOf.MedicalSurgerySuccessChance, Priority.Wanted},
                {StatDef.Named("MedicalOperationSpeed"), Priority.Wanted},
                {StatDefOf.MedicalTendQuality, Priority.Wanted},
                {StatDefOf.MedicalTendSpeed, Priority.Desired},
                {StatDefOf.WorkSpeedGlobal, Priority.Desired},
            });

            ConfigureWorkerOutfit(MakeOutfit(db, "Warden"), new Dictionary<StatDef, float> {
                {StatDefOf.NegotiationAbility, Priority.Wanted},
                {StatDefOf.SocialImpact, Priority.Desired},
                {StatDefOf.TradePriceImprovement, Priority.Wanted},
            });

            ConfigureWorkerOutfit(MakeOutfit(db, "Handler"), new Dictionary<StatDef, float> {
                {StatDefOf.TrainAnimalChance, Priority.Wanted},
                {StatDefOf.TameAnimalChance, Priority.Wanted},
                {StatDefOf.ArmorRating_Sharp, Priority.Neutral},
                {StatDefOf.MeleeDodgeChance, Priority.Desired},
                {StatDefOf.MeleeHitChance, Priority.Neutral},
                {StatDefOf.MoveSpeed, Priority.Neutral},
                {StatDefOf.MeleeDPS, Priority.Neutral},
                {StatDefOf.AccuracyTouch, Priority.Neutral},
                {StatDefOf.MeleeWeapon_CooldownMultiplier, Priority.Unwanted},
                {StatDefOf.MeleeWeapon_DamageMultiplier, Priority.Neutral},
                {StatDefOf.PainShockThreshold, Priority.Wanted},
                {StatDefOf.AnimalGatherYield, Priority.Wanted},
                {StatDefOf.AnimalGatherSpeed, Priority.Wanted},
            });

            ConfigureWorkerOutfit(MakeOutfit(db, "Cook"), new Dictionary<StatDef, float> {
                {StatDef.Named("DrugCookingSpeed"), Priority.Wanted},
                {StatDef.Named("ButcheryFleshSpeed"), Priority.Wanted},
                {StatDef.Named("ButcheryFleshEfficiency"), Priority.Wanted},
                {StatDef.Named("CookSpeed"), Priority.Wanted},
                {StatDefOf.FoodPoisonChance, Priority.Unwanted},
                {StatDefOf.MoveSpeed, Priority.Desired},
                {StatDefOf.WorkSpeedGlobal, Priority.Desired},
            });

            ConfigureSoldierOutfit(MakeOutfit(db, "Hunter"), new Dictionary<StatDef, float> {
                {StatDefOf.ShootingAccuracyPawn, Priority.Wanted},
                {StatDefOf.MoveSpeed, Priority.Desired},
                {StatDefOf.AccuracyShort, Priority.Desired},
                {StatDefOf.AccuracyMedium, Priority.Desired},
                {StatDefOf.AccuracyLong, Priority.Desired},
                {StatDefOf.MeleeDPS, Priority.Neutral},
                {StatDefOf.MeleeHitChance, Priority.Neutral},
                {StatDefOf.ArmorRating_Blunt, Priority.Neutral},
                {StatDefOf.ArmorRating_Sharp, Priority.Neutral},
                {StatDefOf.RangedWeapon_Cooldown, Priority.Unwanted},
                {StatDefOf.AimingDelayFactor, Priority.Unwanted},
                {StatDefOf.PainShockThreshold, Priority.Wanted},
            });

            ConfigureWorkerOutfit(MakeOutfit(db, "Builder"), new Dictionary<StatDef, float> {
                {StatDefOf.FixBrokenDownBuildingSuccessChance, Priority.Wanted},
                {StatDefOf.ConstructionSpeed, Priority.Wanted},
                {StatDefOf.ConstructSuccessChance, Priority.Wanted},
                {StatDefOf.SmoothingSpeed, Priority.Wanted},
                {StatDefOf.MoveSpeed, Priority.Neutral},
                {StatDefOf.WorkSpeedGlobal, Priority.Neutral},
            });

            ConfigureWorkerOutfit(MakeOutfit(db, "Grower"), new Dictionary<StatDef, float> {
                {StatDefOf.PlantHarvestYield, Priority.Wanted},
                {StatDefOf.PlantWorkSpeed, Priority.Wanted},
                {StatDefOf.MoveSpeed, Priority.Neutral},
                {StatDefOf.WorkSpeedGlobal, Priority.Desired},
            });

            ConfigureWorkerOutfit(MakeOutfit(db, "Miner"), new Dictionary<StatDef, float> {
                {StatDefOf.MiningYield, Priority.Wanted},
                {StatDefOf.MiningSpeed, Priority.Wanted},
                {StatDefOf.MoveSpeed, Priority.Neutral},
                {StatDefOf.WorkSpeedGlobal, Priority.Desired},
            });

            ConfigureWorkerOutfit(MakeOutfit(db, "Smith"), new Dictionary<StatDef, float> {
                {StatDef.Named("SmithingSpeed"), Priority.Wanted},
                {StatDefOf.WorkSpeedGlobal, Priority.Desired},
            });

            ConfigureWorkerOutfit(MakeOutfit(db, "Tailor"), new Dictionary<StatDef, float> {
                {StatDef.Named("TailoringSpeed"), Priority.Wanted},
                {StatDefOf.WorkSpeedGlobal, Priority.Desired},
            });

            ConfigureWorkerOutfit(MakeOutfit(db, "Artist"), new Dictionary<StatDef, float> {
                {StatDef.Named("SculptingSpeed"), Priority.Wanted},
                {StatDefOf.WorkSpeedGlobal, Priority.Desired},
            });

            ConfigureWorkerOutfit(MakeOutfit(db, "Crafter"), new Dictionary<StatDef, float> {
                {StatDef.Named("SmeltingSpeed"), Priority.Wanted},
                {StatDef.Named("ButcheryMechanoidSpeed"), Priority.Wanted},
                {StatDef.Named("ButcheryMechanoidEfficiency"), Priority.Wanted},
                {StatDefOf.WorkSpeedGlobal, Priority.Wanted},
            });

            ConfigureWorkerOutfit(MakeOutfit(db, "Hauler"), new Dictionary<StatDef, float> {
                {StatDefOf.MoveSpeed, Priority.Wanted},
                {StatDefOf.CarryingCapacity, Priority.Wanted},
            });

            ConfigureWorkerOutfit(MakeOutfit(db, "Cleaner"), new Dictionary<StatDef, float> {
                {StatDefOf.MoveSpeed, Priority.Wanted},
                {StatDefOf.WorkSpeedGlobal, Priority.Wanted},
            });

            ConfigureWorkerOutfit(MakeOutfit(db, "Researcher"), new Dictionary<StatDef, float> {
                {StatDefOf.ResearchSpeed, Priority.Wanted},
                {StatDefOf.WorkSpeedGlobal, Priority.Desired},
            });

            ConfigureSoldierOutfit(MakeOutfit(db, "Brawler"), new Dictionary<StatDef, float> {
                {StatDefOf.MoveSpeed, Priority.Wanted},
                {StatDefOf.AimingDelayFactor, Priority.Unwanted},
                {StatDefOf.MeleeDPS, Priority.Wanted},
                {StatDefOf.MeleeHitChance, Priority.Wanted},
                {StatDefOf.MeleeDodgeChance, Priority.Wanted},
                {StatDefOf.ArmorRating_Blunt, Priority.Neutral},
                {StatDefOf.ArmorRating_Sharp, Priority.Desired},
                {StatDefOf.AccuracyTouch, Priority.Wanted},
                {StatDefOf.MeleeWeapon_DamageMultiplier, Priority.Wanted},
                {StatDefOf.MeleeWeapon_CooldownMultiplier, Priority.Unwanted},
                {StatDefOf.PainShockThreshold, Priority.Wanted},
            });

            if (vanilla) {
                ConfigureSoldierOutfit(MakeOutfit(db, "Soldier"), new Dictionary<StatDef, float> {
                    {StatDefOf.ShootingAccuracyPawn, Priority.Wanted},
                    {StatDefOf.AccuracyShort, Priority.Desired},
                    {StatDefOf.AccuracyMedium, Priority.Desired},
                    {StatDefOf.AccuracyLong, Priority.Desired},
                    {StatDefOf.MoveSpeed, Priority.Desired},
                    {StatDefOf.ArmorRating_Blunt, Priority.Neutral},
                    {StatDefOf.ArmorRating_Sharp, Priority.Desired},
                    {StatDefOf.MeleeDodgeChance, Priority.Neutral},
                    {StatDefOf.AimingDelayFactor, Priority.Unwanted},
                    {StatDefOf.RangedWeapon_Cooldown, Priority.Unwanted},
                    {StatDefOf.PainShockThreshold, Priority.Wanted},
                });
                ConfigureNudistOutfit(MakeOutfit(db, "Nudist"), new Dictionary<StatDef, float> {
                    {StatDefOf.MoveSpeed, Priority.Desired},
                    {StatDefOf.WorkSpeedGlobal, Priority.Wanted},
                });
            }
        }

        static Outfit MakeOutfit(OutfitDatabase database, string name) {
            var outfit = database.MakeNewOutfit();
            outfit.label = ("Outfit" + name).Translate();
            return outfit;
        }

        static void ConfigureWorkerOutfit(Outfit outfit, Dictionary<StatDef, float> priorities) {
            outfit.filter.SetDisallowAll(null, null);
            outfit.filter.SetAllow(SpecialThingFilterDefOf.AllowDeadmansApparel, false);
            foreach (ThingDef current in DefDatabase<ThingDef>.AllDefs)
            {
                if (current.apparel != null && current.apparel.defaultOutfitTags != null && current.apparel.defaultOutfitTags.Contains("Worker"))
                {
                    outfit.filter.SetAllow(current, true);
                }
            }
            ConfigureOutfit(outfit, priorities);
        }

        static void ConfigureSoldierOutfit(Outfit outfit, Dictionary<StatDef, float> priorities) {
            outfit.filter.SetDisallowAll(null, null);
            outfit.filter.SetAllow(SpecialThingFilterDefOf.AllowDeadmansApparel, false);
            foreach (ThingDef current in DefDatabase<ThingDef>.AllDefs)
            {
                if (current.apparel != null && current.apparel.defaultOutfitTags != null && current.apparel.defaultOutfitTags.Contains("Soldier"))
                {
                    outfit.filter.SetAllow(current, true);
                }
            }
            ConfigureOutfit(outfit, priorities);
        }

        static void ConfigureNudistOutfit(Outfit outfit, Dictionary<StatDef, float> priorities) {
            outfit.filter.SetDisallowAll(null, null);
            outfit.filter.SetAllow(SpecialThingFilterDefOf.AllowDeadmansApparel, false);
            foreach (ThingDef current in DefDatabase<ThingDef>.AllDefs)
            {
                if (current.apparel != null && !current.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Legs) && !current.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso))
                {
                    outfit.filter.SetAllow(current, true);
                }
            }
            ConfigureOutfit(outfit, priorities);
        }

        static void ConfigureOutfit(Outfit outfit, Dictionary<StatDef, float> priorities) {
            var extendedOutfit = outfit as ExtendedOutfit;
            if (extendedOutfit == null) {
                Log.ErrorOnce("Outfitted :: Can't configure, not an ExtendedOutfit", 128848);
                return;
            }
            extendedOutfit.AddRange(priorities.Select(i => new StatPriority(i.Key, i.Value, StatAssignment.Automatic)));
        }
    }
}
