using HarmonyLib;
using RimWorld;
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
}
