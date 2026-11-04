using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

public static class FileShredder {
    public static bool IsProtected(string path) {
        if (string.IsNullOrEmpty(path)) return true;
        try {
            string full = Path.GetFullPath(path).TrimEnd('\\');
            string win = Environment.GetFolderPath(Environment.SpecialFolder.Windows).TrimEnd('\\');
            string sys = Environment.SystemDirectory.TrimEnd('\\');
            string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles).TrimEnd('\\');
            string pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86).TrimEnd('\\');

            if (full.Equals(win, StringComparison.OrdinalIgnoreCase) || full.StartsWith(win + "\\", StringComparison.OrdinalIgnoreCase)) return true;
            if (full.Equals(sys, StringComparison.OrdinalIgnoreCase) || full.StartsWith(sys + "\\", StringComparison.OrdinalIgnoreCase)) return true;
            if (full.Equals(pf, StringComparison.OrdinalIgnoreCase) || full.StartsWith(pf + "\\", StringComparison.OrdinalIgnoreCase)) return true;
            if (!string.IsNullOrEmpty(pf86) && (full.Equals(pf86, StringComparison.OrdinalIgnoreCase) || full.StartsWith(pf86 + "\\", StringComparison.OrdinalIgnoreCase))) return true;

            if (Path.GetPathRoot(full).Equals(full, StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        } catch {
            return true;
        }
    }

    public static void ShredFile(string path, int passes = 3) {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
        if (IsProtected(path)) throw new InvalidOperationException("Không được phép hủy tệp thuộc khu vực hệ thống Windows: " + path);

        var fi = new FileInfo(path);
        File.SetAttributes(path, FileAttributes.Normal);
        long length = fi.Length;

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

                while (written < length) {
                    int toWrite = (int)Math.Min(buffer.Length, length - written);
                    if (isRandom) {
                        rng.GetBytes(buffer);
                    } else {
                        for (int i = 0; i < toWrite; i++) buffer[i] = fillByte;
                    }
                    stream.Write(buffer, 0, toWrite);
                    written += toWrite;
                }
                stream.Flush();
            }
            stream.SetLength(0);
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
        if (IsProtected(dirPath)) throw new InvalidOperationException("Không được phép hủy thư mục thuộc khu vực hệ thống Windows: " + dirPath);

        foreach (var file in Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories)) {
            try { ShredFile(file, passes); } catch {}
        }
        foreach (var sub in Directory.GetDirectories(dirPath, "*", SearchOption.AllDirectories)) {
            try { Directory.Delete(sub, true); } catch {}
        }
        try { Directory.Delete(dirPath, true); } catch {}
    }
}
