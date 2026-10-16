param([string]$Unity = 'C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe', [switch]$Build, [ValidateSet('All','Setup','EditMode','PlayMode','Build')][string]$Stage = 'All')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Set-Location $projectRoot
New-Item -ItemType Directory -Force Logs,TestResults | Out-Null
function Invoke-UnityCheck([string]$Name,[string[]]$Extra) {
    $logPath = Join-Path $projectRoot "Logs\$Name.log"
    $arguments = @('-batchmode','-projectPath',('"'+$projectRoot+'"'),'-logFile',('"'+$logPath+'"')) + $Extra
    $process = Start-Process -FilePath $Unity -ArgumentList $arguments -WindowStyle Hidden -PassThru
    # Wait for the editor itself; compiler/package-server child processes may outlive it.
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) { throw "Unity $Name failed ($($process.ExitCode)). See $logPath" }
}
if ($Stage -in @('All','Setup')) { Invoke-UnityCheck 'setup' @('-nographics','-executeMethod','OrbitDominion.Editor.PrototypeSetup.Setup','-quit') }
if ($Stage -in @('All','EditMode')) { Invoke-UnityCheck 'editmode' @('-nographics','-runTests','-testPlatform','EditMode','-testResults','TestResults/editmode.xml') }
if ($Stage -in @('All','PlayMode')) { Invoke-UnityCheck 'playmode' @('-runTests','-testPlatform','PlayMode','-testResults','TestResults/playmode.xml') }
if (($Stage -eq 'All' -and $Build) -or $Stage -eq 'Build') { Invoke-UnityCheck 'build' @('-executeMethod','OrbitDominion.Editor.PrototypeSetup.BuildWindows','-quit') }
Write-Output 'Requested Unity verification commands completed. Review Logs and TestResults for details.'
