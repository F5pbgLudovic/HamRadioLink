using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace HamRadioLink.Journal
{
    // Zoom du texte du journal, réglé séparément pour chaque zone : « main » (liste des contacts), « cl » (réglette du cluster à droite), « bot » (trames du cluster en bas).
    public static class LogZoom
    {
        public static int Clamp(int v) { return Math.Max(60, Math.Min(250, v)); }
        public static int Get(string zone) { return Clamp(App.Cfg.GetInt("zoom." + zone, 100)); }
        public static float Factor(string zone) { return Get(zone) / 100f; }
        // dir = +1 (agrandir) ou -1 (réduire), par pas de 10 % entre 60 % et 250 % ; renvoie le nouveau pourcentage
        public static int Step(string zone, int dir) { int v = Clamp(Get(zone) + 10 * dir); App.Cfg.Set("zoom." + zone, v.ToString()); App.Cfg.Save(); return v; }

        // remet les trois zones à 100 %
        public static void ResetAll() { foreach (string z in new string[] { "main", "cl", "bot" }) App.Cfg.Set("zoom." + z, "100"); App.Cfg.Save(); }

        // bouton rond « réinit » : flèche circulaire (↺), même taille que + et −
        public static void Reset(Graphics g, Rectangle r, bool hot)
        {
            SmoothingMode old = g.SmoothingMode; g.SmoothingMode = SmoothingMode.AntiAlias;
            Color fill = hot ? Theme.Get("lb.accent") : Theme.Get("lb.btn.back"), ink = hot ? Theme.Get("lb.accent.text") : Theme.Get("lb.btn.text");
            using (Brush b = new SolidBrush(fill)) g.FillEllipse(b, r);
            using (Pen p = new Pen(Theme.Get("lb.grid.line"))) g.DrawEllipse(p, r.X, r.Y, r.Width - 1, r.Height - 1);
            float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f, rad = r.Width * 0.27f;
            using (Pen p = new Pen(ink, 1.9f))
            {
                p.StartCap = LineCap.Round; p.EndCap = LineCap.Round;
                g.DrawArc(p, cx - rad, cy - rad, 2 * rad, 2 * rad, -60, 290);                       // cercle ouvert en haut à droite
                double ae = 230 * Math.PI / 180; float ex = cx + rad * (float)Math.Cos(ae), ey = cy + rad * (float)Math.Sin(ae);   // bout de l'arc (sens horaire) : pointe de flèche
                double tx = -Math.Sin(ae), ty = Math.Cos(ae);                                                                      // direction de la tangente
                foreach (double rot in new double[] { 2.6, -2.6 }) { double bx = tx * Math.Cos(rot) - ty * Math.Sin(rot), by = tx * Math.Sin(rot) + ty * Math.Cos(rot); g.DrawLine(p, ex, ey, ex + (float)bx * 4.6f, ey + (float)by * 4.6f); }
            }
            g.SmoothingMode = old;
        }

        // bouton rond « + » ou « − » aux couleurs du thème
        public static void Circle(Graphics g, Rectangle r, bool plus, bool hot)
        {
            SmoothingMode old = g.SmoothingMode; g.SmoothingMode = SmoothingMode.AntiAlias;
            Color fill = hot ? Theme.Get("lb.accent") : Theme.Get("lb.btn.back"), ink = hot ? Theme.Get("lb.accent.text") : Theme.Get("lb.btn.text");
            using (Brush b = new SolidBrush(fill)) g.FillEllipse(b, r);
            using (Pen p = new Pen(Theme.Get("lb.grid.line"))) g.DrawEllipse(p, r.X, r.Y, r.Width - 1, r.Height - 1);
            float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f, k = r.Width * 0.22f;
            using (Pen p = new Pen(ink, 2f)) { p.StartCap = LineCap.Round; p.EndCap = LineCap.Round; g.DrawLine(p, cx - k, cy, cx + k, cy); if (plus) g.DrawLine(p, cx, cy - k, cx, cy + k); }
            g.SmoothingMode = old;
        }
    }

    // Paire de boutons ronds − / + (un par zone de zoom)
    public class ZoomPair : Control
    {
        public event Action<int> Step; public bool Right; int hot;   // hot : 0 aucun, 1 moins, 2 plus
        const int D = 22;
        public ZoomPair() { SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); Size = new Size(60, 26); Cursor = Cursors.Hand; Theme.Changed += Redo; Disposed += delegate { Theme.Changed -= Redo; }; }
        void Redo() { Invalidate(); }
        Rectangle Minus { get { return new Rectangle(Right ? Width - 2 * D - 8 : 2, (Height - D) / 2, D, D); } }
        Rectangle Plus { get { return new Rectangle(Minus.Right + 4, (Height - D) / 2, D, D); } }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent != null ? Parent.BackColor : Theme.Get("lb.back"));
            LogZoom.Circle(e.Graphics, Minus, false, hot == 1); LogZoom.Circle(e.Graphics, Plus, true, hot == 2);
        }
        protected override void OnMouseMove(MouseEventArgs e) { int h = Minus.Contains(e.Location) ? 1 : Plus.Contains(e.Location) ? 2 : 0; if (h != hot) { hot = h; Invalidate(); } }
        protected override void OnMouseLeave(EventArgs e) { hot = 0; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button != MouseButtons.Left) return; Action<int> a = Step; if (a == null) return; if (Minus.Contains(e.Location)) a(-1); else if (Plus.Contains(e.Location)) a(1); }
    }
}
