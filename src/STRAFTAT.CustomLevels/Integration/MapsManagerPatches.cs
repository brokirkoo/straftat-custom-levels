using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using STRAFTAT.CustomLevels.Registry;
using UnityEngine;

namespace STRAFTAT.CustomLevels.Integration
{
    [HarmonyPatch(typeof(MapsManager), nameof(MapsManager.InitMaps))]
    internal static class MapsManagerInitMapsPatch
    {
        private static readonly FieldInfo MapPrefabField = AccessTools.Field(typeof(MapsManager), "mapInstance");
        private static readonly FieldInfo StandardMapParentField = AccessTools.Field(typeof(MapsManager), "standardMapParent");

        [HarmonyPostfix]
        private static void Postfix(MapsManager __instance)
        {
            try
            {
                InjectCustomMaps(__instance);
            }
            catch (Exception exception)
            {
                Plugin.Log.LogError($"Failed to inject custom maps into MapsManager: {exception}");
            }
        }

        private static void InjectCustomMaps(MapsManager mapsManager)
        {
            if (Plugin.Scenes == null || Plugin.Scenes.Count == 0)
                return;

            if (mapsManager.allMaps == null || mapsManager.allMapsDict == null)
            {
                Plugin.Log.LogError("MapsManager map collections were not initialized; custom maps cannot be injected.");
                return;
            }

            if (MapPrefabField == null || StandardMapParentField == null)
            {
                Plugin.Log.LogError("Could not locate MapsManager's standard map UI fields.");
                return;
            }

            var mapPrefab = MapPrefabField.GetValue(mapsManager) as GameObject;
            var standardMapParent = StandardMapParentField.GetValue(mapsManager) as Transform;
            if (mapPrefab == null || standardMapParent == null)
            {
                Plugin.Log.LogError("MapsManager's standard map prefab or parent is unavailable.");
                return;
            }

            var maps = new List<Map>(mapsManager.allMaps);
            foreach (CustomScene customScene in Plugin.Scenes.Scenes)
            {
                if (ContainsMapName(mapsManager, customScene.Name))
                {
                    Plugin.Log.LogWarning(
                        $"Map name '{customScene.Name}' already exists in STRAFTAT. The existing map will be used.");
                    continue;
                }

                var map = new Map
                {
                    index = maps.Count,
                    mapName = customScene.Name,
                    isDlcExclusive = false,
                    isAltMap = false,
                    isSelected = false,
                    isUnlocked = true,
                    mapInstance = null
                };

                GameObject button = UnityEngine.Object.Instantiate(
                    mapPrefab,
                    standardMapParent.position,
                    Quaternion.identity,
                    standardMapParent);

                MapInstance mapInstance = button.GetComponent<MapInstance>();
                if (mapInstance == null)
                {
                    UnityEngine.Object.Destroy(button);
                    Plugin.Log.LogError(
                        $"Could not create UI for custom map '{customScene.Name}': standard prefab has no MapInstance.");
                    continue;
                }

                map.mapInstance = mapInstance;
                mapInstance.name = map.mapName;
                mapInstance.selected = false;
                mapInstance.UpdateUI();
                button.SetActive(true);

                maps.Add(map);
                mapsManager.allMapsDict.Add(map.mapName, map);
            }

            mapsManager.allMaps = maps.ToArray();
            mapsManager.unlockedMaps = maps
                .Where(map => map != null && map.isUnlocked)
                .Select(map => map.index)
                .ToArray();

            mapsManager.SortMapsFromMapInstanceName();
        }

        private static bool ContainsMapName(MapsManager mapsManager, string name)
        {
            return mapsManager.allMapsDict.Keys.Any(existing =>
                string.Equals(existing, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
