using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace HamRadioLink.Journal
{
    // Déplacement visuel des colonnes du journal : on maintient le clic sur un en-tête puis on glisse ; la colonne entière (en-tête et lignes)
    // suit la souris, les autres colonnes s'écartent pour montrer la place d'arrivée ; on lâche pour déposer (Échap annule).
    // Un simple clic (sans glisser) reste un tri. Aucune donnée n'est touchée : seul l'ordre d'affichage change.
    public class ColumnDragger
    {
        readonly DataGridView grid; Overlay ov; DataGridViewColumn col; Point down; bool active; public bool Dragged;   // Dragged : le dernier appui était un glissement (il ne doit pas trier)
        Bitmap snap; int grab, target; List<DataGridViewColumn> order = new List<DataGridViewColumn>(); List<int> xs = new List<int>(), ws = new List<int>();

        public ColumnDragger(DataGridView g)
        {
            grid = g; grid.AllowUserToOrderColumns = false;   // on remplace le déplacement standard (simple trait) par celui-ci
            grid.MouseDown += OnDown; grid.MouseMove += OnMove; grid.MouseUp += OnUp; grid.KeyDown += (s, e) => { if (active && e.KeyCode == Keys.Escape) Stop(false); };
        }
        public bool Active { get { return active; } }

        void OnDown(object s, MouseEventArgs e)
        {
            Dragged = false; col = null; if (e.Button != MouseButtons.Left) return;
            DataGridView.HitTestInfo h = grid.HitTest(e.X, e.Y); if (h.Type != DataGridViewHitTestType.ColumnHeader || h.ColumnIndex < 0) return;
            Rectangle r = grid.GetColumnDisplayRectangle(h.ColumnIndex, false); if (e.X < r.Left + 5 || e.X > r.Right - 6) return;   // bord de la colonne : c'est le redimensionnement
            col = grid.Columns[h.ColumnIndex]; down = e.Location; grab = e.X - r.Left;
        }

        void OnMove(object s, MouseEventArgs e)
        {
            if (col == null) return;
            if (!active)
            {
                if (Math.Abs(e.X - down.X) < 6 && Math.Abs(e.Y - down.Y) < 6) return;
                try { Start(); } catch { active = false; try { if (ov != null) grid.Controls.Remove(ov); } catch { } ov = null; col = null; return; }
            }
            if (active) { ov.MouseX = e.X; target = Slot(e.X - grab + ws[order.IndexOf(col)] / 2); ov.Target = target; ov.Invalidate(); }
        }

        void OnUp(object s, MouseEventArgs e) { if (active) Stop(true); col = null; }

        void Start()
        {
            order.Clear(); xs.Clear(); ws.Clear();
            List<DataGridViewColumn> v = new List<DataGridViewColumn>(); foreach (DataGridViewColumn c in grid.Columns) if (c.Visible) v.Add(c);
            v.Sort((a, b) => a.DisplayIndex.CompareTo(b.DisplayIndex)); order = v; int x = -grid.HorizontalScrollingOffset + (grid.RowHeadersVisible ? grid.RowHeadersWidth : 0);
            foreach (DataGridViewColumn c in order) { xs.Add(x); ws.Add(c.Width); x += c.Width; }
            Size sz = grid.ClientSize; snap = new Bitmap(sz.Width, sz.Height);
            using (Graphics g = Graphics.FromImage(snap)) g.CopyFromScreen(grid.PointToScreen(Point.Empty), Point.Empty, sz);   // image exacte de la liste, thème compris
            ov = new Overlay { Snap = snap, Grid = grid, Order = order, Xs = xs, Ws = ws, Dragged = order.IndexOf(col), Grab = grab, Enabled = false, Bounds = new Rectangle(Point.Empty, sz) };
            grid.Controls.Add(ov); ov.BringToFront(); active = true; Dragged = true; target = ov.Dragged; ov.Target = target; ov.MouseX = down.X; grid.Cursor = Cursors.SizeAll;
        }

        // Place d'arrivée (0 = tout à gauche) parmi les colonnes autres que celle qu'on déplace, d'après le centre de la colonne déplacée.
        int Slot(int centerX)
        {
            int d = order.IndexOf(col), n = 0;
            for (int i = 0; i < order.Count; i++) { if (i == d) continue; if (xs[i] + ws[i] / 2 < centerX) n++; }
            return n;
        }

        void Stop(bool drop)
        {
            active = false; grid.Cursor = Cursors.Default;
            if (ov != null) { grid.Controls.Remove(ov); ov.Dispose(); ov = null; }
            if (snap != null) { snap.Dispose(); snap = null; }
            if (drop && col != null) { try { if (target != order.IndexOf(col)) col.DisplayIndex = target; } catch { } }
            grid.Invalidate();
        }

        // Calque affiché par-dessus la liste pendant le glissement.
        class Overlay : Control
        {
            public Bitmap Snap; public DataGridView Grid; public List<DataGridViewColumn> Order; public List<int> Xs, Ws; public int Dragged, Target, Grab, MouseX;
            public Overlay() { SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true); }
            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics; int h = Height, dw = Ws[Dragged];
                using (Brush bg = new SolidBrush(Grid.BackgroundColor)) g.FillRectangle(bg, ClientRectangle);
                int x = Xs.Count > 0 ? Xs[0] : 0, k = 0, gapX = -1;
                for (int i = 0; i < Order.Count; i++)
                {
                    if (i == Dragged) continue;
                    if (k == Target) { gapX = x; x += dw; }   // place libre laissée à la colonne qu'on déplace
                    g.DrawImage(Snap, new Rectangle(x, 0, Ws[i], h), new Rectangle(Xs[i], 0, Ws[i], h), GraphicsUnit.Pixel); x += Ws[i]; k++;
                }
                if (gapX < 0) gapX = x;
                using (Brush gb = new SolidBrush(Color.FromArgb(70, 80, 160, 255))) g.FillRectangle(gb, gapX, 0, dw, h);
                using (Pen gp = new Pen(Color.FromArgb(230, 90, 170, 255), 2f)) g.DrawRectangle(gp, gapX + 1, 1, dw - 2, h - 3);
                int px = Math.Max(0, Math.Min(Width - dw, MouseX - Grab));   // la colonne entière suit la souris
                using (Brush sh = new SolidBrush(Color.FromArgb(70, 0, 0, 0))) g.FillRectangle(sh, px + 5, 5, dw, h);
                ColorMatrix cm = new ColorMatrix(); cm.Matrix33 = 0.88f;
                using (ImageAttributes ia = new ImageAttributes()) { ia.SetColorMatrix(cm); g.DrawImage(Snap, new Rectangle(px, 0, dw, h), Xs[Dragged], 0, dw, h, GraphicsUnit.Pixel, ia); }
                using (Pen bp = new Pen(Color.FromArgb(255, 90, 170, 255), 2f)) g.DrawRectangle(bp, px, 0, dw - 1, h - 1);
            }
        }
    }
}

namespace HamRadioLink.Journal
{
    // Liste du journal avec redimensionnement des colonnes sur TOUTE la hauteur : le bord droit d'une colonne se saisit aussi dans les lignes, pas seulement dans l'en-tête.
    public class LogGrid : DataGridView
    {
        int resCol = -1, startX, startW; const int Hot = 4;

        int EdgeAt(int x, int y)
        {
            if (y <= ColumnHeadersHeight) return -1;   // l'en-tête garde son propre redimensionnement
            DataGridViewColumn last = null; foreach (DataGridViewColumn c in Columns) if (c.Visible && (last == null || c.DisplayIndex > last.DisplayIndex)) last = c;
            foreach (DataGridViewColumn c in Columns)
            {
                if (!c.Visible || c == last) continue;   // la dernière colonne remplit la largeur restante
                Rectangle r = GetColumnDisplayRectangle(c.Index, false); if (r.Width > 0 && Math.Abs(x - r.Right) <= Hot) return c.Index;
            }
            return -1;
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (resCol >= 0) { Columns[resCol].Width = Math.Max(Math.Max(24, Columns[resCol].MinimumWidth), startW + e.X - startX); return; }
            if (e.Button == MouseButtons.None) { if (EdgeAt(e.X, e.Y) >= 0) Cursor = Cursors.SizeWE; else if (Cursor == Cursors.SizeWE) Cursor = Cursors.Default; }
            base.OnMouseMove(e);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { int c = EdgeAt(e.X, e.Y); if (c >= 0) { resCol = c; startX = e.X; startW = Columns[c].Width; Capture = true; return; } }
            base.OnMouseDown(e);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (resCol >= 0) { resCol = -1; Capture = false; return; }
            base.OnMouseUp(e);
        }
    }
}
