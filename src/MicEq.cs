using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace HamRadioLink
{
    // Égaliseur paramétrique du micro du FTdx10 (menus EX 03 03 02…19 : « PRMTRC EQ » PROC éteint, « P PRMTRC EQ » PROC allumé) et filtre d'émission SSB (EX 01 01 12).
    // Fenêtre non bloquante, ouverte par un clic droit sur MIC EQ. Chaque réglage part au poste dès qu'on le change ; la courbe montre la correction cumulée des trois bandes (indicative).
    public class MicEqForm : Form
    {
        static MicEqForm inst;
        public static void Open(Rig rig, Form owner)
        {
            if (inst != null && !inst.IsDisposed) { inst.WindowState = FormWindowState.Normal; inst.BringToFront(); inst.Activate(); return; }
            inst = new MicEqForm(rig); inst.Show(owner);
        }

        // bandes : fréquences proposées (Hz) pour chaque valeur 01..n (00 = arrêt)
        static readonly int[] Count = { 7, 9, 18 }, First = { 100, 700, 1500 };
        static int FreqOf(int band, int v) { return v <= 0 ? 0 : First[band] + 100 * (v - 1); }
        static readonly string[] BpfNames = { "50 - 3050", "100 - 2900", "200 - 2800", "300 - 2700", "400 - 2600" };
        static readonly int[][] BpfRange = { new[] { 50, 3050 }, new[] { 100, 2900 }, new[] { 200, 2800 }, new[] { 300, 2700 }, new[] { 400, 2600 } };

        readonly Rig rig; readonly Plot plot; readonly ComboBox[] cf = new ComboBox[3]; readonly NumericUpDown[] ng = new NumericUpDown[3], nb = new NumericUpDown[3];
        readonly RadioButton rOff, rOn, rLines, rBars; readonly Button[] bpf = new Button[5]; readonly Label status;
        readonly int[][][] cur = new int[2][][], old = new int[2][][];   // [jeu: 0 PROC éteint, 1 allumé][bande][freq, gain, bw]
        int bpfSel = 3; bool loading = true, busy; int set;

        static readonly Color Back = Color.FromArgb(58, 58, 58), Edit = Color.Black, Txt = Color.FromArgb(225, 225, 225);

        public MicEqForm(Rig rig)
        {
            this.rig = rig; AutoScaleMode = AutoScaleMode.None; Text = "Microphone Equalizer — " + (rig.Def != null ? rig.Def.Name : ""); FormBorderStyle = FormBorderStyle.FixedSingle; MaximizeBox = false; StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(784, 590); BackColor = Back; ForeColor = Txt; Font = new Font("Segoe UI", 8.25f); Icon = Application.OpenForms.Count > 0 ? Application.OpenForms[0].Icon : null;
            for (int s = 0; s < 2; s++) { cur[s] = new int[3][]; old[s] = new int[3][]; for (int b = 0; b < 3; b++) { cur[s][b] = new int[] { 0, 0, 5 }; old[s][b] = new int[] { 0, 0, 5 }; } }
            plot = new Plot(this) { Left = 10, Top = 10, Width = 764, Height = 376 }; Controls.Add(plot);

            Panel pb = new Panel { Left = 8, Top = 398, Width = 104, Height = 184, BackColor = Color.FromArgb(64, 64, 64) }; Controls.Add(pb);
            Label t = new Label { Text = "SSB Bandwidth", Left = 0, Top = 6, Width = 104, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.White, Font = new Font(Font, FontStyle.Bold) }; pb.Controls.Add(t);
            for (int i = 0; i < 5; i++) { int k = i; Button b = Flat(BpfNames[i], 6, 30 + i * 30, 92, 26); b.Click += delegate { SetBpf(k); }; pb.Controls.Add(b); bpf[i] = b; }

            string[] names = { "Low Range", "Mid Range", "High Range" }; Color[] cols = { Color.FromArgb(70, 140, 255), Color.FromArgb(110, 215, 255), Color.FromArgb(160, 215, 50) };
            Panel pr = new Panel { Left = 120, Top = 398, Width = 656, Height = 104, BackColor = Color.FromArgb(64, 64, 64) }; Controls.Add(pr);
            for (int b = 0; b < 3; b++)
            {
                int x = 14 + b * 216, bi = b;
                pr.Controls.Add(new Label { Text = names[b], Left = x, Top = 6, Width = 200, TextAlign = ContentAlignment.MiddleCenter, ForeColor = cols[b], Font = new Font(Font, FontStyle.Bold) });
                pr.Controls.Add(new Label { Text = "Frequency", Left = x, Top = 30, Width = 70 }); pr.Controls.Add(new Label { Text = "Gain", Left = x + 74, Top = 30, Width = 56 }); pr.Controls.Add(new Label { Text = "Bandwidth", Left = x + 134, Top = 30, Width = 66 });
                ComboBox c = new ComboBox { Left = x, Top = 50, Width = 68, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Edit, ForeColor = Txt, FlatStyle = FlatStyle.Flat }; c.Items.Add("- off -"); for (int v = 1; v <= Count[b]; v++) c.Items.Add(FreqOf(b, v).ToString()); c.SelectedIndex = 0;
                c.SelectedIndexChanged += delegate { if (!loading) Changed(bi, 0, c.SelectedIndex); }; pr.Controls.Add(c); cf[b] = c;
                NumericUpDown g = Num(x + 74, 50, 56, -20, 10); g.ValueChanged += delegate { if (!loading) Changed(bi, 1, (int)g.Value); }; pr.Controls.Add(g); ng[b] = g;
                NumericUpDown w = Num(x + 134, 50, 56, 1, 10); w.ValueChanged += delegate { if (!loading) Changed(bi, 2, (int)w.Value); }; pr.Controls.Add(w); nb[b] = w;
            }
            status = new Label { Left = 14, Top = 82, Width = 628, Height = 18, ForeColor = Color.FromArgb(170, 170, 170) }; pr.Controls.Add(status);

            Panel ps = new Panel { Left = 120, Top = 508, Width = 656, Height = 74, BackColor = Color.FromArgb(64, 64, 64) }; Controls.Add(ps);
            ps.Controls.Add(new Label { Text = "Set Microphone Equalizer with ...", Left = 10, Top = 6, Width = 220, ForeColor = Txt });
            rOff = new RadioButton { Text = "Speech Processor off", Left = 14, Top = 26, Width = 200, ForeColor = Txt }; rOn = new RadioButton { Text = "Speech Processor on", Left = 14, Top = 48, Width = 200, ForeColor = Txt }; ps.Controls.Add(rOff); ps.Controls.Add(rOn);
            ps.Controls.Add(new Label { Text = "Plot ...", Left = 250, Top = 6, Width = 80, ForeColor = Txt });
            rLines = new RadioButton { Text = "Lines", Left = 254, Top = 26, Width = 100, Checked = true, ForeColor = Txt }; rBars = new RadioButton { Text = "Bars", Left = 254, Top = 48, Width = 100, ForeColor = Txt }; ps.Controls.Add(rLines); ps.Controls.Add(rBars);
            rLines.CheckedChanged += delegate { plot.Bars = rBars.Checked; plot.Invalidate(); };
            Button restore = Flat("Restore old menu settings", 380, 40, 150, 26); restore.Click += delegate { Restore(); }; ps.Controls.Add(restore);
            Button close = Flat("Close", 540, 40, 100, 26); close.Click += delegate { Close(); }; ps.Controls.Add(close);
            Ctl pr0 = rig.Def != null ? rig.Def.Find("proc") : null; set = pr0 != null && rig.IsOn(pr0) ? 1 : 0; (set == 1 ? rOn : rOff).Checked = true;
            rOff.CheckedChanged += delegate { if (rOff.Checked) { set = 0; ShowSet(); } }; rOn.CheckedChanged += delegate { if (rOn.Checked) { set = 1; ShowSet(); } };
            ShowSet(); Shown += delegate { ReadAll(); StartPoll(); }; FormClosed += delegate { inst = null; };
        }

        // essai sur un vrai poste : change chaque réglage par les contrôles de la fenêtre, relit le poste, remet la valeur d'origine
        public static string Probe(Rig rig)
        {
            StringBuilder sb = new StringBuilder(); MicEqForm f = new MicEqForm(rig); bool ch; int bad = f.ReadSets(new[] { 0, 1 }, true, out ch); sb.AppendLine("lecture : " + bad + " échec(s)");
            for (int s = 0; s < 2; s++) { f.set = s; f.ShowSet(); for (int b = 0; b < 3; b++) foreach (int w in new[] { 1, 2 })
            {
                int o = f.cur[s][b][w], n = w == 1 ? (o <= 5 ? o + 4 : o - 4) : (o <= 5 ? o + 3 : o - 3); NumericUpDown ctl = w == 1 ? f.ng[b] : f.nb[b];
                ctl.Value = n; Thread.Sleep(600); string a = Addr(s, b, w), ans = rig.AskPrefix(a + ";", a, 1200); int v; bool ok = Dec(w, a, ans, out v) && v == n;
                sb.AppendLine((ok ? "OK    " : "ECHEC ") + a + " : écrit " + Enc(w, n) + ", relu " + (ans ?? "(rien)"));
                ctl.Value = o; Thread.Sleep(600);
            } }
            return sb.ToString();
        }
        // image de la fenêtre avec des réglages d'exemple (--miceqshot=fichier.png)
        public static void Shot(string path)
        {
            Rig r = new Rig(); MicEqForm f = new MicEqForm(r); f.StartPosition = FormStartPosition.Manual; f.Location = new Point(-3000, 0); f.Show();
            f.cur[0][0] = new[] { 3, 6, 4 }; f.cur[0][1] = new[] { 4, -4, 6 }; f.cur[0][2] = new[] { 8, 5, 3 }; f.ShowSet(); try { f.Banner("Commande non acceptée par le poste (-20 dB) — problème de firmware connu.\r\nEntrez la valeur par le menu du poste : OPERATION SETTING > TX AUDIO."); } catch (Exception ex) { System.IO.File.WriteAllText(path + ".err", ex.ToString()); } Application.DoEvents(); Thread.Sleep(300); Application.DoEvents();
            using (Bitmap b = new Bitmap(f.Width, f.Height)) { f.DrawToBitmap(b, new Rectangle(0, 0, f.Width, f.Height)); b.Save(path, System.Drawing.Imaging.ImageFormat.Png); }
            f.Close();
        }

        static Button Flat(string text, int x, int y, int w, int h)
        {
            Button b = new Button { Text = text, Left = x, Top = y, Width = w, Height = h, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(78, 78, 78), ForeColor = Txt, UseVisualStyleBackColor = false };
            b.FlatAppearance.BorderColor = Color.FromArgb(120, 120, 120); b.FlatAppearance.MouseOverBackColor = Color.FromArgb(98, 98, 98); return b;
        }
        static NumericUpDown Num(int x, int y, int w, int min, int max) { return new NumericUpDown { Left = x, Top = y, Width = w, Minimum = min, Maximum = max, BackColor = Edit, ForeColor = Txt, BorderStyle = BorderStyle.FixedSingle, TextAlign = HorizontalAlignment.Center }; }

        // ---- adresses EX : EX 03 03 P3 P4 ; (jeu PROC éteint : 02..10, PROC allumé : 11..19 ; FREQ, LEVEL, BWTH pour chaque bande)
        static string Addr(int s, int band, int what) { return "EX0303" + ((s == 0 ? 2 : 11) + band * 3 + what).ToString("00"); }
        static string Enc(int what, int v) { return what == 1 ? (v < 0 ? "-" : "+") + Math.Abs(v).ToString("00") : v.ToString("00"); }
        static bool Dec(int what, string a, string ans, out int v)
        {
            v = 0; if (ans == null) return false; int i = ans.IndexOf(a); if (i < 0) return false; string p = ans.Substring(i + a.Length).TrimEnd(';').Trim();
            return what == 1 ? int.TryParse(p.Replace("+", ""), out v) : int.TryParse(p, out v);
        }

        void Say(string s) { try { if (IsHandleCreated) BeginInvoke(new Action(delegate { status.Text = s; })); } catch { } }

        // lit le poste (jeux demandés + filtre SSB) ; baseline = mémorise aussi les valeurs « d'origine » ; renvoie le nombre de lectures ratées. changed = au moins une valeur différente de l'affichage
        int ReadSets(int[] sets, bool baseline, out bool changed)
        {
            int bad = 0; changed = false;
            foreach (int s in sets) for (int b = 0; b < 3; b++) for (int w = 0; w < 3; w++)
            {
                string a = Addr(s, b, w), ans = null; for (int tr = 0; tr < 3 && ans == null; tr++) ans = rig.AskPrefix(a + ";", a, 900); int v;
                if (Dec(w, a, ans, out v)) { if (cur[s][b][w] != v) changed = true; cur[s][b][w] = v; if (baseline) old[s][b][w] = v; } else bad++;
            }
            string q = rig.AskPrefix("EX010112;", "EX010112", 900); int bv; if (Dec(0, "EX010112", q, out bv) && bv >= 0 && bv < 5) { if (bpfSel != bv) changed = true; bpfSel = bv; } else bad++;
            return bad;
        }
        void ReadAll()
        {
            if (busy) return; busy = true; Say("Lecture des réglages du poste…");
            new Thread(delegate()
            {
                int bad = 0; bool ch;
                try { bad = ReadSets(new[] { 0, 1 }, true, out ch); } catch { bad++; }
                try { BeginInvoke(new Action(delegate { busy = false; ShowSet(); Say(bad == 0 ? "Réglages lus dans le poste (relus toutes les 3 s : les changements faits sur le poste apparaissent ici)." : "Certaines valeurs n'ont pas pu être lues (" + bad + ") : les valeurs affichées sont à vérifier."); })); } catch { }
            }) { IsBackground = true }.Start();
        }
        // relecture périodique : un réglage modifié sur le poste apparaît ici (sauf pendant une saisie dans la fenêtre)
        DateTime lastEdit = DateTime.MinValue; System.Windows.Forms.Timer poll;
        void StartPoll()
        {
            poll = new System.Windows.Forms.Timer { Interval = 3000 };
            poll.Tick += delegate
            {
                if (busy || rig.Loading || (DateTime.Now - lastEdit).TotalSeconds < 4) return; busy = true; int sx = set;
                new Thread(delegate()
                {
                    bool ch = false; try { ReadSets(new[] { sx }, false, out ch); } catch { }
                    try { BeginInvoke(new Action(delegate { busy = false; if (ch && (DateTime.Now - lastEdit).TotalSeconds >= 4 && sx == set) ShowSet(); })); } catch { }
                }) { IsBackground = true }.Start();
            };
            poll.Start(); FormClosed += delegate { poll.Stop(); poll.Dispose(); };
        }

        void ShowSet()
        {
            loading = true;
            for (int b = 0; b < 3; b++) { int[] c = cur[set][b]; cf[b].SelectedIndex = Math.Max(0, Math.Min(Count[b], c[0])); ng[b].Value = Math.Max(-20, Math.Min(10, c[1])); nb[b].Value = Math.Max(1, Math.Min(10, c[2])); }
            for (int i = 0; i < 5; i++) { bpf[i].BackColor = i == bpfSel ? Color.FromArgb(40, 100, 40) : Color.FromArgb(78, 78, 78); }
            loading = false; plot.Invalidate();
        }

        void Send(string addr, string p4)
        {
            if (rig.Loading) { Say("Le poste est encore en cours de lecture : réglage non envoyé."); return; }
            rig.Raw(addr + p4 + ";");
        }
        // un réglage modifié dans la fenêtre : la courbe suit tout de suite, l'envoi au poste attend 0,6 s sans nouvelle frappe (taper « -20 » ne doit pas envoyer « -2 » en route)
        readonly Dictionary<int, int[]> pend = new Dictionary<int, int[]>(); System.Windows.Forms.Timer deb;
        void Changed(int band, int what, int val)
        {
            lastEdit = DateTime.Now; cur[set][band][what] = val; plot.Invalidate(); pend[set * 9 + band * 3 + what] = new[] { set, band, what, val };
            if (deb == null) { deb = new System.Windows.Forms.Timer { Interval = 600 }; deb.Tick += delegate { deb.Stop(); Flush(); }; FormClosed += delegate { try { deb.Stop(); Flush(); } catch { } }; }
            deb.Stop(); deb.Start();
        }
        void Flush()
        {
            foreach (int[] p in new List<int[]>(pend.Values)) { Send(Addr(p[0], p[1], p[2]), Enc(p[2], p[3])); lastEdit = DateTime.Now; Verify(p[0], p[1], p[2], p[3]); }
            pend.Clear();
        }
        // après l'envoi on relit le poste : s'il n'a pas pris la valeur (il répond « ?; », cas des gains sous -10 dB), l'affichage revient à sa valeur réelle et un bandeau rouge l'explique
        void Verify(int s, int band, int what, int val)
        {
            new Thread(delegate()
            {
                Thread.Sleep(700); string a = Addr(s, band, what), ans = null; for (int tr = 0; tr < 3 && ans == null; tr++) ans = rig.AskPrefix(a + ";", a, 900); int v;
                if (Dec(what, a, ans, out v) && v != val)
                    try { BeginInvoke(new Action(delegate { cur[s][band][what] = v; if (s == set) ShowSet(); Banner("Commande non acceptée par le poste (" + (what == 1 ? val + " dB" : "valeur " + val) + ") — problème de firmware connu.\r\nEntrez la valeur par le menu du poste : OPERATION SETTING > TX AUDIO."); })); } catch { }
            }) { IsBackground = true }.Start();
        }
        Label banner; System.Windows.Forms.Timer bannerTm;
        void Banner(string text)
        {
            if (banner == null)
            {
                banner = new Label { Left = 40, Top = 14, Width = 690, Height = 52, BackColor = Color.FromArgb(190, 20, 20), ForeColor = Color.White, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter, Visible = false, BorderStyle = BorderStyle.FixedSingle };
                plot.Controls.Add(banner); banner.BringToFront(); bannerTm = new System.Windows.Forms.Timer { Interval = 15000 }; bannerTm.Tick += delegate { bannerTm.Stop(); banner.Visible = false; };
                FormClosed += delegate { bannerTm.Stop(); bannerTm.Dispose(); };
                banner.Click += delegate { bannerTm.Stop(); banner.Visible = false; };
            }
            banner.Text = text; banner.Visible = true; banner.BringToFront(); bannerTm.Stop(); bannerTm.Start(); Say(text.Replace("\r\n", " "));
        }
        void SetBpf(int i) { lastEdit = DateTime.Now; bpfSel = i; Send("EX010112", i.ToString()); ShowSet(); }
        void Restore()
        {
            for (int s = 0; s < 2; s++) for (int b = 0; b < 3; b++) for (int w = 0; w < 3; w++) { cur[s][b][w] = old[s][b][w]; Send(Addr(s, b, w), Enc(w, old[s][b][w])); }
            ShowSet(); Say("Réglages d'origine (à l'ouverture de la fenêtre) renvoyés au poste.");
        }

        // correction totale en dB à la fréquence f (Hz) : somme de trois courbes en cloche (largeur en octaves croissante avec BWTH)
        double Gain(double f)
        {
            double sum = 0;
            for (int b = 0; b < 3; b++) { int[] c = cur[set][b]; int fc = FreqOf(b, c[0]); if (fc <= 0) continue; double sig = 0.07 + 0.09 * (11 - c[2]), x = Math.Log(f / fc, 2); sum += c[1] * Math.Exp(-0.5 * x * x / (sig * sig)); }
            return sum;
        }

        class Plot : Panel
        {
            readonly MicEqForm f; public bool Bars;
            const double F0 = 50, F1 = 5500, DB = 22.5;
            public Plot(MicEqForm f) { this.f = f; DoubleBuffered = true; BackColor = Color.Black; }
            float X(double hz, Rectangle r) { return r.Left + (float)(Math.Log(hz / F0) / Math.Log(F1 / F0) * r.Width); }
            float Y(double db, Rectangle r) { return r.Top + (float)((DB - db) / (2 * DB) * r.Height); }
            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics; g.Clear(Color.FromArgb(10, 10, 10)); g.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(46, 16, Width - 60, Height - 56);
                using (SolidBrush bg = new SolidBrush(Color.Black)) g.FillRectangle(bg, r);
                int[] rg = BpfRange[Math.Max(0, Math.Min(4, f.bpfSel))];
                using (SolidBrush sh = new SolidBrush(Color.FromArgb(40, 0, 110, 0))) g.FillRectangle(sh, X(rg[0], r), r.Top, X(rg[1], r) - X(rg[0], r), r.Height);
                using (Pen edge = new Pen(Color.FromArgb(0, 150, 0), 2)) { g.DrawLine(edge, X(rg[0], r), r.Top, X(rg[0], r), r.Bottom); g.DrawLine(edge, X(rg[1], r), r.Top, X(rg[1], r), r.Bottom); }
                using (Pen gp = new Pen(Color.FromArgb(0, 80, 0))) using (Font sf = new Font("Segoe UI", 8f)) using (SolidBrush tb = new SolidBrush(Color.FromArgb(200, 200, 200)))
                {
                    double[] vg = { 50, 60, 70, 80, 90, 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 2000, 3000, 4000, 5000 };
                    foreach (double hz in vg) g.DrawLine(gp, X(hz, r), r.Top, X(hz, r), r.Bottom);
                    double[] lab = { 60, 80, 100, 200, 400, 600, 1000, 2000, 4000 };
                    foreach (double hz in lab) { string s = hz >= 4000 ? hz + " Hz" : hz.ToString(); SizeF sz = g.MeasureString(s, sf); g.DrawString(s, sf, tb, X(hz, r) - sz.Width / 2, r.Bottom + 3); }
                    for (int d = -20; d <= 20; d += 5) { g.DrawLine(gp, r.Left, Y(d, r), r.Right, Y(d, r)); string s = d + " dB"; SizeF sz = g.MeasureString(s, sf); g.DrawString(s, sf, tb, r.Left - sz.Width - 2, Y(d, r) - sz.Height / 2); }
                    string bw = "Bandwidth " + (rg[1] - rg[0]) + " Hz"; SizeF bs = g.MeasureString(bw, sf); using (SolidBrush gb = new SolidBrush(Color.FromArgb(0, 170, 0))) g.DrawString(bw, sf, gb, (X(rg[0], r) + X(rg[1], r)) / 2 - bs.Width / 2, r.Bottom - bs.Height - 4);
                }
                using (Pen zero = new Pen(Color.FromArgb(255, 90, 0), 2)) g.DrawLine(zero, r.Left, Y(0, r), r.Right, Y(0, r));
                g.SetClip(r);
                if (!Bars)
                {
                    List<PointF> pts = new List<PointF>(); for (int i = 0; i <= 400; i++) { double hz = F0 * Math.Pow(F1 / F0, i / 400.0); pts.Add(new PointF(X(hz, r), Y(Math.Max(-DB, Math.Min(DB, f.Gain(hz))), r))); }
                    using (Pen cp = new Pen(Color.FromArgb(255, 120, 20), 2.2f)) g.DrawLines(cp, pts.ToArray());
                }
                else
                {
                    int n = 48; using (SolidBrush bb = new SolidBrush(Color.FromArgb(220, 255, 110, 20)))
                        for (int i = 0; i < n; i++) { double h0 = F0 * Math.Pow(F1 / F0, i / (double)n), h1 = F0 * Math.Pow(F1 / F0, (i + 1) / (double)n), v = f.Gain(Math.Sqrt(h0 * h1)); float y0 = Y(0, r), y1 = Y(v, r); g.FillRectangle(bb, X(h0, r) + 1, Math.Min(y0, y1), Math.Max(1, X(h1, r) - X(h0, r) - 2), Math.Max(1, Math.Abs(y1 - y0))); }
                }
                g.ResetClip(); using (Pen bd = new Pen(Color.FromArgb(70, 70, 70))) g.DrawRectangle(bd, r);
            }
        }
    }
}
