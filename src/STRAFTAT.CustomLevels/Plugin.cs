using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using STRAFTAT.CustomLevels.Discovery;
using STRAFTAT.CustomLevels.Registry;

namespace STRAFTAT.CustomLevels
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.mrbro.straftat.customlevels";
        public const string PluginName = "STRAFTAT Custom Levels";
        public const string PluginVersion = "1.0.0";

        private static bool _initialized;
        private Harmony _harmony;

        internal static ManualLogSource Log { get; private set; }
        internal static CustomSceneRegistry Scenes { get; private set; }

        private void Awake()
        {
            if (_initialized)
            {
                Logger.LogWarning("Plugin initialization was requested more than once; ignoring the duplicate request.");
                return;
            }

            _initialized = true;
            Log = Logger;
            Scenes = new CustomSceneRegistry(Logger);

            try
            {
                new ScenePackScanner(Logger, Scenes).Scan();
            }
            catch (Exception exception)
            {
                Logger.LogError($"Unexpected failure while scanning custom level packs: {exception}");
            }

            try
            {
                _harmony = new Harmony(PluginGuid);
                _harmony.PatchAll(typeof(Plugin).Assembly);
                Logger.LogInfo($"Initialized with {Scenes.Count} custom scene(s).");
            }
            catch (Exception exception)
            {
                Logger.LogError($"Failed to apply Harmony patches: {exception}");
            }
        }
    }
}
