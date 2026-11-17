using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualBasic.FileIO;

public class DevAiCacheItem {
    public string Category { get; set; }
    public string ToolName { get; set; }
    public string Path { get; set; }
    public string Description { get; set; }
    public long Bytes { get; set; }
    public int FileCount { get; set; }
    public bool Exists { get; set; }
    public bool Selected { get; set; }

    public string SizeLabel {
        get { return Features.Size(Bytes); }
    }
}

public static class DevAiCacheCleaner {
    public static List<DevAiCacheItem> GetKnownTargets() {
        var list = new List<DevAiCacheItem>();
        string user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // 1. Web & JavaScript Ecosystem
        list.Add(new DevAiCacheItem {
            Category = "Lập trình Web & JavaScript",
            ToolName = "NPM Cache (Roaming)",
            Path = Path.Combine(appData, "npm-cache"),
            Description = "Bộ đệm các gói thư viện Node.js tải về từ npmjs.org.",
            Selected = true
        });
        list.Add(new DevAiCacheItem {
            Category = "Lập trình Web & JavaScript",
            ToolName = "NPM Cache (Local v7+)",
            Path = Path.Combine(local, "npm-cache"),
            Description = "Bộ đệm _cacache thế hệ mới của trình quản lý gói npm v7+.",
            Selected = true
        });
        list.Add(new DevAiCacheItem {
            Category = "Lập trình Web & JavaScript",
            ToolName = "Yarn Package Cache",
            Path = Path.Combine(local, "Yarn", "Cache"),
            Description = "Bộ nhớ đệm lưu trữ package tarballs của Yarn v1/v2.",
            Selected = true
        });
        list.Add(new DevAiCacheItem {
            Category = "Lập trình Web & JavaScript",
            ToolName = "pnpm Content Store",
            Path = Path.Combine(local, "pnpm", "store"),
            Description = "Kho lưu trữ deduplicated store của trình quản lý pnpm.",
            Selected = false
        });
        list.Add(new DevAiCacheItem {
            Category = "Lập trình Web & JavaScript",
            ToolName = "Bun Cache",
            Path = Path.Combine(user, ".bun", "install", "cache"),
            Description = "Bộ nhớ đệm gói tải về của Bun runtime.",
            Selected = true
        });

        // 2. Python & Trí Tuệ Nhân Tạo (AI)
        list.Add(new DevAiCacheItem {
            Category = "Python & Trí Tuệ Nhân Tạo (AI)",
            ToolName = "Pip Wheel Cache",
            Path = Path.Combine(local, "pip", "cache"),
            Description = "Bộ đệm lưu trữ các file bánh xe (.whl) khi cài đặt thư viện Python.",
            Selected = true
        });
        list.Add(new DevAiCacheItem {
            Category = "Python & Trí Tuệ Nhân Tạo (AI)",
            ToolName = "Astral uv Python Cache",
            Path = Path.Combine(local, "uv", "cache"),
            Description = "Bộ nhớ đệm gói siêu tốc của trình quản lý Python uv.",
            Selected = true
        });
        list.Add(new DevAiCacheItem {
            Category = "Python & Trí Tuệ Nhân Tạo (AI)",
            ToolName = "Hugging Face Model Cache",
            Path = Path.Combine(user, ".cache", "huggingface", "hub"),
            Description = "Bộ đệm snapshot các checkpoint mô hình LLM & Transformers.",
            Selected = false
        });
        list.Add(new DevAiCacheItem {
            Category = "Python & Trí Tuệ Nhân Tạo (AI)",
            ToolName = "PyTorch Kernel & Hub Cache",
            Path = Path.Combine(user, ".cache", "torch"),
            Description = "Bộ nhớ đệm kernel biên dịch CUDA và hub checkpoints của PyTorch.",
            Selected = true
        });

        // 3. Hệ Thống Biên Dịch & Ngôn Ngữ Khác
        list.Add(new DevAiCacheItem {
            Category = "Ngôn ngữ hệ thống & Biên dịch",
            ToolName = ".NET NuGet HTTP Cache",
            Path = Path.Combine(local, "NuGet", "v3-cache"),
            Description = "Bộ đệm phản hồi HTTP metadata khi khôi phục gói NuGet .NET.",
            Selected = true
        });
        list.Add(new DevAiCacheItem {
            Category = "Ngôn ngữ hệ thống & Biên dịch",
            ToolName = "Rust Cargo Registry Cache",
            Path = Path.Combine(user, ".cargo", "registry", "cache"),
            Description = "Tệp nén .crate tải về từ crates.io của ngôn ngữ Rust.",
            Selected = true
        });
        list.Add(new DevAiCacheItem {
            Category = "Ngôn ngữ hệ thống & Biên dịch",
            ToolName = "Java Gradle Build Cache",
            Path = Path.Combine(user, ".gradle", "caches"),
            Description = "Bộ đệm task output & artifact tải về của Gradle build system.",
            Selected = false
        });
        list.Add(new DevAiCacheItem {
            Category = "Ngôn ngữ hệ thống & Biên dịch",
            ToolName = "Go Modules Cache",
            Path = Path.Combine(user, "go", "pkg", "mod", "cache"),
            Description = "Bộ nhớ đệm mã nguồn module tải về của Go runtime.",
            Selected = true
        });
        list.Add(new DevAiCacheItem {
            Category = "Ngôn ngữ hệ thống & Biên dịch",
            ToolName = "Android SDK Build Cache",
            Path = Path.Combine(user, ".android", "build-cache"),
            Description = "Bộ nhớ đệm AAR/DEX biên dịch của Android Studio.",
            Selected = true
        });

        return list;
    }

    public static List<DevAiCacheItem> ScanAll(CancellationToken token, IProgress<string> progress) {
        var targets = GetKnownTargets();
        var results = new List<DevAiCacheItem>();

        for (int i = 0; i < targets.Count; i++) {
            token.ThrowIfCancellationRequested();
            var item = targets[i];
            if (!Directory.Exists(item.Path)) continue;

            item.Exists = true;
            if (progress != null) progress.Report("Đang kiểm tra: " + item.ToolName + "…");

            long totalBytes = 0;
            int fileCount = 0;
            try {
                var stack = new Stack<string>();
                stack.Push(item.Path);
                while (stack.Count > 0) {
                    token.ThrowIfCancellationRequested();
                    string cur = stack.Pop();
                    try {
                        if ((File.GetAttributes(cur) & FileAttributes.ReparsePoint) != 0) continue;
                        foreach (var f in Directory.EnumerateFiles(cur)) {
                            token.ThrowIfCancellationRequested();
                            try {
                                totalBytes += new FileInfo(f).Length;
                                fileCount++;
                            } catch {}
                        }
                        foreach (var sub in Directory.EnumerateDirectories(cur)) {
                            stack.Push(sub);
                        }
                    } catch {}
                }
            } catch (OperationCanceledException) {
                throw;
            } catch {}

            item.Bytes = totalBytes;
            item.FileCount = fileCount;
            if (fileCount > 0) {
                results.Add(item);
            }
        }
        return results;
    }

    static void ClearReadOnlyAttributes(string dirPath) {
        try {
            var stack = new Stack<string>();
            stack.Push(dirPath);
            while (stack.Count > 0) {
                string cur = stack.Pop();
                try {
                    if ((File.GetAttributes(cur) & FileAttributes.ReparsePoint) != 0) continue;
                    foreach (var f in Directory.GetFiles(cur)) {
                        try {
                            var attr = File.GetAttributes(f);
                            if ((attr & FileAttributes.ReadOnly) != 0) {
                                File.SetAttributes(f, attr & ~FileAttributes.ReadOnly);
                            }
                        } catch {}
                    }
                    foreach (var d in Directory.GetDirectories(cur)) stack.Push(d);
                } catch {}
            }
        } catch {}
    }

    public static bool Clean(DevAiCacheItem item, bool sendToRecycleBin) {
        if (item == null || string.IsNullOrEmpty(item.Path) || !Directory.Exists(item.Path)) return false;

        string full = Path.GetFullPath(item.Path).TrimEnd('\\');
        string win = Environment.GetFolderPath(Environment.SpecialFolder.Windows).TrimEnd('\\');
        string sys = Environment.SystemDirectory.TrimEnd('\\');
        if (full.StartsWith(win, StringComparison.OrdinalIgnoreCase) ||
            full.StartsWith(sys, StringComparison.OrdinalIgnoreCase) ||
            !Features.NoLinks(full)) {
            throw new InvalidOperationException("Khu vực hệ thống được bảo vệ.");
        }

        // Go modules cache đặt cờ ReadOnly trên tệp, cần gỡ cờ để xóa không lỗi
        ClearReadOnlyAttributes(full);

        try {
            if (sendToRecycleBin) {
                FileSystem.DeleteDirectory(full, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
            } else {
                Directory.Delete(full, true);
            }
            return true;
        } catch {
            return false;
        }
    }
}
