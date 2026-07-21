using System;
using System.IO;

namespace TEMO.AI;

public static class ExtractCacheCleanupService
{
    private const string ExtractFolderName = "TEMO.AI";

    public static void CleanupOldExtractCaches()
    {
        try
        {
            var extractRoot = Path.Combine(Path.GetTempPath(), ".net", ExtractFolderName);
            if (!Directory.Exists(extractRoot))
                return;

            var activeExtractDirectory = GetActiveExtractDirectory(extractRoot);

            foreach (var directory in Directory.GetDirectories(extractRoot))
            {
                if (IsSameOrParentDirectory(activeExtractDirectory, directory))
                    continue;

                TryDeleteDirectory(directory);
            }
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }

    private static string? GetActiveExtractDirectory(string extractRoot)
    {
        var baseDirectory = Path.GetFullPath(
            AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var normalizedRoot = Path.GetFullPath(
            extractRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

        if (baseDirectory.Equals(normalizedRoot, StringComparison.OrdinalIgnoreCase)
            || baseDirectory.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            return baseDirectory;
        }

        return null;
    }

    private static bool IsSameOrParentDirectory(string? activeDirectory, string candidateDirectory)
    {
        if (string.IsNullOrWhiteSpace(activeDirectory))
            return false;

        var normalizedActive = Path.GetFullPath(
            activeDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var normalizedCandidate = Path.GetFullPath(
            candidateDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

        return normalizedCandidate.Equals(normalizedActive, StringComparison.OrdinalIgnoreCase)
            || normalizedActive.StartsWith(normalizedCandidate + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static void TryDeleteDirectory(string directory)
    {
        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch
        {
            // Older extract folders may still be locked briefly by the OS.
        }
    }
}
