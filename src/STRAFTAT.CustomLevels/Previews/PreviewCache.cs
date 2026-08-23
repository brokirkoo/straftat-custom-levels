using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Logging;
using UnityEngine;

namespace STRAFTAT.CustomLevels.Previews
{
    internal sealed class PreviewCache
    {
        private const long MaximumFileSize = 16L * 1024L * 1024L;
        private const int MaximumDimension = 4096;
        private static readonly byte[] PngSignature = { 137, 80, 78, 71, 13, 10, 26, 10 };

        private readonly ManualLogSource _log;
        private readonly Dictionary<string, Texture2D> _textures =
            new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _failures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public PreviewCache(ManualLogSource log)
        {
            _log = log;
        }

        public bool TryGet(string path, out Texture2D texture)
        {
            texture = null;
            if (string.IsNullOrEmpty(path) || _failures.Contains(path))
                return false;
            if (_textures.TryGetValue(path, out texture))
                return true;

            try
            {
                var info = new FileInfo(path);
                if (!info.Exists)
                    throw new FileNotFoundException("The preview file does not exist.", path);
                if (info.Length > MaximumFileSize)
                    throw new InvalidDataException($"The preview exceeds the {MaximumFileSize / (1024 * 1024)} MiB size limit.");

                byte[] data = File.ReadAllBytes(path);
                if (!HasPngSignature(data))
                    throw new InvalidDataException("The preview is not a valid PNG file.");

                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!ImageConversion.LoadImage(texture, data, true))
                    throw new InvalidDataException("Unity could not decode the PNG image.");
                if (texture.width > MaximumDimension || texture.height > MaximumDimension)
                    throw new InvalidDataException($"The preview dimensions {texture.width}x{texture.height} exceed {MaximumDimension}x{MaximumDimension}.");

                texture.name = $"Custom level preview: {Path.GetFileName(path)}";
                _textures.Add(path, texture);
                return true;
            }
            catch (Exception exception)
            {
                if (texture != null)
                    UnityEngine.Object.Destroy(texture);
                texture = null;
                _failures.Add(path);
                _log.LogWarning($"Could not load custom level preview '{path}'; the normal thumbnail will be used: {exception.Message}");
                return false;
            }
        }

        private static bool HasPngSignature(byte[] data)
        {
            if (data == null || data.Length < PngSignature.Length)
                return false;
            for (int index = 0; index < PngSignature.Length; index++)
                if (data[index] != PngSignature[index])
                    return false;
            return true;
        }
    }
}
