using System;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

namespace STRAFTAT.CustomLevels.Bundles
{
    internal sealed class BundleCache
    {
        private readonly Dictionary<string, AssetBundle> _bundles =
            new Dictionary<string, AssetBundle>(StringComparer.OrdinalIgnoreCase);
        private readonly ManualLogSource _log;

        public BundleCache(ManualLogSource log)
        {
            _log = log;
        }

        public AssetBundle Load(string canonicalPath)
        {
            if (_bundles.TryGetValue(canonicalPath, out AssetBundle cached))
                return cached;

            AssetBundle bundle;
            try
            {
                bundle = AssetBundle.LoadFromFile(canonicalPath);
            }
            catch (Exception exception)
            {
                _log.LogError($"Failed to load AssetBundle '{canonicalPath}': {exception}");
                return null;
            }

            if (bundle == null)
            {
                _log.LogError($"Unity could not load AssetBundle '{canonicalPath}'.");
                return null;
            }

            _bundles.Add(canonicalPath, bundle);
            return bundle;
        }
    }
}
