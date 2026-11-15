using HarmonyLib;
using UnityEngine;
using Verse;

namespace RimHistory
{
    public sealed class RimHistoryMod : Mod
    {
        private readonly RimHistorySettings settings;

        public RimHistoryMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<RimHistorySettings>();
            new Harmony("LiChen.RimHistory").PatchAll(typeof(RimHistoryMod).Assembly);
            Log.Message("[RimHistory] Mod initialized.");
        }

        public override string SettingsCategory()
        {
            return "RimWHistory";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            settings.DoWindowContents(inRect);
        }
    }
}
