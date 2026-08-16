#Requires -Version 5.1
<#
.SYNOPSIS
  Publishes PureFusionIRC (self-contained win-x64) and compiles an Inno Setup installer.
#>
[CmdletBinding()]
param(
    [string] $Version = "1.0.0-B2",
    [string] $Runtime = "win-x64",
    [switch] $SkipZip
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $root "artifacts\publish\$Runtime"
$installerDir = Join-Path $root "artifacts\installer"
$portableDir = Join-Path $root "artifacts\portable"
$iss = Join-Path $PSScriptRoot "PureFusionIRC.iss"

function Get-NumericVersion([string] $label) {
    $core = ($label -split "-", 2)[0]
    $parts = @($core.Split("."))
    while ($parts.Count -lt 4) {
        $parts += "0"
    }
    return ($parts[0..3] -join ".")
}

function Get-IsccPath {
    $candidates = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
        "${env:LocalAppData}\Programs\Inno Setup 6\ISCC.exe"
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
    )
    foreach ($path in $candidates) {
        if ($path -and (Test-Path $path)) {
            return $path
        }
    }

    $cmd = Get-Command iscc -ErrorAction SilentlyContinue
    if ($cmd) {
        return $cmd.Source
    }

    return $null
}

Write-Host "Publishing PureFusionIRC $Version ($Runtime, self-contained)…"
if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $publishDir, $installerDir | Out-Null

dotnet publish (Join-Path $root "src\PureFusionIRC.App\PureFusionIRC.App.csproj") `
    -c Release `
    -r $Runtime `
    --self-contained true `
    --nologo `
    -o $publishDir `
    -p:Version=$Version `
    -p:InformationalVersion=$Version `
    -p:DebugType=none `
    -p:DebugSymbols=false
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed."
}

Get-ChildItem $publishDir -Filter "*.pdb" -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force

$iscc = Get-IsccPath
if (-not $iscc) {
    throw @"
Inno Setup 6 compiler (ISCC.exe) was not found.
Install it from https://jrsoftware.org/isinfo.php then re-run this script.
"@
}

$numeric = Get-NumericVersion $Version
$publishForIss = $publishDir.Replace("\", "/")
$outForIss = $installerDir.Replace("\", "/")

Write-Host "Compiling Inno Setup installer…"
& $iscc `
    "/DMyAppVersion=$Version" `
    "/DMyAppNumericVersion=$numeric" `
    "/DPublishDir=$publishForIss" `
    "/DOutputDir=$outForIss" `
    $iss
if ($LASTEXITCODE -ne 0) {
    throw "ISCC failed."
}

$setup = Join-Path $installerDir "PureFusionIRC-$Version-setup.exe"
if (-not (Test-Path $setup)) {
    throw "Expected installer missing: $setup"
}

if (-not $SkipZip) {
    New-Item -ItemType Directory -Force -Path $portableDir | Out-Null
    $zip = Join-Path $portableDir "PureFusionIRC-$Version-$Runtime.zip"
    if (Test-Path $zip) {
        Remove-Item $zip -Force
    }

    Write-Host "Writing portable zip…"
    Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zip -CompressionLevel Optimal
}

Write-Host "Installer: $setup"
if (-not $SkipZip) {
    Write-Host "Portable:  $(Join-Path $portableDir "PureFusionIRC-$Version-$Runtime.zip")"
}
