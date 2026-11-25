param(
    [Parameter(Mandatory = $true)][string]$GameDir,
    [Parameter(Mandatory = $true)][string]$UnityEditor,
    [string]$BepInExDir = ''
)
$ErrorActionPreference = 'Stop'
$repoRoot = $PSScriptRoot
if (-not $BepInExDir) { $BepInExDir = Join-Path $GameDir 'BepInEx' }
$project = Join-Path $repoRoot 'src/RTLC.DesktopExplorer.RussianTranslate/RTLC.DesktopExplorer.RussianTranslate.csproj'

$gameRoot = [System.IO.Path]::GetFullPath($GameDir)
$pluginFolder = Join-Path $BepInExDir 'plugins/RTLC.DesktopExplorer.RussianTranslate'
$pluginFolder = [System.IO.Path]::GetFullPath($pluginFolder)
$gameExe = Join-Path $gameRoot 'DesktopExplorer.exe'
if (-not (Test-Path -LiteralPath $gameExe -PathType Leaf)) { throw "Game executable not found: $gameExe" }
if (-not (Test-Path -LiteralPath (Join-Path $BepInExDir 'core/BepInEx.dll') -PathType Leaf)) {
    throw "BepInEx installation not found: $BepInExDir"
}
$runningGame = @(Get-Process -Name 'DesktopExplorer' -ErrorAction SilentlyContinue)
if ($runningGame.Count -gt 0) {
    throw 'Close Desktop Explorer before building into its plugin folder.'
}

$stage = Join-Path $repoRoot 'artifacts/bundle-stage'
& (Join-Path $repoRoot 'tools/build-localization-bundle.ps1') -UnityEditor $UnityEditor -OutputFolder $stage
& dotnet build $project -c Release "-p:GameDir=$gameRoot" "-p:BepInExDir=$BepInExDir" --nologo
if ($LASTEXITCODE -ne 0) { throw 'Plugin compilation failed.' }

$dllName = 'RTLC.DesktopExplorer.RussianTranslate.dll'
$compiled = Join-Path (Split-Path -Parent $project) "bin/Release/netstandard2.1/$dllName"
if (!(Test-Path -LiteralPath $compiled -PathType Leaf)) { throw "Missing compiled plugin: $compiled" }

# Stage beside the live directory so each rename stays on the game's volume.
$pluginsRoot = (Resolve-Path -LiteralPath (Join-Path $BepInExDir 'plugins')).Path
$pluginFolder = Join-Path $pluginsRoot 'RTLC.DesktopExplorer.RussianTranslate'
$backupRoot = Join-Path (Split-Path -Parent $pluginsRoot) 'rtlc-deployment-backups'
New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
$stamp = [Guid]::NewGuid().ToString('N')
$incoming = Join-Path $backupRoot "incoming-$stamp"
$previous = Join-Path $backupRoot "previous-$stamp"
New-Item -ItemType Directory -Path $incoming | Out-Null
Copy-Item -LiteralPath $compiled,(Join-Path $stage 'rtlc.bundle') -Destination $incoming
if (Get-Process -Name 'DesktopExplorer' -ErrorAction SilentlyContinue) {
    throw "Close Desktop Explorer before deployment. The new package is staged at $incoming."
}
$hasPrevious = Test-Path -LiteralPath $pluginFolder
if ($hasPrevious) {
    $installed = Get-Item -LiteralPath $pluginFolder
    if ($installed.Attributes -band [IO.FileAttributes]::ReparsePoint) {
        throw "Plugin directory is a junction or symbolic link: $pluginFolder"
    }
    Move-Item -LiteralPath $installed.FullName -Destination $previous
}
try {
    Move-Item -LiteralPath $incoming -Destination $pluginFolder
}
catch {
    if ($hasPrevious) { Move-Item -LiteralPath $previous -Destination $pluginFolder }
    throw
}
Write-Output "Installed plugin and Unity AssetBundle at $pluginFolder"
if ($hasPrevious) { Write-Output "Previous installation: $previous" }
