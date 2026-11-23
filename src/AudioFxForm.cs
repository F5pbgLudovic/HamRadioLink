using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace HamRadioLink
{
    // Égaliseur audio 10 bandes sur le PC : émission (micro vers le poste, avec compresseur / limiteur) et réception (son du poste rejoué sur le PC).
    // Fenêtre non bloquante ; chaque réglage agit tout de suite sur le son. Ouverte par un clic droit sur PTT (émission) ou sur le volume AF (réception).
    public class AudioFxForm : Form
    {
        static AudioFxForm inst;
        public static void Open(Form owner, bool tx)
        {
            if (inst != null && !inst.IsDisposed) { inst.Pick(tx); inst.WindowState = FormWindowState.Normal; inst.BringToFront(); inst.Activate(); return; }
            inst = new AudioFxForm(); inst.Pick(tx); inst.Show(owner);
        }

        static readonly Color Back = Color.FromArgb(58, 58, 58), Edit = Color.Black, Txt = Color.FromArgb(225, 225, 225), Pan = Color.FromArgb(64, 64, 64);
        static readonly string[] PresetNames = { "À plat", "Voix claire (présence)", "Graves coupés", "Chaleureux", "Aigus adoucis", "DX (médium renforcé)" };
        static readonly double[][] Presets = {
            new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new double[] { -6, -5, -3, -1, 0, 1, 3, 4, 2, 0 },
            new double[] { -12, -10, -6, -2, 0, 0, 0, 0, 0, 0 },
            new double[] { 0, 2, 4, 3, 1, 0, -1, -2, -3, -4 },
            new double[] { 0, 0, 0, 0, 0, 0, -1, -3, -6, -9 },
            new double[] { -12, -9, -5, -1, 2, 4, 4, 2, -2, -8 } };

        bool tx = true, loading;
        FxSet cur;
        readonly RadioButton rTx, rRx; readonly CheckBox cEq, cComp; readonly TrackBar[] tb = new TrackBar[10]; readonly Label[] lv = new Label[10];
        readonly NumericUpDown nThr, nRatio, nMake, nCeil, nAtt, nRel; readonly Panel comp; readonly Plot plot; readonly Label status, gr; readonly ComboBox presets;
        readonly Timer saveT = new Timer { Interval = 800 }, meterT = new Timer { Interval = 100 };

        public AudioFxForm()
        {
            AutoScaleMode = AutoScaleMode.None; Text = "Égaliseur audio 10 bandes — HamRadioLink"; FormBorderStyle = FormBorderStyle.FixedSingle; MaximizeBox = false; StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(760, 640); BackColor = Back; ForeColor = Txt; Font = new Font("Segoe UI", 8.25f); Icon = Application.OpenForms.Count > 0 ? Application.OpenForms[0].Icon : null;
            rTx = new RadioButton { Text = "Émission (micro vers le poste)", Left = 14, Top = 10, Width = 240, Checked = true }; rRx = new RadioButton { Text = "Réception (son du poste sur le PC)", Left = 270, Top = 10, Width = 260 };
            Controls.Add(rTx); Controls.Add(rRx); rTx.CheckedChanged += delegate { if (!loading && rTx.Checked) Pick(true); }; rRx.CheckedChanged += delegate { if (!loading && rRx.Checked) Pick(false); };
            cEq = new CheckBox { Text = "Égaliseur actif", Left = 14, Top = 38, Width = 160, ForeColor = Color.White, Font = new Font(Font, FontStyle.Bold) }; Controls.Add(cEq);
            presets = new ComboBox { Left = 400, Top = 36, Width = 190, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Edit, ForeColor = Txt, FlatStyle = FlatStyle.Flat };
            presets.Items.Add("Préréglage…"); foreach (string p in PresetNames) presets.Items.Add(p); presets.SelectedIndex = 0; Controls.Add(presets);
            Button flat = Flat("À plat", 600, 35, 70, 24); flat.Click += delegate { Apply(Presets[0]); }; Controls.Add(flat);
            presets.SelectedIndexChanged += delegate { int k = presets.SelectedIndex - 1; if (k >= 0) { Apply(Presets[k]); presets.SelectedIndex = 0; } };
            plot = new Plot(this) { Left = 10, Top = 68, Width = 740, Height = 200 }; Controls.Add(plot);

            Panel pb = new Panel { Left = 10, Top = 274, Width = 740, Height = 190, BackColor = Pan }; Controls.Add(pb);
            for (int i = 0; i < 10; i++)
            {
                int x = 14 + i * 72, k = i;
                lv[i] = new Label { Left = x, Top = 6, Width = 64, Height = 16, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.FromArgb(140, 215, 255), Text = "0,0 dB" }; pb.Controls.Add(lv[i]);
                TrackBar t = new TrackBar { Orientation = Orientation.Vertical, Minimum = -120, Maximum = 120, TickFrequency = 60, SmallChange = 5, LargeChange = 20, Left = x + 10, Top = 24, Width = 45, Height = 140, TickStyle = TickStyle.Both };
                t.ValueChanged += delegate { if (!loading) { Changed(k, -t.Value / 10.0); } }; t.MouseDown += delegate(object s, MouseEventArgs e) { if (e.Button == MouseButtons.Right) { t.Value = 0; } };
                pb.Controls.Add(t); tb[i] = t;
                int f = FxCfg.Freq[i]; pb.Controls.Add(new Label { Left = x, Top = 168, Width = 64, Height = 16, TextAlign = ContentAlignment.MiddleCenter, Text = f >= 1000 ? (f / 1000) + " kHz" : f + " Hz" });
            }

            comp = new Panel { Left = 10, Top = 470, Width = 740, Height = 110, BackColor = Pan }; Controls.Add(comp);
            cComp = new CheckBox { Text = "Compresseur / limiteur actif", Left = 14, Top = 6, Width = 260, ForeColor = Color.White, Font = new Font(Font, FontStyle.Bold) }; comp.Controls.Add(cComp);
            gr = new Label { Left = 400, Top = 8, Width = 320, Height = 18, TextAlign = ContentAlignment.MiddleRight, ForeColor = Color.FromArgb(160, 215, 50), Text = "Réduction : 0,0 dB" }; comp.Controls.Add(gr);
            nThr = Num(comp, "Seuil (dB)", 14, -40, 0, 0); nRatio = Num(comp, "Rapport (:1)", 134, 1, 20, 1); nMake = Num(comp, "Gain de sortie (dB)", 254, 0, 24, 1);
            nCeil = Num(comp, "Plafond limiteur (dB)", 374, -12, 0, 1); nAtt = Num(comp, "Attaque (ms)", 494, 1, 100, 0); nRel = Num(comp, "Relâche (ms)", 614, 20, 1500, 0);

            status = new Label { Left = 14, Top = 590, Width = 560, Height = 40, ForeColor = Color.FromArgb(180, 180, 180) }; Controls.Add(status);
            Button close = Flat("Fermer", 650, 600, 100, 26); close.Click += delegate { Close(); }; Controls.Add(close);
            cEq.CheckedChanged += delegate { if (!loading) { cur.EqOn = cEq.Checked; Push(); } }; cComp.CheckedChanged += delegate { if (!loading) { cur.CompOn = cComp.Checked; Push(); } };
            nThr.ValueChanged += delegate { Pc(); }; nRatio.ValueChanged += delegate { Pc(); }; nMake.ValueChanged += delegate { Pc(); }; nCeil.ValueChanged += delegate { Pc(); }; nAtt.ValueChanged += delegate { Pc(); }; nRel.ValueChanged += delegate { Pc(); };
            saveT.Tick += delegate { saveT.Stop(); Cfg.Save(); }; meterT.Tick += delegate { gr.Text = "Réduction : " + FxCfg.GrTx.ToString("0.0") + " dB"; }; meterT.Start();
            FormClosed += delegate { meterT.Stop(); if (saveT.Enabled) { saveT.Stop(); Cfg.Save(); } inst = null; };
        }

        void Pick(bool t)
        {
            tx = t; loading = true; (t ? rTx : rRx).Checked = true;
            cur = FxCfg.Clone(FxCfg.Get(tx)); cEq.Checked = cur.EqOn; cComp.Checked = cur.CompOn;
            for (int i = 0; i < 10; i++) { tb[i].Value = (int)Math.Round(-cur.G[i] * 10); lv[i].Text = Db(cur.G[i]); }
            SetN(nThr, cur.Thr); SetN(nRatio, cur.Ratio); SetN(nMake, cur.Makeup); SetN(nCeil, cur.Ceil); SetN(nAtt, cur.Att); SetN(nRel, cur.Rel);
            comp.Visible = tx; gr.Visible = tx; loading = false; plot.Invalidate();
            status.Text = tx ? "Appliqué au micro envoyé au poste : actif quand « Micro du PC vers le poste » est coché (clic droit sur PTT). Clic droit sur un curseur = remise à 0."
                : "Appliqué au son du poste rejoué sur le PC : actif quand « Écouter la réception du poste sur le PC » est coché (clic droit sur le volume AF). Clic droit sur un curseur = remise à 0.";
        }
        static string Db(double g) { return (g > 0 ? "+" : "") + g.ToString("0.0") + " dB"; }
        static void SetN(NumericUpDown n, double v) { n.Value = (decimal)Math.Max((double)n.Minimum, Math.Min((double)n.Maximum, v)); }
        void Changed(int i, double g) { cur.G[i] = g; lv[i].Text = Db(g); Push(); }
        void Pc() { if (loading) return; cur.Thr = (double)nThr.Value; cur.Ratio = (double)nRatio.Value; cur.Makeup = (double)nMake.Value; cur.Ceil = (double)nCeil.Value; cur.Att = (double)nAtt.Value; cur.Rel = (double)nRel.Value; Push(); }
        void Apply(double[] g) { loading = true; for (int i = 0; i < 10; i++) { cur.G[i] = g[i]; tb[i].Value = (int)Math.Round(-g[i] * 10); lv[i].Text = Db(g[i]); } loading = false; Push(); }
        // l'instantané modifié devient celui du fil audio ; un enregistrement sur disque suit 0,8 s après le dernier geste
        void Push() { FxSet s = FxCfg.Clone(cur); FxCfg.Store(tx, s); plot.Invalidate(); saveT.Stop(); saveT.Start(); }

        static Button Flat(string t, int x, int y, int w, int h) { Button b = new Button { Text = t, Left = x, Top = y, Width = w, Height = h, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(80, 80, 80), ForeColor = Txt }; b.FlatAppearance.BorderColor = Color.FromArgb(120, 120, 120); return b; }
        NumericUpDown Num(Panel p, string label, int x, int min, int max, int dec)
        {
            p.Controls.Add(new Label { Text = label, Left = x, Top = 36, Width = 112 });
            NumericUpDown n = new NumericUpDown { Left = x, Top = 56, Width = 80, Minimum = min, Maximum = max, DecimalPlaces = dec, Increment = dec > 0 ? 0.5m : 1, BackColor = Edit, ForeColor = Txt }; p.Controls.Add(n); return n;
        }

        // courbe de réponse de l'égaliseur : 20 Hz à 20 kHz (échelle logarithmique), ±12 dB
        class Plot : Control
        {
            readonly AudioFxForm f;
            public Plot(AudioFxForm f) { this.f = f; SetStyle(ControlStyles.UserPaint |ControlStyles.OptimizedDoubleBuffer, true); }
            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics; g.Clear(Color.Black); g.SmoothingMode = SmoothingMode.AntiAlias; FxSet s = f.cur; if (s == null) return;
                float L = 44, R = Width - 12, T = 12, B = Height - 24, W = R - L, H = B - T;
                using (Pen grid = new Pen(Color.FromArgb(50, 50, 50))) using (Brush tx = new SolidBrush(Color.FromArgb(150, 150, 150)))
                {
                    for (int d = -12; d <= 12; d += 6) { float y = T + H * (12 - d) / 24f; g.DrawLine(grid, L, y, R, y); g.DrawString((d > 0 ? "+" : "") + d, Font, tx, 6, y - 7); }
                    foreach (int fr in new[] { 31, 63, 125, 250, 500, 1000, 2000, 4000, 8000, 16000 }) { float x = L + W * (float)(Math.Log10(fr / 20.0) / 3.0); g.DrawLine(grid, x, T, x, B); string t = fr >= 1000 ? (fr / 1000) + "k" : fr.ToString(); g.DrawString(t, Font, tx, x - 8, B + 4); }
                }
                PointF[] pts = new PointF[300];
                for (int i = 0; i < pts.Length; i++) { double fr = 20 * Math.Pow(1000, i / (double)(pts.Length - 1)); double db = Math.Max(-12, Math.Min(12, s.EqOn ? FxCfg.Response(s, fr, 48000) : 0)); pts[i] = new PointF(L + W * i / (pts.Length - 1), T + H * (float)((12 - db) / 24.0)); }
                using (Pen p = new Pen(f.tx ? Color.FromArgb(70, 140, 255) : Color.FromArgb(160, 215, 50), 2.2f)) g.DrawLines(p, pts);
                for (int i = 0; i < 10; i++) { float x = L + W * (float)(Math.Log10(FxCfg.Freq[i] / 20.0) / 3.0), y = T + H * (float)((12 - Math.Max(-12, Math.Min(12, s.G[i]))) / 24.0); g.FillEllipse(Brushes.White, x - 3, y - 3, 6, 6); }
            }
        }
    }
}
