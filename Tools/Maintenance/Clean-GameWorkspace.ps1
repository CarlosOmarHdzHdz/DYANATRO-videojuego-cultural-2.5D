param([switch]$Apply, [string]$BackupRoot)
$ErrorActionPreference = 'Stop'
$gameRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..')).TrimEnd('\')
$gameParent = Split-Path $gameRoot -Parent
if (-not $BackupRoot) {
    $BackupRoot = Join-Path $gameParent ('DYANATRO_Respaldo_Limpieza_' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
}
$BackupRoot = [IO.Path]::GetFullPath($BackupRoot).TrimEnd('\')
if ((Split-Path $BackupRoot -Parent) -ne $gameParent -or
    (Split-Path $BackupRoot -Leaf) -notlike 'DYANATRO_Respaldo_Limpieza_*') {
    throw 'El respaldo debe ser una carpeta hermana especifica DYANATRO_Respaldo_Limpieza_*.'
}
if (Test-Path -LiteralPath $BackupRoot) { throw 'El respaldo ya existe; no se sobrescribe.' }
$plan = [Collections.Generic.List[object]]::new()
function Add-SafeMove([string]$Relative, [string]$Category) {
    $source = [IO.Path]::GetFullPath((Join-Path $gameRoot $Relative))
    if (-not $source.StartsWith($gameRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw "Ruta fuera del proyecto: $source" }
    if (-not (Test-Path -LiteralPath $source)) { return }
    if ($plan | Where-Object { $source -eq $_.Source -or $source.StartsWith($_.Source + '\', [StringComparison]::OrdinalIgnoreCase) }) { return }
    $destination = [IO.Path]::GetFullPath((Join-Path $BackupRoot (Join-Path $Category $Relative)))
    if (-not $destination.StartsWith($BackupRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Destino no seguro.' }
    $items = @(Get-Item -LiteralPath $source -Force)
    if ($items[0].Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "No se mueve un enlace: $source" }
    if ($items[0].PSIsContainer) { $items += @(Get-ChildItem -LiteralPath $source -Recurse -Force) }
    if ($items | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }) { throw "Carpeta con enlaces: $source" }
    $hashes = @($items | Where-Object { -not $_.PSIsContainer } | ForEach-Object {
        [pscustomobject]@{ Suffix=$_.FullName.Substring($source.Length); Bytes=$_.Length; SHA256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
    })
    $plan.Add([pscustomobject]@{ Source=$source; Destination=$destination; Relative=$Relative; Files=$hashes })
}

# Documentacion academica y fotografias personales, no documentacion tecnica del juego.
foreach ($name in @('Docs/Investigacion', 'Docs/Residencia')) { Add-SafeMove $name 'Documentos' }
Add-SafeMove 'tmp' 'Revisiones'

# Exportaciones y renders academicos sueltos. No se toca Library, Temp, Builds,
# Archive ni output/qa: el ultimo contiene un toolchain con enlaces a Unity.
foreach ($dir in Get-ChildItem -LiteralPath $gameRoot -Directory) {
    if ($dir.Name -match '^(dyanatro-(final-qa|game-protocol-render|revised-)|escape-room-final-render|protocol-conservative-qa|qa_final$|qa_protocolo_|render_(estructura|metodologia)|template-reference-render$)') {
        Add-SafeMove $dir.Name 'Revisiones'
    }
}
foreach ($file in Get-ChildItem -LiteralPath $gameRoot -File) {
    if ($file.Extension.ToLowerInvariant() -in @('.pdf','.doc','.docx','.docm','.dotx','.py') -or
        $file.Name -match '^(image\d+\.(png|jpg)|diagrama_incremental_espanol\.png|sommerville.*\.png|template-reference-render-page-\d+\.png|qa_final_contact\.png|escape_room_artifact\.md)$') {
        Add-SafeMove $file.Name 'MaterialAcademico'
    }
}
foreach ($name in @('anteproyecto-qa','informe-mensual-qa-20261001','pdf','protocol-dyanatro-qa','protocolo-programacion-qa')) {
    Add-SafeMove ('output/' + $name) 'Revisiones'
}
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $gameRoot 'Tools/Maintenance') -File -Filter '*.py') {
    if ($file.Name -match '(anteproyecto|protocol|research)') {
        Add-SafeMove ('Tools/Maintenance/' + $file.Name) 'HerramientasAcademicas'
    }
}

# Defensa adicional: documentos incrustados accidentalmente entre assets, con su meta.
$assetRoot = Join-Path $gameRoot 'Assets'
$pending = [Collections.Generic.Queue[string]]::new()
$pending.Enqueue($assetRoot)
while ($pending.Count -gt 0) {
    foreach ($item in Get-ChildItem -LiteralPath $pending.Dequeue() -Force) {
        if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { continue }
        if ($item.PSIsContainer) {
            if ($item.Name -notin @('Library','Temp','Logs','_Recovery','.git')) { $pending.Enqueue($item.FullName) }
        } elseif ($item.Extension.ToLowerInvariant() -in @('.pdf','.doc','.docx','.docm','.dotx')) {
            $relative = $item.FullName.Substring($gameRoot.Length + 1)
            Add-SafeMove $relative 'DocumentosEnAssets'
            Add-SafeMove ($relative + '.meta') 'DocumentosEnAssets'
        }
    }
}
if ($plan.Count -eq 0) {
    Write-Output 'PASS: proyecto limpio; no hay documentos ni revisiones pendientes de retirar.'
    return
}
$fileCount = ($plan | ForEach-Object { $_.Files.Count } | Measure-Object -Sum).Sum
$totalBytes = ($plan | ForEach-Object { $_.Files } | Measure-Object -Property Bytes -Sum).Sum
$plan | Select-Object Relative,Destination | Format-Table -AutoSize | Out-String | Write-Output
Write-Output "PLAN: $($plan.Count) movimientos; $fileCount archivos; $totalBytes bytes. Respaldo: $BackupRoot"
if (-not $Apply) { Write-Output 'Simulacion: usar -Apply para retirar y verificar.'; return }
New-Item -ItemType Directory -Path $BackupRoot | Out-Null
$manifest = Join-Path $BackupRoot 'manifiesto.json'
@{ Project=$gameRoot; Backup=$BackupRoot; Created=(Get-Date -Format o); Moves=@($plan.ToArray()) } |
    ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $manifest -Encoding UTF8
foreach ($move in $plan) {
    # Validacion absoluta repetida inmediatamente antes de cada movimiento.
    if (-not $move.Source.StartsWith($gameRoot + '\', [StringComparison]::OrdinalIgnoreCase) -or
        -not $move.Destination.StartsWith($BackupRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Movimiento fuera del alcance.' }
    if (Test-Path -LiteralPath $move.Destination) { throw "Destino ocupado: $($move.Destination)" }
    New-Item -ItemType Directory -Path (Split-Path $move.Destination -Parent) -Force | Out-Null
    Move-Item -LiteralPath $move.Source -Destination $move.Destination
    foreach ($file in $move.Files) {
        if ((Get-FileHash -LiteralPath ($move.Destination + $file.Suffix) -Algorithm SHA256).Hash -ne $file.SHA256) {
            throw "Integridad incorrecta: $($move.Destination)$($file.Suffix)"
        }
    }
}
Write-Output "PASS: $fileCount archivos retirados y verificados. Recuperables en $BackupRoot. Manifiesto: $manifest"
