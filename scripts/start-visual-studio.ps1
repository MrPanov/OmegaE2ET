[CmdletBinding()]
param()

$visualStudioPath = 'C:\Program Files\Microsoft Visual Studio\18\Insiders\Common7\IDE\devenv.exe'
$solutionPath = Join-Path $PSScriptRoot '..\UiAutomation.sln'

if (-not (Test-Path -LiteralPath $visualStudioPath)) {
    throw "Visual Studio was not found: $visualStudioPath"
}

if (-not (Test-Path -LiteralPath $solutionPath)) {
    throw "Solution was not found: $solutionPath"
}

$securePassword = Read-Host 'Enter OMEGA_PASSWORD' -AsSecureString
$temporaryCredential = [System.Management.Automation.PSCredential]::new(
    'omega-test-user',
    $securePassword)

try {
    $env:OMEGA_PASSWORD = $temporaryCredential.GetNetworkCredential().Password
    $env:HEADLESS = 'false'

    Start-Process -FilePath $visualStudioPath -ArgumentList $solutionPath
    Write-Host 'Visual Studio started with OMEGA_PASSWORD and HEADLESS=false.'
}
finally {
    Remove-Item Env:OMEGA_PASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:HEADLESS -ErrorAction SilentlyContinue
    $temporaryCredential = $null
    $securePassword = $null
}
