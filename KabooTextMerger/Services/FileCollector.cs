using System.IO;

namespace KabooTextMerger.Services;

public static class FileCollector
{
    public static IReadOnlyList<string> ExpandPaths(
        IEnumerable<string> rawPaths,
        out IReadOnlyList<string> warnings,
        ISet<string>? directoryExtensions = null)
    {
        var files = new List<string>();
        var localWarnings = new List<string>();

        foreach (var path in rawPaths.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            if (File.Exists(path))
            {
                files.Add(Path.GetFullPath(path));
                continue;
            }

            if (!Directory.Exists(path))
            {
                localWarnings.Add($"存在しないパスを無視しました: {path}");
                continue;
            }

            files.AddRange(EnumerateFilesRecursively(path, localWarnings, directoryExtensions));
        }

        warnings = localWarnings;
        return files;
    }

    private static IEnumerable<string> EnumerateFilesRecursively(
        string root,
        List<string> warnings,
        ISet<string>? directoryExtensions)
    {
        var directories = new Stack<string>();
        directories.Push(root);

        while (directories.Count > 0)
        {
            var directory = directories.Pop();

            string[] subDirectories;
            try
            {
                subDirectories = Directory.GetDirectories(directory);
            }
            catch (Exception ex)
            {
                warnings.Add($"フォルダ列挙失敗: {directory} ({ex.Message})");
                continue;
            }

            Array.Sort(subDirectories, StringComparer.OrdinalIgnoreCase);
            foreach (var subDirectory in subDirectories.Reverse())
            {
                directories.Push(subDirectory);
            }

            string[] localFiles;
            try
            {
                localFiles = Directory.GetFiles(directory);
            }
            catch (Exception ex)
            {
                warnings.Add($"ファイル列挙失敗: {directory} ({ex.Message})");
                continue;
            }

            Array.Sort(localFiles, StringComparer.OrdinalIgnoreCase);
            foreach (var file in localFiles)
            {
                if (directoryExtensions is not null)
                {
                    var extension = Path.GetExtension(file);
                    if (!directoryExtensions.Contains(extension))
                    {
                        continue;
                    }
                }

                yield return Path.GetFullPath(file);
            }
        }
    }
}

