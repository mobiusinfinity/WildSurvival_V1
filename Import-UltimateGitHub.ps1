param(
  [string]$OldRepoUrl = "https://github.com/mobiusinfinity/WildSurvival.git",
  [string]$OldBranch  = "main",
  [string]$NewRepoUrl = "https://github.com/mobiusinfinity/WildSurvival_V1.git",
  [switch]$NoCommit
)

$ErrorActionPreference = "Stop"

function Write-Utf8NoBom {
  param([string]$Path, [string]$Content)
  $enc = New-Object System.Text.UTF8Encoding($false)
  [System.IO.File]::WriteAllText($Path, $Content, $enc)
}

function Ensure-Dir { param([string]$p) if (-not (Test-Path $p)) { New-Item -ItemType Directory -Force -Path $p | Out-Null } }

function Copy-Dir {
  param([string]$From, [string]$To)
  Ensure-Dir $To
  # Use robocopy for speed; ignore non-zero return codes that just mean "files copied"
  robocopy $From $To /E /NFL /NDL /NJH /NJS /NC /NS | Out-Null
}

# 0) Sanity checks
if (-not (Test-Path "Assets") -or -not (Test-Path "Packages") -or -not (Test-Path "ProjectSettings")) {
  throw "Run this from your Unity project root (must contain Assets/, Packages/, ProjectSettings/)."
}
if (-not (Get-Command git -ErrorAction SilentlyContinue)) { throw "Git not found on PATH." }

$root    = (Get-Location).Path
$tempOld = Join-Path $root ".tmp_old_ws"
$destHub = Join-Path $root "Assets\WildSurvival\Editor\Hubs\GitShare"
$asmdef  = Join-Path $root "Assets\WildSurvival\Editor\WildSurvival.Editor.asmdef"
$menuCs  = Join-Path $destHub "WSGitShareMenuAdapter.cs"
$readme  = Join-Path $destHub "README_GitShare.md"

# 1) Clone old repo to temp (no linkage to your project)
if (Test-Path $tempOld) { Remove-Item $tempOld -Recurse -Force }
git clone --depth 1 --filter=blob:none --no-checkout $OldRepoUrl $tempOld | Out-Null
Push-Location $tempOld
git sparse-checkout init --cone | Out-Null

# Candidate folders that have contained the Git/Share tools across versions
$sparse = @(
  "Assets/WildSurvival/Editor/Git",
  "Assets/WildSurvival/Imports/WildSurvival/Editor/Git",
  "Assets/_Project/Code/Editor/Git",
  "Assets/WildSurvival/Editor/Share",
  "Assets/_Project/Code/Editor/Share",
  "Assets/WildSurvival/Editor/Collab",
  "Assets/_Project/Code/Editor/Collab",
  "Assets/_Project/Code/Editor/ProjectHub",
  "Assets/WildSurvival/Editor/ProjectHub",
  "Assets/WildSurvival/Editor/MenuConsolidation_G5"
)
git sparse-checkout set $sparse | Out-Null
git checkout $OldBranch | Out-Null
Pop-Location

# 2) Create destination
Ensure-Dir $destHub

# 3) Copy any existing candidates into the new hub
$imported = @()
foreach ($rel in $sparse) {
  $src = Join-Path $tempOld $rel
  if (Test-Path $src) {
    Copy-Dir $src $destHub
    $imported += $rel
  }
}

# 4) Fallback: if we still didn't catch hub files, search by class/file names
if ($imported.Count -eq 0) {
  $patterns = @("PublicMirrorExporter", "ShareHub", "GitHub", "WSGit", "MirrorExporter", "SharePackage")
  $hits = @()
  foreach ($p in $patterns) {
    $hits += Get-ChildItem -Path $tempOld -Recurse -File -Filter "*.cs" |
      Where-Object { $_.Name -match $p -and $_.FullName -match "\\Editor\\" }
  }
  $dirs = $hits | ForEach-Object { $_.DirectoryName } | Sort-Object -Unique
  foreach ($d in $dirs) {
    Copy-Dir $d $destHub
    $imported += ($d.Substring($tempOld.Length+1))
  }
}

if ($imported.Count -eq 0) {
  throw "Could not locate Git/Share editor tools in old repo."
}

# 5) Remove any asmdefs we imported to avoid duplicates
Get-ChildItem -Path $destHub -Recurse -Filter *.asmdef -File -ErrorAction SilentlyContinue | ForEach-Object {
  Remove-Item $_.FullName -Force
}

# 6) Create a single editor asmdef (if missing)
if (-not (Test-Path $asmdef)) {
  $asmJson = @"
{
  "name": "WildSurvival.Editor",
  "includePlatforms": [ "Editor" ],
  "references": [],
  "autoReferenced": true,
  "noEngineReferences": false
}
"@
  Write-Utf8NoBom -Path $asmdef -Content $asmJson
}

# 7) Fix CompressionLevel ambiguity in imported files by injecting an alias
$csFiles = Get-ChildItem -Path $destHub -Recurse -Filter "*.cs" -File
foreach ($f in $csFiles) {
  $txt = Get-Content $f.FullName -Raw
  if ($txt -match "(?ms)\bCompressionLevel\b" -and
      $txt -notmatch "System\.IO\.Compression\.CompressionLevel" -and
      $txt -notmatch "using\s+CompressionLevel\s*=\s*System\.IO\.Compression\.CompressionLevel")
  {
    # find end of using block to insert alias
    $lines = $txt -split "`r?`n"
    $insertIdx = 0
    for ($i=0; $i -lt $lines.Length; $i++) {
      if ($lines[$i] -match "^\s*using\s+") { $insertIdx = $i + 1 } else { if ($lines[$i].Trim() -ne "") { break } }
    }
    $alias = "using CompressionLevel = System.IO.Compression.CompressionLevel;"
    $new = @()
    $new += $lines[0..($insertIdx-1)]
    $new += $alias
    $new += $lines[$insertIdx..($lines.Length-1)]
    $txt2 = ($new -join "`r`n")
    Write-Utf8NoBom -Path $f.FullName -Content $txt2
  }
}

# 8) Write a resilient menu adapter (UTF-8 no BOM)
$menuCode = @"
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace WildSurvival.EditorTools {
  public static class WSGitMenuAdapter {
    [MenuItem(""Wild Survival/Git & Share/Git & Share Hub"", false, 20)]
    public static void Open() {
      // Try the new unified window name first, then known legacy classes
      var typesToTry = new[] {
        ""WildSurvival.EditorTools.WSGitShareHubWindow, Assembly-CSharp-Editor"",
        ""WildSurvival.Editor.Git.GitHubHubWindow, Assembly-CSharp-Editor"",
        ""WildSurvival.Editor.Git.PublicMirrorExporterV2, Assembly-CSharp-Editor"",
        ""WildSurvival.Editor.Share.WildSurvivalShareHub, Assembly-CSharp-Editor""
      };
      foreach (var qn in typesToTry) {
        var t = System.Type.GetType(qn);
        if (t != null && t.IsSubclassOf(typeof(EditorWindow))) {
          EditorWindow.GetWindow(t, false, ""Git & Share Hub"", true).Show();
          return;
        }
      }
      EditorUtility.DisplayDialog(""Git & Share"", ""Could not locate a Git/Share window in imported tools.\nCheck imported scripts and asmdefs."", ""OK"");
    }
  }
}
#endif
"@
Write-Utf8NoBom -Path $menuCs -Content $menuCode

# 9) README for the hub
$readmeText = @"
# Git & Share Hub (Imported)
This folder hosts the consolidated Git / Mirror / Share editor tools imported from the old project.
- Menu: **Wild Survival → Git & Share → Git & Share Hub**
- Single asmdef: **WildSurvival.Editor** (Editor-only)
- Imported from: $($OldRepoUrl) @ $OldBranch
Imported paths:
$(($imported | ForEach-Object { " - " + $_ }) -join "`r`n")
"@
Write-Utf8NoBom -Path $readme -Content $readmeText

# 10) Clean up temp
if (Test-Path $tempOld) { Remove-Item $tempOld -Recurse -Force }

# 11) Ensure current repo configured to your new remote URL
$hasGit = Test-Path ".git"
if (-not $hasGit) {
  git init -b main | Out-Null
}
$origin = ""
try { $origin = (git remote get-url origin) 2>$null } catch {}
if ($origin) {
  if ($origin -ne $NewRepoUrl) {
    git remote set-url origin $NewRepoUrl | Out-Null
  }
} else {
  git remote add origin $NewRepoUrl | Out-Null
}

# 12) Commit (optional)
git add Assets/WildSurvival/Editor/Hubs/GitShare
git add Assets/WildSurvival/Editor/WildSurvival.Editor.asmdef

if (-not $NoCommit) {
  git commit -m "feat(editor): import & unify Ultimate Git + Mirror/Share hub (menu, asmdef, alias fixes)" | Out-Null
  Write-Host "[OK] Imported and committed Git/Share Hub. Remote: $NewRepoUrl"
} else {
  Write-Host "[OK] Imported Git/Share Hub (not committed)."
}

Write-Host "Open Unity and use: Wild Survival > Git & Share > Git & Share Hub."
