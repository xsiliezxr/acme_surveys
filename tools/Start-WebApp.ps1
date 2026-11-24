<#
.SYNOPSIS
    Levanta el sitio en IIS Express por HTTPS (puerto 44385, con certificado de desarrollo) sin abrir Visual Studio. Requiere compilar antes.
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\Start-WebApp.ps1
#>
[CmdletBinding()]
param(
    [int] $Port = 44385
)

$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$sitePath = Join-Path $root 'src\AcmeSurveys.Web'
$iisExpressDir = Join-Path $env:ProgramFiles 'IIS Express'
$configDir = Join-Path $root '.vs\config'
$configFile = Join-Path $configDir 'applicationhost.config'
$siteName = 'AcmeSurveys.Web'

if (-not (Test-Path (Join-Path $iisExpressDir 'iisexpress.exe'))) {
    throw 'No se encontró IIS Express. Instálelo junto con Visual Studio (carga de trabajo "ASP.NET y desarrollo web").'
}
if (-not (Test-Path (Join-Path $sitePath 'bin\AcmeSurveys.Web.dll'))) {
    throw 'El proyecto web no está compilado. Ejecute primero: MSBuild AcmeSurveys.sln -restore'
}

if (-not (Test-Path $configFile)) {
    New-Item -ItemType Directory -Force -Path $configDir | Out-Null
    Copy-Item (Join-Path $iisExpressDir 'config\templates\PersonalWebServer\applicationhost.config') $configFile
    $appcmd = Join-Path $iisExpressDir 'appcmd.exe'
    & $appcmd add site /name:$siteName "/bindings:https/*:${Port}:localhost" "/physicalPath:$sitePath" "/apphostconfig:$configFile" | Out-Null
    & $appcmd set app "$siteName/" /applicationPool:Clr4IntegratedAppPool "/apphostconfig:$configFile" | Out-Null
}

$baseUrl = "https://localhost:$Port"
Write-Host ''
Write-Host "  Aplicación: $baseUrl/" -ForegroundColor Green
Write-Host "  Swagger UI: $baseUrl/swagger" -ForegroundColor Green
Write-Host "  OpenAPI:    $baseUrl/swagger/docs/v1" -ForegroundColor Green
Write-Host "  API REST:   $baseUrl/api/v1" -ForegroundColor Green
Write-Host '  (Ctrl+C para detener)'
Write-Host ''
& (Join-Path $iisExpressDir 'iisexpress.exe') "/config:$configFile" "/site:$siteName"
