using UnityEngine;
using Verse;

namespace RimHistory
{
    public sealed class RimHistorySettings : ModSettings
    {
        public bool EnableDebugLogging = false;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref EnableDebugLogging, "EnableDebugLogging", false);
            base.ExposeData();
        }

        public void DoWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);
            listing.CheckboxLabeled("Enable debug logging", ref EnableDebugLogging);
            listing.End();
        }
    }
}
