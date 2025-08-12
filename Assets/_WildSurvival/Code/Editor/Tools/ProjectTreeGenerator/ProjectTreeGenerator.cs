using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;

// namespace removed by Menu Fixer - check closing brace

// 
namespace WildSurvival.Editor.Tools
{
    public class ProjectTreeGenerator : EditorWindow
    {
        // Configuration
        private string rootPath = "Assets";
        private bool includeMetaFiles = false;
        private bool includeEmptyFolders = true;
        private bool includeFileSize = true;
        private bool includeFileCount = true;
        private bool useIcons = true;
        private bool copyToClipboard = true;
        private bool saveToFile = true;
        private string outputFileName = "PROJECT_TREE.txt";

        // Filters
        private bool filterEnabled = true;
        private List<string> includedExtensions = new List<string> { ".cs", ".prefab", ".asset", ".unity", ".mat", ".shader" };
        private List<string> excludedFolders = new List<string> { "Library", "Temp", "Logs", "obj", ".vs", ".git" };
        private int maxDepth = 10;

        // Output formats
        private enum OutputFormat { Text, Markdown, JSON, HTML, XML }
        private OutputFormat outputFormat = OutputFormat.Text;

        // Statistics
        private int totalFiles = 0;
        private int totalFolders = 0;
        private long totalSize = 0;
        private Dictionary<string, int> extensionCount = new Dictionary<string, int>();

        // UI
        private Vector2 scrollPosition;
        private string generatedTree = "";
        private bool showPreview = false;

        [MenuItem("Tools/Wild Survival/Project Tree Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<ProjectTreeGenerator>("Project Tree Generator");
            window.minSize = new Vector2(600, 500);
            window.Show();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawHeader();
            DrawConfiguration();
            DrawFilters();
            DrawActions();

            if (showPreview && !string.IsNullOrEmpty(generatedTree))
            {
                DrawPreview();
            }

            DrawStatistics();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Project Tree Generator", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Reset", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                ResetSettings();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
        }

        private void DrawConfiguration()
        {
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            rootPath = EditorGUILayout.TextField("Root Path", rootPath);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select Folder", GUILayout.Width(100)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Root Folder", rootPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    // Convert to relative path
                    if (path.StartsWith(Application.dataPath))
                    {
                        rootPath = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                }
            }

            if (GUILayout.Button("Use Selected", GUILayout.Width(100)))
            {
                if (Selection.activeObject != null)
                {
                    string path = AssetDatabase.GetAssetPath(Selection.activeObject);
                    if (AssetDatabase.IsValidFolder(path))
                    {
                        rootPath = path;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            outputFormat = (OutputFormat)EditorGUILayout.EnumPopup("Output Format", outputFormat);

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Options", EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            includeMetaFiles = EditorGUILayout.Toggle("Include .meta Files", includeMetaFiles);
            includeEmptyFolders = EditorGUILayout.Toggle("Include Empty Folders", includeEmptyFolders);
            includeFileSize = EditorGUILayout.Toggle("Show File Sizes", includeFileSize);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            includeFileCount = EditorGUILayout.Toggle("Show File Count", includeFileCount);
            useIcons = EditorGUILayout.Toggle("Use Icons", useIcons);
            copyToClipboard = EditorGUILayout.Toggle("Copy to Clipboard", copyToClipboard);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            saveToFile = EditorGUILayout.Toggle("Save to File", saveToFile);
            if (saveToFile)
            {
                EditorGUI.indentLevel++;
                outputFileName = EditorGUILayout.TextField("Output File Name", outputFileName);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawFilters()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Filters", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            filterEnabled = EditorGUILayout.Toggle("Enable Filters", filterEnabled);

            if (filterEnabled)
            {
                EditorGUI.indentLevel++;

                maxDepth = EditorGUILayout.IntSlider("Max Depth", maxDepth, 1, 20);

                // Extension filter
                EditorGUILayout.LabelField("Included Extensions", EditorStyles.miniLabel);
                DrawStringList(includedExtensions, "Extension");

                EditorGUILayout.Space(5);

                // Excluded folders
                EditorGUILayout.LabelField("Excluded Folders", EditorStyles.miniLabel);
                DrawStringList(excludedFolders, "Folder");

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawStringList(List<string> list, string label)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+", GUILayout.Width(20)))
            {
                list.Add("");
            }
            GUILayout.Label(label + "s", GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < list.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                list[i] = EditorGUILayout.TextField(list[i]);
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    list.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawActions()
        {
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Generate Tree", GUILayout.Height(35)))
            {
                GenerateTree();
            }
            GUI.backgroundColor = Color.white;

            if (!string.IsNullOrEmpty(generatedTree))
            {
                GUI.backgroundColor = new Color(0.5f, 0.5f, 1f);
                if (GUILayout.Button(showPreview ? "Hide Preview" : "Show Preview", GUILayout.Height(35)))
                {
                    showPreview = !showPreview;
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPreview()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Create a scroll view for the preview
            var style = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true,
                font = Font.CreateDynamicFontFromOSFont("Courier New", 10)
            };

            EditorGUILayout.TextArea(generatedTree, style, GUILayout.Height(300));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Copy to Clipboard"))
            {
                EditorGUIUtility.systemCopyBuffer = generatedTree;
                EditorUtility.DisplayDialog("Success", "Tree copied to clipboard!", "OK");
            }

            if (GUILayout.Button("Save to File"))
            {
                SaveToFile();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawStatistics()
        {
            if (totalFiles == 0 && totalFolders == 0) return;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField($"Total Folders: {totalFolders:N0}");
            EditorGUILayout.LabelField($"Total Files: {totalFiles:N0}");
            EditorGUILayout.LabelField($"Total Size: {FormatFileSize(totalSize)}");

            if (extensionCount.Count > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("File Types:", EditorStyles.miniLabel);

                foreach (var kvp in extensionCount.OrderByDescending(x => x.Value))
                {
                    EditorGUILayout.LabelField($"  {kvp.Key}: {kvp.Value:N0}");
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void GenerateTree()
        {
            // Reset statistics
            totalFiles = 0;
            totalFolders = 0;
            totalSize = 0;
            extensionCount.Clear();

            // Generate based on format
            switch (outputFormat)
            {
                case OutputFormat.Text:
                    generatedTree = GenerateTextTree();
                    break;
                case OutputFormat.Markdown:
                    generatedTree = GenerateMarkdownTree();
                    break;
                case OutputFormat.JSON:
                    generatedTree = GenerateJSONTree();
                    break;
                case OutputFormat.HTML:
                    generatedTree = GenerateHTMLTree();
                    break;
                case OutputFormat.XML:
                    generatedTree = GenerateXMLTree();
                    break;
            }

            if (copyToClipboard)
            {
                EditorGUIUtility.systemCopyBuffer = generatedTree;
            }

            if (saveToFile)
            {
                SaveToFile();
            }

            showPreview = true;

            Debug.Log($"Project tree generated! Files: {totalFiles}, Folders: {totalFolders}");
        }

        private string GenerateTextTree()
        {
            var sb = new StringBuilder();

            // Header
            sb.AppendLine("PROJECT STRUCTURE");
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Root: {rootPath}");
            sb.AppendLine(new string('=', 50));
            sb.AppendLine();

            // Generate tree
            GenerateTextNode(sb, rootPath, "", true, 0);

            // Footer
            sb.AppendLine();
            sb.AppendLine(new string('=', 50));
            sb.AppendLine($"Total: {totalFolders} folders, {totalFiles} files");
            sb.AppendLine($"Size: {FormatFileSize(totalSize)}");

            return sb.ToString();
        }

        private void GenerateTextNode(StringBuilder sb, string path, string indent, bool isLast, int depth)
        {
            if (depth > maxDepth) return;

            string name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(name))
                name = path;

            // Check if excluded
            if (filterEnabled && excludedFolders.Contains(name))
                return;

            bool isDirectory = AssetDatabase.IsValidFolder(path);

            // Draw tree line
            if (depth > 0)
            {
                sb.Append(indent);
                sb.Append(isLast ? "└── " : "├── ");

                if (useIcons)
                {
                    sb.Append(isDirectory ? "📁 " : GetFileIcon(path));
                }

                sb.Append(name);

                if (!isDirectory && includeFileSize)
                {
                    FileInfo fi = new FileInfo(path);
                    if (fi.Exists)
                    {
                        sb.Append($" ({FormatFileSize(fi.Length)})");
                        totalSize += fi.Length;
                    }
                }

                if (isDirectory && includeFileCount)
                {
                    int fileCount = GetFileCount(path);
                    if (fileCount > 0)
                    {
                        sb.Append($" [{fileCount} files]");
                    }
                }

                sb.AppendLine();
            }
            else
            {
                sb.AppendLine(name);
            }

            if (isDirectory)
            {
                totalFolders++;

                // Get children
                var children = new List<string>();

                // Add subdirectories
                string[] subdirs = AssetDatabase.GetSubFolders(path);
                children.AddRange(subdirs);

                // Add files
                string[] guids = AssetDatabase.FindAssets("", new[] { path });
                foreach (string guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                    // Only include direct children
                    if (Path.GetDirectoryName(assetPath) == path.Replace('/', Path.DirectorySeparatorChar))
                    {
                        if (!AssetDatabase.IsValidFolder(assetPath))
                        {
                            if (ShouldIncludeFile(assetPath))
                            {
                                children.Add(assetPath);
                            }
                        }
                    }
                }

                // Sort children
                children.Sort((a, b) =>
                {
                    bool aIsDir = AssetDatabase.IsValidFolder(a);
                    bool bIsDir = AssetDatabase.IsValidFolder(b);

                    if (aIsDir && !bIsDir) return -1;
                    if (!aIsDir && bIsDir) return 1;

                    return string.Compare(Path.GetFileName(a), Path.GetFileName(b), StringComparison.OrdinalIgnoreCase);
                });

                // Process children
                for (int i = 0; i < children.Count; i++)
                {
                    string newIndent = indent;
                    if (depth > 0)
                    {
                        newIndent += isLast ? "    " : "│   ";
                    }

                    GenerateTextNode(sb, children[i], newIndent, i == children.Count - 1, depth + 1);
                }
            }
            else
            {
                totalFiles++;

                // Track extension
                string ext = Path.GetExtension(path).ToLower();
                if (!string.IsNullOrEmpty(ext))
                {
                    if (!extensionCount.ContainsKey(ext))
                        extensionCount[ext] = 0;
                    extensionCount[ext]++;
                }
            }
        }

        private string GenerateMarkdownTree()
        {
            var sb = new StringBuilder();

            sb.AppendLine("# Project Structure");
            sb.AppendLine();
            sb.AppendLine($"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}  ");
            sb.AppendLine($"**Root:** `{rootPath}`");
            sb.AppendLine();
            sb.AppendLine("```");
            sb.Append(GenerateTextTree());
            sb.AppendLine("```");

            // Add statistics table
            sb.AppendLine();
            sb.AppendLine("## Statistics");
            sb.AppendLine();
            sb.AppendLine("| Metric | Value |");
            sb.AppendLine("|--------|-------|");
            sb.AppendLine($"| Total Folders | {totalFolders:N0} |");
            sb.AppendLine($"| Total Files | {totalFiles:N0} |");
            sb.AppendLine($"| Total Size | {FormatFileSize(totalSize)} |");

            if (extensionCount.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("## File Types");
                sb.AppendLine();
                sb.AppendLine("| Extension | Count |");
                sb.AppendLine("|-----------|-------|");

                foreach (var kvp in extensionCount.OrderByDescending(x => x.Value))
                {
                    sb.AppendLine($"| {kvp.Key} | {kvp.Value:N0} |");
                }
            }

            return sb.ToString();
        }

        private string GenerateJSONTree()
        {
            var root = new JSONNode();
            GenerateJSONNode(root, rootPath, 0);

            var result = new Dictionary<string, object>
            {
                ["generated"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ["root"] = rootPath,
                ["tree"] = root,
                ["statistics"] = new Dictionary<string, object>
                {
                    ["totalFolders"] = totalFolders,
                    ["totalFiles"] = totalFiles,
                    ["totalSize"] = totalSize,
                    ["fileTypes"] = extensionCount
                }
            };

            return JsonUtility.ToJson(result, true);
        }

        private void GenerateJSONNode(JSONNode node, string path, int depth)
        {
            if (depth > maxDepth) return;

            node.name = Path.GetFileName(path);
            node.path = path;
            node.isDirectory = AssetDatabase.IsValidFolder(path);

            if (node.isDirectory)
            {
                totalFolders++;
                node.children = new List<JSONNode>();

                // Process children
                string[] subdirs = AssetDatabase.GetSubFolders(path);
                foreach (string subdir in subdirs)
                {
                    if (!ShouldExclude(subdir))
                    {
                        var child = new JSONNode();
                        GenerateJSONNode(child, subdir, depth + 1);
                        node.children.Add(child);
                    }
                }

                // Process files
                string[] guids = AssetDatabase.FindAssets("", new[] { path });
                foreach (string guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (Path.GetDirectoryName(assetPath) == path.Replace('/', Path.DirectorySeparatorChar))
                    {
                        if (!AssetDatabase.IsValidFolder(assetPath) && ShouldIncludeFile(assetPath))
                        {
                            var child = new JSONNode
                            {
                                name = Path.GetFileName(assetPath),
                                path = assetPath,
                                isDirectory = false
                            };

                            FileInfo fi = new FileInfo(assetPath);
                            if (fi.Exists)
                            {
                                child.size = fi.Length;
                                totalSize += fi.Length;
                            }

                            node.children.Add(child);
                            totalFiles++;
                        }
                    }
                }
            }
            else
            {
                FileInfo fi = new FileInfo(path);
                if (fi.Exists)
                {
                    node.size = fi.Length;
                    totalSize += fi.Length;
                }
                totalFiles++;
            }
        }

        private string GenerateHTMLTree()
        {
            var sb = new StringBuilder();

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("    <title>Project Structure</title>");
            sb.AppendLine("    <style>");
            sb.AppendLine("        body { font-family: 'Courier New', monospace; }");
            sb.AppendLine("        .folder { font-weight: bold; color: #0066cc; }");
            sb.AppendLine("        .file { color: #333; }");
            sb.AppendLine("        .size { color: #666; font-size: 0.9em; }");
            sb.AppendLine("        ul { list-style-type: none; }");
            sb.AppendLine("        li { margin: 2px 0; }");
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine($"    <h1>Project Structure</h1>");
            sb.AppendLine($"    <p>Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
            sb.AppendLine($"    <p>Root: {rootPath}</p>");
            sb.AppendLine("    <ul>");

            GenerateHTMLNode(sb, rootPath, 0);

            sb.AppendLine("    </ul>");
            sb.AppendLine($"    <hr>");
            sb.AppendLine($"    <p>Total: {totalFolders} folders, {totalFiles} files ({FormatFileSize(totalSize)})</p>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        private void GenerateHTMLNode(StringBuilder sb, string path, int depth)
        {
            if (depth > maxDepth) return;

            string name = Path.GetFileName(path);
            bool isDirectory = AssetDatabase.IsValidFolder(path);

            sb.Append(new string(' ', depth * 4));
            sb.Append("<li>");

            if (isDirectory)
            {
                sb.Append($"<span class='folder'>📁 {name}</span>");
                totalFolders++;

                // Get children
                var children = GetChildren(path);
                if (children.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine(new string(' ', depth * 4) + "<ul>");

                    foreach (string child in children)
                    {
                        GenerateHTMLNode(sb, child, depth + 1);
                    }

                    sb.Append(new string(' ', depth * 4));
                    sb.AppendLine("</ul>");
                }
            }
            else
            {
                sb.Append($"<span class='file'>{GetFileIcon(path)} {name}</span>");

                FileInfo fi = new FileInfo(path);
                if (fi.Exists && includeFileSize)
                {
                    sb.Append($" <span class='size'>({FormatFileSize(fi.Length)})</span>");
                    totalSize += fi.Length;
                }

                totalFiles++;
            }

            sb.AppendLine("</li>");
        }

        private string GenerateXMLTree()
        {
            var sb = new StringBuilder();

            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<project>");
            sb.AppendLine($"    <generated>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</generated>");
            sb.AppendLine($"    <root>{rootPath}</root>");
            sb.AppendLine("    <structure>");

            GenerateXMLNode(sb, rootPath, 2, 0);

            sb.AppendLine("    </structure>");
            sb.AppendLine("    <statistics>");
            sb.AppendLine($"        <folders>{totalFolders}</folders>");
            sb.AppendLine($"        <files>{totalFiles}</files>");
            sb.AppendLine($"        <size>{totalSize}</size>");
            sb.AppendLine("    </statistics>");
            sb.AppendLine("</project>");

            return sb.ToString();
        }

        private void GenerateXMLNode(StringBuilder sb, string path, int indent, int depth)
        {
            if (depth > maxDepth) return;

            string name = Path.GetFileName(path);
            bool isDirectory = AssetDatabase.IsValidFolder(path);

            string indentStr = new string(' ', indent * 4);

            if (isDirectory)
            {
                sb.AppendLine($"{indentStr}<folder name=\"{name}\" path=\"{path}\">");
                totalFolders++;

                var children = GetChildren(path);
                foreach (string child in children)
                {
                    GenerateXMLNode(sb, child, indent + 1, depth + 1);
                }

                sb.AppendLine($"{indentStr}</folder>");
            }
            else
            {
                FileInfo fi = new FileInfo(path);
                long size = fi.Exists ? fi.Length : 0;

                sb.AppendLine($"{indentStr}<file name=\"{name}\" path=\"{path}\" size=\"{size}\" />");

                totalSize += size;
                totalFiles++;
            }
        }

        // Helper methods
        private bool ShouldIncludeFile(string path)
        {
            if (!filterEnabled) return true;

            if (!includeMetaFiles && path.EndsWith(".meta"))
                return false;

            if (includedExtensions.Count > 0)
            {
                string ext = Path.GetExtension(path).ToLower();
                return includedExtensions.Contains(ext);
            }

            return true;
        }

        private bool ShouldExclude(string path)
        {
            if (!filterEnabled) return false;

            string name = Path.GetFileName(path);
            return excludedFolders.Contains(name);
        }

        private List<string> GetChildren(string path)
        {
            var children = new List<string>();

            // Add subdirectories
            string[] subdirs = AssetDatabase.GetSubFolders(path);
            foreach (string subdir in subdirs)
            {
                if (!ShouldExclude(subdir))
                {
                    children.Add(subdir);
                }
            }

            // Add files
            string[] guids = AssetDatabase.FindAssets("", new[] { path });
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                if (Path.GetDirectoryName(assetPath) == path.Replace('/', Path.DirectorySeparatorChar))
                {
                    if (!AssetDatabase.IsValidFolder(assetPath) && ShouldIncludeFile(assetPath))
                    {
                        children.Add(assetPath);
                    }
                }
            }

            // Sort
            children.Sort((a, b) =>
            {
                bool aIsDir = AssetDatabase.IsValidFolder(a);
                bool bIsDir = AssetDatabase.IsValidFolder(b);

                if (aIsDir && !bIsDir) return -1;
                if (!aIsDir && bIsDir) return 1;

                return string.Compare(Path.GetFileName(a), Path.GetFileName(b), StringComparison.OrdinalIgnoreCase);
            });

            return children;
        }

        private int GetFileCount(string folderPath)
        {
            int count = 0;
            string[] guids = AssetDatabase.FindAssets("", new[] { folderPath });

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!AssetDatabase.IsValidFolder(assetPath))
                {
                    count++;
                }
            }

            return count;
        }

        private string GetFileIcon(string path)
        {
            string ext = Path.GetExtension(path).ToLower();

            return ext switch
            {
                ".cs" => "📄 ",
                ".prefab" => "🎯 ",
                ".unity" => "🎬 ",
                ".mat" => "🎨 ",
                ".shader" => "✨ ",
                ".asset" => "📦 ",
                ".png" or ".jpg" or ".jpeg" => "🖼️ ",
                ".fbx" or ".obj" => "🎭 ",
                ".wav" or ".mp3" or ".ogg" => "🎵 ",
                ".txt" or ".md" => "📝 ",
                ".json" or ".xml" => "📋 ",
                _ => "📄 "
            };
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:F2} {sizes[order]}";
        }

        private void SaveToFile()
        {
            string path = EditorUtility.SaveFilePanel(
                "Save Project Tree",
                Application.dataPath,
                outputFileName,
                GetFileExtension()
            );

            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, generatedTree);
                EditorUtility.DisplayDialog("Success", $"Project tree saved to:\n{path}", "OK");

                // Open file location
                EditorUtility.RevealInFinder(path);
            }
        }

        private string GetFileExtension()
        {
            return outputFormat switch
            {
                OutputFormat.Text => "txt",
                OutputFormat.Markdown => "md",
                OutputFormat.JSON => "json",
                OutputFormat.HTML => "html",
                OutputFormat.XML => "xml",
                _ => "txt"
            };
        }

        private void ResetSettings()
        {
            rootPath = "Assets";
            includeMetaFiles = false;
            includeEmptyFolders = true;
            includeFileSize = true;
            includeFileCount = true;
            useIcons = true;
            copyToClipboard = true;
            saveToFile = true;
            outputFileName = "PROJECT_TREE.txt";
            filterEnabled = true;
            includedExtensions = new List<string> { ".cs", ".prefab", ".asset", ".unity", ".mat", ".shader" };
            excludedFolders = new List<string> { "Library", "Temp", "Logs", "obj", ".vs", ".git" };
            maxDepth = 10;
            outputFormat = OutputFormat.Text;
            generatedTree = "";
            showPreview = false;
        }

        [Serializable]
        private class JSONNode
        {
            public string name;
            public string path;
            public bool isDirectory;
            public long size;
            public List<JSONNode> children;
        }
    }
}