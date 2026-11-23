using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

// Outil de préparation (hors exe) : retire l'aiguille rouge des images de S-mètre (fond de l'aiguille au repos) et affiche sa géométrie.
// Usage : CleanNeedle.exe entree.jpg sortie.png [zoom.png]
static class P
{
    static bool Red(Color c) { return c.R > 110 && c.R - Math.Max(c.G, c.B) > 55; }
    static void Main(string[] a)
    {
        Bitmap src = new Bitmap(a[0]); int w = src.Width, h = src.Height; bool[,] m = new bool[w, h]; List<Point> pts = new List<Point>();
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) if (Red(src.GetPixel(x, y))) { m[x, y] = true; pts.Add(new Point(x, y)); }
        // géométrie : axe principal des pixels rouges
        double mx = 0, my = 0; foreach (Point p in pts) { mx += p.X; my += p.Y; } mx /= pts.Count; my /= pts.Count;
        double sxx = 0, syy = 0, sxy = 0; foreach (Point p in pts) { double dx = p.X - mx, dy = p.Y - my; sxx += dx * dx; syy += dy * dy; sxy += dx * dy; }
        double ang = 0.5 * Math.Atan2(2 * sxy, sxx - syy); double ux = Math.Cos(ang), uy = Math.Sin(ang); double tmin = 1e9, tmax = -1e9, wmax = 0;
        foreach (Point p in pts) { double t = (p.X - mx) * ux + (p.Y - my) * uy, n = Math.Abs(-(p.X - mx) * uy + (p.Y - my) * ux); if (t < tmin) tmin = t; if (t > tmax) tmax = t; if (n > wmax) wmax = n; }
        Console.WriteLine("image {0}x{1} ; pixels rouges {2} ; centre ({3:0.0},{4:0.0}) ; direction ({5:0.000},{6:0.000}) ; extrémités ({7:0},{8:0}) -> ({9:0},{10:0}) ; demi-largeur {11:0.0}", w, h, pts.Count, mx, my, ux, uy, mx + tmin * ux, my + tmin * uy, mx + tmax * ux, my + tmax * uy, wmax);
        // masque dilaté
        bool[,] d = new bool[w, h]; int r = 3; foreach (Point p in pts) for (int dy = -r; dy <= r; dy++) for (int dx = -r; dx <= r; dx++) { int x = p.X + dx, y = p.Y + dy; if (x >= 0 && y >= 0 && x < w && y < h) d[x, y] = true; }
        Bitmap o = new Bitmap(src); float[,,] v = new float[w, h, 3]; bool[,] known = new bool[w, h];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) { Color c = src.GetPixel(x, y); v[x, y, 0] = c.R; v[x, y, 1] = c.G; v[x, y, 2] = c.B; known[x, y] = !d[x, y]; }
        // remplissage : couches successives depuis le bord du masque (moyenne des voisins connus)
        bool left = true; while (left)
        {
            left = false; List<Point> add = new List<Point>(); List<float[]> val = new List<float[]>();
            for (int y = 1; y < h - 1; y++) for (int x = 1; x < w - 1; x++) if (!known[x, y])
            {
                float s0 = 0, s1 = 0, s2 = 0; int n = 0;
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++) if ((dx != 0 || dy != 0) && known[x + dx, y + dy]) { s0 += v[x + dx, y + dy, 0]; s1 += v[x + dx, y + dy, 1]; s2 += v[x + dx, y + dy, 2]; n++; }
                if (n > 0) { add.Add(new Point(x, y)); val.Add(new float[] { s0 / n, s1 / n, s2 / n }); }
            }
            for (int i = 0; i < add.Count; i++) { Point p = add[i]; v[p.X, p.Y, 0] = val[i][0]; v[p.X, p.Y, 1] = val[i][1]; v[p.X, p.Y, 2] = val[i][2]; known[p.X, p.Y] = true; left = true; }
        }
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) if (d[x, y]) o.SetPixel(x, y, Color.FromArgb((int)v[x, y, 0], (int)v[x, y, 1], (int)v[x, y, 2]));
        // ---- réparations : l'aiguille cachait des morceaux d'arcs et de chiffres (dessinés par-dessus, limités à la zone de l'aiguille)
        string name = Path.GetFileNameWithoutExtension(a[0]).Replace("S-metre ", "").ToUpperInvariant();
        Bitmap ov = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(ov))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; Pen arc = new Pen(Color.White, 3.0f); arc.StartCap = arc.EndCap = System.Drawing.Drawing2D.LineCap.Round; Brush wb = Brushes.White;
            g.DrawLine(arc, 49f, 143.5f, 78f, 134.5f);                      // arc du haut (S) coupé par l'aiguille
            g.DrawLine(arc, 128f, 211.5f, 153f, 203.5f);                    // arc du bas
            if (name == "COMP" || name == "ID" || name == "PO")
            {   // chiffre « 0 » du début de l'échelle : anneau italique
                g.TranslateTransform(124f, 166f); g.RotateTransform(10f);
                using (Pen ring = new Pen(Color.White, 7.0f)) g.DrawEllipse(ring, -11.5f, -16.5f, 23f, 33f);
                g.ResetTransform();
            }
            if (name == "SWR")
            {   // « 1 » et « . » de « 1.0 »
                g.FillPolygon(wb, new PointF[] { new PointF(107.5f, 142.5f), new PointF(114.5f, 142.5f), new PointF(105f, 179f), new PointF(97.5f, 179f) });
                g.FillPolygon(wb, new PointF[] { new PointF(108f, 142.5f), new PointF(98f, 152.5f), new PointF(94f, 149f), new PointF(104f, 142.5f) });
                g.FillEllipse(wb, 118.5f, 172.5f, 6f, 6f);
            }
        }
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) if (d[x, y]) { int al = ov.GetPixel(x, y).A; if (al > 0) { Color c = o.GetPixel(x, y); o.SetPixel(x, y, Color.FromArgb(Math.Max(c.R, al), Math.Max(c.G, al), Math.Max(c.B, al))); } }
        o.Save(a[1], ImageFormat.Png);
        if (a.Length > 2) { Rectangle rc = new Rectangle(0, 80, 300, 190); Bitmap z = new Bitmap(rc.Width * 3, rc.Height * 3); using (Graphics g = Graphics.FromImage(z)) { g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor; g.DrawImage(o, new Rectangle(0, 0, z.Width, z.Height), rc, GraphicsUnit.Pixel); } z.Save(a[2], ImageFormat.Png); }
    }
}
