using System;
using System.IO;
using System.Linq;

namespace TileTool.Services;

public static class TileNaming
{
    private const string DefaultPrefix = "tile";
    private static readonly char[] WindowsInvalidFileNameChars = ['<', '>', ':', '"', '/', '\\', '|', '?', '*'];

    public static string SanitizePrefix(string? prefix)
    {
        var normalized = string.IsNullOrWhiteSpace(prefix)
            ? DefaultPrefix
            : prefix.Trim();

        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(
            normalized
                .Select(ch => invalid.Contains(ch) || WindowsInvalidFileNameChars.Contains(ch) ? '_' : ch)
                .ToArray())
            .Trim();

        if (string.IsNullOrWhiteSpace(sanitized) || sanitized.All(ch => ch is '.' or '_'))
            return DefaultPrefix;

        return sanitized;
    }

    public static string BuildTileFileName(string? prefix, int index)
    {
        var safePrefix = SanitizePrefix(prefix);
        var safeIndex = Math.Max(1, index);
        return $"{safePrefix}_{safeIndex:D4}.png";
    }
}
