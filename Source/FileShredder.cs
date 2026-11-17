using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

public static class FileShredder {
    public static bool IsProtected(string path) {
        if (string.IsNullOrWhiteSpace(path)) return true;
        try {
            string full = Path.GetFullPath(path).TrimEnd('\\');
            if (full.StartsWith(@"\\")) return true;

            string root = Path.GetPathRoot(full);
            if (!string.IsNullOrEmpty(root) && root.TrimEnd('\\').Equals(full, StringComparison.OrdinalIgnoreCase)) return true;

            // Khu vực hệ thống không bao giờ được phép hủy (kể cả tệp/thư mục con bên trong)
            string[] forbiddenTrees = {
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                Environment.SystemDirectory,
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                Engine.Data,
                AppDomain.CurrentDomain.BaseDirectory
            };

            for (int i = 0; i < forbiddenTrees.Length; i++) {
                string tree = (forbiddenTrees[i] ?? "").TrimEnd('\\');
                if (string.IsNullOrEmpty(tree)) continue;
                if (full.Equals(tree, StringComparison.OrdinalIgnoreCase) ||
                    full.StartsWith(tree + "\\", StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }
            }

            // Các thư mục gốc của người dùng (chặn hủy cả thư mục gốc, vẫn cho phép chọn từng tệp cụ thể bên trong)
            string[] protectedExactRoots = {
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)) // C:\Users
            };

            for (int i = 0; i < protectedExactRoots.Length; i++) {
                string exact = (protectedExactRoots[i] ?? "").TrimEnd('\\');
                if (!string.IsNullOrEmpty(exact) && full.Equals(exact, StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }
            }

            return false;
        } catch {
            return true;
        }
    }

    public static void ShredFile(string path, int passes = 3) {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
        if (IsProtected(path)) throw new InvalidOperationException("Không được phép hủy tệp thuộc khu vực được bảo vệ: " + path);
        if (!Features.NoLinks(path)) throw new InvalidOperationException("Không hủy tệp đi qua liên kết tượng trưng (Symlink/Junction): " + path);

        var fi = new FileInfo(path);
        if ((fi.Attributes & FileAttributes.ReparsePoint) != 0) return;

        File.SetAttributes(path, FileAttributes.Normal);
        long length = fi.Length;
        if (passes < 1) passes = 1;

        using (var rng = new RNGCryptoServiceProvider())
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None)) {
            byte[] buffer = new byte[65536];
            for (int pass = 1; pass <= passes; pass++) {
                stream.Position = 0;
                long written = 0;
                byte fillByte = 0;
                bool isRandom = false;

                if (pass == 1) fillByte = 0x00;
                else if (pass == 2) fillByte = 0xFF;
                else isRandom = true;

                if (!isRandom) {
                    int fillLen = (int)Math.Min(buffer.Length, length);
                    for (int i = 0; i < fillLen; i++) buffer[i] = fillByte;
                }

                while (written < length) {
                    int toWrite = (int)Math.Min(buffer.Length, length - written);
                    if (isRandom) {
                        rng.GetBytes(buffer);
                    }
                    stream.Write(buffer, 0, toWrite);
                    written += toWrite;
                }
                stream.Flush(true);
            }
            stream.SetLength(0);
            stream.Flush(true);
        }

        string dir = Path.GetDirectoryName(path);
        string tempName = Path.Combine(dir, Guid.NewGuid().ToString("N") + ".tmp");
        try {
            File.Move(path, tempName);
            File.Delete(tempName);
        } catch {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    public static void ShredDirectory(string dirPath, int passes = 3) {
        if (string.IsNullOrEmpty(dirPath) || !Directory.Exists(dirPath)) return;
        if (IsProtected(dirPath)) throw new InvalidOperationException("Không được phép hủy thư mục thuộc khu vực được bảo vệ: " + dirPath);
        if (!Features.NoLinks(dirPath)) throw new InvalidOperationException("Không hủy thư mục đi qua liên kết (Symlink/Junction): " + dirPath);

        var allDirs = new List<string>();
        var stack = new Stack<string>();
        stack.Push(Path.GetFullPath(dirPath));

        while (stack.Count > 0) {
            string cur = stack.Pop();
            try {
                var attr = File.GetAttributes(cur);
                if ((attr & FileAttributes.ReparsePoint) != 0) continue;
                allDirs.Add(cur);

                foreach (var file in Directory.GetFiles(cur)) {
                    try { ShredFile(file, passes); } catch {}
                }
                foreach (var sub in Directory.GetDirectories(cur)) {
                    try {
                        if ((File.GetAttributes(sub) & FileAttributes.ReparsePoint) == 0) {
                            stack.Push(sub);
                        }
                    } catch {}
                }
            } catch {}
        }

        // Xóa thư mục từ sâu nhất ngược lên gốc
        for (int i = allDirs.Count - 1; i >= 0; i--) {
            try {
                string d = allDirs[i];
                if (Directory.Exists(d)) {
                    File.SetAttributes(d, FileAttributes.Directory);
                    Directory.Delete(d, false);
                }
            } catch {}
        }
    }
}
