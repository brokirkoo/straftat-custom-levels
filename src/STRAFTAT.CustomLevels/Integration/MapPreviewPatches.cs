using System;
using System.Reflection;
using HarmonyLib;
using STRAFTAT.CustomLevels.Previews;
using STRAFTAT.CustomLevels.Registry;
using UnityEngine;
using UnityEngine.UI;

namespace STRAFTAT.CustomLevels.Integration
{
    internal static class MapPreviewApplicator
    {
        private static readonly PreviewCache Cache = new PreviewCache(Plugin.Log);

        public static void Apply(object instance, string imageFieldName)
        {
            if (instance == null || Plugin.Scenes == null)
                return;
            var component = instance as Component;
            if (component == null || !Plugin.Scenes.TryGet(component.name, out CustomScene scene) ||
                !Cache.TryGet(scene.PreviewPath, out Texture2D texture))
                return;

            FieldInfo spriteField = AccessTools.Field(instance.GetType(), "sprite");
            FieldInfo imageField = AccessTools.Field(instance.GetType(), imageFieldName);
            if (spriteField == null || imageField == null)
            {
                Plugin.Log.LogWarning($"Could not locate preview fields on {instance.GetType().Name} for custom map '{scene.Name}'.");
                return;
            }

            spriteField.SetValue(instance, texture);
            var image = imageField.GetValue(instance) as RawImage;
            if (image != null)
                image.texture = texture;
        }
    }

    [HarmonyPatch(typeof(MapInstance), "Start")]
    internal static class MapInstancePreviewPatch
    {
        [HarmonyPostfix]
        private static void Postfix(MapInstance __instance)
        {
            try { MapPreviewApplicator.Apply(__instance, "img"); }
            catch (Exception exception) { Plugin.Log.LogWarning($"Could not apply a custom map preview: {exception}"); }
        }
    }

    [HarmonyPatch(typeof(SelectSceneInstance), "Start")]
    internal static class SelectSceneInstancePreviewPatch
    {
        [HarmonyPostfix]
        private static void Postfix(SelectSceneInstance __instance)
        {
            try { MapPreviewApplicator.Apply(__instance, "mapImg"); }
            catch (Exception exception) { Plugin.Log.LogWarning($"Could not apply a custom selector preview: {exception}"); }
        }
    }
}
