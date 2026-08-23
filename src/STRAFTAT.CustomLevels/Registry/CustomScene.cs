using UnityEngine;

namespace STRAFTAT.CustomLevels.Registry
{
    internal sealed class CustomScene
    {
        public CustomScene(
            string name,
            string scenePath,
            string bundlePath,
            string manifestPath,
            string previewPath,
            AssetBundle bundle)
        {
            Name = name;
            ScenePath = scenePath;
            BundlePath = bundlePath;
            ManifestPath = manifestPath;
            PreviewPath = previewPath;
            Bundle = bundle;
        }

        public string Name { get; }
        public string ScenePath { get; }
        public string BundlePath { get; }
        public string ManifestPath { get; }
        public string PreviewPath { get; }
        public AssetBundle Bundle { get; }
    }
}
