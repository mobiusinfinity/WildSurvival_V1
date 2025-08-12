# Save this as ExportScripts.ps1 in your Unity project root
$outputFile = "AllProjectScripts.txt"
$files = Get-ChildItem -Path "Assets" -Include *.cs -Recurse

foreach ($file in $files) {
    Add-Content $outputFile "`n`n// ============================================"
    Add-Content $outputFile "// FILE: $($file.FullName.Replace($PWD, ''))"
    Add-Content $outputFile "// ============================================`n"
    Get-Content $file.FullName | Add-Content $outputFile
}

Write-Host "Exported $($files.Count) scripts to $outputFile"