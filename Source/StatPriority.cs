using RimWorld;
using Verse;

namespace Outfitted
{
    public enum StatAssignment
    {
        Manual,
        Override,
        Individual,
        Automatic,
    }

    public class StatPriority : IExposable
    {
        public StatPriority(StatDef stat, float weight, StatAssignment assignment = StatAssignment.Automatic)
        {
            this.stat = stat;
            this.Weight = weight;
            this.Assignment = assignment;
        }

        public StatPriority() {
            
        }


        StatDef stat;

        public float Weight;

        public StatAssignment Assignment;

        public StatDef Stat => stat;

        public void ExposeData()
        {
            Scribe_Defs.Look(ref stat, "Stat");
            Scribe_Values.Look(ref Weight, "Weight");
            Scribe_Values.Look(ref Assignment, "Assignment");
        }
    }
}
