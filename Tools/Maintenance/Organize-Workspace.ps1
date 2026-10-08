param([switch]$Apply)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
function Resolve-SafePath([string]$relative) {
    $path = [IO.Path]::GetFullPath((Join-Path $projectRoot $relative))
    if (-not $path.StartsWith($projectRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw "Outside project: $path" }
    return $path
}
$plan = [Collections.Generic.List[object]]::new()
function Add-Move([string]$from, [string]$to) {
    $source = Resolve-SafePath $from
    $destination = Resolve-SafePath $to
    if (-not (Test-Path -LiteralPath $source)) { return }
    if (Test-Path -LiteralPath $destination) { throw "Destination occupied: $destination" }
    $items = @(Get-Item -LiteralPath $source -Force)
    if ($items[0].PSIsContainer) { $items += @(Get-ChildItem -LiteralPath $source -Recurse -Force) }
    if ($items | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }) { throw "Link in source: $source" }
    $hashes = @($items | Where-Object { -not $_.PSIsContainer } | ForEach-Object {
        [pscustomobject]@{ Suffix=$_.FullName.Substring($source.Length); Hash=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
    })
    $plan.Add([pscustomobject]@{ From=$from; To=$to; Hashes=$hashes })
}
Add-Move 'CONFIGURACION_MEJORAS.md' 'Docs/Development/CONFIGURACION_MEJORAS.md'
foreach ($name in @('DISENO_SOLO_HISTORIA_DYANATRO.md','DISENO_VIDEOJUEGO_XUNJUU.md','HISTORIA_JUGABLE_ACTUAL.md')) { Add-Move $name ('Docs/Design/'+$name) }
Add-Move 'Respaldo' 'Archive/Backups/Project'
Add-Move 'Respaldo_2D_Completo_NoImportar' 'Archive/Backups/Legacy2D'
Add-Move 'Respaldo_PackageManager_NoImportar' 'Archive/Backups/PackageManager'
Add-Move 'tmp' 'Archive/WorkingReferences'
foreach ($name in @('contact.png','qa_contact.png','qa_corregido.pdf')) { Add-Move $name ('output/qa/legacy/'+$name) }
foreach ($file in Get-ChildItem -LiteralPath $projectRoot -File -Filter '*.log') { Add-Move $file.Name ('output/logs/legacy/'+$file.Name) }
$plan | Select-Object From,To | Format-Table -AutoSize
if (-not $Apply) { Write-Output 'Dry run. Use -Apply to move and verify.'; return }
$report = Resolve-SafePath ('output/project-organization/workspace-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.json')
New-Item -ItemType Directory -Path (Split-Path $report) -Force | Out-Null
$plan | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $report -Encoding UTF8
foreach ($move in $plan) {
    $source = Resolve-SafePath $move.From
    $destination = Resolve-SafePath $move.To
    New-Item -ItemType Directory -Path (Split-Path $destination) -Force | Out-Null
    Move-Item -LiteralPath $source -Destination $destination
    foreach ($file in $move.Hashes) {
        if ((Get-FileHash -LiteralPath ($destination+$file.Suffix) -Algorithm SHA256).Hash -ne $file.Hash) { throw "Hash mismatch: $destination$($file.Suffix)" }
    }
}
Write-Output "PASS: $($plan.Count) moves verified. Manifest: $report"
