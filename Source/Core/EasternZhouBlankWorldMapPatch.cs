using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimHistory.Core
{
    /// <summary>Replaces the random world surface with a uniform, buildable template for the Eastern Zhou scenario.</summary>
    [HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GenerateWorld))]
    internal static class EasternZhouBlankWorldMapPatch
    {
        private static void Postfix(World __result)
        {
            if (Find.Scenario == null || Find.Scenario.name != "先秦") return;
            if (__result?.grid?.Surface == null) return;

            BiomeDef biome = DefDatabase<BiomeDef>.GetNamedSilentFail("TemperateForest");
            if (biome == null || biome.impassable || !biome.canBuildBase)
            {
                Log.Error("[RimHistory] Cannot apply the blank world map: TemperateForest is unavailable or unsuitable.");
                return;
            }

            World previousCreatingWorld = Current.CreatingWorld;
            Current.CreatingWorld = __result;
            try
            {
                for (int tileId = 0; tileId < __result.grid.TilesCount; tileId++)
                {
                    Tile tile = __result.grid.Surface.Tiles[tileId];
                    if (tile == null) continue;

                    tile.PrimaryBiome = biome;
                    tile.elevation = 0.1f;
                    tile.hilliness = Hilliness.Flat;
                    tile.mutatorsNullable = new List<TileMutatorDef>();
                    tile.feature = null;

                    SurfaceTile surfaceTile = __result.grid.Surface[tileId];
                    if (surfaceTile != null)
                    {
                        surfaceTile.potentialRivers = null;
                        surfaceTile.potentialRoads = null;
                        surfaceTile.riverDist = 0;
                    }

                    PlanetTile planetTile = new PlanetTile(tileId, __result.grid.Surface);
                    if (__result.landmarks != null && __result.landmarks[planetTile] != null)
                        __result.landmarks.RemoveLandmark(planetTile);
                }

                __result.features?.features?.Clear();
                byte[] tileFeatures = __result.grid.Surface.tileFeature;
                if (tileFeatures != null)
                {
                    for (int index = 0; index < tileFeatures.Length; index++)
                        tileFeatures[index] = byte.MaxValue;
                }

                if (__result.worldObjects != null)
                {
                    List<Settlement> generatedSettlements = __result.worldObjects.Settlements.ToList();
                    foreach (Settlement settlement in generatedSettlements)
                        __result.worldObjects.Remove(settlement);
                    Log.Message("[RimHistory] Removed " + generatedSettlements.Count
                        + " randomly generated settlement(s) for 先秦.");
                }

                __result.pathGrid?.RecalculateAllLayersPathCosts();
                Log.Message("[RimHistory] Replaced the random surface with a uniform TemperateForest template for 先秦.");
            }
            finally
            {
                Current.CreatingWorld = previousCreatingWorld;
            }
        }
    }
}
