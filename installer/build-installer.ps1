# Builds DeskNotes portable publish + Inno Setup installer.
# Requires Inno Setup 6: https://jrsoftware.org/isinfo.php

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$version = (Select-Xml -Path "$root\DeskNotes.csproj" -XPath "//Version").Node.InnerText.Trim()

Write-Host "Publishing DeskNotes $version (win-x64, self-contained)..."
dotnet publish "$root\DeskNotes.csproj" -c Release -r win-x64 --self-contained true -o "$root\publish\portable"

$iscc = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $iscc)) {
    $iscc = "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
}
if (-not (Test-Path $iscc)) {
    throw "Inno Setup 6 not found. Install from https://jrsoftware.org/isinfo.php"
}

Write-Host "Building installer..."
& $iscc "/DMyAppVersion=$version" "$PSScriptRoot\DeskNotes.iss"

$out = "$PSScriptRoot\output\DeskNotes-Setup-$version-win-x64.exe"
Write-Host "Done: $out"