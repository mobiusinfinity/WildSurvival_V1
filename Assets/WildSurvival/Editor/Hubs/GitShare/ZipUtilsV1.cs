using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using UnityEngine;

namespace WildSurvival.Editor.Collab
{
    internal static class ZipUtilsV1
    {
        public static string NormalizeRel(string rel)
        {
            rel = rel.Replace('\\','/');
            while (rel.StartsWith("./")) rel = rel.Substring(2);
            var parts = new List<string>();
            foreach (var p in rel.Split('/'))
            {
                if (p == "" || p == ".") continue;
                if (p == "..")
                {
                    if (parts.Count > 0) parts.RemoveAt(parts.Count - 1);
                    continue;
                }
                parts.Add(p);
            }
            return string.Join("/", parts);
        }

        public static bool HasAssetsRoot(ZipArchive zip)
        {
            foreach (var e in zip.Entries)
            {
                var n = NormalizeRel(e.FullName);
                if (n.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        public static IEnumerable<string> ListEntries(ZipArchive zip)
        {
            foreach (var e in zip.Entries)
                yield return NormalizeRel(e.FullName);
        }

        public static bool MightBeDirectory(ZipArchiveEntry entry, string normalizedRel)
        {
            // Robust directory detection:
            // 1) directory entries usually have empty Name
            // 2) some zips omit trailing slash; skip common folder roots like "Assets" and "ProjectSettings"
            // 3) also treat entries with length==0 and existing destination directory as directories (fallback)
            if (string.IsNullOrEmpty(entry.Name)) return true;
            if (normalizedRel.EndsWith("/")) return true;
            if (normalizedRel.Equals("Assets", StringComparison.OrdinalIgnoreCase)) return true;
            if (normalizedRel.Equals("ProjectSettings", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public static void ExtractEntryTo(string destBase, string rel, ZipArchiveEntry entry)
        {
            if (string.IsNullOrEmpty(rel) || rel.EndsWith("/")) return; // directory
            string full = Path.GetFullPath(Path.Combine(destBase, rel));
            string baseFull = Path.GetFullPath(destBase) + Path.DirectorySeparatorChar;
            if (!full.StartsWith(baseFull, StringComparison.OrdinalIgnoreCase))
            {
                UnityEngine.Debug.LogWarning("[ZipIntake] Zip-slip attempt blocked: " + rel);
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            using (var src = entry.Open())
            using (var dst = new FileStream(full, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                src.CopyTo(dst);
            }
        }

        public static bool IsCompilable(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return ext == ".cs" || ext == ".asmdef" || ext == ".asmref";
        }

        public static void QuarantineCompilableUnder(string folder, List<string> renamedOut)
        {
            if (!Directory.Exists(folder)) return;
            foreach (var f in Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories))
            {
                if (IsCompilable(f))
                {
                    var target = f + ".txt";
                    if (!File.Exists(target))
                    {
                        File.Move(f, target);
                        renamedOut?.Add(Path.GetRelativePath(folder, f).Replace("\\","/"));
                    }
                }
            }
        }
    }
}
