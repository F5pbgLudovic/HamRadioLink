# Compile HamRadioLink en un exe autonome avec le compilateur fourni avec Windows (.NET Framework 4.x, C# 5). Rien à installer.
# Pour -Out dist : l'exe precedent est renomme HamRadioLink-<ancienne version>.exe (renommer un exe ouvert est permis).
param([string]$Out = 'dist')
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$csc  = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
$bin  = [IO.Path]::Combine($root, $Out)
New-Item -ItemType Directory -Force $bin | Out-Null
$ver = ([regex]::Match((Get-Content (Join-Path $root 'src\MainForm.cs') -Raw), 'Version = "([^"]+)"')).Groups[1].Value
$exe = "$bin\HamRadioLink.exe"; $new = "$bin\HamRadioLink.new"; $vf = "$bin\version.txt"
$src = Get-ChildItem (Join-Path $root 'src') -Filter *.cs -Recurse | ForEach-Object { $_.FullName }
$a = @('/nologo', '/target:winexe', '/optimize+', '/platform:anycpu', '/codepage:65001', "/out:$new",
  '/r:System.dll', '/r:System.Core.dll', '/r:System.Drawing.dll', '/r:System.Windows.Forms.dll', '/r:System.IO.Compression.dll', '/r:System.IO.Compression.FileSystem.dll', '/r:System.Xml.dll', "/resource:$root\doc\civ\ic7760.civ,ic7760.civ", "/resource:$root\vendor\com0com\Setup_com0com_v3.0.0.0_W7_x64_signed.exe,com0com-x64.exe", "/resource:$root\vendor\com0com\Setup_com0com_v3.0.0.0_W7_x86_signed.exe,com0com-x86.exe", "/win32manifest:$root\src\app.manifest")
foreach ($f in Get-ChildItem (Join-Path $root 'doc\smeter') -Filter *.png -ErrorAction SilentlyContinue) { $a += "/resource:$($f.FullName),smeter-$($f.Name)" }   # fonds du S-mètre (FTdx10), un par fonction de METER
$ico = Join-Path $root 'src\HamRadioLink.ico'
if (Test-Path $ico) { $a += "/win32icon:$ico" }
& $csc @a @src
if ($LASTEXITCODE -ne 0) { throw 'Compilation échouée' }
if (Test-Path $exe) {
  $old = if (Test-Path $vf) { (Get-Content $vf -Raw).Trim() } else { (Get-Item $exe).LastWriteTime.ToString('yyyyMMdd_HHmm') }
  $dst = "$bin\HamRadioLink-$old.exe"; if (Test-Path $dst) { $dst = "$bin\HamRadioLink-$old-" + (Get-Date).ToString('HHmmss') + '.exe' }
  Move-Item $exe $dst
}
Move-Item $new $exe
$ver | Set-Content $vf
"OK -> $exe ($ver)"
