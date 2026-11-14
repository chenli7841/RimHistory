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
            Log.Message("[Historical RimWorld] Mod initialized.");
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
