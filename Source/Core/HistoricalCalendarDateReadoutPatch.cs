using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimHistory.Core
{
    /// <summary>Shows the active historical date in RimWorld's main date readout.</summary>
    [HarmonyPatch(typeof(GenDate), "DateReadoutStringAt", new[] { typeof(long), typeof(Vector2) })]
    internal static class HistoricalCalendarDateReadoutPatch
    {
        private const string TargetScenarioName = "先秦";

        private static string FormatHistoricalDate(long ticks)
        {
            const long TicksPerDay = 60_000L;
            const long DaysPerYear = 60L;
            const long DaysPerMonth = 5L;

            long totalDays = ticks / TicksPerDay;
            long year = 772 - totalDays / DaysPerYear;
            long dayOfYear = totalDays % DaysPerYear;
            long month = dayOfYear / DaysPerMonth + 1;
            long day = dayOfYear % DaysPerMonth + 1;

            string yearLabel = year > 0
                ? $"公元前{year}年"
                : $"公元{1 - year}年 ";

            return $"{yearLabel} {month}月{day}日";
        }

        private static void Postfix(long absTicks, ref string __result)
        {
            if (Find.Scenario == null || Find.Scenario.name != TargetScenarioName)
            {
                return;
            }

            __result = FormatHistoricalDate(absTicks);
        }
    }

    [HarmonyPatch(typeof(Page_CreateWorldParams), "ResetFactionCounts")]
    internal static class EasternZhouFactionSelectionPatch
    {
        private static readonly HashSet<string> Keep = new HashSet<string>
        {

            // 如果也要保留你自己的郑国派系，再加：
            "Faction_Royal",
            "Faction_Zheng",
            "Faction_Qin",
            "Faction_Chu",
            "Faction_Jin",
            "Faction_Qi",
            "Faction_Lu",
            "Faction_Wei",
            "Faction_Locals"
        };

        private static void Postfix(ref List<FactionDef> ___factions)
        {
            if (Find.Scenario == null || Find.Scenario.name != "先秦")
            {
                ___factions.RemoveAll(faction => Keep.Contains(faction.defName));
            }
            else
            {
                ___factions.RemoveAll(faction => !Keep.Contains(faction.defName));

            }
        }
    }
}
