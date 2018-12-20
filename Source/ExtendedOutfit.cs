using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Outfitted
{
    public class ExtendedOutfit : Outfit, IExposable
    {
        public bool targetTemperaturesOverride;
        public FloatRange targetTemperatures = new FloatRange(-100, 100);

        static IEnumerable<StatCategoryDef> blacklistedCategories = new List<StatCategoryDef>()
        {
            StatCategoryDefOf.BasicsNonPawn,
            StatCategoryDefOf.Building,
            StatCategoryDefOf.StuffStatFactors,
        };

        static readonly IEnumerable<StatDef> blacklistedStats = new List<StatDef> {
            StatDefOf.ComfyTemperatureMin,
            StatDefOf.ComfyTemperatureMax,
            StatDefOf.Insulation_Cold,
            StatDefOf.Insulation_Heat,
            StatDefOf.StuffEffectMultiplierInsulation_Cold,
            StatDefOf.StuffEffectMultiplierInsulation_Heat,
            StatDefOf.StuffEffectMultiplierArmor,
        };

        static IEnumerable<StatDef> AllAvailableStats => DefDatabase<StatDef>
            .AllDefs
            .Where(i => !blacklistedCategories.Contains(i.category))
            .Except(blacklistedStats).ToList();

        public IEnumerable<StatDef> UnnasignedStats => AllAvailableStats
            .Except(StatPriorities.Select(i => i.Stat));

        List<StatPriority> statPriorities = new List<StatPriority>();
        public IEnumerable<StatPriority> StatPriorities => statPriorities;

        public ExtendedOutfit(int uniqueId, string label) : base(uniqueId, label)
        {
            // Used by OutfitDatabase_MakeNewOutfit_Patch
        }

        public ExtendedOutfit(Outfit outfit) : base(outfit.uniqueId, outfit.label) {
            // Used by OutfitDatabase_ExposeData_Patch

            filter.CopyAllowancesFrom(outfit.filter);
        }

        public ExtendedOutfit() {
            // Used by ExposeData
        }

        public void AddStatPriority(StatDef def, float priority, StatAssignment assigment = StatAssignment.Automatic)
        {
            statPriorities.Insert(0, new StatPriority(def, priority, assigment));
        }

        public void AddRange(IEnumerable<StatPriority> priorities) {
            statPriorities.AddRange(priorities);
        }

        public void RemoveStatPriority(StatDef def)
        {
            statPriorities.RemoveAll(i => i.Stat == def);
        }

        new public void ExposeData()
        {
            Scribe_Values.Look(ref uniqueId, "uniqueId");
            Scribe_Values.Look(ref label, "label");
            Scribe_Deep.Look(ref filter, "filter", new object[0]);
            Scribe_Values.Look(ref targetTemperaturesOverride, "targetTemperaturesOverride");
            Scribe_Values.Look(ref targetTemperatures, "targetTemperatures");
            Scribe_Collections.Look(ref statPriorities, "statPriorities", LookMode.Deep);
        }
    }
}