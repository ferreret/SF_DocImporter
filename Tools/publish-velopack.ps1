# publish-velopack.ps1
#
# Empaqueta una de las apps WPF con Velopack y deja los archivos listos
# para subir a la carpeta de red de actualizaciones.
#
# Requisitos previos:
#   - .NET SDK 8 (o superior) en el PATH
#   - vpk instalado globalmente:  dotnet tool install -g vpk
#
# Uso:
#   .\Tools\publish-velopack.ps1 -App GestorExpedientesWpf -Version 1.0.1
#   .\Tools\publish-velopack.ps1 -App GestorRemesasWpf -Version 1.0.1 -UpdateShare \\SERVIDOR\Updates\GestorRemesas
#
# Si se pasa -UpdateShare, copia los archivos generados (RELEASES, *.nupkg,
# instalador Setup.exe) directamente a esa carpeta. Si se omite, se quedan
# en .\publish\<App>\releases para copiarlos manualmente.

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('GestorExpedientesWpf', 'GestorRemesasWpf')]
    [string]$App,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+(\.\d+)?$')]
    [string]$Version,

    [Parameter(Mandatory = $false)]
    [string]$UpdateShare,

    [Parameter(Mandatory = $false)]
    [string]$Configuration = 'Release',

    [Parameter(Mandatory = $false)]
    [string]$Runtime = 'win-x64'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "$App\$App.csproj"
$publishDir = Join-Path $repoRoot "publish\$App\bin"
$releasesDir = Join-Path $repoRoot "publish\$App\releases"

if (-not (Test-Path $projectPath)) {
    throw "No se encuentra el proyecto: $projectPath"
}

$appMetadata = @{
    'GestorExpedientesWpf' = @{
        Id      = 'TecnomediaGestorExpedientes'
        Title   = 'Gestor de Expedientes'
        MainExe = 'GestorExpedientesWpf.exe'
        Icon    = Join-Path $repoRoot 'GestorExpedientesWpf\remesa.ico'
    }
    'GestorRemesasWpf'     = @{
        Id      = 'TecnomediaGestorRemesas'
        Title   = 'Gestor de Remesas'
        MainExe = 'GestorRemesasWpf.exe'
        Icon    = Join-Path $repoRoot 'GestorRemesasWpf\puzzle.ico'
    }
}

$meta = $appMetadata[$App]

Write-Host "[1/3] dotnet publish $App v$Version ($Configuration / $Runtime)" -ForegroundColor Cyan
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
dotnet publish $projectPath `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:Version=$Version `
    -p:AssemblyVersion=$Version `
    -p:FileVersion=$Version `
    -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish fallo con exit code $LASTEXITCODE" }

Write-Host "[2/3] vpk pack" -ForegroundColor Cyan
if (-not (Test-Path $releasesDir)) { New-Item -ItemType Directory -Path $releasesDir | Out-Null }

$vpkArgs = @(
    'pack',
    '-u', $meta.Id,
    '-v', $Version,
    '-p', $publishDir,
    '-o', $releasesDir,
    '--packTitle', $meta.Title,
    '--mainExe', $meta.MainExe
)
if (Test-Path $meta.Icon) {
    $vpkArgs += @('--icon', $meta.Icon)
}

& vpk @vpkArgs
if ($LASTEXITCODE -ne 0) { throw "vpk pack fallo con exit code $LASTEXITCODE" }

Write-Host "[3/3] Paquetes generados en $releasesDir" -ForegroundColor Green
Get-ChildItem $releasesDir | Format-Table Name, Length -AutoSize

if ($UpdateShare) {
    Write-Host "Copiando a $UpdateShare ..." -ForegroundColor Cyan
    if (-not (Test-Path $UpdateShare)) {
        New-Item -ItemType Directory -Path $UpdateShare -Force | Out-Null
    }
    Copy-Item -Path (Join-Path $releasesDir '*') -Destination $UpdateShare -Force
    Write-Host "Copia completada." -ForegroundColor Green
}
else {
    Write-Host "Sugerencia: vuelve a ejecutar con -UpdateShare \\SERVIDOR\Updates\$App para subirlo automaticamente." -ForegroundColor Yellow
}
