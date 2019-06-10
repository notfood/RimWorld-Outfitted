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
        static ISyncField targetTemperaturesOverride;
        static ISyncField targetTemperatures;
        //static ISyncField PenaltyWornByCorpse;
        static ISyncField selectedStatPrioritySF;
        static ISyncField AutoWorkPriorities;

        static int selectedOutfitId;
        static StatPriority selectedStatPriority;

        static ExtendedOutfitProxy()
        {
            if (!MP.enabled) return;

            targetTemperaturesOverride = RegisterSyncField (typeof (ExtendedOutfit), "targetTemperaturesOverride");
            targetTemperatures = RegisterSyncField (typeof (ExtendedOutfit), "targetTemperatures");
            AutoWorkPriorities = RegisterSyncField (typeof(ExtendedOutfit), "AutoWorkPriorities");

            // Static Types require a null target and "Type/Field" format for the name.
            selectedStatPrioritySF = RegisterSyncField (null, typeof (ExtendedOutfitProxy) + "/selectedStatPriority");

            MP.RegisterSyncMethod(typeof(ExtendedOutfit), "RemoveStatPriority");
        }

        static ISyncField RegisterSyncField(Type type, string method)
        {
            return MP.RegisterSyncField(type, method).SetBufferChanges();
        }

        [SyncWorker(shouldConstruct = false)]
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

        [SyncWorker(shouldConstruct = false)]
        static void StatPrioritySyncer (SyncWorker sync, ref StatPriority sp)
        {
            int uid = selectedOutfitId;
            StatDef stat;

            if (sync.isWriting) {
                stat = sp.Stat;

                sync.Bind (ref uid);
                sync.Bind (ref stat);
                sync.Bind (ref sp.Weight);
            } else {
                stat = null;
                float weight = 0;

                sync.Bind (ref uid);
                sync.Bind (ref stat);
                sync.Bind (ref weight);

                var targetOutfit = Current.Game.outfitDatabase.AllOutfits.Find (o => o.uniqueId == uid);
                if (targetOutfit is ExtendedOutfit extendedOutfit) {
                    sp = extendedOutfit.StatPriorities.FirstOrDefault (o => o.Stat == stat);

                    if (sp != null) {
                        sp.Weight = weight;
                    } else {
                        extendedOutfit.AddStatPriority (stat, weight);
                    }
                } else {
                    Log.Warning ("Outfitted :: DESYNC INCOMING");
                }
            }
        }

        public static void Watch (ref ExtendedOutfit outfit)
        {
            selectedOutfitId = outfit.uniqueId;

            targetTemperaturesOverride.Watch (outfit);
            targetTemperatures.Watch (outfit);
            //PenaltyWornByCorpse.Watch (outfit);
            MP.Watch(outfit, nameof(ExtendedOutfit.PenaltyWornByCorpse));
            AutoWorkPriorities.Watch( outfit );
            selectedStatPrioritySF.Watch ();
        }

        public static void SetStatPriority (StatDef stat, float weight)
        {
            selectedStatPriority = new StatPriority (stat, weight);
        }
    }
}
