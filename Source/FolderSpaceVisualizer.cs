using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

public class FolderSpaceItem {
    public string Name { get; set; }
    public string FullPath { get; set; }
    public long Bytes { get; set; }
    public int FileCount { get; set; }
    public double PercentOfTotal { get; set; }

    public string SizeLabel {
        get { return Features.Size(Bytes); }
    }

    public string PercentLabel {
        get { return PercentOfTotal.ToString("0.0") + "%"; }
    }
}

public static class FolderSpaceVisualizer {
    public static List<FolderSpaceItem> Analyze(string rootPath, CancellationToken token, IProgress<string> progress) {
        var results = new List<FolderSpaceItem>();
        if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath)) return results;

        DirectoryInfo rootInfo = new DirectoryInfo(rootPath);
        DirectoryInfo[] subDirs;
        try {
            subDirs = rootInfo.GetDirectories();
        } catch {
            return results;
        }

        long totalAll = 0;
        for (int i = 0; i < subDirs.Length; i++) {
            token.ThrowIfCancellationRequested();
            DirectoryInfo d = subDirs[i];

            // Skip special volume information and recycle bin
            if (d.Name.StartsWith("$") || d.Name.Equals("System Volume Information", StringComparison.OrdinalIgnoreCase)) continue;

            if (progress != null) progress.Report("Đang đo dung lượng: " + d.Name + "…");

            long dirBytes = 0;
            int fileCount = 0;
            try {
                foreach (FileInfo fi in d.EnumerateFiles("*", SearchOption.AllDirectories)) {
                    token.ThrowIfCancellationRequested();
                    try {
                        dirBytes += fi.Length;
                        fileCount++;
                    } catch {}
                }
            } catch {}

            if (dirBytes > 0) {
                var item = new FolderSpaceItem();
                item.Name = d.Name;
                item.FullPath = d.FullName;
                item.Bytes = dirBytes;
                item.FileCount = fileCount;
                results.Add(item);
                totalAll += dirBytes;
            }
        }

        if (totalAll > 0) {
            for (int i = 0; i < results.Count; i++) {
                results[i].PercentOfTotal = (double)results[i].Bytes * 100.0 / totalAll;
            }
        }

        return results.OrderByDescending(delegate(FolderSpaceItem x) { return x.Bytes; }).ToList();
    }
}
