$ErrorActionPreference = 'Stop'
# Each fixture supplies its own game types and must run in a fresh PowerShell process.
$runner = Join-Path $PSHOME $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' })
foreach ($test in Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.Tests.ps1' | Where-Object FullName -ne $PSCommandPath | Sort-Object Name) {
    & $runner -NoProfile -NonInteractive -File $test.FullName
    if ($LASTEXITCODE -ne 0) { throw "Failed: $($test.Name)" }
}
