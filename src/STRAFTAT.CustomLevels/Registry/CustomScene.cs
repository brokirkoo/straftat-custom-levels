using UnityEngine;

namespace STRAFTAT.CustomLevels.Registry
{
    internal sealed class CustomScene
    {
        public CustomScene(string name, string scenePath, string bundlePath, string manifestPath, AssetBundle bundle)
        {
            Name = name;
            ScenePath = scenePath;
            BundlePath = bundlePath;
            ManifestPath = manifestPath;
            Bundle = bundle;
        }

        public string Name { get; }
        public string ScenePath { get; }
        public string BundlePath { get; }
        public string ManifestPath { get; }
        public AssetBundle Bundle { get; }
    }
}
