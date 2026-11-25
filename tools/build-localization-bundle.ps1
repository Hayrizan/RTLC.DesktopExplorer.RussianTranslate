param(
    [Parameter(Mandatory=$true)][string]$UnityEditor,
    [Parameter(Mandatory=$true)][string]$OutputFolder
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repo 'artifacts/bundle-unity-project'
$assets = Join-Path $project 'Assets/RTLC'
foreach ($folder in @('translations','web-assets','fonts','font-bytes','licenses')) {
    New-Item -ItemType Directory -Force -Path (Join-Path $assets $folder) | Out-Null
}
foreach ($folder in @('Assets/Editor','ProjectSettings','Packages')) {
    New-Item -ItemType Directory -Force -Path (Join-Path $project $folder) | Out-Null
}
Set-Content -LiteralPath (Join-Path $project 'ProjectSettings/ProjectVersion.txt') -Value 'm_EditorVersion: 2022.3.62f2'
Set-Content -LiteralPath (Join-Path $project 'Packages/manifest.json') -Value '{"dependencies":{"com.unity.modules.assetbundle":"1.0.0"}}'
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'BuildLocalizationBundle.cs') -Destination (Join-Path $project 'Assets/Editor')
function Copy-Payload($source, $target, $filter, $append) {
    $files = @(Get-ChildItem -LiteralPath $source -File -Filter $filter)
    foreach ($old in Get-ChildItem -LiteralPath $target -File) {
        if ($old.Extension -eq '.meta') { continue }
        if ($old.Name -notin @($files | ForEach-Object { $_.Name + $append })) {
            Remove-Item -LiteralPath $old.FullName
        }
    }
    foreach ($file in $files) {
        $destination = Join-Path $target ($file.Name + $append)
        if (!(Test-Path -LiteralPath $destination) -or
            (Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath $destination).Hash) {
            Copy-Item -LiteralPath $file.FullName -Destination $destination -Force
        }
    }
}
Copy-Payload (Join-Path $repo 'translations') (Join-Path $assets 'translations') '*.json' '.bytes'
Copy-Payload (Join-Path $repo 'web-assets') (Join-Path $assets 'web-assets') '*.png' '.bytes'
Copy-Payload (Join-Path $repo 'fonts') (Join-Path $assets 'fonts') '*.ttf' ''
Copy-Payload (Join-Path $repo 'fonts') (Join-Path $assets 'font-bytes') '*.ttf' '.bytes'
Copy-Payload (Join-Path $repo 'fonts') (Join-Path $assets 'licenses') 'OFL-*.txt' '.bytes'
Copy-Item -LiteralPath (Join-Path $repo 'serialized_text_variants.json') -Destination (Join-Path $assets 'serialized_text_variants.json.bytes')
$log = Join-Path $project 'build.log'
$args = '-batchmode -nographics -quit -projectPath "{0}" -executeMethod BuildLocalizationBundle.Build -logFile "{1}"' -f $project,$log
$proc = Start-Process -FilePath $UnityEditor -ArgumentList $args -WindowStyle Hidden -Wait -PassThru
if ($proc.ExitCode -ne 0) {
    Get-Content -LiteralPath $log -Tail 35
    throw "Unity AssetBundle build failed with exit code $($proc.ExitCode)."
}
$bundle = Join-Path $project 'BundleOutput/rtlc.bundle'
if (!(Select-String -LiteralPath $log -SimpleMatch 'RTLC bundle resources:') -or !(Test-Path -LiteralPath $bundle)) {
    throw "Unity did not produce the bundle. See $log"
}
New-Item -ItemType Directory -Path $OutputFolder -Force | Out-Null
Copy-Item -LiteralPath $bundle -Destination (Join-Path $OutputFolder 'rtlc.bundle') -Force
