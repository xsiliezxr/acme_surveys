<#
.SYNOPSIS
    Crea la base AcmeSurveys ejecutando database/00-02. -Recreate la elimina antes (borra los datos).
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\Initialize-Database.ps1 [-Server ".\SQLEXPRESS"] [-Recreate]
#>
[CmdletBinding()]
param(
    [string] $Server = '(localdb)\MSSQLLocalDB',
    [switch] $Recreate
)

$ErrorActionPreference = 'Stop'
$databaseDir = Join-Path $PSScriptRoot '..\database'

Add-Type -AssemblyName System.Data

function Invoke-SqlScript([string] $path, [string] $database) {
    $sql = Get-Content -Path $path -Raw -Encoding UTF8
    # ADO.NET no entiende el separador GO de sqlcmd: se divide en lotes.
    $batches = [regex]::Split($sql, '^\s*GO\s*$', [System.Text.RegularExpressions.RegexOptions]::Multiline) |
        Where-Object { $_.Trim().Length -gt 0 }

    $connection = New-Object System.Data.SqlClient.SqlConnection "Data Source=$Server;Initial Catalog=$database;Integrated Security=True;Connect Timeout=30"
    $connection.Open()
    try {
        foreach ($batch in $batches) {
            $command = $connection.CreateCommand()
            $command.CommandText = $batch
            $command.CommandTimeout = 120
            [void] $command.ExecuteNonQuery()
        }
    }
    finally {
        $connection.Dispose()
    }
}

if ($Server -like '(localdb)*') {
    $localDb = Get-Command SqlLocalDB.exe -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source
    if (-not $localDb) {
        $localDb = Get-ChildItem "$env:ProgramFiles\Microsoft SQL Server\*\Tools\Binn\SqlLocalDB.exe" -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
    }
    if (-not $localDb) { throw 'No se encontró SQL Server LocalDB. Instálelo o use -Server para otra instancia.' }
    & $localDb start MSSQLLocalDB | Out-Null
}

if ($Recreate) {
    Write-Host 'Eliminando base de datos existente...' -ForegroundColor Yellow
    $drop = "IF DB_ID(N'AcmeSurveys') IS NOT NULL BEGIN ALTER DATABASE AcmeSurveys SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE AcmeSurveys; END"
    $connection = New-Object System.Data.SqlClient.SqlConnection "Data Source=$Server;Initial Catalog=master;Integrated Security=True"
    $connection.Open()
    try { $cmd = $connection.CreateCommand(); $cmd.CommandText = $drop; [void] $cmd.ExecuteNonQuery() } finally { $connection.Dispose() }
}

$scripts = @(
    @{ File = '00_create_database.sql'; Database = 'master' },
    @{ File = '01_schema.sql';          Database = 'AcmeSurveys' },
    @{ File = '02_seed.sql';            Database = 'AcmeSurveys' }
)

foreach ($script in $scripts) {
    Write-Host "Ejecutando $($script.File)..."
    Invoke-SqlScript -path (Join-Path $databaseDir $script.File) -database $script.Database
}

Write-Host "Base de datos lista en $Server." -ForegroundColor Green
