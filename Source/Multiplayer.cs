using System;
using System.Linq;
using RimWorld;
using UnofficialMultiplayerAPI;
using Verse;

namespace Outfitted
{
    public class ExtendedOutfitProxy : IMultiplayerInit
    {
        static ISyncField targetTemperaturesOverride;
        static ISyncField targetTemperatures;
        static ISyncField PenaltyWornByCorpse;
        static ISyncField selectedStatPrioritySF;
        static ISyncField AutoWorkPriorities;

        static int selectedOutfitId;
        static StatPriority selectedStatPriority;

        public void Init ()
        {
            targetTemperaturesOverride = SyncField (typeof (ExtendedOutfit), "targetTemperaturesOverride");
            targetTemperatures = SyncField (typeof (ExtendedOutfit), "targetTemperatures");
            PenaltyWornByCorpse = SyncField (typeof (ExtendedOutfit), "PenaltyWornByCorpse");
            AutoWorkPriorities = SyncField( typeof( ExtendedOutfit ), "AutoWorkPriorities" );
            selectedStatPrioritySF = SyncField (typeof (ExtendedOutfitProxy), "selectedStatPriority");
        }

        static ISyncField SyncField (Type type, string member)
        {
            return MPApi.SyncField (type, member).SetBufferChanges ();
        }

        // workaround until MPApi allows null Watches
        static readonly ExtendedOutfitProxy Instance = new ExtendedOutfitProxy ();
        [Syncer (shouldConstruct = false)]
        static void ProxySyncer (SyncWorker sync, ref ExtendedOutfitProxy proxy)
        {
            if (!sync.isWriting) {
                proxy = Instance;
            }
        }

        [Syncer (shouldConstruct = false)]
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

        [Syncer (shouldConstruct = false)]
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
            PenaltyWornByCorpse.Watch (outfit);
            AutoWorkPriorities.Watch( outfit );
            selectedStatPrioritySF.Watch (Instance);
        }

        public static void SetStatPriority (StatDef stat, float weight)
        {
            selectedStatPriority = new StatPriority (stat, weight);
        }
    }
}
