using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

public static class SqliteOptimizer {
    [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_open16", CallingConvention = CallingConvention.Cdecl)]
    static extern int sqlite3_open16([MarshalAs(UnmanagedType.LPWStr)] string filename, out IntPtr db);

    [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_exec", CallingConvention = CallingConvention.Cdecl)]
    static extern int sqlite3_exec(IntPtr db, [MarshalAs(UnmanagedType.LPStr)] string sql, IntPtr callback, IntPtr arg, out IntPtr errmsg);

    [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_close", CallingConvention = CallingConvention.Cdecl)]
    static extern int sqlite3_close(IntPtr db);

    public static bool IsAvailable {
        get {
            try {
                return File.Exists(Path.Combine(Environment.SystemDirectory, "winsqlite3.dll"));
            } catch {
                return false;
            }
        }
    }

    public static bool IsSqliteDatabase(string path) {
        if (!File.Exists(path)) return false;
        try {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                if (fs.Length < 16) return false;
                byte[] header = new byte[16];
                fs.Read(header, 0, 16);
                string str = Encoding.ASCII.GetString(header);
                return str.StartsWith("SQLite format 3");
            }
        } catch {
            return false;
        }
    }

    public static bool Vacuum(string path, out long bytesFreed) {
        bytesFreed = 0;
        if (!IsAvailable || !File.Exists(path)) return false;
        try {
            long sizeBefore = new FileInfo(path).Length;
            IntPtr db;
            int rc = sqlite3_open16(path, out db);
            if (rc != 0) return false;
            try {
                IntPtr errmsg;
                int execRc = sqlite3_exec(db, "VACUUM; REINDEX;", IntPtr.Zero, IntPtr.Zero, out errmsg);
                sqlite3_close(db);
                if (execRc == 0) {
                    long sizeAfter = new FileInfo(path).Length;
                    if (sizeBefore > sizeAfter) {
                        bytesFreed = sizeBefore - sizeAfter;
                    }
                    return true;
                }
                return false;
            } finally {
                try { sqlite3_close(db); } catch {}
            }
        } catch {
            return false;
        }
    }

    public static List<string> FindBrowserDatabases() {
        var result = new List<string>();
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        var chromiumDirs = new[] {
            Path.Combine(local, @"Google\Chrome\User Data"),
            Path.Combine(local, @"Microsoft\Edge\User Data"),
            Path.Combine(local, @"CocCoc\Browser\User Data"),
            Path.Combine(local, @"BraveSoftware\Brave-Browser\User Data"),
            Path.Combine(local, @"Opera Software\Opera Stable"),
            Path.Combine(local, @"Opera Software\Opera GX Stable")
        };

        foreach (var baseDir in chromiumDirs) {
            if (!Directory.Exists(baseDir)) continue;
            try {
                var searchDirs = new List<string> { baseDir };
                foreach (var sub in Directory.GetDirectories(baseDir)) {
                    string name = Path.GetFileName(sub);
                    if (name.Equals("Default", StringComparison.OrdinalIgnoreCase) || name.StartsWith("Profile ", StringComparison.OrdinalIgnoreCase)) {
                        searchDirs.Add(sub);
                    }
                }

                foreach (var dir in searchDirs) {
                    if (!Directory.Exists(dir)) continue;
                    try {
                        foreach (var file in Directory.GetFiles(dir)) {
                            string fname = Path.GetFileName(file);
                            if (fname.Equals("History", StringComparison.OrdinalIgnoreCase) ||
                                fname.Equals("Cookies", StringComparison.OrdinalIgnoreCase) ||
                                fname.Equals("Web Data", StringComparison.OrdinalIgnoreCase) ||
                                fname.Equals("Favicons", StringComparison.OrdinalIgnoreCase) ||
                                fname.Equals("Top Sites", StringComparison.OrdinalIgnoreCase) ||
                                fname.Equals("Shortcuts", StringComparison.OrdinalIgnoreCase) ||
                                fname.Equals("Network Action Predictor", StringComparison.OrdinalIgnoreCase) ||
                                fname.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase) ||
                                fname.EndsWith(".db", StringComparison.OrdinalIgnoreCase)) {
                                if (IsSqliteDatabase(file)) {
                                    result.Add(file);
                                }
                            }
                        }
                    } catch {}
                }
            } catch {}
        }

        string ffProfiles = Path.Combine(appData, @"Mozilla\Firefox\Profiles");
        if (Directory.Exists(ffProfiles)) {
            try {
                foreach (var profile in Directory.GetDirectories(ffProfiles)) {
                    try {
                        foreach (var file in Directory.GetFiles(profile, "*.sqlite")) {
                            if (IsSqliteDatabase(file)) {
                                result.Add(file);
                            }
                        }
                    } catch {}
                }
            } catch {}
        }

        return result;
    }

    public static int OptimizeAll(out int successCount, out long totalFreed) {
        successCount = 0;
        totalFreed = 0;
        var list = FindBrowserDatabases();
        foreach (var path in list) {
            long freed;
            if (Vacuum(path, out freed)) {
                successCount++;
                totalFreed += freed;
            }
        }
        return list.Count;
    }
}
