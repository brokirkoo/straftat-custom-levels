using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;

namespace STRAFTAT.CustomLevels.Registry
{
    internal sealed class CustomSceneRegistry
    {
        private readonly Dictionary<string, CustomScene> _scenesByName =
            new Dictionary<string, CustomScene>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, CustomScene> _scenesByPath =
            new Dictionary<string, CustomScene>(StringComparer.OrdinalIgnoreCase);
        private readonly ManualLogSource _log;

        public CustomSceneRegistry(ManualLogSource log)
        {
            _log = log;
        }

        public int Count => _scenesByName.Count;

        public IEnumerable<CustomScene> Scenes =>
            _scenesByName.Values.OrderBy(scene => scene.Name, StringComparer.OrdinalIgnoreCase);

        public bool Register(CustomScene scene)
        {
            if (_scenesByName.TryGetValue(scene.Name, out CustomScene existing))
            {
                _log.LogWarning(
                    $"Scene name '{scene.Name}' from '{scene.ManifestPath}' is already registered by " +
                    $"'{existing.ManifestPath}'. The first declaration will be used.");
                return false;
            }

            if (_scenesByPath.TryGetValue(scene.ScenePath, out existing))
            {
                _log.LogWarning(
                    $"Scene path '{scene.ScenePath}' from '{scene.ManifestPath}' is already registered as " +
                    $"'{existing.Name}'. The first declaration will be used.");
                return false;
            }

            _scenesByName.Add(scene.Name, scene);
            _scenesByPath.Add(scene.ScenePath, scene);
            return true;
        }

        public bool TryResolve(string requestedName, out string scenePath)
        {
            scenePath = null;
            if (string.IsNullOrWhiteSpace(requestedName))
                return false;

            if (_scenesByName.TryGetValue(requestedName, out CustomScene scene) ||
                _scenesByPath.TryGetValue(requestedName, out scene))
            {
                scenePath = scene.ScenePath;
                return true;
            }

            return false;
        }
    }
}
