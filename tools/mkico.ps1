Add-Type -AssemblyName System.Drawing
function Draw([int]$n) {
  $bm = New-Object Drawing.Bitmap $n,$n
  $g = [Drawing.Graphics]::FromImage($bm); $g.SmoothingMode='AntiAlias'; $g.Clear([Drawing.Color]::Transparent)
  $s = $n/256.0
  function RR($g,$br,$x,$y,$w,$h,$r){ $p=New-Object Drawing.Drawing2D.GraphicsPath; $d=2*$r
    $p.AddArc($x,$y,$d,$d,180,90);$p.AddArc($x+$w-$d,$y,$d,$d,270,90);$p.AddArc($x+$w-$d,$y+$h-$d,$d,$d,0,90);$p.AddArc($x,$y+$h-$d,$d,$d,90,90);$p.CloseFigure();$g.FillPath($br,$p) }
  $c = { param($r,$gg,$b) [Drawing.Color]::FromArgb(255,$r,$gg,$b) }
  # antenne
  $pen = New-Object Drawing.Pen (& $c 200 200 210), ([single](9*$s)); $pen.StartCap='Round'; $pen.EndCap='Round'
  $g.DrawLine($pen, [single](205*$s),[single](70*$s),[single](235*$s),[single](12*$s))
  # boitier
  $b1 = New-Object Drawing.SolidBrush (& $c 30 36 48); RR $g $b1 (10*$s) (62*$s) (236*$s) (172*$s) (26*$s)
  $b2 = New-Object Drawing.SolidBrush (& $c 58 68 88); RR $g $b2 (20*$s) (72*$s) (216*$s) (152*$s) (20*$s)
  # afficheur
  $b3 = New-Object Drawing.SolidBrush (& $c 12 16 22); RR $g $b3 (34*$s) (86*$s) (188*$s) (62*$s) (10*$s)
  $amb = New-Object Drawing.SolidBrush (& $c 255 176 32)
  $f = New-Object Drawing.Font 'Consolas', ([single](40*$s)), ([Drawing.FontStyle]::Bold), ([Drawing.GraphicsUnit]::Pixel)
  if ($n -ge 32) { $g.DrawString('14.2', $f, $amb, [single](44*$s), [single](94*$s)) } else { $g.FillRectangle($amb,[single](50*$s),[single](104*$s),[single](150*$s),[single](30*$s)) }
  # gros bouton
  $kb = New-Object Drawing.SolidBrush (& $c 210 214 224); $g.FillEllipse($kb,[single](140*$s),[single](158*$s),[single](78*$s),[single](78*$s))
  $kb2 = New-Object Drawing.SolidBrush (& $c 90 98 116); $g.FillEllipse($kb2,[single](152*$s),[single](170*$s),[single](54*$s),[single](54*$s))
  $pen2 = New-Object Drawing.Pen (& $c 255 176 32), ([single](6*$s)); $g.DrawLine($pen2,[single](179*$s),[single](176*$s),[single](179*$s),[single](192*$s))
  # petits boutons + haut-parleur
  $g.FillEllipse($kb,[single](40*$s),[single](170*$s),[single](30*$s),[single](30*$s)); $g.FillEllipse($kb,[single](82*$s),[single](170*$s),[single](30*$s),[single](30*$s))
  $gp = New-Object Drawing.Pen (& $c 30 36 48), ([single](5*$s)); foreach($i in 0..2){ $y=(206+$i*8)*$s; $g.DrawLine($gp,[single](40*$s),[single]$y,[single](112*$s),[single]$y) }
  $g.Dispose(); $ms = New-Object IO.MemoryStream; $bm.Save($ms,[Drawing.Imaging.ImageFormat]::Png); $bm.Dispose(); ,$ms.ToArray()
}
$sizes = 16,32,48,64,256
$imgs = foreach($n in $sizes){ ,(Draw $n) }
$out = New-Object IO.MemoryStream; $w = New-Object IO.BinaryWriter $out
$w.Write([uint16]0);$w.Write([uint16]1);$w.Write([uint16]$sizes.Count)
$off = 6 + 16*$sizes.Count
for($i=0;$i -lt $sizes.Count;$i++){ $n=$sizes[$i]; $d=$imgs[$i]; $w.Write([byte]($n % 256));$w.Write([byte]($n % 256));$w.Write([byte]0);$w.Write([byte]0);$w.Write([uint16]1);$w.Write([uint16]32);$w.Write([uint32]$d.Length);$w.Write([uint32]$off); $off += $d.Length }
foreach($d in $imgs){ $w.Write($d) }
[IO.File]::WriteAllBytes((Join-Path (Split-Path -Parent $PSScriptRoot) 'src/HamRadioLink.ico'),$out.ToArray())
'ok'
