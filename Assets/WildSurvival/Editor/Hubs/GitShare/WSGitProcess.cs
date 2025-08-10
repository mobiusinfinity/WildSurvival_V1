// Assets/_Project/Code/Editor/Git/WSGitProcess.cs
// Minimal, robust git runner + helpers (Editor-only). No external deps.

#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace WildSurvival.EditorTools
{
    internal static class WSGitEnv
    {
        const string PREF_GIT_PATH = "WS_GIT_PATH";
        public static string ProjectRoot { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..")); } }
        public static string BuildsRoot { get { return Path.Combine(ProjectRoot, "Builds"); } }
        public static string MirrorRoot { get { return Path.Combine(BuildsRoot, "PublicMirror"); } }

        public static string GitPath
        {
            get { return EditorPrefs.GetString(PREF_GIT_PATH, "git"); }
            set { EditorPrefs.SetString(PREF_GIT_PATH, value); }
        }

        public static bool VerifyGit(out string version, out string error)
        {
            if (!TryVersion(out version, out error))
            {
#if UNITY_EDITOR_WIN
                string[] paths = {
            @"C:\Program Files\Git\bin\git.exe",
            @"C:\Program Files\Git\cmd\git.exe"
        };
#elif UNITY_EDITOR_OSX
        string[] paths = { "/opt/homebrew/bin/git", "/usr/local/bin/git", "/usr/bin/git" };
#else
        string[] paths = { "/usr/bin/git", "/usr/local/bin/git" };
#endif
                foreach (var p in paths)
                {
                    GitPath = p;
                    if (TryVersion(out version, out error))
                        return true;
                }
                return false;
            }
            return true;
        }
        static bool TryVersion(out string version, out string error)
        {
            try
            {
                var r = WSGit.RunBlocking("--version", 5000);
                if (r.ExitCode == 0)
                { version = r.StdOut.Trim(); error = null; return true; }
                version = null;
                error = r.StdErr;
                return false;
            }
            catch (Exception ex) { version = null; error = ex.Message; return false; }
        }

    }

    internal class GitResult
    {
        public int ExitCode;
        public string StdOut;
        public string StdErr;
        public string CommandLine;
        public TimeSpan Duration;
        public override string ToString()
        {
            return "$ git " + CommandLine + "\n(exit " + ExitCode + ")\n\n" + StdOut + (string.IsNullOrEmpty(StdErr) ? "" : ("\n[stderr]\n" + StdErr));
        }
    }

    internal static class WSGit
    {
        public static GitResult RunBlocking(string args, int timeoutMs = 0)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using (var p = new Process())
            {
                p.StartInfo.FileName = WSGitEnv.GitPath;
                p.StartInfo.Arguments = args;
                p.StartInfo.WorkingDirectory = WSGitEnv.ProjectRoot;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;

                var so = new StringBuilder();
                var se = new StringBuilder();
                p.OutputDataReceived += (s, e) => { if (e.Data != null) so.AppendLine(e.Data); };
                p.ErrorDataReceived += (s, e) => { if (e.Data != null) se.AppendLine(e.Data); };

                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();

                if (timeoutMs > 0)
                {
                    if (!p.WaitForExit(timeoutMs))
                    {
                        try
                        { p.Kill(); }
                        catch { }
                        return new GitResult { ExitCode = -1, StdOut = so.ToString(), StdErr = "Timeout after " + timeoutMs + " ms", CommandLine = args, Duration = sw.Elapsed };
                    }
                }
                else
                {
                    p.WaitForExit();
                }

                return new GitResult { ExitCode = p.ExitCode, StdOut = so.ToString(), StdErr = se.ToString(), CommandLine = args, Duration = sw.Elapsed };
            }
        }

        public static void RunAsync(string args, Action<GitResult> onDone)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                GitResult r;
                try
                { r = RunBlocking(args, 0); }
                catch (Exception ex)
                {
                    r = new GitResult { ExitCode = -1, StdOut = "", StdErr = ex.Message, CommandLine = args, Duration = TimeSpan.Zero };
                }
                EditorApplication.delayCall += () => { if (onDone != null) onDone(r); };
            });
        }

        public static string Quote(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "\"\"";
            if (s.IndexOf(' ') >= 0 || s.IndexOf('"') >= 0)
                return "\"" + s.Replace("\"", "\\\"") + "\"";
            return s;
        }
    }

    internal static class WSGitParse
    {
        public static string CurrentBranch(string statusShort)
        {
            // status -sb prints first line like: ## main...origin/main
            using (var sr = new StringReader(statusShort ?? ""))
            {
                string first = sr.ReadLine();
                if (first != null && first.StartsWith("## "))
                {
                    // strip '## '
                    string rest = first.Substring(3).Trim();
                    int dots = rest.IndexOf("...");
                    return dots > 0 ? rest.Substring(0, dots) : rest;
                }
            }
            return "(detached)";
        }

        public static List<string> BranchesLocal(string list)
        {
            var arr = new List<string>();
            using (var sr = new StringReader(list ?? ""))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.StartsWith("*"))
                        line = line.Substring(1).Trim();
                    if (line.Length > 0)
                        arr.Add(line);
                }
            }
            return arr;
        }

        public static Dictionary<string, string> Remotes(string remoteV)
        {
            // git remote -v → lines like: origin https://... (fetch)
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var sr = new StringReader(remoteV ?? ""))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        string name = parts[0];
                        string url = parts[1];
                        if (!map.ContainsKey(name))
                            map[name] = url;
                    }
                }
            }
            return map;
        }
    }
}
#endif
