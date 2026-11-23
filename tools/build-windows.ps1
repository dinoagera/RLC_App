param(
  [Parameter(Mandatory = $true)]
  [string]$UnityEditor
)
$ErrorActionPreference = "Stop"
$project = Split-Path $PSScriptRoot -Parent
New-Item -ItemType Directory -Force (Join-Path $project "Builds") | Out-Null
$log = Join-Path $project "Builds/windows-build.log"
$argsList = @("-batchmode", "-nographics", "-quit", "-projectPath", "`"$project`"",
  "-buildTarget", "Win64", "-executeMethod", "RLCLab.Editor.ProjectSetup.BuildWindows",
  "-logFile", "`"$log`"")
$process = Start-Process -FilePath $UnityEditor -ArgumentList $argsList -Wait -PassThru
if ($process.ExitCode -ne 0) { throw "Сборка завершилась с ошибкой. Лог: $log" }
Write-Host "Готово: $project/Builds/Windows/RLC Lab.exe"
