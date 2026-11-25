using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace HamRadioLink
{
    // Réglage « grand S-mètre » (FTdx10) : mémorisé par poste, commuté par un clic droit sur un vumètre (barres <-> S-mètre) ou sur le S-mètre lui-même.
    public static class BigMeterCfg
    {
        public static event Action Changed;
        public static bool Applies(RadioDef d) { return d != null && d.Find("bigmeter") != null; }
        public static bool On(RadioDef d) { return Applies(d) && Cfg.GetBool("bigmeter." + d.Key, false); }
        public static void Toggle(RadioDef d) { if (d == null) return; Cfg.SetBool("bigmeter." + d.Key, !On(d)); Cfg.Save(); Action a = Changed; if (a != null) a(); }
        // texte du S-mètre : S1..S9, au-delà S9+NNdB
        public static string SText(Rig r, string id)
        {
            if (r == null || r.Def == null) return "—"; Ctl c = r.Def.Find(id); string s = r.Get(id); int v; if (c == null || s == null || !int.TryParse(s, out v)) return "—";
            double f = v / (double)(c.Max > 0 ? c.Max : 255); if (f <= 0.5) { int n = (int)Math.Round(f / 0.5 * 9); return "S" + n; } return "S9+" + (int)Math.Round((f - 0.5) / 0.5 * 60) + "dB";
        }
        static string Meas(Rig r, string id)
        {
            Ctl c = r.Def.Find(id); string s = r.Get(id); int v; if (c == null || !r.Tx || s == null || !int.TryParse(s, out v)) return "—";
            Sc sc = MeterCal.Scale(r.Def, c); return sc != null ? sc.Text(v).Replace(" ", "") : (c.Max > 0 ? (int)Math.Round(v * 100.0 / c.Max) + "%" : s);
        }
        // ligne du grand S-mètre : « Po: 47W  SWR: 1.5  59+10dB »
        public static string Line(Rig r, string sid)
        {
            if (r == null || r.Def == null) return ""; string s = r.Tx && r.Def.Find("po") != null ? "S0" : SText(r, sid);
            return (r.Def.Find("po") != null ? Meas(r, "po") : "") + "	" + (r.Def.Find("swr") != null ? Meas(r, "swr") : "") + "	" + (s.StartsWith("S") ? s.Substring(1) : s);
        }
    }

    // Grand S-mètre à aiguille (tous les postes) : le fond est l'image de l'afficheur du FTdx10 pour la fonction choisie (PO, COMP, ALC, VDD, ID, SWR), l'aiguille rouge est redessinée par-dessus.
    // L'aiguille balaie ±45,8° autour d'un pivot situé sous l'image (géométrie relevée sur l'aiguille de repos des images) : en réception elle suit le signal S, en émission la mesure choisie.
    // Clic gauche = fonction suivante (commande METER du poste si elle existe, sinon choix local) ; clic droit = retour aux barres. L'image garde ses proportions, le reste de la zone est noir.
    public class HBigMeter : HW
    {
        class Fn { public string Name, Id, Code; }
        static readonly string[] AllNames = { "PO", "SWR", "ALC", "COMP", "ID", "VDD" }, AllIds = { "po", "swr", "alc", "comp", "idd", "vdd" };
        static readonly Dictionary<string, Bitmap> imgs = new Dictionary<string, Bitmap>();
        const double HalfSweep = 45.8;   // degrés de part et d'autre de la verticale
        double val; int cur = 0; string curName = "PO", line = "";

        public static bool HasImage(string label) { string n = Canon(label); return n != null && Img(n) != null; }
        static Bitmap Img(string n)
        {
            Bitmap b; lock (imgs)
            {
                if (imgs.TryGetValue(n, out b)) return b;
                try { using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("smeter-" + n + ".png")) b = s == null ? null : new Bitmap(s); } catch { b = null; }
                imgs[n] = b; return b;
            }
        }
        static string Canon(string label)
        {
            string l = (label ?? "").Trim().ToUpperInvariant(); if (l == "PO" || l.StartsWith("PO ") || l == "POWER") return "PO"; if (l.StartsWith("SWR") || l.StartsWith("ROS")) return "SWR"; if (l.StartsWith("ALC")) return "ALC";
            if (l.StartsWith("COMP")) return "COMP"; if (l == "ID" || l.StartsWith("ID ") || l == "IDD") return "ID"; if (l.StartsWith("VDD")) return "VDD"; return null;
        }
        Ctl MeterCtl { get { return Rig != null && Rig.Def != null ? Rig.Def.Find("meter") : null; } }
        // fonctions proposées : celles de la commande METER du poste, sinon les vumètres d'émission présents
        List<Fn> Fns()
        {
            List<Fn> l = new List<Fn>(); Ctl mc = MeterCtl;
            if (mc != null && mc.ItemList.Length > 0)
            {
                foreach (Item it in mc.ItemList) { string n = Canon(it.Label); int k = Array.IndexOf(AllNames, n); if (n != null) l.Add(new Fn { Name = n, Id = AllIds[k], Code = it.Code }); }
                if (l.Count > 0) return l;
            }
            for (int i = 0; i < AllNames.Length; i++) if (Rig.Def.Find(AllIds[i]) != null) l.Add(new Fn { Name = AllNames[i], Id = AllIds[i], Code = null });
            if (l.Count == 0) l.Add(new Fn { Name = "PO", Id = "po", Code = null });
            return l;
        }
        // puissance max réglée à 200 W ou plus : le Vdd nominal est 50 V (image « 50 » au lieu de « 13.8 », aiguille sur le repère à 50 V)
        bool Vdd200 { get { return Rig != null && Rig.Def != null && MeterCal.PMax(Rig.Def.Key) >= 200; } }
        string SId { get { foreach (string s in new[] { "sm", "s" }) if (Rig.Def.Find(s) != null) return s; return "sm"; } }
        double Norm(string id, string s) { int v = 0; if (s != null) int.TryParse(s, out v); Ctl c = Rig.Def.Find(id); double mx = c != null && c.Max > 0 ? c.Max : 255; return Math.Max(0, Math.Min(1, v / mx)); }
        public override void Sync()
        {
            if (!Ready) return; List<Fn> fns = Fns(); int i = -1; Ctl mc = MeterCtl;
            if (mc != null && fns[0].Code != null) { string v = St("meter"); if (v != null) for (int k = 0; k < fns.Count; k++) if (fns[k].Code == v || (mc.ItemList.Length > k && mc.ItemList[Array.FindIndex(mc.ItemList, it => it.Code == fns[k].Code)].Match(v))) { i = k; break; } }
            if (i < 0) i = Math.Max(0, Math.Min(fns.Count - 1, Cfg.GetInt("bigmeter.fn." + Rig.Def.Key, cur)));
            bool tx = Rig.Tx; double nv = tx ? Norm(fns[i].Id, St(fns[i].Id)) : Norm(SId, St(SId)); if (tx && fns[i].Id == "po") { Ctl pc = Rig.Def.Find("po"); int rp; if (pc != null && int.TryParse(St("po") ?? "", out rp)) { int pm = MeterCal.PMax(Rig.Def.Key); nv = Math.Sqrt(Math.Max(0, Math.Min(1, MeterCal.Watts(MeterCal.Points(Rig.Def.Key), pm, rp * 255.0 / (pc.Max > 0 ? pc.Max : 255)) / pm))); } }   /* aiguille = racine carrée de la puissance en watts / puissance max (échelle de l image) */
            if (tx && fns[i].Id == "vdd" && Vdd200) { Sc vs = MeterCal.Scale(Rig.Def, Rig.Def.Find("vdd")); int rv; if (vs != null && int.TryParse(St("vdd") ?? "", out rv)) nv = Math.Min(1, 0.854 * vs.Frac(rv)); }
            double nval = val + (nv - val) * (nv > val ? 0.7 : 0.35); if (Math.Abs(nval - nv) < 0.003) nval = nv;
            string ln = BigMeterCfg.Line(Rig, SId); if (ln != line) { line = ln; Invalidate(); } if (Math.Abs(nval - val) > 0.0005 || i != cur || fns[i].Name != curName) { val = nval; cur = i; curName = fns[i].Name; Invalidate(); }
        }
        protected override void Up(MouseEventArgs e)
        {
            if (Rig == null || Rig.Def == null || !ClientRectangle.Contains(e.Location)) return;
            if (e.Button == MouseButtons.Right) { BigMeterCfg.Toggle(Rig.Def); return; }
            if (e.Button != MouseButtons.Left) return;
            List<Fn> fns = Fns(); if (fns.Count == 0) return; int n = (cur + 1) % fns.Count; Fn f = fns[n];
            if (f.Code != null && MeterCtl != null) Rig.Set(MeterCtl, f.Code); else { Cfg.SetInt("bigmeter.fn." + Rig.Def.Key, n); Cfg.Save(); }
            cur = n; curName = f.Name; Invalidate();
        }
        // images de contrôle (--metershot=dossier) : une par fonction de METER, aiguille à trois positions
        public static void Shot(string dir)
        {
            Rig r = new Rig(); r.Def = Radios.ByKey("ftdx10");
            for (int i = 0; i < 6; i++) foreach (int raw in new[] { 0, 128, 255 })
            {
                r.V["meter"] = i.ToString(); r.V[AllIds[Array.IndexOf(AllNames, new[] { "PO", "COMP", "ALC", "VDD", "ID", "SWR" }[i])]] = raw.ToString(); r.V["sm"] = raw.ToString(); r.V["ptt"] = "1";
                HBigMeter m = new HBigMeter(); m.Def = r.Def.Find("bigmeter"); m.Rig = r; m.Scale = 1f; m.Width = 480; m.Height = 148; m.CreateControl();
                for (int k = 0; k < 12; k++) m.Sync();
                using (Bitmap b = new Bitmap(480, 148)) { m.DrawToBitmap(b, new Rectangle(0, 0, 480, 148)); b.Save(Path.Combine(dir, "smetre-" + new[] { "PO", "COMP", "ALC", "VDD", "ID", "SWR" }[i] + "-" + raw + ".png"), System.Drawing.Imaging.ImageFormat.Png); }
                m.Dispose();
            }
            // puissance max 200 W (en mémoire seulement) : échelle de puissance et position de l'aiguille
            Cfg.SetInt("cal.ftdx10.pmax", 200);
            foreach (int raw in new[] { 0, 96, 128, 255 })
            {
                r.V["meter"] = "0"; r.V["po"] = raw.ToString(); r.V["sm"] = raw.ToString(); r.V["ptt"] = "1";
                HBigMeter m2 = new HBigMeter(); m2.Def = r.Def.Find("bigmeter"); m2.Rig = r; m2.Scale = 1f; m2.Width = 480; m2.Height = 148; m2.CreateControl();
                for (int k = 0; k < 12; k++) m2.Sync();
                using (Bitmap b = new Bitmap(480, 148)) { m2.DrawToBitmap(b, new Rectangle(0, 0, 480, 148)); b.Save(Path.Combine(dir, "smetre-PO200-" + raw + ".png"), System.Drawing.Imaging.ImageFormat.Png); }
                m2.Dispose();
            }
        }
        // ---- échelle de puissance : l'image « PO0 » (échelle des watts effacée) reçoit une graduation 0 → puissance max du poste (loi en racine carrée comme l'image d'origine)
        static readonly Dictionary<int, Bitmap> poImgs = new Dictionary<int, Bitmap>();
        const double PoPivX = 378, PoPivY = 432.9, PoArcY0 = 179.5, PoArcK = 2200.0;
        static PointF PoArcAt(double aDeg, out double sin, out double cos)
        {
            double a = aDeg * Math.PI / 180.0; sin = Math.Sin(a); cos = Math.Cos(a); double lo = 100, hi = 450;
            for (int i = 0; i < 50; i++) { double t = (lo + hi) / 2, x = PoPivX + t * sin, y = PoPivY - t * cos, ya = PoArcY0 + (x - PoArcX0) * (x - PoArcX0) / PoArcK; if (y > ya) lo = t; else hi = t; }
            double tt = (lo + hi) / 2; return new PointF((float)(PoPivX + tt * sin), (float)(PoPivY - tt * cos));
        }
        const double PoArcX0 = 378;
        static string PoLabel(double w) { return w >= 20 || Math.Abs(w - Math.Round(w)) < 0.01 ? Math.Round(w).ToString(CultureInfo.InvariantCulture) : w.ToString("0.#", CultureInfo.InvariantCulture); }
        static Bitmap PoImg(int pmax)
        {
            lock (poImgs)
            {
                Bitmap c; if (poImgs.TryGetValue(pmax, out c)) return c;
                Bitmap bg = Img("PO0"); if (bg == null) { poImgs[pmax] = null; return null; }
                c = new Bitmap(bg.Width, bg.Height); double p = pmax;
                using (Graphics g = Graphics.FromImage(c))
                {
                    g.DrawImage(bg, 0, 0, bg.Width, bg.Height); g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                    Func<double, double> ang = delegate(double w) { return -HalfSweep + 2 * HalfSweep * Math.Sqrt(Math.Max(0, Math.Min(1, w / p))); };
                    // graduations mineures : tous les p/50 jusqu'à p/10, puis tous les p/20
                    List<double> minors = new List<double>(); for (int i = 0; i <= 5; i++) minors.Add(i * p / 50.0); for (int j = 3; j <= 20; j++) minors.Add(j * p / 20.0);
                    // repères numérotés : pmax, 0, puis p/2, p/10, p/4, 3p/4, en gardant un écart suffisant entre étiquettes
                    double[] cand = { p, 0, p / 2, p / 10, p / 4 }; List<double> labs = new List<double>(); List<float> lx = new List<float>();
                    foreach (double w in cand)
                    {
                        double s, co; PointF pt = PoArcAt(ang(w), out s, out co); string t = PoLabel(w); float need = 14 + t.Length * 14f; bool ok = true;
                        for (int k = 0; k < labs.Count; k++) { float need2 = (need + (14 + PoLabel(labs[k]).Length * 14f)) / 2f; if (Math.Abs(lx[k] - pt.X) < need2 + 6) ok = false; }
                        if (ok) { labs.Add(w); lx.Add(pt.X); }
                    }
                    using (Pen mp = new Pen(Color.White, 2.2f)) using (Pen Mp = new Pen(Color.White, 3.2f)) using (Font f = new Font("Arial", 50f, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Pixel)) using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    {
                        foreach (double w in minors) { bool major = false; foreach (double l in labs) if (Math.Abs(l - w) < p * 0.002) major = true; if (major) continue; double s, co; PointF pt = PoArcAt(ang(w), out s, out co); g.DrawLine(mp, pt.X + (float)(s * 3), pt.Y - (float)(co * 3), pt.X + (float)(s * 12), pt.Y - (float)(co * 12)); }
                        foreach (double w in labs)
                        {
                            double s, co; PointF pt = PoArcAt(ang(w), out s, out co); g.DrawLine(Mp, pt.X + (float)(s * 3), pt.Y - (float)(co * 3), pt.X + (float)(s * 19), pt.Y - (float)(co * 19));
                            g.DrawString(PoLabel(w), f, Brushes.White, new PointF(pt.X + (float)(s * 46) - (w >= p ? 12f : 0f), pt.Y - (float)(co * 46)), sf);
                        }
                    }
                }
                poImgs[pmax] = c; return c;
            }
        }
        protected override void Draw(Graphics g)
        {
            g.Clear(Color.Black); Bitmap im = curName == "PO" && Rig != null && Rig.Def != null ? (PoImg(MeterCal.PMax(Rig.Def.Key)) ?? Img("PO")) : Img(Vdd200 && curName == "VDD" ? "VDD50" : curName); if (im == null) return;
            float sc = Math.Min(Width / (float)im.Width, Height / (float)im.Height); float w = im.Width * sc, h = im.Height * sc, x0 = (Width - w) / 2f, y0 = (Height - h) / 2f;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic; g.PixelOffsetMode = PixelOffsetMode.HighQuality; g.DrawImage(im, new RectangleF(x0, y0, w, h), new RectangleF(0, 0, im.Width, im.Height), GraphicsUnit.Pixel);
            // aiguille : pivot au centre de l'image, 1,503 hauteurs plus bas ; partie visible entre 1,045 et 1,552 hauteurs ; épaisseur 0,0224 hauteur
            g.SmoothingMode = SmoothingMode.AntiAlias; double a = (-HalfSweep + 2 * HalfSweep * val) * Math.PI / 180.0; float H = im.Height * sc, px = x0 + w / 2f, py = y0 + 1.503f * H, r0 = 1.045f * H, r1 = 1.552f * H;
            PointF p0 = new PointF(px + r0 * (float)Math.Sin(a), py - r0 * (float)Math.Cos(a)), p1 = new PointF(px + r1 * (float)Math.Sin(a), py - r1 * (float)Math.Cos(a));
            g.SetClip(new RectangleF(x0, y0, w, h));
            using (Pen p = new Pen(Color.FromArgb(245, 18, 6), Math.Max(1.5f, 0.0224f * H))) { p.StartCap = LineCap.Flat; p.EndCap = LineCap.Flat; g.DrawLine(p, p0, p1); }
            g.ResetClip();
if (line.Length > 0)            {                string[] pv = line.Split('	'); float fs = Math.Max(8f, H * 0.1f);                using (Font nf = new Font("Segoe UI Semibold", fs, FontStyle.Regular, GraphicsUnit.Pixel)) using (Brush yb = new SolidBrush(T.SVal)) using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Far, FormatFlags = StringFormatFlags.NoWrap })                {                    string[] lab = { "Po:", "SWR:", "S" }; float[] fx = { 0.10f, 0.37f, 0.70f };                    for (int q = 0; q < 3 && q < pv.Length; q++) { if (q < 2 && pv[q].Length == 0) continue; float lx = x0 + w * fx[q]; g.DrawString(lab[q], nf, yb, new RectangleF(lx, y0, w, h - 2), sf); g.DrawString(pv[q], nf, yb, new RectangleF(lx + fs * (q == 1 ? 3.1f : q == 0 ? 2.2f : 0.62f), y0, w, h - 2), sf); }                }            }
        }
    }
}
