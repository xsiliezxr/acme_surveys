<#
.SYNOPSIS
    Genera src/AcmeSurveys.Web/secrets.config con claves aleatorias de 256 bits. Regenerar la clave de cifrado deja ilegibles las respuestas existentes.
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\New-DevSecrets.ps1 [-Force]
#>
[CmdletBinding()]
param(
    [switch] $Force
)

$ErrorActionPreference = 'Stop'
$target = Join-Path $PSScriptRoot '..\src\AcmeSurveys.Web\secrets.config'

if ((Test-Path $target) -and -not $Force) {
    Write-Host "Ya existe $target. Use -Force para sobrescribirlo (las respuestas cifradas existentes quedarían ilegibles)." -ForegroundColor Yellow
    return
}

function New-RandomKey {
    $bytes = New-Object byte[] 32
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
    [Convert]::ToBase64String($bytes)
}

$content = @"
<?xml version="1.0" encoding="utf-8"?>
<!-- Generado por tools/New-DevSecrets.ps1 el $(Get-Date -Format s). NO versionar. -->
<appSettings>
  <add key="Acme:Jwt:SigningKey" value="$(New-RandomKey)" />
  <add key="Acme:DataProtection:ActiveKeyId" value="1" />
  <add key="Acme:DataProtection:Key:1" value="$(New-RandomKey)" />
</appSettings>
"@

Set-Content -Path $target -Value $content -Encoding UTF8
Write-Host "secrets.config generado en $((Resolve-Path $target).Path)" -ForegroundColor Green
