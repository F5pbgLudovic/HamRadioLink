using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Windows.Forms;

namespace HamRadioLink
{
    public static class T
    {
        // échelle de la fenêtre (1 = taille de conception) et résolution de l'écran
        public static float Scale = 1f, Dpi = 96f;
        static readonly Dictionary<string, Font> fc = new Dictionary<string, Font>();
        public static Font Fn(string name, float pt) { return Fn(name, pt, FontStyle.Regular); }
        public static Font Fn(string name, float pt, FontStyle st)
        {
            string k = name + "|" + pt + "|" + (int)st + "|" + Scale.ToString("0.000"); Font f;
            lock (fc) { if (!fc.TryGetValue(k, out f)) { if (fc.Count > 300) fc.Clear(); f = new Font(name, Math.Max(4f, pt * Scale * 96f / Dpi), st); fc[k] = f; } }
            return f;
        }
        public static Color Bg = Color.FromArgb(12, 15, 21), Panel = Color.FromArgb(22, 27, 36), Panel2 = Color.FromArgb(30, 37, 49), Edge = Color.FromArgb(56, 68, 86);
        public static Color Text = Color.FromArgb(222, 230, 238), Dim = Color.FromArgb(128, 145, 163), Amber = Color.FromArgb(255, 178, 20), Cyan = Color.FromArgb(40, 205, 235);
        public static Color Red = Color.FromArgb(255, 66, 58), Green = Color.FromArgb(70, 224, 112), Blue = Color.FromArgb(70, 150, 255), Lock = Color.FromArgb(235, 30, 30), PttBg = Color.FromArgb(150, 64, 6), LockBtn = Color.FromArgb(225, 35, 35), Veil = Color.FromArgb(120, 18, 18), OnBg = Color.FromArgb(14, 36, 92), ConnOk = Color.FromArgb(28, 134, 50), ConnKo = Color.FromArgb(176, 34, 30);
        public static Color Acc(string n, Color def)
        {
            switch (n) { case "amber": return Amber; case "cyan": return Cyan; case "red": return Red; case "green": return Green; case "blue": return Blue; default: return def; }
        }
        public static GraphicsPath Round(RectangleF r, float rad)
        {
            GraphicsPath p = new GraphicsPath(); float d = Math.Max(1f, rad * 2); if (d > r.Width) d = r.Width; if (d > r.Height) d = r.Height;
            p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90); p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90); p.CloseFigure(); return p;
        }
        public static StringFormat Center() { StringFormat sf = new StringFormat(); sf.Alignment = StringAlignment.Center; sf.LineAlignment = StringAlignment.Center; sf.Trimming = StringTrimming.EllipsisCharacter; sf.FormatFlags = StringFormatFlags.NoWrap; return sf; }
    }

    // ------------------------------------------------------------------ base des éléments de la face avant
    // En « vue config », tous se déplacent à la souris (coin bas-droit = taille) et le clic droit ouvre l'éditeur de commande CAT.
    public abstract class HW : Control
    {
        public static bool Edit; public static int HeaderH = 118, Shift = 36; public static int WheelPct = 1, VeilPct = 67;      // VeilPct : opacité (10 à 100 %) du voile posé sur un organe verrouillé
        public static int VeilAlpha { get { return (int)Math.Round(Math.Max(10, Math.Min(100, VeilPct)) * 2.55); } }
        // verrou (clic droit sur un bouton ou un potentiomètre ; Ctrl + clic droit pour un choix) : bloque l'action, mémorisé par poste
        bool locked; public bool Locked { get { return locked; } set { if (locked != value) { locked = value; Invalidate(); } } }
        // FT-991A DF5DP : sur CTCSS ENC, FREQ CTCSS et CODE DCS, le clic droit ouvre la liste de toutes les valeurs au lieu de verrouiller
        bool DropOnRight { get { return Def.K == Kind.Choice && Rig != null && Rig.Def != null && Rig.Def.Key == "ft991a-df5dp" && (Def.Id == "ctcss" || Def.Id == "ctone" || Def.Id == "dcsc"); } }
        protected bool StepLike { get { return Def.K == Kind.Choice && Def.Id == "step"; } }   // bouton PAS : clic gauche = valeur suivante, clic droit = précédente, Ctrl + clic droit = verrou
        protected bool LeftDrop { get { return Def.K == Kind.Choice && Def.Id == "md1" && Rig != null && Rig.Def != null && Rig.Def.Key == "ftdx10"; } }   // mode du second VFO : clic gauche = liste déroulante
        bool Lockable { get { return Def.K == Kind.Toggle || Def.K == Kind.Action || Def.K == Kind.Knob || Def.K == Kind.Choice || Def.K == Kind.Chain || Def.K == Kind.Dsp || Def.K == Kind.Freq || Def.K == Kind.Dial; } }
        // essais automatiques : simulent un clic ou une molette sur l'élément (Ctrl simulé par CtrlOverride)
        public static bool CtrlOverride, NoSave;
        // Ctrl enfoncé (pendant les essais automatiques, seul CtrlOverride compte : le clavier réel de l'utilisateur ne doit pas fausser un essai)
        static bool CtrlHeld { get { return NoSave ? CtrlOverride : (CtrlOverride || (Control.ModifierKeys & Keys.Control) != 0); } }
        public void SimClick(MouseButtons b) { MouseEventArgs e = new MouseEventArgs(b, 1, 4, 4, 0); OnMouseDown(e); OnMouseUp(e); }
        public void SimWheel(int d) { OnMouseWheel(new MouseEventArgs(MouseButtons.None, 0, 4, 4, d)); }
        public string LockKey { get { return "lock." + (Rig != null && Rig.Def != null ? Rig.Def.Key : "") + "." + Def.Id; } }
        public Ctl Def; public Rig Rig; public float Scale = 1f;      // Scale : pixels par unité de conception (chaque poste affiché a la sienne)
        public event Action<HW> RequestEdit, Changed, Touched;
        // verrous : un organe entier, ou une case d'une rangée (HRow) ; nombre de verrous posés
        public virtual int LockCount { get { return locked ? 1 : 0; } }
        public virtual void LoadLock() { Locked = Cfg.GetBool(LockKey, false); }
        public virtual void UnlockAll() { if (locked) { Locked = false; Cfg.SetBool(LockKey, false); } }
        public void SimClickAt(MouseButtons b, Point p) { MouseEventArgs e = new MouseEventArgs(b, 1, p.X, p.Y, 0); OnMouseDown(e); OnMouseUp(e); }
        public static readonly int[] PttTimeouts = { 0, 30, 60, 120, 180, 300, 600, 900, 1200, 1500, 1800 };
        public static string TimeoutLabel(int sec) { return sec == 0 ? "Désactivé" : (sec < 60 ? sec + " s" : (sec / 60) + " min"); }
        void PttMenu(Point p)
        {
            ContextMenuStrip mn = new ContextMenuStrip(); mn.BackColor = T.Panel2; mn.ForeColor = T.Text;
            ToolStripMenuItem t = new ToolStripMenuItem("Délai de sécurité TX (touche PTT, jusqu'à 30 min)"); t.Enabled = false; mn.Items.Add(t); mn.Items.Add(new ToolStripSeparator());
            int cur = Rig.PttLimit; string rk = Rig.Def.Key;
            foreach (int sec in PttTimeouts)
            {
                int s2 = sec; ToolStripMenuItem it = new ToolStripMenuItem(TimeoutLabel(sec)); it.Checked = cur == sec;
                it.Click += delegate { Cfg.SetInt("ptt.timeout." + rk, s2); Cfg.Save(); CatLog.Event(rk, "délai de sécurité TX : " + TimeoutLabel(s2)); Invalidate(); };
                mn.Items.Add(it);
            }
            MicRoute.AddItems(mn);
            mn.Items.Add(new ToolStripSeparator()); ToolStripMenuItem lk = new ToolStripMenuItem(locked ? "Déverrouiller la touche" : "Verrouiller la touche");
            lk.Click += delegate { Locked = !locked; Cfg.SetBool(LockKey, locked); Cfg.Save(); }; mn.Items.Add(lk);
            mn.Show(this, p);
        }
        // FTdx10 : source audio du VOX (menu EX030405 « VOX SELECT » : 0 = micro, 1 = data)
        void VoxMenu(Point p)
        {
            ContextMenuStrip mn = new ContextMenuStrip(); mn.BackColor = T.Panel2; mn.ForeColor = T.Text;
            ToolStripMenuItem t = new ToolStripMenuItem("Source audio du VOX"); t.Enabled = false; mn.Items.Add(t); mn.Items.Add(new ToolStripSeparator());
            ToolStripMenuItem mic = new ToolStripMenuItem("VOX micro (MIC)"), dat = new ToolStripMenuItem("VOX data (DATA)"); mn.Items.Add(mic); mn.Items.Add(dat);
            Rig rg = Rig; Action<int> send = delegate(int v) { if (rg.Loading) return; rg.Raw("EX030405" + v + ";"); CatLog.Event(rg.Def.Key, "VOX : source " + (v == 0 ? "micro" : "data")); };
            mic.Click += delegate { send(0); }; dat.Click += delegate { send(1); };
            mn.Items.Add(new ToolStripSeparator()); ToolStripMenuItem lk = new ToolStripMenuItem(locked ? "Déverrouiller la touche (Ctrl + clic droit)" : "Verrouiller la touche (Ctrl + clic droit)");
            lk.Click += delegate { Locked = !locked; Cfg.SetBool(LockKey, locked); Cfg.Save(); }; mn.Items.Add(lk);
            new System.Threading.Thread(delegate()
            {   // état actuel lu sur le poste, la coche apparaît dès la réponse
                string a = null; for (int i = 0; i < 2 && a == null; i++) a = rg.AskPrefix("EX030405;", "EX030405", 700);
                int v; if (a != null && int.TryParse(a.Substring(a.IndexOf("EX030405") + 8).TrimEnd(';'), out v))
                    try { BeginInvoke(new Action(delegate { mic.Checked = v == 0; dat.Checked = v == 1; })); } catch { }
            }) { IsBackground = true }.Start();
            mn.Show(this, p);
        }
        void AfMenu(Point p)
        {
            ContextMenuStrip mn = new ContextMenuStrip(); mn.BackColor = T.Panel2; mn.ForeColor = T.Text;
            RxAudio.AddItems(mn, Rig);
            mn.Items.Add(new ToolStripSeparator()); ToolStripMenuItem lk = new ToolStripMenuItem(locked ? "Déverrouiller le bouton" : "Verrouiller le bouton");
            lk.Click += delegate { Locked = !locked; Cfg.SetBool(LockKey, locked); Cfg.Save(); }; mn.Items.Add(lk);
            mn.Show(this, p);
        }
        float UseScale() { float o = T.Scale; T.Scale = Scale; return o; }
        int mode; Point p0; Rectangle r0; const int Grid = 4;
        protected HW() { SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true); }
        public abstract void Sync();
        protected abstract void Draw(Graphics g);
        protected virtual void Down(MouseEventArgs e) { }
        protected virtual void Moving(MouseEventArgs e) { }
        protected virtual void Up(MouseEventArgs e) { }
        protected virtual void Wheel(MouseEventArgs e) { }
        protected virtual void DblClick(MouseEventArgs e) { }
        protected virtual void Leaving() { }
        protected bool Ready { get { return Rig != null; } }
        public void ApplyBounds() { float s = Scale > 0 ? Scale : T.Scale; Bounds = new Rectangle((int)Math.Round(Def.X * s), (int)Math.Round((Def.Y - HeaderH) * s), Math.Max(4, (int)Math.Round(Def.W * s)), Math.Max(4, (int)Math.Round(Def.H * s))); }

        protected override void OnPaint(PaintEventArgs e) { float o = UseScale(); try { PaintInner(e); } finally { T.Scale = o; } }
        void PaintInner(PaintEventArgs e)
        {
            Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            Draw(g);
                        if (locked && !Edit && Lockable) using (GraphicsPath lp = T.Round(new RectangleF(0.5f, 0.5f, Width - 2, Height - 2), 5 * T.Scale))
            {
                using (Brush gb = new SolidBrush(Color.FromArgb(VeilAlpha, T.Veil))) g.FillPath(gb, lp);      // voile de couleur (rouge foncé par défaut) : élément désactivé
                using (Pen pen = new Pen(T.Lock, 2f)) g.DrawPath(pen, lp);
            }
            if (Edit)
            {
                using (Pen p = new Pen(Color.FromArgb(220, 255, 200, 40), 1.5f)) { p.DashStyle = DashStyle.Dash; g.DrawRectangle(p, 1, 1, Width - 3, Height - 3); }
                using (Brush b = new SolidBrush(Color.FromArgb(230, 255, 200, 40))) g.FillRectangle(b, Width - 10, Height - 10, 9, 9);
                if (Def.HasCat && Def.Dirty) using (Brush b = new SolidBrush(Color.FromArgb(230, 70, 224, 112))) g.FillEllipse(b, 3, 3, 8, 8);
            }
        }
        protected sealed override void OnMouseDown(MouseEventArgs e) { float o = UseScale(); try { DownInner(e); } finally { T.Scale = o; } }
        void DownInner(MouseEventArgs e)
        {
            if (Touched != null) Touched(this);
            if (Edit)
            {
                if (e.Button == MouseButtons.Left) { mode = (e.X > Width - 14 && e.Y > Height - 14) ? 2 : 1; p0 = Control.MousePosition; r0 = new Rectangle(Def.X, Def.Y, Def.W, Def.H); Capture = true; if (Def.K == Kind.Frame) SendToBack(); else BringToFront(); }
                return;
            }
            if (locked && Lockable) return;
            Down(e); base.OnMouseDown(e);
        }
        protected sealed override void OnMouseMove(MouseEventArgs e) { float o = UseScale(); try { MoveInner(e); } finally { T.Scale = o; } }
        void MoveInner(MouseEventArgs e)
        {
            if (Edit)
            {
                if (mode != 0)
                {
                    Point cur = Control.MousePosition; float s = T.Scale; int dx = (int)Math.Round((cur.X - p0.X) / s), dy = (int)Math.Round((cur.Y - p0.Y) / s);
                    if (mode == 1) { Def.X = Snap(Math.Max(0, Math.Min(1860 - Def.W, r0.X + dx))); Def.Y = Snap(Math.Max(HeaderH, Math.Min(980 - Def.H, r0.Y + dy))); }
                    else { Def.W = Math.Max(24, Snap(Math.Min(1860 - Def.X, r0.Width + dx))); Def.H = Math.Max(16, Snap(Math.Min(980 - Def.Y, r0.Height + dy))); }
                    ApplyBounds();
                }
                else Cursor = (e.X > Width - 14 && e.Y > Height - 14) ? Cursors.SizeNWSE : Cursors.SizeAll;
                return;
            }
            Moving(e); base.OnMouseMove(e);
        }
        static int Snap(int v) { return (int)Math.Round(v / (double)Grid) * Grid; }
        protected sealed override void OnMouseUp(MouseEventArgs e) { float o = UseScale(); try { UpInner(e); } finally { T.Scale = o; } }
        void UpInner(MouseEventArgs e)
        {
            if (Edit)
            {
                if (mode != 0) { mode = 0; Capture = false; if (Changed != null) Changed(this); }
                else if (e.Button == MouseButtons.Right && RequestEdit != null) RequestEdit(this);
                return;
            }
            if (Def.Id == "af" && e.Button == MouseButtons.Right && Rig != null && Rig.Def != null && !NoSave && !CtrlHeld) { AfMenu(e.Location); return; }     
            if (Def.Id == "meq" && e.Button == MouseButtons.Right && Rig != null && Rig.Def != null && Rig.Def.Key == "ftdx10" && !NoSave && !CtrlHeld) { MicEqForm.Open(Rig, FindForm()); return; }   // clic droit sur MIC EQ : fenêtre de l'égaliseur paramétrique (Ctrl + clic droit : verrou)
            if ((Def.Id == "nb" || Def.Id == "dnr") && e.Button == MouseButtons.Right && Rig != null && Rig.Def != null && Rig.Def.Key == "ftdx10" && !NoSave && !CtrlHeld) { RxFxForm.Open(Rig, FindForm(), Def.Id); return; }   // clic droit sur NB / DNR : fenêtre de réglage détaillé (Ctrl + clic droit : verrou)
            if ((Def.Id == "ptt" || Def.Id == "mox") && Def.K == Kind.Toggle && e.Button == MouseButtons.Right && Rig != null && Rig.Def != null && !NoSave && !CtrlHeld) { PttMenu(e.Location); return; }
            if (Def.Id == "notch" && Def.K == Kind.Toggle && e.Button == MouseButtons.Right && Rig != null && Rig.Def != null && Rig.Def.Key == "ftdx10" && !NoSave && !CtrlHeld) { (this as HBtn).NotchAsync(true); return; }   // clic droit sur NOTCH : largeur du notch large / étroit (Ctrl + clic droit : verrou)
            if (Def.Id == "vox" && Def.K == Kind.Toggle && e.Button == MouseButtons.Right && Rig != null && Rig.Def != null && Rig.Def.Key == "ftdx10" && !NoSave && !CtrlHeld) { VoxMenu(e.Location); return; }   // clic droit sur VOX : VOX micro / VOX data (Ctrl + clic droit : verrou)      // clic droit sur PTT : délai de sécurité (Ctrl + clic droit : verrou)
            HRow hr = this as HRow; if (hr != null && e.Button == MouseButtons.Right && Rig != null) { hr.ToggleCell(e.Location); return; }      // rangée : le clic droit ne verrouille que la case touchée
            if (Lockable && e.Button == MouseButtons.Right && !(Def.K == Kind.Choice && !(this is HRow) && (StepLike ? !CtrlHeld : (DropOnRight ? !CtrlHeld : CtrlHeld))) && Rig != null) { Locked = !locked; Cfg.SetBool(LockKey, locked); if (!NoSave) Cfg.Save(); return; }
            if (locked && Lockable && !(DropOnRight && e.Button == MouseButtons.Right && !CtrlHeld)) return;
            Up(e); base.OnMouseUp(e);
        }
        protected sealed override void OnMouseWheel(MouseEventArgs e) { float o = UseScale(); try { if (!Edit && !(locked && Lockable)) Wheel(e); base.OnMouseWheel(e); } finally { T.Scale = o; } }
        protected sealed override void OnMouseDoubleClick(MouseEventArgs e) { float o = UseScale(); try { if (!Edit && !(locked && Lockable)) DblClick(e); base.OnMouseDoubleClick(e); } finally { T.Scale = o; } }
        protected sealed override void OnMouseLeave(EventArgs e) { float o = UseScale(); try { Leaving(); base.OnMouseLeave(e); } finally { T.Scale = o; } }
        protected Color Accent(Color def) { return T.Acc(Def.Accent, def); }
        protected string St(string id) { return Rig != null ? Rig.Get(id) : null; }
        public static string FmtVal(Ctl c, int v, Rig rig)
        {
            string kt = rig != null ? MeterCal.KnobText(rig.Def, c, v) : null; if (kt != null) return kt;   // Width en Hz, RF PWR en watts (étalonnage)
            switch (c.Fmt)
            {
                case "pct": return (c.Max > 0 ? (int)Math.Round(v * 100.0 / c.Max) : v) + "%";
                case "sgn": return (v > 0 ? "+" : "") + v;
                case "hz": return (v > 0 ? "+" : "") + v + " Hz";
                case "wpm": return v + " wpm";
                case "watt": return v + " W";
                case "ms": return v + " ms";
                case "pitch10": return (300 + 10 * v) + " Hz";
                case "pitch50": return (300 + 50 * v) + " Hz";
                case "x10hz": return (v * 10) + " Hz";
                case "apf": return ((v - 25) * 10 > 0 ? "+" : "") + ((v - 25) * 10) + " Hz";
                case "w": return WidthText(v);
                case "wd": return Dsp.WidthText(rig != null ? rig.Get(c.Id.EndsWith("b") ? "md1" : "md0") : "", v);
                case "x100hz": return (v * 100) + " Hz";
                default: return v.ToString();
            }
        }
        // largeur de filtre FTdx10 (tableau 3 du manuel CAT, colonne SSB)
        static readonly string[] SsbW = { "déf.", "déf.", "300", "400", "600", "850", "1100", "1200", "1500", "1650", "1800", "1950", "2100", "2250", "2400", "2450", "2500", "2600", "2700", "2800", "2900", "3000", "3200", "3500" };
        public static string WidthText(int v) { return v >= 0 && v < SsbW.Length ? (v < 2 ? SsbW[v] : SsbW[v] + " Hz") : "n°" + v; }
    }

    // ------------------------------------------------------------------ bouton (bascule, action, choix par clic)
    public class HBtn : HW
    {
        bool hot, down; long flash;
        public override void Sync() { Invalidate(); if (NotchBtn && Ready && (DateTime.Now - notchRead).TotalSeconds > 5) NotchAsync(false); }
        // FTdx10 : clic droit sur NOTCH = largeur du notch IF large / étroit (menu EX030204 : 0 étroit, 1 large) ; Ctrl + clic droit = verrou. La largeur courante est affichée sur la touche.
        string notchW; DateTime notchRead = DateTime.MinValue; volatile bool notchBusy;
        bool NotchBtn { get { return Def.Id == "notch" && Rig != null && Rig.Def != null && Rig.Def.Key == "ftdx10"; } }
        int ReadNotchW(Rig rg) { string a = null; for (int i = 0; i < 3 && a == null; i++) a = rg.AskPrefix("EX030204;", "EX030204", 900); int v; return a != null && int.TryParse(a.Substring(a.IndexOf("EX030204") + 8).TrimEnd(';'), out v) ? v : -1; }
        public void NotchAsync(bool flip)
        {
            Rig rg = Rig; if (notchBusy || rg == null || rg.Loading) return; notchBusy = true;
            new System.Threading.Thread(delegate()
            {
                try
                {
                    int v = ReadNotchW(rg);
                    if (v >= 0 && flip) { int n = v == 1 ? 0 : 1; rg.Raw("EX030204" + n + ";"); CatLog.Event(rg.Def.Key, "largeur du notch : " + (n == 1 ? "large" : "étroit")); System.Threading.Thread.Sleep(600); int w = ReadNotchW(rg); if (w >= 0) v = w; }
                    if (v >= 0) { notchW = v.ToString(); notchRead = DateTime.Now; try { BeginInvoke(new Action(Invalidate)); } catch { } }
                }
                catch { }
                finally { notchBusy = false; notchRead = DateTime.Now; }
            }) { IsBackground = true }.Start();
        }
        Action tuneCb;
        protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); tuneCb = delegate { Invalidate(); }; TuneMic.Changed += tuneCb; }
        protected override void OnHandleDestroyed(EventArgs e) { if (tuneCb != null) TuneMic.Changed -= tuneCb; tuneCb = null; base.OnHandleDestroyed(e); }
        protected override void Leaving() { hot = false; down = false; Invalidate(); }
        protected override void OnMouseEnter(EventArgs e) { hot = true; Invalidate(); base.OnMouseEnter(e); }
        // liste longue (CTCSS, DCS…) : fenêtre avec ascenseur et molette, au lieu d'un menu qui ne défile pas bien
        void LongPick(Item[] it, int cu, Point at)
        {
            Form f = new Form(); f.FormBorderStyle = FormBorderStyle.None; f.StartPosition = FormStartPosition.Manual; f.ShowInTaskbar = false; f.BackColor = T.Edge; f.Padding = new Padding(1);
            ListBox lb = new ListBox(); lb.Dock = DockStyle.Fill; lb.BackColor = T.Panel2; lb.ForeColor = T.Text; lb.BorderStyle = BorderStyle.None; lb.IntegralHeight = false; lb.Font = new Font("Segoe UI", 10f);
            foreach (Item q in it) lb.Items.Add(q.Label); if (cu >= 0 && cu < it.Length) lb.SelectedIndex = cu;
            int rows = Math.Min(it.Length, 16); f.ClientSize = new Size(Math.Max(Width, 190), rows * lb.ItemHeight + 4); f.Controls.Add(lb);
            Point sp = PointToScreen(at); Rectangle wa = Screen.FromPoint(sp).WorkingArea; if (sp.Y + f.Height > wa.Bottom) sp.Y = Math.Max(wa.Top, wa.Bottom - f.Height); if (sp.X + f.Width > wa.Right) sp.X = wa.Right - f.Width; f.Location = sp;
            bool done = false; Action close = delegate { if (!done) { done = true; f.Close(); } };
            lb.MouseUp += delegate(object s, MouseEventArgs ev) { int i = lb.IndexFromPoint(ev.Location); if (i >= 0 && i < it.Length) { Rig.Set(Def, it[i].Code); close(); } };
            lb.KeyDown += delegate(object s, KeyEventArgs ev) { if (ev.KeyCode == Keys.Escape) close(); else if (ev.KeyCode == Keys.Enter && lb.SelectedIndex >= 0) { Rig.Set(Def, it[lb.SelectedIndex].Code); close(); } };
            f.Deactivate += delegate { close(); }; f.Shown += delegate { lb.Focus(); }; f.Show(FindForm());
        }
        protected override void Down(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { down = true; Invalidate(); } }
        protected override void Up(MouseEventArgs e)
        {
            bool was = down; down = false; Invalidate(); if (!Ready) return;
            bool inside = ClientRectangle.Contains(e.Location);
            if (Def.K == Kind.Choice)
            {
                if (!inside || (e.Button != MouseButtons.Left && e.Button != MouseButtons.Right)) return;
                Item[] it = Def.ItemList; if (it.Length == 0) return; int cur = Cur(it);
                if (StepLike && e.Button == MouseButtons.Right) { Rig.Set(Def, it[cur <= 0 ? it.Length - 1 : cur - 1].Code); return; }   // sens inverse
                if (e.Button == MouseButtons.Right || (e.Button == MouseButtons.Left && LeftDrop))
                {
                    if (it.Length > 20) { LongPick(it, Cur(it), e.Location); return; }
                    ContextMenuStrip m = new ContextMenuStrip(); m.BackColor = T.Panel2; m.ForeColor = T.Text; int cu = Cur(it);
                    for (int i = 0; i < it.Length; i++) { Item q = it[i]; ToolStripMenuItem mi = new ToolStripMenuItem(q.Label); mi.Checked = i == cu; mi.Click += delegate { Rig.Set(Def, q.Code); }; m.Items.Add(mi); }
                    m.Show(this, new Point(e.X, e.Y)); return;
                }
                int n = e.Button == MouseButtons.Right ? (cur <= 0 ? it.Length - 1 : cur - 1) : (cur + 1) % it.Length;
                Rig.Set(Def, it[n].Code); return;
            }
            if (!was || !inside || e.Button != MouseButtons.Left) return;
            if (Def.Id == "tunemic") { TuneMic.Toggle(Rig); Invalidate(); return; }
            if (Def.Id == "menu" && Rig != null) { if (Rig.Def != null && Rig.Def.Key == "ftdx10") Ftdx10MenuForm.Open(Rig, FindForm()); else RigMenu.Open(Rig, FindForm()); return; }
            if (Def.K == Kind.Toggle) { bool turnOn = !Rig.IsOn(Def); Rig.Set(Def, turnOn ? Def.OnSend : Def.OffSend); if (Def.Id == "ptt" || Def.Id == "mox") { if (turnOn) Rig.PttButtonStart(); else Rig.PttButtonStop(); } }
            else if (Def.K == Kind.Action)
            {
                if (Def.Id == "poff" && MessageBox.Show(FindForm(), "Éteindre le poste ?", "Poste", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                flash = Environment.TickCount; Rig.Set(Def, null); Invalidate();
                Timer t = new Timer(); t.Interval = 180; t.Tick += delegate { t.Stop(); t.Dispose(); Invalidate(); }; t.Start();
            }
        }
        // touche PTT : pendant une émission lancée par la touche avec un délai de sécurité, le compte à rebours remplace le nom
        string Lbl() { if (Def.Id == "tunemic" && TuneMic.ActiveFor(Rig)) return "● TUNE  " + TuneMic.Left; if ((Def.Id == "ptt" || Def.Id == "mox") && Def.K == Kind.Toggle && Ready && Rig.PttByButton && Rig.IsOn(Def)) { int l = Rig.PttLeft; if (l >= 0) return "\u25CF TX  " + (l / 60) + ":" + (l % 60).ToString("00"); } return Def.Label + (NotchBtn && notchW != null ? (notchW == "1" ? " LARGE" : " ÉTROIT") : ""); }
        int Cur(Item[] it) { string v = St(Def.Id); for (int i = 0; i < it.Length; i++) if (it[i].Match(v ?? "\u0001")) return i; return -1; }
        protected override void Draw(Graphics g)
        {
            g.Clear(Parent != null ? Parent.BackColor : T.Panel);
            bool lk = Locked && !HW.Edit; bool on = Ready && Def.K == Kind.Toggle && Rig.IsOn(Def); bool flashing = Def.K == Kind.Action && Environment.TickCount - flash < 180;
            bool tmx = Def.Id == "tunemic"; if (tmx) on = TuneMic.ActiveFor(Rig);
            Color acc = Accent(T.Amber);
            RectangleF r = new RectangleF(0.5f, 0.5f, Width - 2, Height - 2);
            Color top = on || flashing ? ControlPaint.Light(T.OnBg, 0.12f) : Color.FromArgb(46, 55, 70), bot = on || flashing ? T.OnBg : Color.FromArgb(28, 34, 45);
            if (Def.Id == "ptt" && Def.K == Kind.Toggle)      // touche ÉMISSION : toute la case VERTE au repos, ROUGE en émission
            {
                Color pc = on ? Color.FromArgb(T.Red.R * 80 / 100, T.Red.G * 55 / 100, T.Red.B * 55 / 100) : Color.FromArgb(T.Green.R * 40 / 100, T.Green.G * 60 / 100, T.Green.B * 45 / 100);
                top = ControlPaint.Light(pc, 0.12f); bot = ControlPaint.Dark(pc, 0.12f);
            }
            if (tmx) { Color pc = on ? TuneMic.ColOn : TuneMic.ColOff; top = ControlPaint.Light(pc, 0.12f); bot = ControlPaint.Dark(pc, 0.12f); }
            if (hot) { top = ControlPaint.Light(top, 0.15f); bot = ControlPaint.Light(bot, 0.1f); }
            if (down) { Color t = top; top = bot; bot = t; }
            using (GraphicsPath p = T.Round(r, 5 * T.Scale))
            {
                using (LinearGradientBrush b = new LinearGradientBrush(r, top, bot, 90f)) g.FillPath(b, p);
                using (Pen pen = new Pen(on || flashing ? acc : T.Edge, on ? 1.6f : 1f)) g.DrawPath(pen, p);
            }
            float k = T.Scale, tx = 0; bool nar = Def.K == Kind.Toggle && Def.Id == "narrow" && Rig != null && Rig.Def != null && Rig.Def.Key == "ftdx10";   // NAR (à gauche de MODE VFO-B) : voyant en haut à gauche, texte sur la ligne du mode, même police et même taille
            if (Def.K == Kind.Toggle)
            {
                float d = 7 * k, lx = nar ? Width / 2f - 3.5f * k : 8 * k, ly = nar ? 2 * k + Height * 0.34f / 2 - d / 2 : Height / 2f - d / 2;
                using (Brush b = new SolidBrush(lk ? Color.FromArgb(96, 102, 112) : (on ? acc : Color.FromArgb(60, 68, 80)))) g.FillEllipse(b, lx, ly, d, d);      // coche grisée si verrouillé
                if (on && !lk) using (Brush b = new SolidBrush(Color.FromArgb(60, acc))) g.FillEllipse(b, lx - 3 * k, ly - 3 * k, d + 6 * k, d + 6 * k);
                tx = 8 * k;
            }
            using (StringFormat sf = T.Center())
            {
                if (Def.K == Kind.Choice)
                {
                    Item[] it = Def.ItemList; int c = Ready ? Cur(it) : -1; string val = c >= 0 ? it[c].Label : "—";
                    float lh = Height * 0.34f;
                    using (Brush b = new SolidBrush(T.Dim)) g.DrawString(Def.Label, T.Fn("Segoe UI", 8f), b, new RectangleF(0, 2 * k, Width, lh), sf);
                    using (Brush b = new SolidBrush(c >= 0 ? T.Text : T.Dim)) g.DrawString(val, T.Fn("Segoe UI Semibold", Height > 60 * k ? 14f : 10.5f), b, new RectangleF(2, lh, Width - 4, Height - lh - 2 * k), sf);
                }
                else if (nar) { float lh = Height * 0.34f; using (Brush b = new SolidBrush(on ? T.Text : Color.FromArgb(196, 208, 222))) g.DrawString(Lbl(), T.Fn("Segoe UI Semibold", Height > 60 * k ? 14f : 10.5f), b, new RectangleF(2, lh, Width - 4, Height - lh - 2 * k), sf); }   // même ligne, même police et même taille que le mode
                else using (Brush b = new SolidBrush(on ? T.Text : Color.FromArgb(196, 208, 222))) g.DrawString(Lbl(), T.Fn("Segoe UI Semibold", 9f), b, new RectangleF(tx, 0, Width - tx - 2, Height), sf);
            }
        }
    }

    // ------------------------------------------------------------------ rangée de boutons (bandes, modes...)
    public class HRow : HW
    {
        int hotI = -1; Rectangle[] cells = new Rectangle[0];
        public override void Sync() { Invalidate(); }
        void Lay()
        {
            Item[] it = Def.ItemList; int n = it.Length, rows = Math.Max(1, Def.Rows), cols = n == 0 ? 1 : (int)Math.Ceiling(n / (double)rows);
            if (cells.Length != n) cells = new Rectangle[n]; int gap = Math.Max(2, (int)(4 * T.Scale));
            float cw = (Width - gap * (cols - 1)) / (float)cols, ch = (Height - gap * (rows - 1)) / (float)rows;
            for (int i = 0; i < n; i++) { int cx = i % cols, cy = i / cols; cells[i] = new Rectangle((int)(cx * (cw + gap)), (int)(cy * (ch + gap)), (int)cw, (int)ch); }
        }
        int Hit(Point p) { for (int i = 0; i < cells.Length; i++) if (cells[i].Contains(p)) return i; return -1; }
        protected override void Leaving() { hotI = -1; Invalidate(); }
        protected override void Moving(MouseEventArgs e) { int h = Hit(e.Location); if (h != hotI) { hotI = h; Invalidate(); } }
        // verrous par case : « LSB », « USB »… se verrouillent chacune séparément (clé lock.<poste>.<id>.<code>)
        readonly HashSet<string> lockedCells = new HashSet<string>();
        string CellKey(Item it) { return LockKey + "." + it.Code; }
        public override int LockCount { get { return lockedCells.Count; } }
        public override void LoadLock() { lockedCells.Clear(); foreach (Item it in Def.ItemList) if (Cfg.GetBool(CellKey(it), false)) lockedCells.Add(it.Code); Invalidate(); }
        public override void UnlockAll() { foreach (string c in lockedCells) Cfg.SetBool(LockKey + "." + c, false); lockedCells.Clear(); Invalidate(); }
        public bool CellLocked(int i) { Item[] it = Def.ItemList; return i >= 0 && i < it.Length && lockedCells.Contains(it[i].Code); }
        public Point CellCenter(int i) { Lay(); return new Point(cells[i].X + cells[i].Width / 2, cells[i].Y + cells[i].Height / 2); }
        public int CellCount { get { return Def.ItemList.Length; } }
        public void ToggleCell(Point p)
        {
            Lay(); int h = Hit(p); Item[] it = Def.ItemList; if (h < 0 || h >= it.Length) return;
            if (!lockedCells.Remove(it[h].Code)) lockedCells.Add(it[h].Code);
            Cfg.SetBool(CellKey(it[h]), lockedCells.Contains(it[h].Code)); if (!NoSave) Cfg.Save(); Invalidate();
        }
        protected override void Up(MouseEventArgs e)
        {
            if (!Ready || e.Button != MouseButtons.Left) return; Lay(); int h = Hit(e.Location); Item[] it = Def.ItemList; if (h < 0 || h >= it.Length) return;
            if (lockedCells.Contains(it[h].Code)) return;
            Rig.Set(Def, it[h].Code);
        }
        string ActiveCode()
        {
            if (!Ready) return null;
            if (Def.Id == "band") { string fa = Rig.Get("fa"); long f; return fa != null && long.TryParse(fa, out f) ? Radios.BandCode(f) : null; }
            return St(Def.Id);
        }
        protected override void Draw(Graphics g)
        {
            g.Clear(Parent != null ? Parent.BackColor : T.Panel); Lay(); Item[] it = Def.ItemList; string act = ActiveCode(); Color acc = Accent(T.Amber); float k = T.Scale;
            using (StringFormat sf = T.Center())
                for (int i = 0; i < it.Length; i++)
                {
                    Rectangle c = cells[i]; bool on = act != null && it[i].Match(act); bool hot = i == hotI;
                    RectangleF r = new RectangleF(c.X + 0.5f, c.Y + 0.5f, c.Width - 1, c.Height - 1);
                    Color top = on ? ControlPaint.Light(T.OnBg, 0.12f) : Color.FromArgb(46, 55, 70), bot = on ? T.OnBg : Color.FromArgb(28, 34, 45);
                    if (hot) { top = ControlPaint.Light(top, 0.15f); bot = ControlPaint.Light(bot, 0.1f); }
                    using (GraphicsPath p = T.Round(r, 4 * k))
                    {
                        using (LinearGradientBrush b = new LinearGradientBrush(r, top, bot, 90f)) g.FillPath(b, p);
                        using (Pen pen = new Pen(on ? acc : T.Edge, on ? 1.6f : 1f)) g.DrawPath(pen, p);
                    }
                    using (Brush b = new SolidBrush(on ? acc : Color.FromArgb(196, 208, 222))) g.DrawString(it[i].Label, T.Fn("Segoe UI Semibold", 9f), b, r, sf);
                    if (lockedCells.Contains(it[i].Code)) using (GraphicsPath lp = T.Round(r, 4 * k)) { using (Brush vb = new SolidBrush(Color.FromArgb(VeilAlpha, T.Veil))) g.FillPath(vb, lp); using (Pen lpn = new Pen(T.Lock, 2f)) g.DrawPath(lpn, lp); }
                }
        }
    }

    // ------------------------------------------------------------------ potentiomètre
    public class HKnob : HW
    {
        int val, dragY, dragV; bool dragging;
        int calVer = -1;
        public override void Sync() { if (calVer != MeterCal.Ver) { calVer = MeterCal.Ver; Invalidate(); } if (dragging || !Ready) return; if (Ftdx10Clar) { int cv = Def.Id == "clartxk" ? Rig.ClarTx : Rig.ClarRx; if (cv != val) { val = cv; Invalidate(); } return; } string s = St(Def.Id); int v; if (s != null && int.TryParse(s, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out v)) { v = ClarShown(v); if (v != val) { val = v; Invalidate(); } } }
        int Mx { get { return Rig != null && Rig.Def != null ? MeterCal.KnobMax(Rig.Def, Def) : Def.Max; } }   // pleine échelle du bouton (étalonnage RF PWR : valeur du poste correspondant à la puissance max)
        int Clamp(int v) { int mx = Mx; if (v < Def.Min) v = Def.Min; if (v > mx) v = mx; int st = Math.Max(1, Def.Step); if (st > 1) v = (int)Math.Round(v / (double)st) * st; if (v < Def.Min) v = Def.Min; if (v > mx) v = mx; return v; }
        // FTdx10 : un seul décalage de clarifier dans le poste, commun à RX et TX. CLAR RX et CLAR TX ne bougent pas ensemble : chaque bouton n'affiche le décalage que si son clarifier est actif,
        // et tourner l'un active son clarifier et coupe l'autre.
        bool Ftdx10Clar { get { return (Def.Id == "clar" || Def.Id == "clartxk") && Rig != null && Rig.Def != null && Rig.Def.Key == "ftdx10"; } }
        bool FlagOn(string id) { Ctl c = Rig.Def.Find(id); return c != null && Rig.IsOn(c); }
        int ClarShown(int v) { if (!Ftdx10Clar) return v; bool rx = FlagOn("clarrx"), tx = FlagOn("clartx"); return Def.Id == "clartxk" ? (tx ? v : 0) : (tx && !rx ? 0 : v); }
        void ClarFlags()
        {
            Ctl rx = Rig.Def.Find("clarrx"), tx = Rig.Def.Find("clartx"); if (rx == null || tx == null) return; bool rxOn = Rig.IsOn(rx), txOn = Rig.IsOn(tx);
            if (Def.Id == "clartxk") { if (!txOn) Rig.Set(tx, tx.OnSend); if (rxOn) Rig.Set(rx, rx.OffSend); } else { if (!rxOn) Rig.Set(rx, rx.OnSend); if (txOn) Rig.Set(tx, tx.OffSend); }
        }
        void User(int v) { v = Clamp(v); if (v == val) return; val = v; Invalidate(); if (Ready) { if (Ftdx10Clar) Rig.ClarMove(Def.Id == "clartxk", v); else Rig.Set(Def, v.ToString()); } }
        protected override void Down(MouseEventArgs e) { Focus(); if (e.Button == MouseButtons.Left) { dragging = true; dragY = e.Y; dragV = val; Capture = true; } }
        bool Fine { get { return (Def.Id == "clar" || Def.Id == "clartxk") && Def.Fmt == "hz" && Def.Step >= 1; } }   // CLAR : un cran de souris (3 pixels, 1 cran de molette) = un pas de 10 Hz
        protected override void Moving(MouseEventArgs e)
        {
            if (!dragging) return; if (Fine) { int nst = (int)((dragY - e.Y) / (((Control.ModifierKeys & Keys.Shift) != 0 ? 12.0 : 3.0) * T.Scale)); User(dragV + nst * Math.Max(1, Def.Step)); return; } double sens = (Control.ModifierKeys & Keys.Shift) != 0 ? 0.25 : 1.0;
            User(dragV + (int)((dragY - e.Y) * sens * (Mx - Def.Min) / (150.0 * T.Scale)));
        }
        protected override void Up(MouseEventArgs e) { dragging = false; Capture = false; }
        protected override void DblClick(MouseEventArgs e) { if (Def.Min < 0 && Def.Max > 0) User(0); }
        int lastWh, fastWh;   // molette rapide sur CLAR : le pas passe à 100 Hz, il revient à 10 Hz dès que la molette ralentit
        protected override void Wheel(MouseEventArgs e) { if (Fine) { int now = Environment.TickCount; fastWh = now - lastWh < 150 ? fastWh + 1 : 0; lastWh = now; int st = fastWh >= 2 ? Math.Max(100, Def.Step) : Math.Max(1, Def.Step); User(val + (e.Delta > 0 ? 1 : -1) * st); return; } int s = Math.Max(Math.Max(1, Def.Step), (int)Math.Round((Mx - Def.Min) * WheelPct / 100.0)); if ((Control.ModifierKeys & Keys.Shift) != 0) s = Math.Max(Math.Max(1, Def.Step), s / 4); User(val + (e.Delta > 0 ? s : -s)); }
        protected override void Draw(Graphics g)
        {
            g.Clear(Parent != null ? Parent.BackColor : T.Panel); float k = T.Scale;
            float d = Math.Max(10f, Math.Min(Width - 8 * k, Height - 32 * k)); float cx = Width / 2f, cy = 6 * k + d / 2f;
            RectangleF r = new RectangleF(cx - d / 2, cy - d / 2, d, d); Color acc = Accent(T.Cyan);
            using (Brush b = new SolidBrush(Color.FromArgb(18, 22, 30))) g.FillEllipse(b, r.X + 3 * k, r.Y + 3 * k, d - 6 * k, d - 6 * k);
            RectangleF ar = new RectangleF(r.X + 1.5f * k, r.Y + 1.5f * k, d - 3 * k, d - 3 * k);
            using (Pen p = new Pen(Color.FromArgb(52, 62, 78), 4f * k)) { p.StartCap = LineCap.Round; p.EndCap = LineCap.Round; g.DrawArc(p, ar, 135, 270); }
            int mxd = Mx; double t = mxd > Def.Min ? Math.Max(0, Math.Min(1, (val - Def.Min) / (double)(mxd - Def.Min))) : 0; bool center = Def.Min < 0 && Def.Max > 0;
            using (Pen p = new Pen(acc, 4f * k))
            {
                p.StartCap = LineCap.Round; p.EndCap = LineCap.Round;
                if (center) { float sw = (float)((t - 0.5) * 270); if (Math.Abs(sw) > 0.5) g.DrawArc(p, ar, 270, sw); }
                else if (t > 0.002) g.DrawArc(p, ar, 135, (float)(270 * t));
            }
            double ang = (135 + 270 * t) * Math.PI / 180.0; float rr = d / 2 - 6 * k;
            using (Pen p = new Pen(T.Text, 2f * k)) { p.EndCap = LineCap.Round; g.DrawLine(p, cx + (float)Math.Cos(ang) * (rr * 0.3f), cy + (float)Math.Sin(ang) * (rr * 0.3f), cx + (float)Math.Cos(ang) * rr, cy + (float)Math.Sin(ang) * rr); }
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center; bool have = Ready && St(Def.Id) != null;
                using (Brush b = new SolidBrush(have ? T.Text : T.Dim)) g.DrawString(have ? FmtVal(Def, val, Rig) : "—", T.Fn("Segoe UI Semibold", 8.5f), b, new RectangleF(0, Height - 28 * k, Width, 14 * k), sf);
                using (Brush b = new SolidBrush(T.Dim)) g.DrawString(Def.Label, T.Fn("Segoe UI", 7.5f), b, new RectangleF(0, Height - 15 * k, Width, 14 * k), sf);
            }
        }
    }

    // ------------------------------------------------------------------ bargraphe
    public class HMeter : HW
    {
        double val, peak; int peakTick; int shown = -1; string txt = ""; Sc sc; int scVer = -1;
        double F(double x) { return sc != null ? sc.Frac(x) : x / Math.Max(1, Def.Max); }
        public static int HoldMs { get { return Math.Max(500, Math.Min(20000, Cfg.GetInt("meter.hold", 10000))); } }
        static readonly string[] sT = { "1", "3", "5", "7", "9", "+20", "+40", "+60" }; static readonly double[] sP = { 0.07, 0.19, 0.31, 0.43, 0.5, 0.67, 0.83, 1.0 };
        public override void Sync()
        {
            if (!Ready) return; string s = St(Def.Id); int v = 0; if (s != null) int.TryParse(s, out v); if (Def.TxOnly && !Rig.Tx) v = 0;
            double nv = Math.Max(0, Math.Min(Def.Max, v)); int tick = Environment.TickCount;
            if (nv >= peak) { peak = nv; peakTick = tick; } else if (tick - peakTick > HoldMs) peak = Math.Max(nv, peak - Def.Max * 0.02);
            if (Math.Abs(nv - val) > 0.01 || peak != nv || v != shown || scVer != MeterCal.Ver) { scVer = MeterCal.Ver; sc = Rig != null ? MeterCal.Scale(Rig.Def, Def) : null; val = nv; shown = v; txt = Text2(peak > nv + 0.01 ? (int)Math.Round(peak) : v); Invalidate(); }
        }
        string Text2(int v)
        {
            if (sc != null) return sc.Text(v);
            if (Def.Fmt == "s") { double f = v / (double)Def.Max; if (f <= 0.5) return "S" + (int)Math.Round(f / 0.5 * 9); return "S9+" + (int)Math.Round((f - 0.5) / 0.5 * 60); }
            return Def.Max > 0 ? (int)Math.Round(v * 100.0 / Def.Max) + "%" : v.ToString();
        }
        // ----- aiguille (clic droit : barres <-> aiguille, mémorisé par vumètre)
        public bool Analog; bool anInit;
        protected override void Up(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || Rig == null || Rig.Def == null) return;
            if (BigMeterCfg.Applies(Rig.Def)) { BigMeterCfg.Toggle(Rig.Def); return; }   // les vumètres recouverts par le grand S-mètre sont remplacés par lui
            Analog = !Analog; Cfg.SetBool("analog." + Rig.Def.Key + "." + Def.Id, Analog); Cfg.Save(); Invalidate();
        }
        void DrawAnalog(Graphics g, string[] tt, double[] tp)
        {
            float k = T.Scale; RectangleF box = new RectangleF(0.5f, 1.5f, Width - 2, Height - 3);
            using (GraphicsPath bp = T.Round(box, 4 * k)) { using (Brush b = new SolidBrush(Color.FromArgb(6, 8, 12))) g.FillPath(b, bp); using (Pen p = new Pen(T.Edge)) g.DrawPath(p, bp); g.SetClip(bp); }
            float lw = 46 * k, tw = 58 * k; float x0 = lw, wA = Width - lw - tw; if (wA < 20) { g.ResetClip(); return; }
            float apexY = Math.Min(Height * 0.42f, 16 * k) + 1.5f; float sag = Math.Max(2f, (Height - apexY - 4 * k) * 0.55f);
            double th = Math.Max(0.06, Math.Min(0.5, 2 * Math.Atan(2 * sag / wA))); float R = (wA / 2) / (float)Math.Sin(th); float cx = x0 + wA / 2, py = apexY + R;
            Func<float, double, PointF> P = delegate(float r, double a) { return new PointF(cx + r * (float)Math.Sin(a), py - r * (float)Math.Cos(a)); };
            double hotF = sc != null ? (sc.NoHot ? 2.0 : sc.Hot) : Def.Fmt == "s" ? 0.5 : 0.8; float a0 = (float)(-90 - th * 180 / Math.PI), sw = (float)(2 * th * 180 / Math.PI); RectangleF rc = new RectangleF(cx - R, py - R, 2 * R, 2 * R);
            using (Pen p = new Pen(sc != null && sc.BlueRed ? T.Blue : Color.FromArgb(235, 235, 235), Math.Max(1f, 1.6f * k))) g.DrawArc(p, rc, a0, (float)(sw * Math.Min(1.0, hotF)));
            using (Pen p = new Pen(T.Red, Math.Max(1.5f, 2.6f * k))) g.DrawArc(p, rc, a0 + (float)(sw * Math.Min(1.0, hotF)), (float)(sw * Math.Max(0.0, 1 - hotF)));
            double prev = 0;
            for (int i = 0; i <= tt.Length; i++)
            {
                double cu = i < tt.Length ? tp[i] : 1.0;
                for (int q = 1; q < 5; q++) { double f = prev + (cu - prev) * q / 5.0; double a = -th + 2 * th * f; using (Pen p = new Pen(f > hotF ? T.Red : Color.FromArgb(200, 200, 200), 1f)) g.DrawLine(p, P(R, a), P(R + 2.5f * k, a)); }
                prev = cu;
            }
            for (int i = 0; i < tt.Length; i++)
            {
                double a = -th + 2 * th * tp[i]; Color c = tp[i] > hotF ? T.Red : Color.FromArgb(235, 235, 235);
                using (Pen p = new Pen(c, Math.Max(1f, 1.4f * k))) g.DrawLine(p, P(R, a), P(R + 5 * k, a));
                PointF pt = P(R + 6 * k, a); SizeF sz = g.MeasureString(tt[i], T.Fn("Segoe UI", 6.5f)); using (Brush b = new SolidBrush(c)) g.DrawString(tt[i], T.Fn("Segoe UI", 6.5f), b, pt.X - sz.Width / 2 + 1, pt.Y - sz.Height + 1);
            }
            double fr = Math.Max(0, Math.Min(1, F(val))), ang = -th + 2 * th * fr;
            if (!(Def.TxOnly && !(Ready && Rig.Tx))) { double pf = Math.Max(0, Math.Min(1, F(peak))), pa = -th + 2 * th * pf; if (pf > fr + 0.02) using (Pen p = new Pen(T.Red, Math.Max(1f, 1.6f * k))) g.DrawLine(p, P(R - 6 * k, pa), P(R + 2 * k, pa)); }
            double ca = Math.Cos(ang); float r0 = (float)((py - (Height - 3 * k)) / Math.Max(0.2, ca));
            using (Pen p = new Pen(Color.White, Math.Max(1.5f, 2f * k))) { p.StartCap = LineCap.Round; p.EndCap = LineCap.Round; g.DrawLine(p, P(r0, ang), P(R + 3 * k, ang)); }
            g.ResetClip(); bool dim = Def.TxOnly && !(Ready && Rig.Tx);
            using (Brush b = new SolidBrush(T.Dim)) g.DrawString(Def.Label, T.Fn("Segoe UI Semibold", 8f), b, 4 * k, 2 * k);
            using (StringFormat sf = new StringFormat()) { sf.Alignment = StringAlignment.Far; sf.LineAlignment = StringAlignment.Center; using (Brush b = new SolidBrush(dim ? T.Dim : T.Text)) g.DrawString(dim ? "—" : txt, T.Fn("Segoe UI Semibold", 8.5f), b, new RectangleF(Width - tw - 2, 0, tw, Height), sf); }
        }
        protected override void Draw(Graphics g)
        {
            g.Clear(Parent != null ? Parent.BackColor : T.Panel);
            if (!anInit && Rig != null && Rig.Def != null) { anInit = true; Analog = !BigMeterCfg.Applies(Rig.Def) && Cfg.GetBool("analog." + Rig.Def.Key + "." + Def.Id, Cfg.GetBool("analog.all", false)); }   // avec le grand S-mètre, les barres restent des barres (anciens réglages « aiguille » ignorés)
            if (sc == null && Rig != null && scVer != MeterCal.Ver) { scVer = MeterCal.Ver; sc = MeterCal.Scale(Rig.Def, Def); }
            if (Analog) { DrawAnalog(g, sc != null ? sc.Ticks : Def.Fmt == "s" ? sT : new string[] { "0", "25", "50", "75", "100" }, sc != null ? sc.Pos : Def.Fmt == "s" ? sP : new double[] { 0, 0.25, 0.5, 0.75, 1 }); return; }
            float k = T.Scale; int tw = (int)(60 * k), lw = (int)(70 * k);
            Rectangle bar = new Rectangle(lw, (int)(3 * k), Width - lw - tw - (int)(4 * k), (int)(Math.Min(14, Height * 0.42f) * k) + 4);
            bool tx = Ready && Rig.Tx; bool dim = Def.TxOnly && !tx;
            using (Brush b = new SolidBrush(T.Dim)) g.DrawString(Def.Label, T.Fn("Segoe UI Semibold", 8f), b, 0, 2 * k);
            using (Brush b = new SolidBrush(Color.FromArgb(8, 10, 14))) g.FillRectangle(b, bar);
            int seg = Math.Max(3, (int)(4 * k)), gap = Math.Max(1, (int)k); int n = Math.Max(1, bar.Width / (seg + gap)); double frac = F(val); int lit = (int)Math.Round(frac * n);
            Color cool = Def.Accent == "blue" ? T.Blue : T.Green;
            for (int i = 0; i < n; i++)
            {
                double p = (i + 0.5) / n; Color c = (Def.Fmt == "s" ? p > 0.5 : p > 0.8) ? (p > 0.9 ? T.Red : T.Amber) : cool; if (Def.Fmt == "s" && p > 0.5) c = p > 0.84 ? T.Red : T.Amber;
                if (sc != null) { c = sc.NoHot || p <= sc.Hot ? (sc.BlueRed ? T.Blue : cool) : (sc.BlueRed || p > 0.9 ? T.Red : T.Amber); }
                if (i >= lit) c = Color.FromArgb(34, c); using (Brush b = new SolidBrush(c)) g.FillRectangle(b, bar.X + i * (seg + gap), bar.Y + 1, seg, bar.Height - 2);
            }
            if (!dim) { int px = bar.X + (int)(Math.Max(0, Math.Min(1, F(peak))) * (bar.Width - 2)); using (Pen p = new Pen(Color.FromArgb(220, 255, 255, 255))) g.DrawLine(p, px, bar.Y, px, bar.Bottom); }
            string[] tt = sc != null ? sc.Ticks : Def.Fmt == "s" ? sT : new string[] { "0", "25", "50", "75", "100" }; double[] tp = sc != null ? sc.Pos : Def.Fmt == "s" ? sP : new double[] { 0, 0.25, 0.5, 0.75, 1 };
            for (int i = 0; i < tt.Length; i++)
            {
                int x = bar.X + (int)(tp[i] * (bar.Width - 1)); using (Pen p = new Pen(T.Edge)) g.DrawLine(p, x, bar.Bottom, x, bar.Bottom + 2);
                SizeF sz = g.MeasureString(tt[i], T.Fn("Segoe UI", 6.5f)); using (Brush b = new SolidBrush(T.Dim)) g.DrawString(tt[i], T.Fn("Segoe UI", 6.5f), b, x - sz.Width / 2 + 1, bar.Bottom + 1);
            }
            using (StringFormat sf = new StringFormat()) { sf.Alignment = StringAlignment.Far; using (Brush b = new SolidBrush(dim ? T.Dim : T.Text)) g.DrawString(dim ? "—" : txt, T.Fn("Segoe UI Semibold", 8.5f), b, new RectangleF(Width - tw - 2, 1, tw, 16 * k), sf); }
        }
    }

    // ------------------------------------------------------------------ afficheur de fréquence
    public class HFreq : HW
    {
        long freq; string mode = ""; bool sel, tx; int hover = -1; Font fd;
        Rectangle[] cell = new Rectangle[9];
        int N { get { return Rig != null && Rig.Def != null && Rig.Def.MaxFreq >= 100000000 ? 9 : 8; } }          // 9 chiffres dès que le poste monte au-delà de 100 MHz (144 / 430 MHz)
        long PlaceAt(int i) { long p = 1; for (int q = 0; q < N - 1 - i; q++) p *= 10; return p; }
        public override void Sync()
        {
            if (!Ready) return; string s = St(Def.Id); long f = 0; if (s != null) long.TryParse(s, out f);
            string m = ""; Ctl mc = Rig.Def.Find(Def.Ref); string mv = St(Def.Ref); if (mc != null && mv != null) foreach (Item it in mc.ItemList) if (it.Match(mv)) m = it.Label;
            string vs = St("vs"); bool sl = vs == (Def.Id == "fa" ? "0" : "1") || (vs == null && Def.Id == "fa"); bool t = Rig.Tx && sl;
            if (f != freq || m != mode || sl != sel || t != tx) { freq = f; mode = m; sel = sl; tx = t; Invalidate(); }
        }
        void Calc()
        {
            float k = T.Scale; float fsz = Math.Max(14, Math.Min(Height * 0.60f, (Width - 24 * k) / (N == 9 ? 5.7f : 5.1f)));
            if (fd == null || Math.Abs(fd.Size - fsz) > 0.5f) { if (fd != null) fd.Dispose(); fd = new Font("Consolas", fsz, FontStyle.Bold, GraphicsUnit.Pixel); }
            float dw = fsz * 0.56f, dotw = fsz * 0.30f, total = dw * N + dotw * 2, x = Width - total - 12 * k; if (x < 6) x = 6; float y = Height * 0.24f, h = Height * 0.70f;
            for (int i = 0; i < N; i++) { cell[i] = new Rectangle((int)x, (int)y, (int)dw, (int)h); x += dw; if (i < N - 1 && (N - 1 - i) % 3 == 0) x += dotw; }
        }
        protected override void Draw(Graphics g)
        {
            g.Clear(Parent != null ? Parent.BackColor : T.Panel); Calc(); Color acc = Accent(T.Amber); float k = T.Scale;
            RectangleF r = new RectangleF(0.5f, 0.5f, Width - 2, Height - 2); Color border = tx ? T.Red : (sel ? acc : T.Edge); bool oled = Def.Fmt == "oled"; if (oled) border = tx ? T.Red : Color.FromArgb(sel ? 200 : 120, 90, 30);
            using (GraphicsPath p = T.Round(r, 8 * k))
            {
                if (oled) { using (Brush b = new SolidBrush(Color.FromArgb(3, 3, 4))) g.FillPath(b, p); } else using (LinearGradientBrush b = new LinearGradientBrush(r, Color.FromArgb(10, 13, 18), Color.FromArgb(20, 26, 36), 90f)) g.FillPath(b, p);
                using (Pen pen = new Pen(border, sel || tx ? 2f : 1f)) g.DrawPath(pen, p);
            }
            using (Brush b = new SolidBrush(sel ? acc : T.Dim)) g.DrawString(Def.Label, T.Fn("Segoe UI Semibold", 10f), b, 10 * k, 6 * k);
            using (Brush b = new SolidBrush(tx ? T.Red : T.Cyan)) g.DrawString(mode, T.Fn("Segoe UI Semibold", 11f), b, 10 * k, 28 * k);
            bool have = Ready && St(Def.Id) != null; string s = have ? freq.ToString().PadLeft(N) : new string(' ', N); bool lead = true;
            Color on = tx ? Color.FromArgb(255, 110, 100) : (sel ? Color.FromArgb(255, 226, 150) : Color.FromArgb(190, 205, 220)); if (oled && !tx) on = sel ? Color.FromArgb(255, 200, 80) : Color.FromArgb(200, 150, 60);
            for (int i = 0; i < N; i++)
            {
                char c = s[i]; if (c != ' ' && c != '0') lead = false; else if (c == '0' && i >= N - 6 && lead) lead = false; bool dim = c == ' ' || (c == '0' && lead);
                if (i == hover) using (Brush hb = new SolidBrush(Color.FromArgb(40, 255, 255, 255))) g.FillRectangle(hb, cell[i]);
                if (!dim) using (Brush b = new SolidBrush(on)) using (StringFormat sf = T.Center()) g.DrawString(c == ' ' ? "8" : c.ToString(), fd, b, new RectangleF(cell[i].X - 4, cell[i].Y, cell[i].Width + 8, cell[i].Height), sf);
                if (i < N - 1 && (N - 1 - i) % 3 == 0) { int dx = cell[i].Right + (int)(fd.Size * 0.12f); using (Brush b = new SolidBrush(dim ? Color.FromArgb(50, 60, 74) : on)) g.FillRectangle(b, dx, cell[i].Bottom - (int)(fd.Size * 0.14f), Math.Max(3, (int)(fd.Size * 0.08f)), Math.Max(3, (int)(fd.Size * 0.08f))); }
            }
            using (Brush b = new SolidBrush(T.Dim)) g.DrawString("Hz", T.Fn("Segoe UI Semibold", 9f), b, Width - 26 * k, 6 * k);
        }
        int Hit(Point p) { for (int i = 0; i < N; i++) { Rectangle c = cell[i]; c.Inflate(2, 0); if (c.Contains(p)) return i; } return -1; }
        protected override void Moving(MouseEventArgs e) { int h = Hit(e.Location); if (h != hover) { hover = h; Invalidate(); } Cursor = h >= 0 ? Cursors.SizeNS : Cursors.Default; }
        protected override void Leaving() { hover = -1; Invalidate(); }
        protected override void Down(MouseEventArgs e)
        {
            if (!Ready) return; Ctl vs = Rig.Def.Find("vs"); if (vs != null && St("vs") != (Def.Id == "fa" ? "0" : "1")) Rig.Set(vs, Def.Id == "fa" ? "0" : "1");
            int h = Hit(e.Location); if (h >= 0 && e.Button == MouseButtons.Left) Bump(h, e.Y < cell[h].Top + cell[h].Height / 2 ? 1 : -1);
        }
        protected override void Wheel(MouseEventArgs e) { int h = Hit(e.Location); if (h < 0) h = 4; Bump(h, e.Delta > 0 ? 1 : -1); }
        void Bump(int i, int dir) { if (!Ready) return; long nf = freq + dir * PlaceAt(i); Setf(nf); }
        void Setf(long nf)
        {
            long max = Rig.Def != null ? Rig.Def.MaxFreq : 60000000; if (nf < 30000) nf = 30000; if (nf > max) nf = max;
            if (nf != freq) { freq = nf; Invalidate(); Rig.Set(Def, nf.ToString()); }
        }
        protected override void DblClick(MouseEventArgs e)
        {
            if (!Ready) return; string v = Prompt.Ask("Fréquence (en kHz, ou MHz avec un point) :", Def.Label, (freq / 1000.0).ToString("0.###", CultureInfo.InvariantCulture)); if (v == null) return;
            double d; v = v.Trim().Replace(',', '.').Replace(" ", ""); if (!double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out d)) return;
            Setf(d < 100 ? (long)Math.Round(d * 1e6) : (long)Math.Round(d * 1e3));
        }
    }

    // ------------------------------------------------------------------ molette d'accord
    public class HDial : HW
    {
        double ang, lastA; bool drag; double acc2;
        public override void Sync() { }
        long StepHz() { long s; string v = St(Def.Items); return v != null && long.TryParse(v, out s) && s > 0 ? s : 100; }
        double AngleAt(Point p) { return Math.Atan2(p.Y - Height / 2.0, p.X - Width / 2.0) * 180 / Math.PI; }
        protected override void Down(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { drag = true; lastA = AngleAt(e.Location); Capture = true; } }
        protected override void Up(MouseEventArgs e) { drag = false; Capture = false; }
        protected override void Moving(MouseEventArgs e)
        {
            if (!drag) return; double a = AngleAt(e.Location), d = a - lastA; if (d > 180) d -= 360; if (d < -180) d += 360; lastA = a; ang += d; acc2 += d;
            int n = (int)(acc2 / 8.0); if (n != 0) { acc2 -= n * 8.0; Bump(n); } Invalidate();
        }
        protected override void Wheel(MouseEventArgs e) { Bump(e.Delta > 0 ? 1 : -1); ang += e.Delta > 0 ? 8 : -8; Invalidate(); }
        void Bump(int n)
        {
            if (!Ready) return; Ctl fc = Rig.Def.Find(Def.Ref); if (fc == null) return; long f = Rig.GetL(Def.Ref); if (f <= 0) return;
            long nf = f + n * StepHz(); if (nf < 30000) nf = 30000; if (nf > Rig.Def.MaxFreq) nf = Rig.Def.MaxFreq; Rig.Set(fc, nf.ToString());
        }
        protected override void Draw(Graphics g)
        {
            g.Clear(Parent != null ? Parent.BackColor : T.Panel); float k = T.Scale; float d = Math.Min(Width, Height) - 8 * k; float cx = Width / 2f, cy = Height / 2f; RectangleF r = new RectangleF(cx - d / 2, cy - d / 2, d, d);
            using (Brush b = new SolidBrush(Color.FromArgb(10, 13, 18))) g.FillEllipse(b, r.X - 3, r.Y - 3, r.Width + 6, r.Height + 6);
            using (LinearGradientBrush b = new LinearGradientBrush(r, Color.FromArgb(70, 80, 98), Color.FromArgb(24, 29, 38), 70f)) g.FillEllipse(b, r);
            using (Pen p = new Pen(T.Edge, 2f * k)) g.DrawEllipse(p, r);
            using (Pen p = new Pen(Color.FromArgb(120, 140, 160), 1.5f * k)) for (int i = 0; i < 48; i++) { double a = (ang + i * 7.5) * Math.PI / 180; g.DrawLine(p, cx + (float)Math.Cos(a) * d / 2 * 0.90f, cy + (float)Math.Sin(a) * d / 2 * 0.90f, cx + (float)Math.Cos(a) * d / 2 * 0.97f, cy + (float)Math.Sin(a) * d / 2 * 0.97f); }
            float dd = d * 0.34f; double ka = (ang - 90) * Math.PI / 180; float kx = cx + (float)Math.Cos(ka) * d * 0.28f, ky = cy + (float)Math.Sin(ka) * d * 0.28f;
            using (Brush b = new SolidBrush(Color.FromArgb(14, 17, 23))) g.FillEllipse(b, kx - dd / 2, ky - dd / 2, dd, dd);
            using (Pen p = new Pen(T.Edge, 1.5f * k)) g.DrawEllipse(p, kx - dd / 2, ky - dd / 2, dd, dd);
            using (StringFormat sf = T.Center()) using (Brush b = new SolidBrush(T.Dim)) g.DrawString("pas " + StepHz() + " Hz — glisser en rond / molette", T.Fn("Segoe UI", 7.5f), b, new RectangleF(0, Height - 16 * k, Width, 14 * k), sf);
        }
    }

    // ------------------------------------------------------------------ voyant
    public class HLed : HW
    {
        bool on;
        public override void Sync()
        {
            if (!Ready) return; string src = string.IsNullOrEmpty(Def.Ref) ? Def.Id : Def.Ref; string v = St(src); bool o = Def.IsOn(v);
            if (src == "ptt" || Def.Id == "l_tx") o = Rig.Tx; if (o != on) { on = o; Invalidate(); }
        }
        protected override void Draw(Graphics g)
        {
            g.Clear(Parent != null ? Parent.BackColor : T.Panel); Color acc = Accent(T.Green); float k = T.Scale; float d = Math.Min(Height - 8 * k, 12 * k);
            using (Brush b = new SolidBrush(on ? acc : Color.FromArgb(48, 56, 68))) g.FillEllipse(b, 4 * k, Height / 2f - d / 2, d, d);
            if (on) using (Brush b = new SolidBrush(Color.FromArgb(60, acc))) g.FillEllipse(b, 4 * k - 3, Height / 2f - d / 2 - 3, d + 6, d + 6);
            using (StringFormat sf = new StringFormat()) { sf.LineAlignment = StringAlignment.Center; using (Brush b = new SolidBrush(on ? T.Text : T.Dim)) g.DrawString(Def.Label, T.Fn("Segoe UI Semibold", 8.5f), b, new RectangleF(d + 10 * k, 0, Width - d - 10 * k, Height), sf); }
        }
    }

    // ------------------------------------------------------------------ cadre titré et texte
    public class HFrame : HW
    {
        public override void Sync() { }
        protected override void Draw(Graphics g)
        {
            g.Clear(Parent != null ? Parent.BackColor : T.Bg); RectangleF r = new RectangleF(0.5f, 0.5f, Width - 2, Height - 2);
            using (GraphicsPath p = T.Round(r, 7 * T.Scale)) { using (Brush b = new SolidBrush(T.Panel)) g.FillPath(b, p); using (Pen pen = new Pen(T.Edge)) g.DrawPath(pen, p); }
            using (Brush b = new SolidBrush(T.Amber)) g.DrawString((Def.Label ?? "").ToUpperInvariant(), T.Fn("Segoe UI Semibold", 8f), b, 9 * T.Scale, 4 * T.Scale);
        }
    }

    public static class Make
    {
        public static HW For(Ctl c)
        {
            HW w;
            switch (c.K)
            {
                case Kind.Toggle: case Kind.Action: w = new HBtn(); break;
                case Kind.Choice: w = c.Row ? (HW)new HRow() : new HBtn(); break;
                case Kind.Knob: w = new HKnob(); break;
                case Kind.Meter: w = new HMeter(); break;
                case Kind.Freq: w = new HFreq(); break;
                case Kind.Dial: w = new HDial(); break;
                case Kind.Led: w = new HLed(); break;
                case Kind.Gauge: if (c.Id == "bigmeter") w = new HBigMeter(); else w = new HGauge(); break;
                case Kind.Chain: w = new HChain(); break;
                case Kind.Dsp: w = new HDsp(); break;
                default: w = new HFrame(); break;
            }
            w.Def = c; return w;
        }
    }

    // ------------------------------------------------------------------ boîtes de dialogue et listes sombres
    public static class Prompt
    {
        public static string Ask(string text, string title, string def)
        {
            using (Form f = new Form())
            {
                f.Text = title; f.FormBorderStyle = FormBorderStyle.FixedDialog; f.StartPosition = FormStartPosition.CenterParent; f.MaximizeBox = false; f.MinimizeBox = false; f.ShowInTaskbar = false;
                f.BackColor = T.Panel; f.ForeColor = T.Text; f.ClientSize = new Size(380, 110); f.Font = new Font("Segoe UI", 9f);
                Label l = new Label(); l.Text = text; l.SetBounds(12, 10, 356, 20);
                TextBox tb = new TextBox(); tb.Text = def; tb.SetBounds(12, 34, 356, 24); tb.BackColor = T.Bg; tb.ForeColor = T.Text; tb.BorderStyle = BorderStyle.FixedSingle;
                Button ok = new Button(); ok.Text = "OK"; ok.DialogResult = DialogResult.OK; ok.SetBounds(200, 70, 80, 28); Button ca = new Button(); ca.Text = "Annuler"; ca.DialogResult = DialogResult.Cancel; ca.SetBounds(288, 70, 80, 28);
                foreach (Button b in new Button[] { ok, ca }) { b.FlatStyle = FlatStyle.Flat; b.BackColor = T.Panel2; b.ForeColor = T.Text; b.FlatAppearance.BorderColor = T.Edge; }
                f.Controls.AddRange(new Control[] { l, tb, ok, ca }); f.AcceptButton = ok; f.CancelButton = ca; tb.SelectAll();
                return f.ShowDialog() == DialogResult.OK ? tb.Text : null;
            }
        }
    }

    public class DarkCombo : ComboBox
    {
        public DarkCombo()
        {
            DrawMode = DrawMode.OwnerDrawFixed; DropDownStyle = ComboBoxStyle.DropDownList; FlatStyle = FlatStyle.Flat;
            BackColor = Color.FromArgb(30, 37, 49); ForeColor = T.Text; Font = new Font("Segoe UI", 8.5f); ItemHeight = 20;
        }
        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            bool sel = (e.State & DrawItemState.Selected) != 0 && (e.State & DrawItemState.ComboBoxEdit) == 0;
            using (Brush b = new SolidBrush(sel ? Color.FromArgb(58, 78, 110) : Color.FromArgb(30, 37, 49))) e.Graphics.FillRectangle(b, e.Bounds);
            if (e.Index >= 0) using (Brush b = new SolidBrush(T.Text)) e.Graphics.DrawString(GetItemText(Items[e.Index]), Font, b, e.Bounds.X + 3, e.Bounds.Y + 2);
        }
    }
}
