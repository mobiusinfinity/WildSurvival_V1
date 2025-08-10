using UnityEditor;
using UnityEngine;
using System.IO;
using System;

namespace WildSurvival.Editor.Git
{
    public class GitReleaseHelperV1 : EditorWindow
    {
        [MenuItem("WildSurvival/Tools (V3)/Git/Create GitHub Release (V1)")]
        public static void Open() => GetWindow<GitReleaseHelperV1>("GitHub Release");

        string _tagName = "";
        string _releaseTitle = "";
        string _notes = "Playtest release";
        string _artifactPath = "";
        bool _openBrowser = true;

        void OnGUI()
        {
            EditorGUILayout.HelpBox("Creates a tag, pushes it, and opens GitHub Releases prefilled. Attach your build zip manually.", MessageType.Info);
            if (string.IsNullOrEmpty(_tagName)) _tagName = $"playtest-{DateTime.Now:yyyyMMdd-HHmm}";

            _tagName = EditorGUILayout.TextField("Tag", _tagName);
            _releaseTitle = EditorGUILayout.TextField("Title", string.IsNullOrEmpty(_releaseTitle) ? _tagName : _releaseTitle);
            _notes = EditorGUILayout.TextArea(_notes, GUILayout.MinHeight(60));
            _artifactPath = EditorGUILayout.TextField("Artifact (zip optional)", _artifactPath);
            _openBrowser = EditorGUILayout.ToggleLeft("Open release page in browser", _openBrowser);

            if (GUILayout.Button("Create Tag (+ Push)"))
            {
                if (!GitUtilsV1.IsGitAvailable(out var v)) { EditorUtility.DisplayDialog("Git", "git not found on PATH.", "OK"); return; }
                if (!GitUtilsV1.RunGit(Directory.GetCurrentDirectory(), $"tag {_tagName}", out var o1, out var e1))
                    UnityEngine.Debug.LogError("git tag error: " + e1);
                if (!GitUtilsV1.RunGit(Directory.GetCurrentDirectory(), $"push origin {_tagName}", out var o2, out var e2))
                    UnityEngine.Debug.LogError("git push tag error: " + e2);
                else UnityEngine.Debug.Log("[GitRelease] Pushed tag " + _tagName);
            }

            if (GUILayout.Button("Open GitHub Release Page"))
            {
                if (_openBrowser)
                {
                    string repoUrl = "";
                    if (GitUtilsV1.RunGit(Directory.GetCurrentDirectory(), "remote get-url origin", out var url, out var err))
                        repoUrl = (url ?? string.Empty).Trim();

                    // Normalize GitHub URL to https form
                    string page = NormalizeGitHubRemote(repoUrl);
                    if (!page.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        page = "https://github.com";

                    string body = Uri.EscapeDataString(_notes ?? "");
                    string title = Uri.EscapeDataString(string.IsNullOrEmpty(_releaseTitle) ? _tagName : _releaseTitle);
                    string href = $"{page}/releases/new?tag={Uri.EscapeDataString(_tagName)}&title={title}&body={body}";
                    Application.OpenURL(href);
                }
            }

            if (GUILayout.Button("Reveal Artifact"))
            {
                if (!string.IsNullOrEmpty(_artifactPath) && File.Exists(_artifactPath))
                    EditorUtility.RevealInFinder(_artifactPath);
                else
                    EditorUtility.DisplayDialog("Artifact", "No artifact found.", "OK");
            }
        }

        static string NormalizeGitHubRemote(string remote)
        {
            if (string.IsNullOrEmpty(remote)) return "";
            var url = remote.Trim();

            // Convert common SSH-like forms to https
            url = url.Replace("git@github.com:", "https://github.com/");
            url = url.Replace("ssh://git@github.com/", "https://github.com/");
            url = url.Replace("git://", "https://");

            // Strip trailing .git
            if (url.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
                url = url.Substring(0, url.Length - 4);

            return url;
        }
    }
}
