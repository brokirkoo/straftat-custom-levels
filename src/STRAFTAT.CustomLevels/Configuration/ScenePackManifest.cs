using System.Collections.Generic;
using Newtonsoft.Json;

namespace STRAFTAT.CustomLevels.Configuration
{
    internal sealed class ScenePackManifest
    {
        [JsonProperty("bundles", Required = Required.Always)]
        public List<BundleManifestEntry> Bundles { get; set; }
    }

    internal sealed class BundleManifestEntry
    {
        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; }

        [JsonProperty("scenes", Required = Required.Always)]
        public List<string> Scenes { get; set; }
    }
}
