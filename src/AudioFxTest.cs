using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace HamRadioLink
{
    // --fxtest=dossier : vérifie l'égaliseur (gain mesuré sur sinus = gain réglé), le compresseur / limiteur, et fait une image de la fenêtre.
    static class AudioFxTest
    {
        static double Rms(FxChain c, double f, int fs, bool tx, double amp)
        {
            double sum = 0; int n = fs, cnt = 0;
            for (int i = 0; i < n; i++) { double x = amp * Math.Sin(2 * Math.PI * f * i / fs), l = x, r = x; c.Process(fs, ref l, ref r); if (i > n / 2) { sum += l * l; cnt++; } }
            return 20 * Math.Log10(Math.Sqrt(sum / cnt) / (amp / Math.Sqrt(2)));
        }
        public static void Run(string dir)
        {
            Directory.CreateDirectory(dir); StringBuilder sb = new StringBuilder(); int bad = 0;
            Action<bool, string> chk = delegate(bool ok, string m) { sb.AppendLine((ok ? "OK    " : "ECHEC ") + m); if (!ok) bad++; };
            FxSet s = new FxSet(); s.EqOn = true; s.G[4] = 9; s.G[1] = -9; FxCfg.Store(false, s);   // +9 dB à 1 kHz, -9 dB à 125 Hz
            FxChain c = new FxChain(false); double g1 = Rms(c, 800, 48000, false, 0.1), g2 = Rms(new FxChain(false), 100, 48000, false, 0.1), g3 = Rms(new FxChain(false), 4000, 48000, false, 0.1);
            chk(Math.Abs(g1 - 9) < 0.5, "800 Hz : " + g1.ToString("0.00") + " dB (attendu +9)"); chk(Math.Abs(g2 + 9) < 0.5, "100 Hz : " + g2.ToString("0.00") + " dB (attendu -9)"); chk(Math.Abs(g3) < 1.5, "4 kHz : " + g3.ToString("0.00") + " dB (attendu ~0)");
            chk(Math.Abs(FxCfg.Response(s, 800, 48000) - 9) < 0.3, "courbe à 800 Hz : " + FxCfg.Response(s, 800, 48000).ToString("0.00"));
            s = new FxSet(); s.CompOn = true; s.Thr = -20; s.Ratio = 4; s.Makeup = 0; s.Ceil = -3; FxCfg.Store(true, s);
            FxChain t = new FxChain(true); double low = Rms(t, 1000, 48000, true, 0.05), high = Rms(new FxChain(true), 1000, 48000, true, 0.8);
            chk(Math.Abs(low) < 0.5, "signal sous le seuil inchangé : " + low.ToString("0.00") + " dB"); chk(high < -3, "signal fort réduit : " + high.ToString("0.00") + " dB");
            double peak = 0; FxChain t2 = new FxChain(true); for (int i = 0; i < 48000; i++) { double x = 0.99 * Math.Sin(2 * Math.PI * 300 * i / 48000), l = x, r = x; t2.Process(48000, ref l, ref r); if (i > 4800) peak = Math.Max(peak, Math.Abs(l)); }
            chk(peak <= Math.Pow(10, -3 / 20.0) * 1.02, "limiteur : crête " + (20 * Math.Log10(peak)).ToString("0.0") + " dBFS (plafond -3)");
            s = new FxSet(); s.GateOn = true; s.GThr = -40; s.GRange = -30; s.GHold = 50; FxCfg.Store(true, s);
            double gLow = Rms(new FxChain(true), 1000, 48000, true, 0.003), gHigh = Rms(new FxChain(true), 1000, 48000, true, 0.2);
            chk(gLow < -20, "noise gate : signal sous le seuil atténué de " + gLow.ToString("0.0") + " dB"); chk(Math.Abs(gHigh) < 0.5, "noise gate : signal au-dessus du seuil inchangé (" + gHigh.ToString("0.00") + " dB)");
            FxSet ps = new FxSet(); ps.EqOn = true; ps.G[2] = -6; ps.G[5] = 6; ps.G[7] = 3; FxCfg.Store(true, ps);
            AudioFxForm f = null; try { f = new AudioFxForm(); f.StartPosition = FormStartPosition.Manual; f.Location = new Point(-3000, 0); f.Show(); typeof(AudioFxForm).GetMethod("Pick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(f, new object[] { true });
                Application.DoEvents(); using (Bitmap b = new Bitmap(f.Width, f.Height)) { f.DrawToBitmap(b, new Rectangle(0, 0, f.Width, f.Height)); b.Save(Path.Combine(dir, "fenetre-egaliseur.png"), System.Drawing.Imaging.ImageFormat.Png); } chk(true, "fenêtre construite"); }
            catch (Exception ex) { chk(false, "fenêtre : " + ex.Message); } finally { if (f != null) f.Dispose(); }
            sb.AppendLine(bad == 0 ? "TOUT OK" : bad + " échec(s)"); File.WriteAllText(Path.Combine(dir, "fxtest.txt"), sb.ToString());
        }
    }
}
