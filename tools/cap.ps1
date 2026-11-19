param([string]$Out = (Join-Path $env:TEMP "hrl-win.png"))
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing @"
using System; using System.Runtime.InteropServices; using System.Drawing;
public class WinCap {
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint f);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
  public static void Cap(IntPtr h, string path) { RECT r; GetWindowRect(h, out r); Bitmap b = new Bitmap(r.R - r.L, r.B - r.T); Graphics g = Graphics.FromImage(b); IntPtr dc = g.GetHdc(); PrintWindow(h, dc, 2); g.ReleaseHdc(dc); b.Save(path); }
}
"@
$p = Get-Process HamRadioLink | Where-Object { $_.MainWindowTitle -like "*HamRadioLink*" } | Select-Object -First 1
[WinCap]::Cap($p.MainWindowHandle, $Out)
"saved " + $Out
