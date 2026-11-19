# Capture d'une fenetre reelle : powershell -File tools\shot.ps1 -Radio ic7610 -Out capture.png  (exe = dist/HamRadioLink.exe, poste virtuel)
param([string]$Radio, [string]$Out)
$root = Split-Path -Parent $PSScriptRoot
$d = Join-Path $env:TEMP 'hrl-shot'; New-Item -ItemType Directory -Force $d | Out-Null
$p = Start-Process -FilePath (Join-Path $root 'dist/HamRadioLink.exe') -ArgumentList "--radio=$Radio","--virtual","--data=$d" -PassThru
Start-Sleep -Seconds 5
powershell.exe -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'cap.ps1') -Out $Out
Stop-Process -Id $p.Id -Force
