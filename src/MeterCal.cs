using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace HamRadioLink
{
    // Échelles des vumètres d'émission (Po, SWR, ALC, COMP, Id, Vdd) et étalonnage du wattmètre.
    // Réglages par poste : cal.<poste>.pmax (puissance max de l'émetteur, W) et cal.<poste>.po (points « brut:watts;… » relevés par l'étalonnage).
    public class Sc
    {
        public Func<double, double> Frac;        // valeur brute (0..Max) -> position 0..1 sur l'échelle
        public Func<double, string> Text;        // valeur brute -> texte affiché
        public string[] Ticks; public double[] Pos;
        public double Hot = 0.8;                 // position à partir de laquelle les segments passent à l'orange / rouge
        public bool BlueRed;                     // ALC : bleu jusqu'au milieu, rouge au-delà
        public bool NoHot;                       // jamais de zone chaude (Vdd)
    }

    public static class MeterCal
    {
        // postes où les échelles ci-dessous sont actives (les autres gardent l'échelle en % d'origine tant que l'essai n'est pas validé)
        static readonly string[] MeterIds = { "po", "swr", "alc", "comp", "idd", "vdd" };
        // Réglages d'origine du FT-2000 (étalonnage relevé sur le poste) : utilisés tant que l'utilisateur n'a rien enregistré
        static string DefPts(string key, string id)
        {
            if (key == "ftdx10" && id == "width") return "1:300;13:2400;23:4000";   // étalonnage de WIDTH relevé sur le FTdx10
            if (key != "ft2000") return "";
            switch (id)
            {
                case "po": return "54:10;69:20;91:30;102:40;115:50;122:60;132:70;143:80;147:90;160:100;176:120;183:140;196:160;206:170;224:200";
                case "contf": return "1:100;20:2000;40:4000"; case "width": return "1:200;16:2400;31:4000"; case "rfpwr": return "1:10;58:20;70:30;79:40;95:50;103:60;115:70;122:80;131:90;142:100;153:120;166:140;180:160;198:170;249:200"; case "swr": return "77:1.7"; case "alc": return "129:50"; case "idd": return "20:1.1"; case "vdd": return "195:50";
            }
            return "";
        }
        // puissance max par défaut selon le poste (à régler ensuite dans Paramètres > Réglage S-mètre puissance)
        static readonly Dictionary<string, int> pmaxCache = new Dictionary<string, int>();
        public static int DefPMax(string key)
        {
            int v; lock (pmaxCache) { if (pmaxCache.TryGetValue(key, out v)) return v; }
            if (key == "ft2000") v = 200;
            else
            {
                RadioDef d = Radios.ByKey(key); string n = d != null ? d.Name : ""; v = 100;
                if (System.Text.RegularExpressions.Regex.IsMatch(n, @"IC-77\d\d|IC-78\d\d|IC-7760|TS-990|FTdx5000|FTdx9000|FT-1000D|FT-1000 |FT-1000MP Mark-V$")) v = 200;
                else if (System.Text.RegularExpressions.Regex.IsMatch(n, @"TS-950")) v = 150;
                else if (System.Text.RegularExpressions.Regex.IsMatch(n, @"IC-705|IC-905|KX2|KX3|FTX-1")) v = 10;
                else if (System.Text.RegularExpressions.Regex.IsMatch(n, @"FT-817|FT-818|X5105|X6100|X6200")) v = 5;
                else if (System.Text.RegularExpressions.Regex.IsMatch(n, @"G90|X108G")) v = 20;
            }
            lock (pmaxCache) pmaxCache[key] = v; return v;
        }
        // ---- boutons Width (position -> Hz) et RF PWR (valeur du poste -> watts) du FT-2000 : points « valeur du poste : valeur réelle » relevés à l'étalonnage
        public static bool KnobActive(RadioDef d, Ctl c) { return d != null && c != null && c.K == Kind.Knob && (c.Id == "width" || c.Id == "pwr" || c.Id == "contf"); }
        // vrai si le poste a au moins un de ces boutons (Width, CONT, RF PWR)
        public static bool KnobAny(RadioDef d) { if (d == null) return false; foreach (string id in new string[] { "width", "contf", "pwr" }) if (KnobActive(d, d.Find(id))) return true; return false; }
        public static string KnobId(Ctl c) { return c.Id == "width" ? "width" : c.Id == "contf" ? "contf" : "rfpwr"; }
        public static string KnobText(RadioDef d, Ctl c, int v)
        {
            if (!KnobActive(d, c)) return null; List<double[]> pts = Points(d.Key, KnobId(c)); if (pts.Count == 0) return null;
            double y = c.Id == "width" ? Math.Max(200, Math.Min(4000, InterpFree(pts, v))) : c.Id == "contf" ? Math.Max(100, Math.Min(4000, InterpFree(pts, v))) : Math.Max(0, Interp(pts, v, 0.0)); if (c.Id == "pwr") y = Math.Min(y, PFull(d.Key));   // au-delà de la pleine déviation choisie, le bouton reste au maximum
            return c.Id == "width" || c.Id == "contf" ? (Math.Round(y / 10) * 10).ToString("0", Inv) + " Hz" : Math.Round(y).ToString("0", Inv) + " W";
        }
        // pleine échelle du bouton RF PWR : valeur du poste qui correspond à la pleine déviation de l'échelle Po (par défaut la puissance max) (si étalonné), sinon celle du catalogue
        public static int KnobMax(RadioDef d, Ctl c)
        {
            if (!KnobActive(d, c) || c.Id != "pwr") return c.Max; List<double[]> pts = Points(d.Key, "rfpwr"); if (pts.Count == 0) return c.Max;
            double target = PFull(d.Key); for (int v = c.Min; v <= 600; v++) if (Interp(pts, v, 0.0) >= target - 0.5) return Math.Max(c.Min + 1, v);
            return c.Max;
        }
        public static int Ver;                 // augmente à chaque changement : les vumètres reconstruisent leur échelle
        // vrai si le poste a au moins un vumètre d'émission à échelle propre (Po, SWR, ALC, COMP, Id, Vdd)
        public static bool Active(RadioDef d) { if (d == null) return false; foreach (string id in MeterIds) { Ctl c = d.Find(id); if (c != null && c.K == Kind.Meter && c.Fmt == "pct") return true; } return false; }
        public static bool Has(RadioDef d, string id) { Ctl c = d != null ? d.Find(id) : null; return c != null && c.K == Kind.Meter && c.Fmt == "pct"; }
        static CultureInfo Inv { get { return CultureInfo.InvariantCulture; } }

        public static int PMax(string key) { return Math.Max(5, Math.Min(2000, Cfg.GetInt("cal." + key + ".pmax", DefPMax(key)))); }
        // pleine déviation de l'échelle Po (W) : par défaut la puissance max ; plus petite pour agrandir le bas de l'échelle (par ex. 50 W sur un poste 200 W), sans refaire l'étalonnage
        public static int PFull(string key) { int p = PMax(key); int f = Cfg.GetInt("cal." + key + ".pfull", key == "ft2000" ? 50 : 0); return f >= 5 && f < p ? f : p; }
        public static void SetPFull(string key, int w) { if (w >= PMax(key) || w < 5) Cfg.Set("cal." + key + ".pfull", key == "ft2000" ? PMax(key).ToString() : ""); else Cfg.SetInt("cal." + key + ".pfull", w); Cfg.Save(); Ver++; }
        public static void SetPMax(string key, int w) { Cfg.SetInt("cal." + key + ".pmax", Math.Max(5, Math.Min(2000, w))); Cfg.Save(); Ver++; }
        // tension : pleine échelle du Vdd et valeur nominale (200 W = 50 V, sinon 13,8 V)
        public static double VddFull(int pmax) { return pmax >= 200 ? 50.0 : 15.0; }
        public static double VddNominal(int pmax) { return pmax >= 200 ? 50.0 : 13.8; }

        // ---- points d'étalonnage du wattmètre (brut sur 0..255 -> watts)
        public static List<double[]> Points(string key) { return Points(key, "po"); }
        public static List<double[]> Points(string key, string id)
        {
            List<double[]> l = new List<double[]>(); string s = Cfg.Get("cal." + key + "." + id, DefPts(key, id));
            foreach (string p in s.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] q = p.Split(':'); double r, w;
                if (q.Length == 2 && double.TryParse(q[0], NumberStyles.Float, Inv, out r) && double.TryParse(q[1], NumberStyles.Float, Inv, out w)) l.Add(new double[] { r, w });
            }
            l.Sort(delegate(double[] a, double[] b) { return a[0].CompareTo(b[0]); }); return l;
        }
        public static void SavePoints(string key, List<double[]> l) { SavePoints(key, "po", l); }
        public static void SavePoints(string key, string id, List<double[]> l)
        {
            List<string> p = new List<string>(); foreach (double[] x in l) p.Add(x[0].ToString("0.#", Inv) + ":" + x[1].ToString("0.#", Inv));
            Cfg.Set("cal." + key + "." + id, string.Join(";", p.ToArray())); Cfg.Save(); Ver++;
        }
        public static bool Calibrated(string key) { return Points(key).Count > 0; }
        // brut (0..255) -> watts : interpolation entre les points relevés (0 W au départ) ; sans étalonnage : linéaire jusqu'à la puissance max
        public static double Watts(List<double[]> pts, int pmax, double raw)
        {
            if (raw <= 0) return 0;
            if (pts == null || pts.Count == 0) return Math.Min(pmax, raw / 255.0 * pmax);
            double r0 = 0, w0 = 0;
            foreach (double[] p in pts)
            {
                if (raw <= p[0]) return p[0] <= r0 ? p[1] : w0 + (p[1] - w0) * (raw - r0) / (p[0] - r0);
                r0 = p[0]; w0 = p[1];
            }
            return Math.Min(pmax, w0);            // au-delà du dernier point : pleine déviation
        }
        // paliers demandés à l'étalonnage, jusqu'à la puissance max du poste (la dernière étape est toujours la puissance max)
        public static List<int> Steps(int pmax)
        {
            int[] all = { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 120, 140, 160, 170, 200 }; List<int> l = new List<int>();
            foreach (int w in all) if (w <= pmax) l.Add(w);
            if (l.Count == 0 || l[l.Count - 1] != pmax) l.Add(pmax);
            return l;
        }

        // ---- ROS : 1 au début, infini au bout, 3 au milieu (position = coefficient de réflexion (ROS-1)/(ROS+1))
        public static double SwrPos(double ratio) { if (ratio < 1) ratio = 1; return (ratio - 1) / (ratio + 1); }
        // ROS (1 à plus de 100) d'après la lecture brute 0..255 : étalonnage de l'utilisateur s'il existe (interpolé / extrapolé sur le coefficient de réflexion), sinon table d'origine
        public static double SwrRatio(string key, double raw255)
        {
            List<double[]> pts = Points(key, "swr"); if (pts.Count == 0) return RosGuard.RatioOf(raw255, 255);
            List<double[]> gp = new List<double[]>(); foreach (double[] q in pts) gp.Add(new double[] { q[0], SwrPos(q[1]) });
            double g = Math.Max(0, Math.Min(1, Interp(gp, raw255, 0.0))); return g >= 0.999 ? 1e6 : (1 + g) / (1 - g);
        }
        public static double CompDb(double f) { return f <= 0.5 ? 20 * f : 10 + (f - 0.5) * 40; }   // 0 dB au départ, 10 dB au milieu, 30 dB en pleine déviation

        // échelle d'un vumètre, ou null si l'échelle d'origine (%) reste en vigueur
        public static Sc Scale(RadioDef d, Ctl c)
        {
            if (!Active(d) || c == null || c.K != Kind.Meter || c.Fmt != "pct" || c.Max <= 0) return null;
            string key = d.Key; double mx = c.Max; int pmax = PMax(key); Sc s = new Sc();
            switch (c.Id)
            {
                case "po":
                    {
                        List<double[]> pts = Points(key); int pf = PFull(key);
                        s.Frac = delegate(double v) { return Math.Max(0, Math.Min(1, Watts(pts, pmax, v * 255.0 / mx) / pf)); };
                        s.Text = delegate(double v) { double w = Watts(pts, pmax, v * 255.0 / mx); return (w < 10 ? w.ToString("0.0", Inv) : w.ToString("0", Inv)) + " W"; };
                        int step = 1; foreach (int t in new int[] { 1, 2, 5, 10, 20, 25, 50, 100, 200, 500 }) { step = t; if (pf / (double)t <= 5.01) break; }
                        List<string> tt = new List<string>(); List<double> tp = new List<double>();
                        for (int w = 0; w <= pf; w += step) { tt.Add(w.ToString(Inv)); tp.Add(w / (double)pf); }
                        if (tp[tp.Count - 1] < 0.999) { tt.Add(pf.ToString(Inv)); tp.Add(1.0); }
                        s.Ticks = tt.ToArray(); s.Pos = tp.ToArray(); return s;
                    }
                case "swr":
                    {
                        List<double[]> pts = Points(key, "swr"); bool cal = pts.Count > 0;
                        List<double[]> gp = new List<double[]>(); foreach (double[] q in pts) gp.Add(new double[] { q[0], SwrPos(q[1]) });   // le vumètre suit le coefficient de réflexion : interpolation et extrapolation sur cette grandeur
                        Func<double, double> gam = delegate(double v) { return Math.Max(0, Math.Min(1, Interp(gp, v * 255.0 / mx, 0.0))); };
                        Func<double, double> ratio = delegate(double v) { if (!cal) return RosGuard.RatioOf(v, mx); double g = gam(v); return g >= 0.999 ? 1e6 : (1 + g) / (1 - g); };
                        Func<double, bool> inf = delegate(double v) { return cal ? gam(v) >= 0.95 : v >= mx * 0.98; };
                        s.Frac = delegate(double v) { return inf(v) ? 1.0 : SwrPos(ratio(v)); };
                        s.Text = delegate(double v) { return inf(v) ? "∞" : ratio(v).ToString("0.0", Inv); };
                        s.Ticks = new string[] { "1", "1.5", "2", "3", "5", "10", "∞" };
                        s.Pos = new double[] { 0, SwrPos(1.5), SwrPos(2), SwrPos(3), SwrPos(5), SwrPos(10), 1 }; s.Hot = 0.5; return s;
                    }
                case "alc":
                    {
                        List<double[]> pts = Points(key, "alc"); bool cal = pts.Count > 0;
                        Func<double, double> pct = delegate(double v) { return cal ? Interp(pts, v * 255.0 / mx, 0.0) : v * 100.0 / mx; };
                        s.Frac = delegate(double v) { return Math.Max(0, Math.Min(1, pct(v) / 100.0)); };
                        s.Text = delegate(double v) { return (int)Math.Round(Math.Max(0, pct(v))) + "%"; };
                        s.Ticks = new string[] { "0", "25", "50", "75", "100" }; s.Pos = new double[] { 0, 0.25, 0.5, 0.75, 1 }; s.Hot = 0.5; s.BlueRed = true; return s;
                    }
                case "comp":
                    {
                        List<double[]> pts = Points(key, "comp"); bool cal = pts.Count > 0;
                        Func<double, double> db = delegate(double v) { return cal ? Math.Max(0, Interp(pts, v * 255.0 / mx, 0.0)) : CompDb(Math.Max(0, Math.Min(1, v / mx))); };
                        s.Frac = delegate(double v) { return CompPos(db(v)); };
                        s.Text = delegate(double v) { return db(v).ToString("0", Inv) + " dB"; };
                        s.Ticks = new string[] { "0", "5", "10", "20", "30" }; s.Pos = new double[] { 0, 0.25, 0.5, 0.75, 1 }; return s;
                    }
                case "idd":
                    {
                        List<double[]> pts = Points(key, "idd"); bool cal = pts.Count > 0;
                        Func<double, double> amp = delegate(double v) { return Math.Max(0, cal ? Interp(pts, v * 255.0 / mx, 0.0) : Math.Min(1, v / mx) * 15); };
                        s.Frac = delegate(double v) { return Math.Max(0, Math.Min(1, amp(v) / 15.0)); };
                        s.Text = delegate(double v) { return amp(v).ToString("0.0", Inv) + " A"; };
                        s.Ticks = new string[] { "0", "5", "10", "15" }; s.Pos = new double[] { 0, 1 / 3.0, 2 / 3.0, 1 }; return s;
                    }
                case "vdd":
                    {
                        double full = VddFull(pmax); List<double[]> pts = Points(key, "vdd"); bool cal = pts.Count > 0;
                        Func<double, double> volt = delegate(double v) { return Math.Max(0, cal ? Interp(pts, v * 255.0 / mx, 0.0) : Math.Min(1, v / mx) * full); };
                        s.Frac = delegate(double v) { return Math.Max(0, Math.Min(1, volt(v) / full)); };
                        s.Text = delegate(double v) { return volt(v).ToString("0.0", Inv) + " V"; };
                        int n = full >= 50 ? 5 : 3; List<string> tt = new List<string>(); List<double> tp = new List<double>();
                        for (int i = 0; i <= n; i++) { tt.Add((full * i / n).ToString("0", Inv)); tp.Add(i / (double)n); }
                        s.Ticks = tt.ToArray(); s.Pos = tp.ToArray(); s.NoHot = true; return s;
                    }
            }
            return null;
        }
        // valeur réelle (lue sur le poste) en fonction de la lecture brute 0..255 : droite depuis l'origine jusqu'au premier point, puis segments entre points, prolongés au-delà du dernier
        public static double Interp(List<double[]> pts, double raw, double origin)
        {
            if (pts.Count == 0) return origin;
            double r0 = 0, y0 = origin;
            for (int i = 0; i < pts.Count; i++)
            {
                double r1 = pts[i][0], y1 = pts[i][1];
                if (raw <= r1 || i == pts.Count - 1)
                {
                    if (i == pts.Count - 1 && raw > r1 && pts.Count > 1) { r0 = pts[i - 1][0]; y0 = pts[i - 1][1]; }
                    return r1 == r0 ? y1 : y0 + (y1 - y0) * (raw - r0) / (r1 - r0);
                }
                r0 = r1; y0 = y1;
            }
            return y0;
        }
        // interpolation sans origine : segments entre les points, prolongés au-delà des extrémités par le segment voisin (Width)
        public static double InterpFree(List<double[]> pts, double x)
        {
            if (pts.Count == 0) return 0; if (pts.Count == 1) return pts[0][1];
            int i = 1; while (i < pts.Count - 1 && x > pts[i][0]) i++;
            double[] a = pts[i - 1], b = pts[i]; return b[0] == a[0] ? b[1] : a[1] + (b[1] - a[1]) * (x - a[0]) / (b[0] - a[0]);
        }
        public static double CompPos(double db) { db = Math.Max(0, db); return Math.Min(1, db <= 10 ? db / 20.0 : 0.5 + (db - 10) / 40.0); }
    }

    // Étalonnage du wattmètre : l'opérateur règle 10 W, 20 W… et valide quand l'émission est stable ; la lecture brute du poste est mémorisée à chaque palier
    public class CalDlg : Form
    {
        Rig rig; string key; int pmax; List<int> steps; int idx; List<double[]> got = new List<double[]>();
        Label lbl, live; Button ok, cancel; System.Windows.Forms.Timer tm;
        public CalDlg(Rig r)
        {
            rig = r; key = r.Def.Key; pmax = MeterCal.PMax(key); steps = MeterCal.Steps(pmax);
            Text = "Étalonnage du wattmètre — " + CmdServer.ModelName(r.Def); FormBorderStyle = FormBorderStyle.FixedDialog; StartPosition = FormStartPosition.CenterParent; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
            BackColor = T.Panel; ForeColor = T.Text; ClientSize = new Size(560, 230); Font = new Font("Segoe UI", 10f);
            lbl = new Label(); lbl.SetBounds(14, 12, 532, 100); lbl.Font = new Font("Segoe UI", 11f, FontStyle.Bold); Controls.Add(lbl);
            live = new Label(); live.SetBounds(14, 118, 532, 40); live.ForeColor = T.Dim; Controls.Add(live);
            ok = new Button(); ok.Text = "OK : l'émission est à la bonne puissance"; ok.SetBounds(14, 170, 340, 44); ok.FlatStyle = FlatStyle.Flat; ok.BackColor = T.Panel2; ok.ForeColor = T.Text; ok.Click += delegate { Take(); }; Controls.Add(ok);
            cancel = new Button(); cancel.Text = "Abandonner"; cancel.SetBounds(366, 170, 180, 44); cancel.FlatStyle = FlatStyle.Flat; cancel.BackColor = T.Panel2; cancel.ForeColor = T.Text; cancel.DialogResult = DialogResult.Cancel; Controls.Add(cancel); CancelButton = cancel;
            tm = new System.Windows.Forms.Timer(); tm.Interval = 200; tm.Tick += delegate { Refresh2(); }; tm.Start(); FormClosed += delegate { tm.Stop(); tm.Dispose(); };
            Show2();
        }
        double Raw() { string s = rig.Get("po"); double v; return s != null && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v * 255.0 / Math.Max(1, PoMax()) : -1; }
        int PoMax() { Ctl c = rig.Def.Find("po"); return c != null && c.Max > 0 ? c.Max : 255; }
        void Show2()
        {
            int w = steps[idx]; bool last = idx == steps.Count - 1;
            lbl.Text = "Étape " + (idx + 1) + " / " + steps.Count + "\n" + (last ? "Dernier palier : " + w + " W = pleine déviation.\n" : "") + "Réglez la puissance du poste à " + w + " W, émettez une porteuse (TUNE, AM ou CW), puis cliquez OK quand l'émission est à " + w + " W.";
            Refresh2();
        }
        void Refresh2() { double r = Raw(); live.Text = "Émission : " + (rig.Tx ? "oui" : "non") + "    Lecture du wattmètre : " + (r < 0 ? "—" : r.ToString("0")) + " / 255"; }
        void Take()
        {
            if (!rig.Tx) { MessageBox.Show(this, "Le poste n'émet pas : lancez l'émission à " + steps[idx] + " W puis cliquez OK.", "Étalonnage"); return; }
            double sum = 0; int n = 0; DateTime end = DateTime.UtcNow.AddMilliseconds(700);
            while (DateTime.UtcNow < end) { double r = Raw(); if (r >= 0) { sum += r; n++; } Application.DoEvents(); System.Threading.Thread.Sleep(40); }
            if (n == 0 || sum / n < 1) { MessageBox.Show(this, "Aucune lecture de puissance : vérifiez l'émission et la liaison avec le poste.", "Étalonnage"); return; }
            got.Add(new double[] { Math.Round(sum / n, 1), steps[idx] }); idx++;
            if (idx >= steps.Count)
            {
                got.Sort(delegate(double[] a, double[] b) { return a[0].CompareTo(b[0]); });
                MeterCal.SavePoints(key, got); MeterCal.SetPMax(key, pmax);
                MessageBox.Show(this, "Étalonnage enregistré (" + got.Count + " points). Le wattmètre Po est gradué jusqu'à " + pmax + " W. Coupez l'émission.", "Étalonnage"); DialogResult = DialogResult.OK; Close();
            }
            else Show2();
        }
    }

    // Étalonnage d'un autre vumètre (SWR, ALC, COMP, Id, Vdd) : à chaque point, l'opérateur lit la valeur réelle sur le poste et la saisit ; la lecture brute est mémorisée
    public class MeterTestDlg : Form
    {
        Rig rig; string key, id, unit; List<double[]> got = new List<double[]>();
        Label lbl, live, pts; TextBox val; Button add, done, cancel; System.Windows.Forms.Timer tm;
        public static readonly string[] Ids = { "swr", "alc", "comp", "idd", "vdd" };
        public static string Name(string id) { switch (id) { case "swr": return "SWR (ROS)"; case "alc": return "ALC"; case "comp": return "COMP"; case "idd": return "Id (courant)"; default: return "Vdd (tension)"; } }
        public static string Unit(string id) { switch (id) { case "swr": return ""; case "alc": return "%"; case "comp": return "dB"; case "idd": return "A"; default: return "V"; } }
        static string How(string id)
        {
            switch (id)
            {
                case "swr": return "Émettez (TUNE, AM ou CW) sur une antenne ou une charge dont vous connaissez le ROS, ou lisez le ROS sur l'afficheur du poste. Saisissez le ROS réel (1.0 = adapté). Plusieurs points à des ROS différents (1.0, 2, 3…) donnent une échelle plus juste.";
                case "alc": return "Émettez en parlant (ou avec un niveau de micro variable) et lisez l'ALC en % sur l'afficheur du poste. Saisissez cette valeur ; plusieurs points (par ex. 20, 50, 80 %) donnent une échelle plus juste.";
                case "comp": return "Activez le compresseur, émettez et lisez le niveau de compression en dB sur l'afficheur du poste. Saisissez cette valeur ; plusieurs points (par ex. 5, 10, 20 dB) donnent une échelle plus juste.";
                case "idd": return "Émettez à une puissance donnée et lisez le courant consommé (A) sur l'afficheur du poste ou sur l'alimentation. Saisissez cette valeur ; plusieurs puissances donnent une échelle plus juste.";
                default: return "Émettez et lisez la tension d'alimentation (V) sur l'afficheur du poste ou sur l'alimentation. Saisissez cette valeur ; plusieurs points donnent une échelle plus juste.";
            }
        }
        public MeterTestDlg(Rig r, string meter)
        {
            rig = r; key = r.Def.Key; id = meter; unit = Unit(id);
            Text = "Étalonnage " + Name(id) + " — " + CmdServer.ModelName(r.Def); FormBorderStyle = FormBorderStyle.FixedDialog; StartPosition = FormStartPosition.CenterParent; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
            BackColor = T.Panel; ForeColor = T.Text; ClientSize = new Size(600, 330); Font = new Font("Segoe UI", 10f);
            lbl = new Label(); lbl.SetBounds(14, 10, 572, 96); lbl.Text = How(id); Controls.Add(lbl);
            live = new Label(); live.SetBounds(14, 112, 572, 24); live.ForeColor = T.Dim; Controls.Add(live);
            Label l2 = new Label(); l2.Text = "Valeur réelle lue sur le poste" + (unit.Length > 0 ? " (" + unit + ")" : "") + " :"; l2.SetBounds(14, 146, 330, 24); Controls.Add(l2);
            val = new TextBox(); val.SetBounds(350, 144, 90, 26); val.BackColor = T.Panel2; val.ForeColor = T.Text; Controls.Add(val);
            add = new Button(); add.Text = "OK : ajouter ce point"; add.SetBounds(450, 140, 136, 34); add.FlatStyle = FlatStyle.Flat; add.BackColor = T.Panel2; add.ForeColor = T.Text; add.Click += delegate { Take(); }; Controls.Add(add); AcceptButton = add;
            pts = new Label(); pts.SetBounds(14, 184, 572, 70); pts.ForeColor = T.Dim; Controls.Add(pts);
            done = new Button(); done.Text = "Terminer et enregistrer"; done.SetBounds(14, 270, 280, 44); done.FlatStyle = FlatStyle.Flat; done.BackColor = T.Panel2; done.ForeColor = T.Text; done.Click += delegate { Finish(); }; Controls.Add(done);
            cancel = new Button(); cancel.Text = "Abandonner"; cancel.SetBounds(306, 270, 280, 44); cancel.FlatStyle = FlatStyle.Flat; cancel.BackColor = T.Panel2; cancel.ForeColor = T.Text; cancel.DialogResult = DialogResult.Cancel; Controls.Add(cancel); CancelButton = cancel;
            tm = new System.Windows.Forms.Timer(); tm.Interval = 200; tm.Tick += delegate { Refresh2(); }; tm.Start(); FormClosed += delegate { tm.Stop(); tm.Dispose(); };
            Refresh2(); List2();
        }
        int Max() { Ctl c = rig.Def.Find(id); return c != null && c.Max > 0 ? c.Max : 255; }
        double Raw() { string s = rig.Get(id); double v; return s != null && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v * 255.0 / Max() : -1; }
        void Refresh2() { double r = Raw(); live.Text = "Émission : " + (rig.Tx ? "oui" : "non") + "    Lecture brute du vumètre : " + (r < 0 ? "—" : r.ToString("0")) + " / 255"; }
        void List2()
        {
            StringBuilder sb = new StringBuilder(); sb.Append(got.Count == 0 ? "Aucun point pour l'instant (un seul suffit, plusieurs sont mieux)." : "Points relevés : ");
            foreach (double[] p in got) sb.Append("lecture " + p[0].ToString("0") + " = " + p[1].ToString("0.##", CultureInfo.InvariantCulture) + " " + unit + "   "); pts.Text = sb.ToString();
        }
        void Take()
        {
            double real; string s = val.Text.Trim().Replace(',', '.');
            if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out real) || real < 0 || (id == "swr" && real < 1)) { MessageBox.Show(this, "Saisissez la valeur lue sur le poste" + (id == "swr" ? " (ROS : 1.0 ou plus)" : "") + ".", "Étalonnage"); return; }
            if (!rig.Tx) { MessageBox.Show(this, "Le poste n'émet pas : émettez, lisez la valeur sur le poste, saisissez-la puis cliquez OK.", "Étalonnage"); return; }
            double sum = 0; int n = 0; DateTime end = DateTime.UtcNow.AddMilliseconds(700);
            while (DateTime.UtcNow < end) { double r = Raw(); if (r >= 0) { sum += r; n++; } Application.DoEvents(); System.Threading.Thread.Sleep(40); }
            if (n == 0 || sum / n < 1) { MessageBox.Show(this, "Lecture brute nulle : le vumètre ne bouge pas (valeur trop faible ou poste non lu).", "Étalonnage"); return; }
            got.RemoveAll(delegate(double[] p) { return Math.Abs(p[0] - Math.Round(sum / n, 1)) < 1.5; });
            got.Add(new double[] { Math.Round(sum / n, 1), real }); got.Sort(delegate(double[] a, double[] b) { return a[0].CompareTo(b[0]); }); val.Text = ""; List2();
        }
        void Finish()
        {
            if (got.Count == 0) { MessageBox.Show(this, "Ajoutez au moins un point (OK) avant d'enregistrer.", "Étalonnage"); return; }
            MeterCal.SavePoints(key, id, got); MessageBox.Show(this, "Étalonnage de " + Name(id) + " enregistré (" + got.Count + " point(s)). L'échelle est extrapolée à partir de ces points.", "Étalonnage"); DialogResult = DialogResult.OK; Close();
        }
    }

    // Étalonnage du ROS (valable pour tous les postes qui lisent le ROS) : on émet sur une charge de ROS connu (ou on lit le ROS sur un appareil de mesure), on saisit le ROS réel,
    // le programme relève la lecture brute du vumètre SWR du poste et la fait coïncider avec cette valeur. Plusieurs mesures possibles ; « Passer » saute une étape ; le reste est extrapolé.
    public class SwrCalDlg : Form
    {
        Rig rig; string key; Ctl sw; List<double[]> got = new List<double[]>(); int idx;
        static readonly double[] Steps = { 1.5, 2.0, 3.0, 5.0 };
        Label lbl, live, pts; TextBox val; Button ok, skip, done, cancel; System.Windows.Forms.Timer tm;
        public SwrCalDlg(Rig r)
        {
            rig = r; key = r.Def.Key; sw = r.Def.Find("swr");
            Text = "Étalonnage du ROS — " + CmdServer.ModelName(r.Def); FormBorderStyle = FormBorderStyle.FixedDialog; StartPosition = FormStartPosition.CenterParent; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
            BackColor = T.Panel; ForeColor = T.Text; ClientSize = new Size(600, 360); Font = new Font("Segoe UI", 10f);
            lbl = new Label(); lbl.SetBounds(14, 10, 572, 104); Controls.Add(lbl);
            live = new Label(); live.SetBounds(14, 118, 572, 44); live.ForeColor = T.Dim; Controls.Add(live);
            Label l2 = new Label(); l2.Text = "ROS réel (appareil de mesure ou charge connue) :"; l2.SetBounds(14, 172, 360, 24); Controls.Add(l2);
            val = new TextBox(); val.SetBounds(380, 170, 80, 26); val.BackColor = T.Panel2; val.ForeColor = T.Text; Controls.Add(val);
            pts = new Label(); pts.SetBounds(14, 206, 572, 56); pts.ForeColor = T.Dim; Controls.Add(pts);
            ok = Btn("OK : relever", 14, 276, 140); ok.Click += delegate { Take(); }; AcceptButton = ok;
            skip = Btn("Passer", 162, 276, 110); skip.Click += delegate { Next(); };
            done = Btn("Terminer et enregistrer", 280, 276, 190); done.Click += delegate { Finish(); };
            cancel = Btn("Abandonner", 478, 276, 108); cancel.DialogResult = DialogResult.Cancel; CancelButton = cancel;
            tm = new System.Windows.Forms.Timer(); tm.Interval = 200; tm.Tick += delegate { Refresh2(); }; tm.Start(); FormClosed += delegate { tm.Stop(); tm.Dispose(); };
            Show2();
        }
        Button Btn(string t, int x, int y, int w) { Button b = new Button(); b.Text = t; b.SetBounds(x, y, w, 50); b.FlatStyle = FlatStyle.Flat; b.BackColor = T.Panel2; b.ForeColor = T.Text; Controls.Add(b); return b; }
        double Raw() { string s = rig.Get("swr"); double v; return s != null && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v * 255.0 / Math.Max(1, sw.Max) : -1; }
        void Show2()
        {
            lbl.Text = "Étape " + (idx + 1) + " / " + Steps.Length + "\nÉmettez (TUNE, AM ou CW) sur une antenne ou une charge dont le ROS est d'environ " + Steps[idx].ToString("0.0", CultureInfo.InvariantCulture) + ", en gardant l'émission pendant que vous cliquez sur OK. Saisissez le ROS réel lu sur votre appareil de mesure (1.0 = adapté). « Passer » saute cette étape si vous n'avez pas ce ROS : l'échelle est extrapolée entre les points relevés.";
            val.Text = Steps[idx].ToString("0.0", CultureInfo.InvariantCulture); val.SelectAll(); val.Focus(); Refresh2(); List2();
        }
        void Refresh2() { double r = Raw(); double shown = r < 0 ? -1 : MeterCal.SwrRatio(key, r); live.Text = "Émission : " + (rig.Tx ? "oui" : "non") + "    Lecture brute du vumètre : " + (r < 0 ? "—" : r.ToString("0")) + " / 255\nROS affiché actuellement par le programme : " + (shown < 0 ? "—" : (shown >= 100 ? "∞" : shown.ToString("0.0", CultureInfo.InvariantCulture))); }
        void List2()
        {
            StringBuilder sb = new StringBuilder(got.Count == 0 ? "Aucune mesure pour l'instant (une seule suffit, plusieurs sont plus justes)." : "Mesures relevées : ");
            foreach (double[] p in got) sb.Append("lecture " + p[0].ToString("0") + " = ROS " + p[1].ToString("0.0#", CultureInfo.InvariantCulture) + "   "); pts.Text = sb.ToString();
        }
        void Take()
        {
            double real; string s = val.Text.Trim().Replace(',', '.');
            if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out real) || real < 1) { MessageBox.Show(this, "Saisissez le ROS réel (1.0 ou plus).", "Étalonnage du ROS"); return; }
            if (!rig.Tx) { MessageBox.Show(this, "Le poste n'émet pas : émettez, puis cliquez sur OK pendant l'émission.", "Étalonnage du ROS"); return; }
            double sum = 0; int n = 0; DateTime end = DateTime.UtcNow.AddMilliseconds(700);
            while (DateTime.UtcNow < end) { double r = Raw(); if (r >= 0) { sum += r; n++; } Application.DoEvents(); System.Threading.Thread.Sleep(40); }
            if (n == 0 || sum / n < 1) { MessageBox.Show(this, "Lecture brute nulle : le vumètre ne bouge pas (ROS trop faible pour la lecture, ou poste non lu).", "Étalonnage du ROS"); return; }
            double raw = Math.Round(sum / n, 1); got.RemoveAll(delegate(double[] p) { return Math.Abs(p[0] - raw) < 1.5; });
            got.Add(new double[] { raw, real }); got.Sort(delegate(double[] a, double[] b) { return a[0].CompareTo(b[0]); }); List2(); Next();
        }
        void Next() { if (idx + 1 >= Steps.Length) { idx = Steps.Length - 1; if (got.Count > 0) { Finish(); return; } MessageBox.Show(this, "Plus d'étape : relevez au moins un point (OK) ou abandonnez.", "Étalonnage du ROS"); return; } idx++; Show2(); }
        void Finish()
        {
            if (got.Count == 0) { MessageBox.Show(this, "Relevez au moins une mesure (OK) avant d'enregistrer.", "Étalonnage du ROS"); return; }
            MeterCal.SavePoints(key, "swr", got); MessageBox.Show(this, "Étalonnage du ROS enregistré (" + got.Count + " point(s)). L'échelle est extrapolée à partir de ces points ; l'alerte ROS et les vumètres l'utilisent.", "Étalonnage du ROS"); DialogResult = DialogResult.OK; Close();
        }
    }

    // Étalonnage des boutons Width (largeur de filtre en Hz) et RF PWR (puissance en W) : l'opérateur règle le poste sur chaque valeur, le programme relève la position correspondante
    public class KnobCalDlg : Form
    {
        Rig rig; RadioDef r0 { get { return rig.Def; } } string key, id, unit; Ctl ctl; List<double> steps = new List<double>(); int idx; List<double[]> got = new List<double[]>();
        Label lbl, live, pts; Button ok, skip, done, cancel; System.Windows.Forms.Timer tm;
        public static string Title(string id) { return id == "width" ? "Width (largeur de filtre)" : id == "contf" ? "CONT (fréquence du contour)" : "RF PWR (puissance HF)"; }
        bool IsHz { get { return id == "width" || id == "contf"; } }
        public KnobCalDlg(Rig r, string knob)
        {
            rig = r; key = r.Def.Key; id = knob; unit = IsHz ? "Hz" : "W"; ctl = r.Def.Find(knob == "width" ? "width" : knob == "contf" ? "contf" : "pwr");
            // WIDTH : un point tous les 100 Hz de 200 à 4000 Hz (« Passer » saute ceux qu'on ne veut pas relever)
            if (id == "width") { for (int hz = 200; hz <= 4000; hz += 100) steps.Add(hz); } else if (id == "contf") { steps.Add(100); steps.Add(2000); steps.Add(4000); } else foreach (int w in MeterCal.Steps(MeterCal.PMax(key))) steps.Add(w);
            Text = "Étalonnage " + Title(id) + " — " + CmdServer.ModelName(r.Def); FormBorderStyle = FormBorderStyle.FixedDialog; StartPosition = FormStartPosition.CenterParent; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
            BackColor = T.Panel; ForeColor = T.Text; ClientSize = new Size(600, 330); Font = new Font("Segoe UI", 10f);
            lbl = new Label(); lbl.SetBounds(14, 10, 572, 100); lbl.Font = new Font("Segoe UI", 11f, FontStyle.Bold); Controls.Add(lbl);
            live = new Label(); live.SetBounds(14, 114, 572, 24); live.ForeColor = T.Dim; Controls.Add(live);
            pts = new Label(); pts.SetBounds(14, 146, 572, 100); pts.ForeColor = T.Dim; Controls.Add(pts);
            ok = new Button(); ok.Text = "OK : le poste est réglé"; ok.SetBounds(14, 262, 190, 50); ok.FlatStyle = FlatStyle.Flat; ok.BackColor = T.Panel2; ok.ForeColor = T.Text; ok.Click += delegate { Take(); }; Controls.Add(ok); AcceptButton = ok;
            skip = new Button(); skip.Text = "Passer"; skip.SetBounds(212, 262, 110, 50); skip.FlatStyle = FlatStyle.Flat; skip.BackColor = T.Panel2; skip.ForeColor = T.Text; skip.Click += delegate { Next(); }; Controls.Add(skip);
            done = new Button(); done.Text = "Terminer"; done.SetBounds(330, 262, 120, 50); done.FlatStyle = FlatStyle.Flat; done.BackColor = T.Panel2; done.ForeColor = T.Text; done.Click += delegate { Finish(); }; Controls.Add(done);
            cancel = new Button(); cancel.Text = "Abandonner"; cancel.SetBounds(458, 262, 128, 50); cancel.FlatStyle = FlatStyle.Flat; cancel.BackColor = T.Panel2; cancel.ForeColor = T.Text; cancel.DialogResult = DialogResult.Cancel; Controls.Add(cancel); CancelButton = cancel;
            tm = new System.Windows.Forms.Timer(); tm.Interval = 200; tm.Tick += delegate { Refresh2(); }; tm.Start(); FormClosed += delegate { tm.Stop(); tm.Dispose(); };
            Show2();
        }
        int Cur() { string s = rig.Get(ctl.Id); int v; return s != null && int.TryParse(s, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out v) ? v : int.MinValue; }
        void Show2()
        {
            double t = steps[idx];
            lbl.Text = "Étape " + (idx + 1) + " / " + steps.Count + "\n" + (id == "width"
                ? "Réglez le bouton WIDTH du poste pour que l'afficheur indique " + t + " Hz" + (t == 200 && r0.Key == "ftdx10" ? " (un FTdx10 ne descend pas sous 300 Hz : passez cette étape)" : "") + ". Puis cliquez OK, ou « Passer » si le poste n'a pas cette valeur. Les valeurs intermédiaires sont interpolées, la largeur ne dépasse jamais 4000 Hz."
                : "Réglez le bouton RF PWR du poste sur " + t + " W (lecture sur l'afficheur du poste), puis cliquez OK. Pas d'émission nécessaire.");
            Refresh2(); List2();
        }
        void Refresh2() { int v = Cur(); live.Text = "Valeur lue sur le poste : " + (v == int.MinValue ? "—" : v.ToString()); }
        void List2()
        {
            StringBuilder sb = new StringBuilder(got.Count == 0 ? "Aucun point pour l'instant." : "Points relevés : ");
            foreach (double[] p in got) sb.Append("valeur " + p[0].ToString("0") + " = " + p[1].ToString("0") + " " + unit + "   "); pts.Text = sb.ToString();
        }
        void Take()
        {
            int v = Cur(); if (v == int.MinValue) { MessageBox.Show(this, "Le poste ne répond pas (liaison ?).", "Étalonnage"); return; }
            got.RemoveAll(delegate(double[] p) { return p[0] == v; }); got.Add(new double[] { v, steps[idx] }); got.Sort(delegate(double[] a, double[] b) { return a[0].CompareTo(b[0]); }); Next();
        }
        void Next() { idx++; if (idx >= steps.Count) Finish(); else Show2(); }
        void Finish()
        {
            if (got.Count == 0) { MessageBox.Show(this, "Aucun point relevé : rien n'est enregistré.", "Étalonnage"); DialogResult = DialogResult.Cancel; Close(); return; }
            if (IsHz && got.Count < 2) { MessageBox.Show(this, "Il faut au moins deux points pour graduer " + Title(id) + ".", "Étalonnage"); return; }
            MeterCal.SavePoints(key, id, got);
            MessageBox.Show(this, "Étalonnage de " + Title(id) + " enregistré (" + got.Count + " point(s)).", "Étalonnage"); DialogResult = DialogResult.OK; Close();
        }
    }
}
