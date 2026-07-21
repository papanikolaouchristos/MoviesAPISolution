Write-Host ""
Write-Host "Installing Git Hooks..."

git config --local core.hooksPath .githooks

Write-Host ""
Write-Host "Git hooks installed successfully."
Write-Host ""

git config --get core.hooksPath