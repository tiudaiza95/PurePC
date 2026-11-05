using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;

public class ContextMenuItem {
    public string Name { get; set; }
    public string Location { get; set; }
    public string Clsid { get; set; }
    public string ParentKeyPath { get; set; }
    public string SubKeyName { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsSystemProtected { get; set; }
    public bool Selected { get; set; }

    public string StatusLabel {
        get {
            if (IsSystemProtected) return "Bảo vệ hệ thống";
            return IsEnabled ? "Đang bật" : "Đã tắt";
        }
    }
}

public static class ContextMenuManager {
    static readonly string[] ProtectedNames = new[] {
        "Open With", "Open With EncryptionMenu", "Sharing", "Workspaces",
        "ModernSharing", "EPP", "WindowsDefender"
    };

    public static List<ContextMenuItem> Scan() {
        var list = new List<ContextMenuItem>();

        var targets = new[] {
            new { Path = @"*\shellex\ContextMenuHandlers", Label = "Tệp tin (*)" },
            new { Path = @"Directory\shellex\ContextMenuHandlers", Label = "Thư mục (Directory)" },
            new { Path = @"Directory\Background\shellex\ContextMenuHandlers", Label = "Nền Desktop / Folder" },
            new { Path = @"Drive\shellex\ContextMenuHandlers", Label = "Ổ đĩa (Drive)" }
        };

        for (int i = 0; i < targets.Length; i++) {
            var t = targets[i];
            try {
                using (var k = Registry.ClassesRoot.OpenSubKey(t.Path)) {
                    if (k == null) continue;
                    string[] subNames = k.GetSubKeyNames();
                    for (int j = 0; j < subNames.Length; j++) {
                        string sub = subNames[j];
                        try {
                            using (var sk = k.OpenSubKey(sub)) {
                                if (sk == null) continue;
                                string clsid = Convert.ToString(sk.GetValue(""));
                                bool disabled = sub.StartsWith("-");
                                string cleanName = disabled ? sub.Substring(1) : sub;

                                bool isProtected = false;
                                for (int p = 0; p < ProtectedNames.Length; p++) {
                                    if (cleanName.Equals(ProtectedNames[p], StringComparison.OrdinalIgnoreCase)) {
                                        isProtected = true;
                                        break;
                                    }
                                }

                                var item = new ContextMenuItem();
                                item.Name = cleanName;
                                item.Location = t.Label;
                                item.Clsid = clsid;
                                item.ParentKeyPath = t.Path;
                                item.SubKeyName = sub;
                                item.IsEnabled = !disabled;
                                item.IsSystemProtected = isProtected;
                                item.Selected = false;
                                list.Add(item);
                            }
                        } catch {}
                    }
                }
            } catch {}
        }

        return list;
    }

    public static bool Toggle(ContextMenuItem item, out string error) {
        error = null;
        if (item == null || item.IsSystemProtected) {
            error = "Mục hệ thống được bảo vệ.";
            return false;
        }

        try {
            using (var parent = Registry.ClassesRoot.OpenSubKey(item.ParentKeyPath, true)) {
                if (parent == null) {
                    error = "Không mở được khóa registry cha.";
                    return false;
                }

                string oldName = item.SubKeyName;
                string newName = item.IsEnabled ? ("-" + item.Name) : item.Name;

                // Rename subkey by copying value and deleting old
                using (var oldKey = parent.OpenSubKey(oldName)) {
                    if (oldKey == null) {
                        error = "Khóa nguồn không tồn tại.";
                        return false;
                    }
                    object defaultVal = oldKey.GetValue("");
                    using (var newKey = parent.CreateSubKey(newName)) {
                        if (defaultVal != null) newKey.SetValue("", defaultVal);
                    }
                }
                parent.DeleteSubKeyTree(oldName, false);

                item.SubKeyName = newName;
                item.IsEnabled = !item.IsEnabled;
                return true;
            }
        } catch (Exception ex) {
            error = ex.Message;
            return false;
        }
    }
}
