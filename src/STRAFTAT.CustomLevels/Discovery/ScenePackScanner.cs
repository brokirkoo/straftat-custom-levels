using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using Newtonsoft.Json;
using STRAFTAT.CustomLevels.Bundles;
using STRAFTAT.CustomLevels.Configuration;
using STRAFTAT.CustomLevels.Registry;
using UnityEngine;

namespace STRAFTAT.CustomLevels.Discovery
{
    internal sealed class ScenePackScanner
    {
        private const string ManifestFileName = "scenes.json";

        private readonly ManualLogSource _log;
        private readonly CustomSceneRegistry _registry;
        private readonly BundleCache _bundleCache;
        private readonly Dictionary<string, string> _bundleOwners =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public ScenePackScanner(ManualLogSource log, CustomSceneRegistry registry)
        {
            _log = log;
            _registry = registry;
            _bundleCache = new BundleCache(log);
        }

        public void Scan()
        {
            string bundleRoot = Path.GetFullPath(Path.Combine(Paths.BepInExRootPath, "Assets", "AssetBundles"));
            Directory.CreateDirectory(bundleRoot);

            string[] packDirectories;
            try
            {
                packDirectories = Directory.GetDirectories(Paths.PluginPath)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch (Exception exception)
            {
                _log.LogError($"Could not enumerate plugin pack directories under '{Paths.PluginPath}': {exception}");
                return;
            }

            int manifestCount = 0;
            foreach (string packDirectory in packDirectories)
            {
                string manifestPath = Path.Combine(packDirectory, ManifestFileName);
                if (!File.Exists(manifestPath))
                    continue;

                manifestCount++;
                ScanManifest(manifestPath, bundleRoot);
            }

            _log.LogInfo($"Scanned {manifestCount} scene manifest(s) and registered {_registry.Count} scene(s).");
        }

        private void ScanManifest(string manifestPath, string bundleRoot)
        {
            ScenePackManifest manifest;
            try
            {
                string json = File.ReadAllText(manifestPath);
                manifest = JsonConvert.DeserializeObject<ScenePackManifest>(json);
            }
            catch (Exception exception)
            {
                _log.LogError($"Ignoring malformed scene manifest '{manifestPath}': {exception.Message}");
                return;
            }

            if (manifest?.Bundles == null)
            {
                _log.LogError($"Ignoring scene manifest '{manifestPath}': 'bundles' must be an array.");
                return;
            }

            for (int index = 0; index < manifest.Bundles.Count; index++)
            {
                try
                {
                    ScanBundleEntry(manifest.Bundles[index], index, manifestPath, bundleRoot);
                }
                catch (Exception exception)
                {
                    _log.LogError(
                        $"Ignoring bundle entry {index} in '{manifestPath}' after an unexpected error: {exception}");
                }
            }
        }

        private void ScanBundleEntry(
            BundleManifestEntry entry,
            int entryIndex,
            string manifestPath,
            string bundleRoot)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.Name))
            {
                _log.LogError($"Ignoring bundle entry {entryIndex} in '{manifestPath}': 'name' is required.");
                return;
            }

            if (entry.Scenes == null || entry.Scenes.Count == 0 || entry.Scenes.Any(string.IsNullOrWhiteSpace))
            {
                _log.LogError(
                    $"Ignoring bundle '{entry.Name}' in '{manifestPath}': 'scenes' must contain at least one non-empty name.");
                return;
            }

            if (!IsSafeFileName(entry.Name))
            {
                _log.LogError(
                    $"Ignoring bundle '{entry.Name}' in '{manifestPath}': bundle names must be filenames, not paths.");
                return;
            }

            string bundlePath = Path.GetFullPath(Path.Combine(bundleRoot, entry.Name));
            if (!IsWithinDirectory(bundlePath, bundleRoot))
            {
                _log.LogError($"Ignoring bundle '{entry.Name}' because it resolves outside '{bundleRoot}'.");
                return;
            }

            if (_bundleOwners.TryGetValue(bundlePath, out string firstManifest))
            {
                if (!string.Equals(firstManifest, manifestPath, StringComparison.OrdinalIgnoreCase))
                {
                    _log.LogWarning(
                        $"Bundle '{entry.Name}' is declared by both '{firstManifest}' and '{manifestPath}'. " +
                        "Only the first declaration will be used.");
                    return;
                }
            }
            else
            {
                _bundleOwners.Add(bundlePath, manifestPath);
            }

            if (!File.Exists(bundlePath))
            {
                _log.LogError($"Bundle '{entry.Name}' declared by '{manifestPath}' does not exist at '{bundlePath}'.");
                return;
            }

            AssetBundle bundle = _bundleCache.Load(bundlePath);
            if (bundle == null)
                return;

            string[] internalPaths;
            try
            {
                internalPaths = bundle.GetAllScenePaths() ?? Array.Empty<string>();
            }
            catch (Exception exception)
            {
                _log.LogError($"Could not inspect scenes in bundle '{bundlePath}': {exception}");
                return;
            }

            Dictionary<string, string> previews = ResolvePreviews(entry, manifestPath);
            foreach (string declaredScene in entry.Scenes.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                string sceneName = declaredScene.Trim();
                previews.TryGetValue(sceneName, out string previewPath);
                RegisterDeclaredScene(sceneName, internalPaths, bundle, bundlePath, manifestPath, previewPath);
            }
        }

        private Dictionary<string, string> ResolvePreviews(BundleManifestEntry entry, string manifestPath)
        {
            var resolved = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (entry.Previews == null)
                return resolved;

            var scenes = new HashSet<string>(entry.Scenes.Select(scene => scene.Trim()), StringComparer.OrdinalIgnoreCase);
            string packDirectory = Path.GetFullPath(Path.GetDirectoryName(manifestPath));
            foreach (KeyValuePair<string, string> preview in entry.Previews)
            {
                string sceneName = preview.Key?.Trim();
                if (string.IsNullOrWhiteSpace(sceneName) || !scenes.Contains(sceneName))
                {
                    _log.LogWarning($"Ignoring preview mapping '{preview.Key}' in '{manifestPath}': only declared scenes may have previews.");
                    continue;
                }

                string relativePath = preview.Value;
                if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
                {
                    _log.LogWarning($"Ignoring preview for scene '{sceneName}' in '{manifestPath}': its path must be pack-relative.");
                    continue;
                }

                try
                {
                    string fullPath = Path.GetFullPath(Path.Combine(packDirectory, relativePath));
                    if (!IsWithinDirectory(fullPath, packDirectory) ||
                        !string.Equals(Path.GetExtension(fullPath), ".png", StringComparison.OrdinalIgnoreCase))
                    {
                        _log.LogWarning($"Ignoring preview for scene '{sceneName}' in '{manifestPath}': path must be a PNG inside the pack directory.");
                        continue;
                    }

                    resolved[sceneName] = fullPath;
                }
                catch (Exception exception)
                {
                    _log.LogWarning($"Ignoring preview for scene '{sceneName}' in '{manifestPath}': invalid path ({exception.Message}).");
                }
            }

            return resolved;
        }

        private void RegisterDeclaredScene(
            string declaredScene,
            IEnumerable<string> internalPaths,
            AssetBundle bundle,
            string bundlePath,
            string manifestPath,
            string previewPath)
        {
            string[] matches = internalPaths
                .Where(path => string.Equals(
                    Path.GetFileNameWithoutExtension(path),
                    declaredScene,
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (matches.Length == 0)
            {
                _log.LogError(
                    $"Scene '{declaredScene}' declared by '{manifestPath}' was not found in bundle '{bundlePath}'.");
                return;
            }

            if (matches.Length > 1)
            {
                _log.LogError(
                    $"Scene name '{declaredScene}' is ambiguous in bundle '{bundlePath}': {string.Join(", ", matches)}");
                return;
            }

            var scene = new CustomScene(declaredScene, matches[0], bundlePath, manifestPath, previewPath, bundle);
            if (_registry.Register(scene))
                _log.LogInfo($"Registered custom scene '{declaredScene}' as '{matches[0]}'.");
        }

        private static bool IsSafeFileName(string name)
        {
            return string.Equals(name, Path.GetFileName(name), StringComparison.Ordinal) &&
                   name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 &&
                   !string.Equals(name, ".", StringComparison.Ordinal) &&
                   !string.Equals(name, "..", StringComparison.Ordinal);
        }

        private static bool IsWithinDirectory(string path, string directory)
        {
            string directoryWithSeparator = directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                                            Path.DirectorySeparatorChar;
            return path.StartsWith(directoryWithSeparator, StringComparison.OrdinalIgnoreCase);
        }
    }
}
