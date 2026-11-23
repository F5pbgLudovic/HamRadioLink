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
        double val; int cur = 0; string curName = "PO";

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
        string SId { get { foreach (string s in new[] { "sm", "s" }) if (Rig.Def.Find(s) != null) return s; return "sm"; } }
        double Norm(string id, string s) { int v = 0; if (s != null) int.TryParse(s, out v); Ctl c = Rig.Def.Find(id); double mx = c != null && c.Max > 0 ? c.Max : 255; return Math.Max(0, Math.Min(1, v / mx)); }
        public override void Sync()
        {
            if (!Ready) return; List<Fn> fns = Fns(); int i = -1; Ctl mc = MeterCtl;
            if (mc != null && fns[0].Code != null) { string v = St("meter"); if (v != null) for (int k = 0; k < fns.Count; k++) if (fns[k].Code == v || (mc.ItemList.Length > k && mc.ItemList[Array.FindIndex(mc.ItemList, it => it.Code == fns[k].Code)].Match(v))) { i = k; break; } }
            if (i < 0) i = Math.Max(0, Math.Min(fns.Count - 1, Cfg.GetInt("bigmeter.fn." + Rig.Def.Key, cur)));
            bool tx = Rig.Tx; double nv = tx ? Norm(fns[i].Id, St(fns[i].Id)) : Norm(SId, St(SId));
            double nval = val + (nv - val) * (nv > val ? 0.7 : 0.35); if (Math.Abs(nval - nv) < 0.003) nval = nv;
            if (Math.Abs(nval - val) > 0.0005 || i != cur || fns[i].Name != curName) { val = nval; cur = i; curName = fns[i].Name; Invalidate(); }
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
        }
        protected override void Draw(Graphics g)
        {
            g.Clear(Color.Black); Bitmap im = Img(curName); if (im == null) return;
            float sc = Math.Min(Width / (float)im.Width, Height / (float)im.Height); float w = im.Width * sc, h = im.Height * sc, x0 = (Width - w) / 2f, y0 = (Height - h) / 2f;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic; g.PixelOffsetMode = PixelOffsetMode.HighQuality; g.DrawImage(im, new RectangleF(x0, y0, w, h), new RectangleF(0, 0, im.Width, im.Height), GraphicsUnit.Pixel);
            // aiguille : pivot au centre de l'image, 1,503 hauteurs plus bas ; partie visible entre 1,045 et 1,552 hauteurs ; épaisseur 0,0224 hauteur
            g.SmoothingMode = SmoothingMode.AntiAlias; double a = (-HalfSweep + 2 * HalfSweep * val) * Math.PI / 180.0; float H = im.Height * sc, px = x0 + w / 2f, py = y0 + 1.503f * H, r0 = 1.045f * H, r1 = 1.552f * H;
            PointF p0 = new PointF(px + r0 * (float)Math.Sin(a), py - r0 * (float)Math.Cos(a)), p1 = new PointF(px + r1 * (float)Math.Sin(a), py - r1 * (float)Math.Cos(a));
            g.SetClip(new RectangleF(x0, y0, w, h));
            using (Pen p = new Pen(Color.FromArgb(245, 18, 6), Math.Max(1.5f, 0.0224f * H))) { p.StartCap = LineCap.Flat; p.EndCap = LineCap.Flat; g.DrawLine(p, p0, p1); }
            g.ResetClip();
        }
    }
}
