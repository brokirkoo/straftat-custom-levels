using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FishNet.Managing.Scened;
using HarmonyLib;

namespace STRAFTAT.CustomLevels.Integration
{
    [HarmonyPatch]
    internal static class FishNetSceneLoadPatch
    {
        [HarmonyTargetMethods]
        private static IEnumerable<MethodBase> TargetMethods()
        {
            Type sceneManagerType = typeof(FishNet.Managing.Scened.SceneManager);
            return sceneManagerType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(method =>
                    (method.Name == "LoadGlobalScenes" || method.Name == "LoadConnectionScenes") &&
                    method.GetParameters().Any(parameter => parameter.ParameterType == typeof(SceneLoadData)));
        }

        [HarmonyPrefix]
        private static void Prefix(MethodBase __originalMethod, object[] __args)
        {
            try
            {
                foreach (object argument in __args)
                {
                    if (argument is SceneLoadData sceneLoadData)
                        Redirect(sceneLoadData);
                }
            }
            catch (Exception exception)
            {
                Plugin.Log.LogError($"Failed to redirect scenes for FishNet {__originalMethod.Name}: {exception}");
            }
        }

        private static void Redirect(SceneLoadData sceneLoadData)
        {
            if (sceneLoadData == null || Plugin.Scenes == null)
                return;

            SceneLookupData[] lookups = sceneLoadData.SceneLookupDatas;
            if (lookups != null)
            {
                foreach (SceneLookupData lookup in lookups)
                    Redirect(lookup);
            }

            Redirect(sceneLoadData.PreferredActiveScene);
        }

        private static void Redirect(SceneLookupData lookup)
        {
            if (lookup == null || lookup.Handle != 0)
                return;

            if (Plugin.Scenes.TryResolve(lookup.Name, out string scenePath))
                lookup.Name = scenePath;
        }
    }
}
