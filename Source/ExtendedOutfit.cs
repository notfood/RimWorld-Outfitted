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

        public bool PenaltyWornByCorpse = true;

        public bool AutoWorkPriorities;
        private bool _autoTemp;
        public bool AutoTemp
        {
            get
            {
                return _autoTemp;
            }
            set
            {
                _autoTemp = value;
                if(_autoTemp)
                {
                    targetTemperaturesOverride = true;
                }
            }
        }

        public int autoTempOffset = 20;

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

        internal static IEnumerable<StatDef> AllAvailableStats => DefDatabase<StatDef>
            .AllDefs
            .Where(i => !blacklistedCategories.Contains(i.category))
            .Except(blacklistedStats).ToList();

        public IEnumerable<StatDef> UnassignedStats => AllAvailableStats
            .Except(StatPriorities.Select(i => i.Stat));

        List<StatPriority> statPriorities = new List<StatPriority>();

        public IEnumerable<StatPriority> StatPriorities => statPriorities;

        public ExtendedOutfit(int uniqueId, string label) : base(uniqueId, label)
        {
            // Used by OutfitDatabase_MakeNewOutfit_Patch
        }

        public ExtendedOutfit(Outfit outfit) : base(outfit.uniqueId, outfit.label)
        {
            // Used by OutfitDatabase_ExposeData_Patch

            filter.CopyAllowancesFrom(outfit.filter);
        }

        public ExtendedOutfit()
        {
            // Used by ExposeData
        }

        public void AddStatPriority(StatDef def, float priority, float defaultPriority = float.NaN)
        {
            statPriorities.Insert(0, new StatPriority(def, priority, defaultPriority));
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
            Scribe_Values.Look(ref PenaltyWornByCorpse, "PenaltyWornByCorpse", true);
            Scribe_Collections.Look(ref statPriorities, "statPriorities", LookMode.Deep);
            Scribe_Values.Look(ref AutoWorkPriorities, "AutoWorkPriorities", false );
            Scribe_Values.Look(ref _autoTemp, "AutoTemp" );
            Scribe_Values.Look(ref autoTempOffset, "autoTempOffset" );
        }

        public void CopyFrom(ExtendedOutfit outfit)
        {
            filter.CopyAllowancesFrom(outfit.filter);
            targetTemperaturesOverride = outfit.targetTemperaturesOverride;
            targetTemperatures = outfit.targetTemperatures;
            PenaltyWornByCorpse = outfit.PenaltyWornByCorpse;
            statPriorities.Clear();
            foreach(var sp in outfit.statPriorities) {
                statPriorities.Add(new StatPriority(sp.Stat, sp.Weight, sp.Default));
            }
            AutoWorkPriorities = outfit.AutoWorkPriorities;
            _autoTemp = outfit._autoTemp;
            autoTempOffset = outfit.autoTempOffset;
        }
    }
}