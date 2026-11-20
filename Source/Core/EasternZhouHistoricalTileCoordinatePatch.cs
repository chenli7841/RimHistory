using HarmonyLib;
using RimWorld.Planet;
using System.Globalization;
using UnityEngine;
using Verse;

namespace RimHistory.Core
{
    /// <summary>Adds the inverse-projected historical coordinate to the selected world tile description.</summary>
    [HarmonyPatch(typeof(WorldInspectPane), "get_TileInspectString")]
    internal static class EasternZhouHistoricalTileCoordinatePatch
    {
        private static void Postfix(ref string __result)
        {
            EasternZhouWorldMapSettingsDef settings = EasternZhouWorldMapSettings.GetCurrentSettings();
            if (settings == null || Find.World?.grid?.Surface == null || Find.WorldSelector == null) return;

            int tileId = (int)Find.WorldSelector.SelectedTile;
            if (tileId < 0 || tileId >= Find.World.grid.TilesCount) return;

            try
            {
                Vector2 gameCoordinate = Find.World.grid.Surface.LongLatOf(tileId);
                var historical = new AzimuthalEquidistantProjection(settings.centerLatitude, settings.centerLongitude)
                    .ToHistoricalCoordinate(gameCoordinate.y, gameCoordinate.x, settings.scaleFactor);
                string latitudeDirection = historical.historicalLatitude >= 0d ? "N" : "S";
                string longitudeDirection = historical.historicalLongitude >= 0d ? "E" : "W";
                __result = (__result ?? string.Empty) + "\n历史坐标（比例 "
                    + settings.scaleFactor.ToString("0.##", CultureInfo.InvariantCulture) + "×）："
                    + System.Math.Abs(historical.historicalLatitude).ToString("0.00000", CultureInfo.InvariantCulture)
                    + "°" + latitudeDirection + " "
                    + System.Math.Abs(historical.historicalLongitude).ToString("0.00000", CultureInfo.InvariantCulture)
                    + "°" + longitudeDirection;
            }
            catch (System.Exception exception)
            {
                Log.Warning("[RimHistory] Could not calculate the selected tile's historical coordinate: " + exception.Message);
            }
        }
    }
}
