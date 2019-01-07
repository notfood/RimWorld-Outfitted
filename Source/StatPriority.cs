using RimWorld;
using Verse;

namespace Outfitted
{
    public class StatPriority : IExposable
    {
        public StatPriority(StatDef stat, float weight, float defaultWeight = float.NaN)
        {
            this.stat = stat;
            this.Weight = weight;
            this.Default = defaultWeight;
        }

        public StatPriority()
        {
            // Used by ExposeData
        }

        StatDef stat;

        public float Weight;

        public float Default;

        public StatDef Stat => stat;

        public bool IsDefault => Default == Weight;
        public bool IsManual => float.IsNaN(Default);
        public bool IsOverride => !IsManual && !IsDefault;

        //public bool Inverted => stat.GetModExtension<OutfittedModExtension>()?.inverted ?? false;

        public void ExposeData()
        {
            Scribe_Defs.Look(ref stat, "Stat");
            Scribe_Values.Look(ref Weight, "Weight");
            Scribe_Values.Look(ref Default, "Default", float.NaN);
        }
    }

    /*public class OutfittedModExtension : DefModExtension
    {
        // Signals Outfitted to invert the slider. Less is More, more is less.
        public bool inverted;
    }*/
}
