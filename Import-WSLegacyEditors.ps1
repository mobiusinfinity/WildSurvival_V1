param(
  [Parameter(Mandatory=$true)][string]$OldRepo,   # e.g. C:\Repos\WildSurvival  (old project root)
  [Parameter(Mandatory=$true)][string]$NewProj    # e.g. C:\Unity_Projects\WildSurvival_V1.1 (new project root)
)

$ErrorActionPreference = "Stop"

function Write-Note($msg){ Write-Host "[WS] $msg" -ForegroundColor Cyan }

# 0) Guards
if (!(Test-Path "$OldRepo\Assets")) { throw "Old repo does not look like a Unity project (Assets missing): $OldRepo" }
if (!(Test-Path "$NewProj\Assets")) { throw "New project does not look like a Unity project (Assets missing): $NewProj" }

# 1) Find all Editor folders under old Assets (case-insensitive exact name 'Editor')
$editorDirs = Get-ChildItem -Path "$OldRepo\Assets" -Recurse -Directory |
  Where-Object { $_.Name -ieq 'Editor' }

if ($editorDirs.Count -eq 0) { throw "No Editor folders found in old repo. Are you pointing at the right path?" }

Write-Note "Found $($editorDirs.Count) Editor folders."

# 2) Mirror those Editor folders into the new project under the same relative path
$copied = @()
foreach ($dir in $editorDirs) {
  $rel = $dir.FullName.Substring(("$OldRepo\Assets").Length).TrimStart('\')
  $dst = Join-Path "$NewProj\Assets" $rel
  New-Item -ItemType Directory -Force -Path $dst | Out-Null

  # Copy everything, but skip Library/Packages/ProjectSettings (shouldn't be inside Editor anyway),
  # and optionally skip legacy asmdefs to avoid collisions.
  robocopy $dir.FullName $dst /MIR /NFL /NDL /NJH /NJS /NP /XF *.csproj *.sln *.user *.meta~ *.asmdef |
    Out-Null

  $copied += $dst
  Write-Note "Copied → Assets\$rel"
}

# 3) Write a consolidated Editor-only asmdef to compile all imported editors
$asmDir = "$NewProj\Assets\WildSurvival\Editor\_Imported"
New-Item -ItemType Directory -Force -Path $asmDir | Out-Null

$asmdef = @"
{
  "name": "WildSurvival.LegacyEditors",
  "includePlatforms": ["Editor"],
  "references": [],
  "optionalUnityReferences": [],
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": true,
  "noEngineReferences": false
}
"@

$asmPath = Join-Path $asmDir "_LegacyEditors.asmdef"
Set-Content -Path $asmPath -Value $asmdef -Encoding UTF8

# 4) A small manifest of what we copied
$manifest = @{
  importedAt = (Get-Date).ToString("s")
  oldRepo    = $OldRepo
  newProject = $NewProj
  folders    = $copied | ForEach-Object { $_.Substring($NewProj.Length+1) }
} | ConvertTo-Json -Depth 3

$manPath = Join-Path $asmDir "WS_ImportedTools.manifest.json"
Set-Content -Path $manPath -Value $manifest -Encoding UTF8

Write-Note "Wrote consolidated asmdef: $(Resolve-Path $asmPath)"
Write-Note "Wrote copy manifest:        $(Resolve-Path $manPath)"

Write-Host "`nNext:" -ForegroundColor Yellow
Write-Host "  1) Close & reopen Unity if it was open." -ForegroundColor Yellow
Write-Host "  2) Drop in the WS unified menu + hubs (code below)." -ForegroundColor Yellow
Write-Host "  3) In Unity, use Wild Survival / Tools / Tool Browser to find everything." -ForegroundColor Yellow
