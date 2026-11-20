using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using Verse;
using UnityEngine;

namespace RimHistory.Core
{
    public sealed class EasternZhouWorldMapSettingsDef : Def
    {
        public string scenarioName;
        public double scaleFactor = 1d;
        public double centerLatitude;
        public double centerLongitude;
        public double minLatitude;
        public double maxLatitude;
        public double minLongitude;
        public double maxLongitude;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors()) yield return error;
            if (string.IsNullOrWhiteSpace(scenarioName)) yield return "scenarioName is required.";
            if (double.IsNaN(scaleFactor) || double.IsInfinity(scaleFactor) || scaleFactor <= 0d)
                yield return "scaleFactor must be greater than zero.";
            if (centerLatitude < -90d || centerLatitude > 90d) yield return "centerLatitude must be within -90..90.";
            if (centerLongitude < -180d || centerLongitude > 180d) yield return "centerLongitude must be within -180..180.";
            if (minLatitude < -90d || maxLatitude > 90d || minLatitude >= maxLatitude)
                yield return "Latitude bounds must be ordered and within -90..90.";
            if (minLongitude < -180d || maxLongitude > 180d || minLongitude >= maxLongitude)
                yield return "Longitude bounds must be ordered and within -180..180.";
        }
    }

    internal static class EasternZhouWorldMapSettings
    {
        private const double CoverageMarginDegrees = 3d;
        private static Vector3? activeCenter;
        private static float activeRadius;

        public static bool IsActive
        {
            get
            {
                Scenario scenario = Find.Scenario;
                return scenario != null && DefDatabase<EasternZhouWorldMapSettingsDef>.AllDefsListForReading
                    .Exists(item => item.scenarioName == scenario.name);
            }
        }

        public static EasternZhouWorldMapSettingsDef GetCurrentSettings()
        {
            Scenario scenario = Find.Scenario;
            return scenario == null ? null : DefDatabase<EasternZhouWorldMapSettingsDef>
                .AllDefsListForReading.Find(item => item.scenarioName == scenario.name);
        }

        public static void Prepare(ref float planetCoverage, bool logResult = true)
        {
            activeCenter = null;
            activeRadius = 0f;
            Scenario scenario = Find.Scenario;
            if (scenario == null) return;

            EasternZhouWorldMapSettingsDef settings = DefDatabase<EasternZhouWorldMapSettingsDef>
                .AllDefsListForReading.Find(item => item.scenarioName == scenario.name);
            if (settings == null) return;

            try
            {
                AzimuthalEquidistantProjection projection = new AzimuthalEquidistantProjection(
                    settings.centerLatitude, settings.centerLongitude);
                Vector3 center = Project(settings, projection,
                    (settings.minLatitude + settings.maxLatitude) / 2d,
                    (settings.minLongitude + settings.maxLongitude) / 2d);
                double radius = 0d;
                for (double latitude = settings.minLatitude; latitude <= settings.maxLatitude + 0.0001d; latitude += 1d)
                {
                    radius = Math.Max(radius, Angle(center, Project(settings, projection, latitude, settings.minLongitude)));
                    radius = Math.Max(radius, Angle(center, Project(settings, projection, latitude, settings.maxLongitude)));
                }
                for (double longitude = settings.minLongitude; longitude <= settings.maxLongitude + 0.0001d; longitude += 1d)
                {
                    radius = Math.Max(radius, Angle(center, Project(settings, projection, settings.minLatitude, longitude)));
                    radius = Math.Max(radius, Angle(center, Project(settings, projection, settings.maxLatitude, longitude)));
                }

                activeCenter = center;
                activeRadius = (float)Math.Min(180d, radius + CoverageMarginDegrees);
                planetCoverage = Mathf.Clamp(activeRadius / 180f, 0.01f, 1f);
                if (logResult)
                    Log.Message("[RimHistory] Eastern Zhou world coverage set to "
                        + planetCoverage.ToString("P0") + "; projection center "
                        + settings.centerLatitude.ToString("0.##") + "°N, "
                        + settings.centerLongitude.ToString("0.##") + "°E; scale "
                        + settings.scaleFactor.ToString("0.##") + "x.");
            }
            catch (Exception exception)
            {
                activeCenter = null;
                Log.Error("[RimHistory] Could not apply the Eastern Zhou world map bounds: " + exception.Message);
            }
        }

        public static void ApplyToWorldGrid(ref Vector3 center, ref float angle)
        {
            if (!activeCenter.HasValue) return;
            center = activeCenter.Value;
            angle = activeRadius;
        }

        public static void Clear() => activeCenter = null;

        private static Vector3 Project(EasternZhouWorldMapSettingsDef settings,
            AzimuthalEquidistantProjection projection, double latitude, double longitude)
        {
            var point = projection.Project(latitude, longitude);
            var scaled = projection.Unproject(point.degreeX * settings.scaleFactor, point.degreeY * settings.scaleFactor);
            return ToUnitVector(scaled.historicalLongitude, scaled.historicalLatitude);
        }

        private static Vector3 ToUnitVector(double longitude, double latitude)
        {
            double lon = longitude * Math.PI / 180d;
            double lat = latitude * Math.PI / 180d;
            double cosLat = Math.Cos(lat);
            return new Vector3((float)(Math.Sin(lon) * cosLat), (float)Math.Sin(lat),
                (float)(-Math.Cos(lon) * cosLat));
        }

        private static double Angle(Vector3 a, Vector3 b) =>
            Math.Acos(Math.Max(-1d, Math.Min(1d, Vector3.Dot(a, b)))) * 180d / Math.PI;
    }

    [HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GenerateWorld))]
    internal static class EasternZhouWorldCoveragePatch
    {
        private static void Prefix(ref float planetCoverage) =>
            EasternZhouWorldMapSettings.Prepare(ref planetCoverage);
    }

    [HarmonyPatch(typeof(Page_CreateWorldParams), nameof(Page_CreateWorldParams.DoWindowContents))]
    internal static class EasternZhouCoverageReadoutPatch
    {
        private static void Prefix(ref float ___planetCoverage) =>
            EasternZhouWorldMapSettings.Prepare(ref ___planetCoverage, false);

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var buttonText = AccessTools.Method(typeof(Widgets), "ButtonText", new[]
            {
                typeof(Rect), typeof(string), typeof(bool), typeof(bool), typeof(bool), typeof(TextAnchor?)
            });
            var readout = AccessTools.Method(typeof(EasternZhouCoverageReadoutPatch), nameof(DrawReadOnlyCoverage));
            bool coverageLabelSeen = false;
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == System.Reflection.Emit.OpCodes.Ldstr
                    && Equals(instruction.operand, "PlanetCoverage"))
                    coverageLabelSeen = true;
                else if (coverageLabelSeen && instruction.Calls(buttonText))
                {
                    instruction.operand = readout;
                    coverageLabelSeen = false;
                }
                yield return instruction;
            }
        }

        private static bool DrawReadOnlyCoverage(Rect rect, string label, bool first, bool second,
            bool third, TextAnchor? anchor)
        {
            if (!EasternZhouWorldMapSettings.IsActive)
                return Widgets.ButtonText(rect, label, first, second, third, anchor);

            TextAnchor previous = Text.Anchor;
            Text.Anchor = anchor ?? TextAnchor.MiddleCenter;
            Widgets.Label(rect, label);
            Text.Anchor = previous;
            return false;
        }
    }

    [HarmonyPatch(typeof(WorldGrid), "CalculateViewCenterAndAngle")]
    internal static class EasternZhouWorldCenterPatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(ref Vector3 ___surfaceViewCenter, ref float ___surfaceViewAngle) =>
            EasternZhouWorldMapSettings.ApplyToWorldGrid(ref ___surfaceViewCenter, ref ___surfaceViewAngle);
    }

    [HarmonyPatch(typeof(PlanetLayer), "InitializeLayer")]
    internal static class EasternZhouPlanetLayerCenterPatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(ref Vector3 ___viewCenter, ref float ___viewAngle) =>
            EasternZhouWorldMapSettings.ApplyToWorldGrid(ref ___viewCenter, ref ___viewAngle);
    }
}
