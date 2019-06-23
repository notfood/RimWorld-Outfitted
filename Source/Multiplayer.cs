using System;
using System.Linq;
using RimWorld;
using Multiplayer.API;
using Verse;

namespace Outfitted
{
    [StaticConstructorOnStartup]
    public static class ExtendedOutfitProxy
    {
        static readonly ISyncField[] ExtendedOutfitFields;
        static readonly ISyncField[] ProxyFields;

        static int targetOutfitId;
        static StatDef targetStat;
        static float targetWeight;

        static ExtendedOutfitProxy()
        {
            if (!MP.enabled) return;

            ProxyFields = new ISyncField[] {
                MP.RegisterSyncField(typeof(ExtendedOutfitProxy), nameof(targetWeight))
                    .SetBufferChanges().PostApply(Update)
            };

            ExtendedOutfitFields = new ISyncField[] {
                MP.RegisterSyncField (typeof(ExtendedOutfit), nameof(ExtendedOutfit.targetTemperaturesOverride)),
                MP.RegisterSyncField (typeof(ExtendedOutfit), nameof(ExtendedOutfit.targetTemperatures)),
                MP.RegisterSyncField (typeof(ExtendedOutfit), nameof(ExtendedOutfit.PenaltyWornByCorpse)),
                MP.RegisterSyncField (typeof(ExtendedOutfit), nameof(ExtendedOutfit.AutoWorkPriorities)),
            };

            MP.RegisterSyncMethod(typeof(ExtendedOutfit), nameof(ExtendedOutfit.AddStatPriority));
            MP.RegisterSyncMethod(typeof(ExtendedOutfit), nameof(ExtendedOutfit.RemoveStatPriority));

            MP.RegisterSyncMethod(typeof(ExtendedOutfitProxy), nameof(SetStat));

            MP.RegisterSyncWorker<ExtendedOutfit>(ExtendedOutfitSyncer);
        }

        static void Update(object arg1, object arg2)
        {
            float targetWeight = (float) arg2;

            var outfit = Current.Game.outfitDatabase.AllOutfits.Find(o => o.uniqueId == targetOutfitId) as ExtendedOutfit;

            if (outfit == null) throw new Exception("Not an ExtendedOutfit");

            var statPriority = outfit.StatPriorities.FirstOrDefault(sp => sp.Stat == targetStat);

            if (statPriority == null) {
                outfit.AddStatPriority(targetStat, targetWeight);
            } else {
                statPriority.Weight = targetWeight;
            }
        }

        static void Watch(this ISyncField[] fields, object target = null)
        {
            foreach(var field in fields) {
                field.Watch(target);
            }
        }

        public static void Watch(ref ExtendedOutfit outfit)
        {
            ProxyFields.Watch();
            ExtendedOutfitFields.Watch(outfit);
        }

        // For sliders, we must buffer weight but stat must be accurate
        public static void SetStatPriority(int selectedOutfitId, StatDef stat, float weight)
        {
            if (targetOutfitId != selectedOutfitId || !targetStat.Equals(stat)) {
                // Forces any changes
                SetStat(selectedOutfitId, stat, weight);
            } else {
                // Buffers the rest
                targetWeight = weight;
            }
        }

        // That's why it gets its own SyncMethod, SyncFields suffer from buffers
        static void SetStat(int uid, StatDef stat, float weight)
        {
            targetOutfitId = uid;
            targetStat = stat;
            Update(null, weight);
        }

        static void ExtendedOutfitSyncer (SyncWorker sync, ref ExtendedOutfit outfit)
        {
            if (sync.isWriting) {
                sync.Bind (ref outfit.uniqueId);
            } else {
                int uid = 0;

                sync.Bind (ref uid);

                var currentOutfit = Current.Game.outfitDatabase.AllOutfits.Find (o => o.uniqueId == uid);
                if (currentOutfit is ExtendedOutfit extendedOutfit) {
                    outfit = extendedOutfit;
                }
            }
        }
    }
}
