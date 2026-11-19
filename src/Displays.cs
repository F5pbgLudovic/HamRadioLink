using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace HamRadioLink
{
    // Écrans « décor » de la face avant (aucune commande CAT : ils lisent l'état des autres éléments) :
    //  - HGauge : galvanomètre à aiguille (S-mètre en réception, fonction choisie par METER en émission)
    //  - HChain : schéma de principe de l'afficheur VFD (ANT / ATT / FLT / IPO / R.FLT / AGC)
    //  - HDsp   : petit afficheur OLED « SUB DISPLAY » (passe-bande FI : SHIFT, WIDTH, NOTCH, CONTOUR, APF)

    public static class Dsp
    {
        // largeurs de filtre FI du FTdx5000 (manuel CAT, commande SH, colonne par mode)
        static readonly int[] Ssb = { 2400, 200, 400, 600, 850, 1100, 1350, 1500, 1650, 1800, 1950, 2100, 2250, 2400, 2400, 2500, 2600, 2700, 2800, 2900, 3000, 3200, 3400, 3600, 3800, 4000 };
        static readonly int[] Cw = { 500, 50, 100, 150, 200, 250, 300, 350, 400, 450, 500, 800, 1200, 1400, 1700, 2000, 2400 };
        // catégorie du mode : 0 inconnu, 1 SSB, 2 CW, 3 RTTY/PKT, 4 AM/FM (largeur fixe)
        public static int Cat(string m)
        {
            switch (m) { case "1": case "2": return 1; case "3": case "7": return 2; case "6": case "9": case "8": case "C": return 3; case "4": case "5": case "A": case "B": return 4; default: return 0; }
        }
        public static int WidthHz(string mode, int idx)
        {
            int c = Cat(mode); int[] t = c == 2 || c == 3 ? Cw : Ssb; if (c == 4) return 0; if (idx < 0 || idx >= t.Length) return 0; return t[idx];
        }
        public static string WidthText(string mode, int idx)
        {
            int w = WidthHz(mode, idx); if (Cat(mode) == 4) return "fixe"; return w > 0 ? w + " Hz" : "n°" + idx;
        }
    }

    // ------------------------------------------------------------------ galvanomètre
    public class HGauge : HW
    {
        double val; string func = "S", txt = ""; bool txMode;
        static readonly string[] ids = { "comp", "alc", "po", "swr", "idd", "vdd" }, names = { "COMP", "ALC", "PO", "SWR", "ID", "VDD" };
        static readonly string[] sT = { "1", "3", "5", "7", "9", "+20", "+40", "+60" }; static readonly double[] sP = { 0.07, 0.19, 0.31, 0.43, 0.5, 0.67, 0.83, 1.0 };
        public override void Sync()
        {
            if (!Ready) return; bool tx = Rig.Tx; string id = "sm", f = "S";
            if (tx) { int i; string ms = St("meter"); if (ms == null || !int.TryParse(ms, out i) || i < 0 || i > 5) i = 2; id = ids[i]; f = names[i]; }
            string s = St(id); int v = 0; if (s != null) int.TryParse(s, out v); double nv = Math.Max(0, Math.Min(255, v)) / 255.0;
            double nval = val + (nv - val) * (nv > val ? 0.7 : 0.35); if (Math.Abs(nval - nv) < 0.004) nval = nv;
            string t = tx ? (int)Math.Round(nv * 100) + " %" : (nv <= 0.5 ? "S" + (int)Math.Round(nv / 0.5 * 9) : "S9+" + (int)Math.Round((nv - 0.5) / 0.5 * 60));
            if (Math.Abs(nval - val) > 0.001 || f != func || t != txt || tx != txMode) { val = nval; func = f; txt = t; txMode = tx; Invalidate(); }
        }
        PointF At(float cx, float cy, float r, double deg) { double a = deg * Math.PI / 180.0; return new PointF((float)(cx + r * Math.Sin(a)), (float)(cy - r * Math.Cos(a))); }
        protected override void Draw(Graphics g)
        {
            g.Clear(Parent != null ? Parent.BackColor : T.Panel); float k = T.Scale; RectangleF r = new RectangleF(0.5f, 0.5f, Width - 2, Height - 2);
            using (GraphicsPath p = T.Round(r, 8 * k))
            {
                using (LinearGradientBrush b = new LinearGradientBrush(r, Color.FromArgb(62, 46, 20), Color.FromArgb(22, 15, 8), 90f)) g.FillPath(b, p);
                using (Pen pen = new Pen(Color.FromArgb(120, 92, 44), 1.5f)) g.DrawPath(pen, p);
            }
            float cx = Width / 2f, cy = Height * 1.05f, rad = Math.Min(Width * 0.50f, Height * 0.88f); const double A0 = -50, A1 = 50;
            Color ink = Color.FromArgb(255, 214, 140), red = Color.FromArgb(255, 90, 70);
            string[] tt = txMode ? new string[] { "0", "20", "40", "60", "80", "100" } : sT; double[] tp = txMode ? new double[] { 0, 0.2, 0.4, 0.6, 0.8, 1 } : sP;
            double redFrom = txMode ? 0.8 : 0.5;
            using (Pen pn = new Pen(ink, 1.5f * k)) g.DrawArc(pn, cx - rad, cy - rad, rad * 2, rad * 2, (float)(-90 + A0), (float)((redFrom) * (A1 - A0)));
            using (Pen pn = new Pen(red, 2.5f * k)) g.DrawArc(pn, cx - rad, cy - rad, rad * 2, rad * 2, (float)(-90 + A0 + redFrom * (A1 - A0)), (float)((1 - redFrom) * (A1 - A0)));
            for (int i = 0; i <= 40; i++)
            {
                double f = i / 40.0; double a = A0 + f * (A1 - A0); bool big = i % 8 == 0; PointF p1 = At(cx, cy, rad, a), p2 = At(cx, cy, rad - (big ? 9 : 5) * k, a);
                using (Pen pn = new Pen(f > redFrom ? red : ink, 1f)) g.DrawLine(pn, p1, p2);
            }
            using (Brush b = new SolidBrush(ink))
                for (int i = 0; i < tt.Length; i++)
                {
                    PointF q = At(cx, cy, rad - 22 * k, A0 + tp[i] * (A1 - A0)); Font fo = T.Fn("Segoe UI Semibold", 8f); SizeF sz = g.MeasureString(tt[i], fo);
                    using (Brush bb = new SolidBrush(tp[i] > redFrom ? red : ink)) g.DrawString(tt[i], fo, bb, q.X - sz.Width / 2, q.Y - sz.Height / 2);
                }
            using (Brush b = new SolidBrush(Color.FromArgb(200, 255, 214, 140))) { g.DrawString(txMode ? func : "S", T.Fn("Segoe UI Semibold", 11f), b, 12 * k, 8 * k); g.DrawString(txMode ? "%" : "dB", T.Fn("Segoe UI", 8f), b, 12 * k, 28 * k); }
            using (StringFormat sf = new StringFormat()) { sf.Alignment = StringAlignment.Far; using (Brush b = new SolidBrush(txMode ? red : ink)) g.DrawString(txt, T.Fn("Consolas", 11f, FontStyle.Bold), b, new RectangleF(0, 8 * k, Width - 12 * k, 24 * k), sf); }
            PointF tip = At(cx, cy, rad - 3 * k, A0 + val * (A1 - A0));
            using (Pen pn = new Pen(Color.FromArgb(255, 240, 230), 2f * k)) g.DrawLine(pn, new PointF(cx, cy), tip);
        }
    }

    // ------------------------------------------------------------------ schéma de principe du VFD
    public class HChain : HW
    {
        string[] vals = new string[6]; string sig = "";
        static readonly string[][] cellsA = { new string[] { "ANT", "ant" }, new string[] { "ATT", "att" }, new string[] { "FLT", "vrf" }, new string[] { "IPO", "ipo" }, new string[] { "R.FLT", "rfil" }, new string[] { "AGC", "agc" } };
        static readonly string[][] cellsB = { new string[] { "ANT", "antb" }, new string[] { "ATT", "attb" }, new string[] { "FLT", "vrfs" }, new string[] { "IPO", "ipob" }, new string[] { "R.FLT", "rfilb" }, new string[] { "AGC", "agcb" } };
        string[][] Cells { get { return Def.Ref == "b" ? cellsB : cellsA; } }
        string Lab(string id)
        {
            Ctl c = Rig.Def.Find(id); string v = St(id); if (c == null || v == null) return "—";
            if (c.K == Kind.Toggle) return id.StartsWith("vrf") ? (c.IsOn(v) ? "VRF" : "THRU") : (c.IsOn(v) ? "ON" : "OFF");
            foreach (Item it in c.ItemList) if (it.Match(v)) return it.Label.Replace("ANT ", "").Replace(" kHz", "k").Replace(" Hz", "").Replace("AMP ", "AMP").Replace("IPO ", "IPO"); return "?";
        }
        public override void Sync()
        {
            if (!Ready) return; StringBuilder sb = new StringBuilder(); string[][] cl = Cells;
            for (int i = 0; i < 6; i++)
            {
                string t = Lab(cl[i][1]); if (i == 0 && St(Def.Ref == "b" ? "rxantb" : "rxant") == "1") t = "RX"; vals[i] = t; sb.Append(t).Append('|');
            }
            string s = sb.ToString(); if (s != sig) { sig = s; Invalidate(); }
        }
        protected override void Draw(Graphics g)
        {
            g.Clear(Parent != null ? Parent.BackColor : T.Panel); float k = T.Scale; RectangleF r = new RectangleF(0.5f, 0.5f, Width - 2, Height - 2);
            Color teal = Color.FromArgb(120, 235, 225), dim = Color.FromArgb(60, 120, 120);
            using (GraphicsPath p = T.Round(r, 5 * k)) { using (Brush b = new SolidBrush(Color.FromArgb(5, 14, 18))) g.FillPath(b, p); using (Pen pen = new Pen(Color.FromArgb(40, 90, 96))) g.DrawPath(pen, p); }
            string[][] cl = Cells; float cw = (Width - 6 * k) / 6f;
            for (int i = 0; i < 6; i++)
            {
                float x = 3 * k + i * cw; using (StringFormat sf = T.Center())
                {
                    using (Brush b = new SolidBrush(dim)) g.DrawString(cl[i][0], T.Fn("Segoe UI", 7f), b, new RectangleF(x, 2 * k, cw, Height * 0.36f), sf);
                    using (Brush b = new SolidBrush(teal)) g.DrawString(vals[i] ?? "—", T.Fn("Consolas", 10f, FontStyle.Bold), b, new RectangleF(x, Height * 0.36f, cw, Height * 0.60f), sf);
                }
                if (i > 0) using (Pen pen = new Pen(Color.FromArgb(30, 70, 76))) g.DrawLine(pen, x, 4 * k, x, Height - 4 * k);
            }
        }
    }

    // ------------------------------------------------------------------ afficheur OLED « SUB DISPLAY » (passe-bande FI)
    public class HDsp : HW
    {
        string sig = ""; int shift, wIdx, nf, cf; bool notch, apf, cont, dnf; string mode = ""; int pitch;
        string Sfx { get { return Def.Ref == "b" ? "b" : ""; } }
        int Num(string id) { string s = St(id); int v = 0; if (s != null) int.TryParse(s, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out v); return v; }
        public override void Sync()
        {
            if (!Ready) return; string x = Sfx; string m = St(Def.Ref == "b" ? "md1" : "md0") ?? "";
            int sh = Num("shift" + x), wi = Num("width" + x), nfv = Num("notchf" + x), cfv = Num("contf" + x), pit = Num("kp");
            string n = St("notch" + x), c = St("cont" + x), d = St("dnf" + x);
            Ctl nc = Rig.Def.Find("notch" + x); bool no = nc != null && nc.IsOn(n);
            int cv = 0; if (c != null) int.TryParse(c, out cv); Ctl dc = Rig.Def.Find("dnf" + x); bool dn = dc != null && dc.IsOn(d);
            string sg = m + "|" + sh + "|" + wi + "|" + nfv + "|" + cfv + "|" + no + "|" + cv + "|" + dn + "|" + pit;
            if (sg != sig) { sig = sg; mode = m; shift = sh; wIdx = wi; nf = nfv; cf = cfv; notch = no; cont = cv == 1; apf = cv == 2; dnf = dn; pitch = pit; Invalidate(); }
        }
        protected override void Draw(Graphics g)
        {
            g.Clear(Parent != null ? Parent.BackColor : T.Panel); float k = T.Scale; RectangleF r = new RectangleF(0.5f, 0.5f, Width - 2, Height - 2);
            Color amber = Color.FromArgb(255, 196, 70), dim = Color.FromArgb(120, 90, 30), cy = Color.FromArgb(90, 220, 240), red = Color.FromArgb(255, 90, 70);
            using (GraphicsPath p = T.Round(r, 5 * k)) { using (Brush b = new SolidBrush(Color.FromArgb(3, 3, 4))) g.FillPath(b, p); using (Pen pen = new Pen(dim, 1.5f)) g.DrawPath(pen, p); }
            Ctl mc = Ready ? Rig.Def.Find(Def.Ref == "b" ? "md1" : "md0") : null; string mn = ""; if (mc != null) foreach (Item it in mc.ItemList) if (it.Match(mode)) mn = it.Label;
            using (Brush b = new SolidBrush(amber)) g.DrawString((Def.Ref == "b" ? "SUB DISPLAY-III  VFO-B  " : "SUB DISPLAY-II  VFO-A  ") + mn, T.Fn("Segoe UI Semibold", 8.5f), b, 8 * k, 4 * k);
            int cat = Dsp.Cat(mode); string wt = Dsp.WidthText(mode, wIdx);
            string line = "WIDTH " + wt + "   SHIFT " + (shift > 0 ? "+" : "") + shift + " Hz";
            using (StringFormat sf = new StringFormat()) { sf.Alignment = StringAlignment.Far; using (Brush b = new SolidBrush(amber)) g.DrawString(line, T.Fn("Consolas", 9f, FontStyle.Bold), b, new RectangleF(0, 4 * k, Width - 8 * k, 18 * k), sf); }
            RectangleF gr = new RectangleF(10 * k, 26 * k, Width - 20 * k, Height - 26 * k - 16 * k); float baseY = gr.Bottom;
            const double Fmax = 4000.0; Func<double, float> X = delegate(double hz) { return (float)(gr.X + Math.Max(0, Math.Min(Fmax, hz)) / Fmax * gr.Width); };
            using (Pen pn = new Pen(Color.FromArgb(120, 90, 30))) g.DrawLine(pn, gr.X, baseY, gr.Right, baseY);
            for (int h = 0; h <= 4000; h += 500)
            {
                float x = X(h); using (Pen pn = new Pen(Color.FromArgb(70, 55, 20))) g.DrawLine(pn, x, baseY, x, baseY + 3 * k);
                if (h % 1000 == 0) using (Brush b = new SolidBrush(dim)) g.DrawString((h / 1000.0).ToString("0.#") + "k", T.Fn("Segoe UI", 6.5f), b, x - 7 * k, baseY + 1);
            }
            double center = cat == 2 ? 300 + 10 * pitch : 1500; double w = Dsp.WidthHz(mode, wIdx); if (cat == 4 || w <= 0) w = 3000;
            double lo = center + shift - w / 2, hi = center + shift + w / 2; float xl = X(lo), xh = X(hi); float top = gr.Y + 4 * k, bot = baseY - 1;
            PointF[] poly = { new PointF(xl, bot), new PointF(xl + 3 * k, top), new PointF(xh - 3 * k, top), new PointF(xh, bot) };
            using (Brush b = new SolidBrush(Color.FromArgb(70, amber))) g.FillPolygon(b, poly);
            using (Pen pn = new Pen(amber, 1.5f * k)) g.DrawPolygon(pn, poly);
            if (cont) { float x = X(cf * 100.0); using (Brush b = new SolidBrush(Color.FromArgb(200, 0, 0, 0))) g.FillRectangle(b, x - 5 * k, top + 8 * k, 10 * k, bot - top - 8 * k); using (Pen pn = new Pen(red, 1.5f * k)) g.DrawLine(pn, x, top + 8 * k, x, bot); using (Brush b = new SolidBrush(red)) g.DrawString("CONT", T.Fn("Segoe UI", 6.5f), b, x + 4 * k, top + 8 * k); }
            if (apf) { float x = X(center + shift); using (Pen pn = new Pen(cy, 2f * k)) { g.DrawLine(pn, x, bot, x, top - 2 * k); } using (Brush b = new SolidBrush(cy)) g.DrawString("APF", T.Fn("Segoe UI", 6.5f), b, x + 4 * k, top - 2 * k); }
            if (notch) { float x = X(nf * 10.0); using (Pen pn = new Pen(cy, 1.5f * k)) { pn.DashStyle = DashStyle.Dash; g.DrawLine(pn, x, top - 2 * k, x, bot); } using (Brush b = new SolidBrush(cy)) g.DrawString("NOTCH " + nf * 10 + " Hz", T.Fn("Segoe UI", 6.5f), b, x + 3 * k, bot - 14 * k); }
            if (dnf) using (Brush b = new SolidBrush(cy)) g.DrawString("DNF", T.Fn("Segoe UI Semibold", 7.5f), b, 8 * k, Height - 16 * k);
        }
    }
}
