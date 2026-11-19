$enc = New-Object Text.UTF8Encoding($false)
$sb = New-Object Text.StringBuilder
[void]$sb.AppendLine("using System.Collections.Generic;`nnamespace HamRadioLink`n{`n    // Généré par tmp_gen.ps1 depuis catalogues\*.new.cat (commande|nom|format d'envoi|format de lecture). Source : manuels CAT officiels.`n    public static class Catalogs`n    {`n        public static readonly Dictionary<string, string> D = new Dictionary<string, string>();`n        static Catalogs()`n        {")
foreach ($f in Get-ChildItem (Join-Path (Split-Path -Parent $PSScriptRoot) "catalogues/*.new.cat")) {
  $key = $f.BaseName -replace '\.new$',''
  $lines = [IO.File]::ReadAllLines($f.FullName, [Text.Encoding]::UTF8) | % { $_.TrimStart([char]0xFEFF) } | ? { $_ -match '\|' } | % { $p = $_.Split('|'); ($p[0],$p[1],$p[2],$p[3]) -join '|' }
  [void]$sb.AppendLine('            D["' + $key + '"] = @"' + "`n" + (($lines -join "`n").Replace('"','""')) + "`n" + '";')
}
[void]$sb.AppendLine("        }`n    }`n}")
[IO.File]::WriteAllText((Join-Path (Split-Path -Parent $PSScriptRoot) "src/Catalogs.cs"), $sb.ToString(), $enc)
