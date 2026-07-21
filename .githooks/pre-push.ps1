[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "Running security checks before push..."

Write-Host "Running security checks before push..."

New-Item -ItemType Directory -Force logs | Out-Null

$FAILED = $false

$repo = git rev-parse --show-toplevel

Write-Host "Repository:"
Write-Host $repo


# ============================
# TRUFFLEHOG
# ============================

Write-Host ""
Write-Host "Running TruffleHog..."


docker run --rm `
-v "${repo}:/repo" `
-w /repo `
ghcr.io/trufflesecurity/trufflehog:latest `
filesystem /repo `
--fail `
--exclude-paths=trufflehog-exclude.txt `
> logs/trufflehog.log 2>&1


if ($LASTEXITCODE -ne 0) {

    Write-Host ""
    Write-Host "❌ Secrets detected"
    Write-Host "See logs/trufflehog.log"

    $FAILED = $true

}
else {

    Write-Host "✅ TruffleHog clean"

}



# ============================
# SEMGREP
# ============================

Write-Host ""
Write-Host "Running Semgrep..."


semgrep `
--config ./semgrep-rules `
--error `
> logs/semgrep.log 2>&1


if ($LASTEXITCODE -ne 0) {

    Write-Host ""
    Write-Host "❌ Security issues detected"
    Write-Host "See logs/semgrep.log"

    $FAILED = $true

}
else {

    Write-Host "✅ Semgrep clean"

}



# ============================
# FINAL RESULT
# ============================

Write-Host ""

if ($FAILED -eq $true) {

    Write-Host "================================"
    Write-Host " SECURITY CHECK FAILED "
    Write-Host " Push blocked "
    Write-Host "================================"

    exit 1

}
else {

    Write-Host "================================"
    Write-Host " SECURITY CHECK PASSED "
    Write-Host " Push allowed "
    Write-Host "================================"

    exit 0
}