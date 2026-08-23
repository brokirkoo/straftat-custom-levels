# STRAFTAT Custom Levels

A BepInEx plugin that discovers custom Unity scenes in AssetBundles, adds them
to STRAFTAT's map-selection interfaces, and lets them participate in the game's
normal playlists and scene-loading flow.

Custom map packs are data-only: a pack needs an AssetBundle and a small
`scenes.json` manifest. It may also provide a PNG preview for each map.

## Requirements

- STRAFTAT for Windows
- BepInEx 5 (`BepInExPack` 5.4.2305 is the development baseline)
- The `STRAFTAT.CustomLevels.dll` plugin
- Map AssetBundles built for Windows with Unity 2021.3.45f2

## Installing the plugin

Install BepInEx for STRAFTAT, then place the plugin DLL under the profile's
`BepInEx/plugins` directory. For example:

```text
<STRAFTAT or profile directory>/
└── BepInEx/
    └── plugins/
        └── STRAFTAT.CustomLevels/
            └── STRAFTAT.CustomLevels.dll
```

Start the game once and check the BepInEx log for an initialization message
from **STRAFTAT Custom Levels**.

## Installing a custom map pack

A map pack has two parts:

1. Its AssetBundle goes in `BepInEx/Assets/AssetBundles`.
2. Its `scenes.json` and optional previews go together in a direct child
   directory of `BepInEx/plugins`.

For example:

```text
<BepInEx profile>/
└── BepInEx/
    ├── Assets/
    │   └── AssetBundles/
    │       └── fred-maps
    └── plugins/
        └── author-fred-maps/
            ├── scenes.json
            └── previews/
                ├── FredLevel.png
                └── FredArena.png
```

Do not put `scenes.json` in a nested directory below the pack directory. The
plugin scans direct children of `BepInEx/plugins` for manifests.

After installing a pack, restart STRAFTAT. Registered maps appear in the
standard map selectors and can be added to playlists like built-in maps.

## Creating `scenes.json`

The minimal manifest declares an AssetBundle filename and one or more scene
names:

```json
{
  "bundles": [
    {
      "name": "fred-maps",
      "scenes": ["FredLevel", "FredArena"]
    }
  ]
}
```

- `name` is the exact filename of the bundle in
  `BepInEx/Assets/AssetBundles`. It must be a filename, not a path.
- `scenes` contains the scene names exposed by that bundle. Each value must
  match the filename portion of exactly one scene in the bundle. For example,
  `Assets/Levels/FredLevel.unity` is declared as `FredLevel`.
- One manifest may contain multiple bundle entries.
- Scene names must not collide with built-in maps or scenes registered by
  another pack. The first registered map wins when names collide.

An invalid bundle entry is skipped and reported in the BepInEx log. Problems
with one pack do not disable unrelated packs.

## Adding map previews

Add an optional `previews` object to a bundle entry. Its keys are declared
scene names and its values are paths relative to the directory containing
`scenes.json`:

```json
{
  "bundles": [
    {
      "name": "fred-maps",
      "scenes": ["FredLevel", "FredArena", "FredNoPreview"],
      "previews": {
        "FredLevel": "previews/FredLevel.png",
        "FredArena": "previews/FredArena.png"
      }
    }
  ]
}
```

Previews are optional both for the whole manifest and for individual scenes.
In the example, `FredNoPreview` uses STRAFTAT's normal fallback thumbnail.

Preview requirements:

- PNG format only
- No larger than 16 MiB
- No wider or taller than 4096 pixels
- A 16:9 aspect ratio is recommended, but not required
- Paths must be relative and must remain inside the pack directory
- A preview key must name a scene in the same bundle entry

Images are loaded only when their map UI is created and are then cached. A
valid preview is used for the editable map tile, its larger hover image, and
the lobby/map-pool selector. Playlist-name rows remain text-only.

Missing, unreadable, oversized, or invalid previews do not disable a map. The
plugin logs a warning once and leaves STRAFTAT's normal thumbnail in place.

## Multiplayer use

Every participant should install the same custom map packs. The plugin can
translate registered custom scene names through STRAFTAT's local and FishNet
scene-loading paths, but the current version does not transfer AssetBundles or
automatically prove that peers have identical bundle contents.

Before hosting a custom-map playlist, make sure all players have the same pack
versions. A missing or different bundle can prevent a peer from loading the
selected scene.

## Troubleshooting

Check `BepInEx/LogOutput.log` when a map does not appear or a preview falls
back to the default image.

Common causes include:

- The bundle is not in `BepInEx/Assets/AssetBundles`.
- The manifest's bundle `name` does not exactly match its filename.
- `scenes.json` is malformed or is not directly inside a plugin pack folder.
- A declared scene name does not match a scene inside the AssetBundle.
- Two packs, or a pack and the base game, use the same map name.
- A preview path is rooted, leaves the pack directory, or points to a non-PNG
  file.
- The AssetBundle was built for the wrong Unity version or target platform.

Maps without previews are expected to work normally and do not produce a
warning.

## Building the plugin

The project targets .NET Framework 4.7.2 and references assemblies from the
installed game and BepInEx profile. Override the reference roots if they differ
from the defaults in the project file:

```powershell
dotnet build STRAFTAT.CustomLevels.sln -c Release `
  -p:STRAFTATManagedDir="C:\path\to\STRAFTAT_Data\Managed" `
  -p:BepInExCoreDir="C:\path\to\BepInEx\core"
```

The resulting plugin is
`src/STRAFTAT.CustomLevels/bin/Release/net472/STRAFTAT.CustomLevels.dll`.
