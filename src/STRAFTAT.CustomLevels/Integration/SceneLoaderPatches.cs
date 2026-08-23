using HarmonyLib;

namespace STRAFTAT.CustomLevels.Integration
{
    [HarmonyPatch(typeof(SceneLoader), nameof(SceneLoader.LoadScene), new[] { typeof(string) })]
    internal static class SceneLoaderLoadScenePatch
    {
        [HarmonyPrefix]
        private static void Prefix(ref string sceneName)
        {
            if (Plugin.Scenes != null && Plugin.Scenes.TryResolve(sceneName, out string scenePath))
                sceneName = scenePath;
        }
    }
}
