#!/usr/bin/env pwsh

$ScriptDir = $PSScriptRoot
$BaselineDir = Join-Path $ScriptDir "baseline"
$RewriteDir = Join-Path $ScriptDir "rewrite"

# Build projects
Write-Host "Building baseline (Rust)..."
Push-Location $BaselineDir
cargo build --release --quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "Rust build failed"
    Pop-Location
    exit 1
}
Pop-Location

Write-Host "Building rewrite (C#)..."
Push-Location $RewriteDir
dotnet build -c Release --nologo -v q
if ($LASTEXITCODE -ne 0) {
    Write-Host "C# build failed"
    Pop-Location
    exit 1
}
Pop-Location

Write-Host ""
Write-Host "Running compatibility tests..."
Write-Host "=============================="

$Passed = 0
$Failed = 0
$Cases = @("case0", "case1", "case2", "case3", "case4", "case5", "case6", "case7", "case8", "case9", "case10", "case11")

foreach ($case in $Cases) {
    # Run baseline
    Push-Location $BaselineDir
    $BaselineOutput = & ./target/release/baseline $case 2>&1 | Out-String
    $BaselineExitCode = $LASTEXITCODE
    if ($BaselineExitCode -ne 0) {
        $BaselineOutput = "ERROR: $BaselineExitCode"
    }
    Pop-Location

    # Run rewrite
    Push-Location $RewriteDir
    $RewriteOutput = dotnet run -c Release --no-build -- $case 2>&1 | Out-String
    $RewriteExitCode = $LASTEXITCODE
    if ($RewriteExitCode -ne 0) {
        $RewriteOutput = "ERROR: $RewriteExitCode"
    }
    Pop-Location

    # Compare outputs
    if ($BaselineOutput.Trim() -eq $RewriteOutput.Trim()) {
        Write-Host "✓ ${case}: PASSED"
        $Passed++
    } else {
        Write-Host "✗ ${case}: FAILED"
        Write-Host "  Baseline output:"
        $BaselineOutput -split "`n" | ForEach-Object { Write-Host "    $_" }
        Write-Host "  Rewrite output:"
        $RewriteOutput -split "`n" | ForEach-Object { Write-Host "    $_" }
        $Failed++
    }
}

Write-Host ""
Write-Host "=============================="
Write-Host "Results: $Passed passed, $Failed failed"

if ($Failed -gt 0) {
    exit 1
}
