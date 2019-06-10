// WorkPriorities.cs

using System.Collections.Generic;
using System.Linq;
using Outfitted.Database;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Outfitted
{
    public class WorkPriorities : WorldComponent
    {
        private static List<WorktypePriorities> _worktypePriorities;
        private static WorkPriorities           _instance;

        public WorkPriorities( World world ) : base( world )
        {
            _instance = this;
            Log.Message( "WorldComponent created!" );
        }

        public static List<StatPriority> WorktypeStatPriorities( Pawn pawn )
        {
            // get stat weights for each non-zero work priority.
            var worktypeStats = DefDatabase<WorkTypeDef>
                .AllDefsListForReading
                .Select(wtd => new {
                    priority = pawn?.workSettings?.GetPriority(wtd) ?? 0,
                    worktype = wtd
                })
                .Where(x => x.priority > 0)
                .Select(x => new {
                    x.priority,
                    x.worktype,
                    weights = WorktypeStatPriorities(x.worktype)
                });

            // no work assigned.
            if (!worktypeStats.Any())
                return new List<StatPriority>();

            // normalize worktype priorities;
            // 1 - get the range (usually within 1-4, but may be up to 1-9 with Work Tab)
            var range = new IntRange( worktypeStats.Min( s => s.priority ), worktypeStats.Max( s => s.priority ) );
            var weights = new Dictionary<StatDef,StatPriority>();
            var sumOfWeights = 0f;


            foreach ( var worktype in worktypeStats )
            {
                // 2 - base to 0 (subtract minimum), scale to 0-1 (divide by maximum-minimum)
                // 3 - invert, so that 1 is 1, and max is 0.
                var normalizedPriority = range.min == range.max
                    ? 1
                    : 1 - ( worktype.priority - range.min ) / ( range.max - range.min );
                foreach ( var weight in worktype.weights )
                {
                    StatPriority statPriority;
                    if ( weights.TryGetValue( weight.Stat, out statPriority ) )
                    {
                        statPriority.Weight += normalizedPriority * weight.Weight;
                    } else {
                        statPriority = new StatPriority( weight.Stat, normalizedPriority * weight.Weight );
                        weights.Add( weight.Stat, statPriority );
                    }

                    sumOfWeights += statPriority.Weight;
                }
            }

            // 4 - multiply weights by constant c, so that sum of weights is 10
            if ( weights.Any() && sumOfWeights != 0 )
                foreach ( var weight in weights )
                    weight.Value.Weight *= 10 / sumOfWeights;

            return weights.Values.ToList();
        }

        public static List<StatPriority> WorktypeStatPriorities( WorkTypeDef worktype )
        {
            var worktypePriorities = _worktypePriorities.Find( wp => wp.worktype == worktype );
            if ( worktypePriorities == null )
            {
                Log.Warning( $"Outfitted :: Created worktype stat priorities for '{worktype.defName}' after initial init. This should never happen!"  );
                worktypePriorities = new WorktypePriorities( worktype, DefaultPriorities( worktype ) );
                _worktypePriorities.Add( worktypePriorities );
            }

            return worktypePriorities.priorities;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look( ref _worktypePriorities, "worktypePriorities", LookMode.Deep );
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();

            if ( _worktypePriorities.NullOrEmpty() )
            {
                _worktypePriorities = new List<WorktypePriorities>();

                // initialize with defaults
                foreach ( var worktype in DefDatabase<WorkTypeDef>.AllDefsListForReading )
                    _worktypePriorities.Add( new WorktypePriorities( worktype, DefaultPriorities( worktype ) ) );
            }
        }

        private static List<StatPriority> DefaultPriorities( WorkTypeDef worktype )
        {
            var stats = new List<StatPriority>();

            if ( worktype == WorkTypeDefOf.Art )
            {
                // Work :: SculptingSpeed :: Sculpting speed
                stats.Add( new StatPriority( StatDefOf.SculptingSpeed, Priority.Wanted ) );
            }

            if ( worktype == WorkTypeDefOf.BasicWorker )
            {
                // Work :: UnskilledLaborSpeed :: Unskilled labor speed
                stats.Add( new StatPriority( StatDefOf.UnskilledLaborSpeed, Priority.Wanted ) );
            }

            if ( worktype == WorkTypeDefOf.Cleaning )
            {
                // Basics :: MoveSpeed :: Move speed
                // Work :: WorkSpeedGlobal :: Global work speed
                stats.Add( new StatPriority( StatDefOf.MoveSpeed, Priority.Wanted ) );
                stats.Add( new StatPriority( StatDefOf.WorkSpeedGlobal, Priority.Desired ) );
            }

            if ( worktype == WorkTypeDefOf.Cooking )
            {
                // Work :: CookSpeed :: Cooking speed
                // Work :: FoodPoisonChance :: Food poison chance
                // Work :: ButcheryFleshSpeed :: Butchery speed
                // Work :: ButcheryFleshEfficiency :: Butchery efficiency
                stats.Add( new StatPriority( StatDefOf.CookSpeed, Priority.Wanted ) );
                stats.Add( new StatPriority( StatDefOf.FoodPoisonChance, Priority.Unwanted ) );
                stats.Add( new StatPriority( StatDefOf.ButcheryFleshSpeed, Priority.Desired ) );
                stats.Add( new StatPriority( StatDefOf.ButcheryFleshEfficiency, Priority.Desired ) );

            }

            if ( worktype == WorkTypeDefOf.Construction )
            {
                /**
                 * Work :: ConstructionSpeed :: Construction speed
                 * Work :: ConstructSuccessChance :: Construct success chance
                 * Work :: FixBrokenDownBuildingSuccessChance :: Repair success chance
                   Work :: SmoothingSpeed :: Smoothing speed
                 */
                stats.Add( new StatPriority( StatDefOf.ConstructionSpeed, Priority.Wanted ) );
                stats.Add( new StatPriority( StatDefOf.ConstructSuccessChance, Priority.Wanted ) );
                stats.Add( new StatPriority( StatDefOf.FixBrokenDownBuildingSuccessChance, Priority.Desired ) );
                stats.Add( new StatPriority( StatDefOf.SmoothingSpeed, Priority.Desired ) );

            }

            if ( worktype == WorkTypeDefOf.Crafting )
            {
                // Work :: DrugSynthesisSpeed :: Drug synthesis speed
                // Work :: DrugCookingSpeed :: Drug cooking speed
                // Work :: ButcheryMechanoidSpeed :: Mechanoid disassembly speed
                // Work :: ButcheryMechanoidEfficiency :: Mechanoid disassembly efficiency
                stats.Add( new StatPriority( StatDefOf.WorkSpeedGlobal, Priority.Wanted ) );
                stats.Add( new StatPriority( StatDefOf.DrugSynthesisSpeed, Priority.Desired ) );
                stats.Add( new StatPriority( StatDefOf.DrugCookingSpeed, Priority.Desired ) );
                stats.Add( new StatPriority( StatDefOf.ButcheryMechanoidSpeed, Priority.Desired ) );
                stats.Add( new StatPriority( StatDefOf.ButcheryMechanoidEfficiency, Priority.Desired ) );

            }

            if ( worktype == WorkTypeDefOf.Doctor )
            {
                /**
                 * Work :: MedicalTendSpeed :: Medical tend speed
                 * Work :: MedicalTendQuality :: Medical tend quality
                 * Work :: MedicalOperationSpeed :: Medical operation speed
                 * Work :: MedicalSurgerySuccessChance :: Medical surgery success chance
                 */
                stats.Add( new StatPriority( StatDefOf.MedicalTendSpeed, Priority.Desired ) );
                stats.Add( new StatPriority( StatDefOf.MedicalTendQuality, Priority.Wanted ) );
                stats.Add( new StatPriority( StatDefOf.MedicalOperationSpeed, Priority.Desired ) );
                stats.Add( new StatPriority( StatDefOf.MedicalSurgerySuccessChance, Priority.Wanted ) );
            }

            if ( worktype == WorkTypeDefOf.Firefighter )
            {
                // Basics :: MoveSpeed :: Move speed
                stats.Add( new StatPriority( StatDefOf.MoveSpeed, Priority.Wanted ) );
            }

            if ( worktype == WorkTypeDefOf.Growing )
            {
                // Work :: PlantWorkSpeed :: Plant work speed
                // Work :: PlantHarvestYield :: Plant harvest yield
                stats.Add( new StatPriority( StatDefOf.PlantWorkSpeed, Priority.Wanted ) );
                stats.Add( new StatPriority( StatDefOf.PlantHarvestYield, Priority.Wanted ) );

            }

            if ( worktype == WorkTypeDefOf.Handling )
            {
                /**
                 * Basics :: MoveSpeed :: Move speed
                 * Social :: TameAnimalChance :: Tame animal chance
                 * Social :: TrainAnimalChance :: Train animal chance
                 * Work :: AnimalGatherSpeed :: Animal gather speed
                 * Work :: AnimalGatherYield :: Animal gather yield
                 */
                stats.Add( new StatPriority( StatDefOf.MoveSpeed, Priority.Desired ) );
                stats.Add( new StatPriority( StatDefOf.TameAnimalChance, Priority.Wanted ) );
                stats.Add( new StatPriority( StatDefOf.TrainAnimalChance, Priority.Wanted ) );
                stats.Add( new StatPriority( StatDefOf.AnimalGatherSpeed, Priority.Desired ) );
                stats.Add( new StatPriority( StatDefOf.AnimalGatherYield, Priority.Desired ) );

            }

            if ( worktype == WorkTypeDefOf.Hauling )
            {
                /**
                 * Basics :: CarryingCapacity :: Carrying capacity
                 * Basics :: MoveSpeed :: Move speed
                 */
                stats.Add( new StatPriority( StatDefOf.CarryingCapacity, Priority.Wanted ) );
                stats.Add( new StatPriority( StatDefOf.MoveSpeed, Priority.Wanted ) );

            }

            if ( worktype == WorkTypeDefOf.Hunting )
            {
                /**
                 * Basics :: MoveSpeed :: Move speed
                 * Combat :: ShootingAccuracyPawn :: Shooting accuracy
                 * Combat :: AimingDelayFactor :: Aiming time
                 * Work :: HuntingStealth :: Hunting stealth
                 * Weapon :: AccuracyTouch :: Accuracy (close)
                 * Weapon :: AccuracyShort :: Accuracy (short)
                 * Weapon :: AccuracyMedium :: Accuracy (medium)
                 * Weapon :: AccuracyLong :: Accuracy (long)
                 * Weapon :: RangedWeapon_Cooldown :: Ranged cooldown
                 * Weapon :: RangedWeapon_DamageMultiplier :: Damage multiplier
                 */
                stats.Add( new StatPriority( StatDefOf.MoveSpeed, Priority.Desired ) );
                stats.Add( new StatPriority( StatDefOf.ShootingAccuracyPawn, Priority.Wanted ) );
                stats.Add( new StatPriority( StatDefOf.AimingDelayFactor, Priority.Desired ) );
                stats.Add( new StatPriority( StatDefOf.HuntingStealth, Priority.Wanted ) );
                stats.Add( new StatPriority( StatDefOf.AccuracyTouch, Priority.Desired ) );
                stats.Add( new StatPriority( StatDefOf.AccuracyShort, Priority.Desired ) );
                stats.Add( new StatPriority( StatDefOf.AccuracyMedium, Priority.Desired ) );
                stats.Add( new StatPriority( StatDefOf.AccuracyLong, Priority.Wanted ) );
                stats.Add( new StatPriority( StatDefOf.RangedWeapon_Cooldown, Priority.Desired ) );
                stats.Add( new StatPriority( StatDefOf.RangedWeapon_DamageMultiplier, Priority.Desired ) );

            }

            if ( worktype == WorkTypeDefOf.Mining )
            {
                /**
                 * Work :: MiningSpeed :: Mining speed
                 * Work :: MiningYield :: Mining yield
                 */
                stats.Add( new StatPriority( StatDefOf.MiningSpeed, Priority.Wanted ) );
                stats.Add( new StatPriority( StatDefOf.MiningYield, Priority.Wanted ) );

            }

            if ( worktype == WorkTypeDefOf.Patient || worktype == WorkTypeDefOf.PatientBedRest )
            {
                //
            }

            if ( worktype == WorkTypeDefOf.PlantCutting )
            {
                // Work :: PlantWorkSpeed :: Plant work speed
                // Work :: PlantHarvestYield :: Plant harvest yield
                stats.Add( new StatPriority( StatDefOf.PlantWorkSpeed, Priority.Wanted ) );
                stats.Add( new StatPriority( StatDefOf.PlantHarvestYield, Priority.Wanted ) );

            }

            if ( worktype == WorkTypeDefOf.Research )
            {
                // Work :: ResearchSpeed :: Research speed
                stats.Add( new StatPriority( StatDefOf.ResearchSpeed, Priority.Wanted ) );

            }

            if ( worktype == WorkTypeDefOf.Smithing )
            {
                // Work :: SmithingSpeed :: Smithing speed
                // Work :: SmeltingSpeed :: Smelting speed
                stats.Add( new StatPriority( StatDefOf.SmeltingSpeed, Priority.Desired ) );
                stats.Add( new StatPriority( StatDefOf.SmithingSpeed, Priority.Wanted ) );

            }

            if ( worktype == WorkTypeDefOf.Tailoring )
            {
                // Work :: TailoringSpeed :: Tailoring speed
                stats.Add( new StatPriority( StatDefOf.TailoringSpeed, Priority.Wanted ) );

            }

            if ( worktype == WorkTypeDefOf.Warden )
            {
                /**
                 * Social :: NegotiationAbility :: Negotiation ability
                 * Social :: TradePriceImprovement :: Trade price improvement
                 * Social :: SocialImpact :: Social impact
                 */
                stats.Add( new StatPriority( StatDefOf.NegotiationAbility, Priority.Wanted ) );
                stats.Add( new StatPriority( StatDefOf.TradePriceImprovement, Priority.Desired ) );
                stats.Add( new StatPriority( StatDefOf.SocialImpact, Priority.Wanted ) );

            }

            return stats;
        }
    }

    public class WorktypePriorities : IExposable
    {
        public List<StatPriority> priorities = new List<StatPriority>();
        public WorkTypeDef        worktype;

        public WorktypePriorities()
        {
            // used by ExposeData
        }

        public WorktypePriorities( WorkTypeDef worktype, List<StatPriority> priorities )
        {
            this.worktype   = worktype;
            this.priorities = priorities;
        }

        public void ExposeData()
        {
            Scribe_Defs.Look( ref worktype, "worktype" );
            Scribe_Collections.Look( ref priorities, "statPriorities", LookMode.Deep );
        }
    }
}