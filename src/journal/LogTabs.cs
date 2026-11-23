using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace HamRadioLink.Journal
{
    // Onglets des journaux (en haut du Logbook) : un clic passe d'un journal a l'autre, « + » en cree un nouveau.
    public class LogTabs : Control
    {
        public event Action<LogRef> TabSelected; public event Action AddClicked; public event Action<LogRef> RenameRequested; public event Action<LogRef> RemoveRequested, DeleteRequested;
        readonly List<Rectangle> rects = new List<Rectangle>(); Rectangle plus; int hot = -2;
        public event Action<int> ZoomStep; public event Action ZoomReset; Rectangle zMinus, zPlus, zReset; int zhot;   // boutons ronds − / + (zoom du texte de la liste), tout à droite de la ligne
        // Boutons d'action (Add, Delete…) dessinés sur la même ligne que les onglets, avec la même forme que le « + ».
        class TabItem { public string Text; public ToolStripButton Src; public int Kind; public Rectangle R; }   // Kind : 0 bouton, 1 séparateur, 2 libellé, 3 zone de recherche
        readonly List<TabItem> items = new List<TabItem>(); TextBox box; int hotItem = -1;
        public void SetItems(ToolStrip ts, TextBox searchBox)
        {
items.Clear(); box = null;   // la zone de recherche n'est plus affichée (elle reste en interne pour « Tout » et le filtre)
            foreach (ToolStripItem it in ts.Items)
            {
                if (it is ToolStripSeparator) items.Add(new TabItem { Kind = 1 });
                else if (it is ToolStripButton) items.Add(new TabItem { Kind = 0, Text = it.Text, Src = (ToolStripButton)it });
                else if (it is ToolStripLabel) items.Add(new TabItem { Kind = 2, Text = it.Text });
            }
            Invalidate();
        }

        public LogTabs() { Theme.Changed += Invalidate2; Disposed += delegate { Theme.Changed -= Invalidate2; }; SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); Height = 32; Dock = DockStyle.Top; }

        void Invalidate2() { Invalidate(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            Looks.Override = Looks.LookOf("lb"); try { Draw(e); } finally { Looks.Override = -1; }
        }
        void Draw(PaintEventArgs e)
        {
            Graphics g = e.Graphics; Color back = Theme.Get("lb.back"); g.Clear(back); Looks.Dark = Theme.IsDark(back); rects.Clear();
            int x = 6; Color tt = Theme.Get("lb.tool.text");
            using (Font f = new Font(Tr.T("Segoe UI"), 9.5f, FontStyle.Bold))
            {
                for (int i = 0; i < App.Logs.Count; i++)
                {
                    LogRef l = App.Logs[i]; string t = l.Name; int w = TextRenderer.MeasureText(t, f).Width + 30; Rectangle r = new Rectangle(x, 3, w, Height - 6); rects.Add(r);
                    bool active = l == App.CurrentLog; Color text = Looks.DrawButton(g, r, active ? (Color?)Theme.Get("lb.accent") : null, tt, hot == i || active, active, true, back);
                    TextRenderer.DrawText(g, t, f, r, active ? Theme.Get("lb.accent.text") : text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                    x += w + 4;
                }
                plus = new Rectangle(x, 3, 30, Height - 6); Color tp = Looks.DrawButton(g, plus, null, tt, hot == -1, false, true, back);
                TextRenderer.DrawText(g, "+", f, plus, tp, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                x = plus.Right + 16;
                for (int i = 0; i < items.Count; i++)
                {
                    TabItem it = items[i];
                    if (it.Kind == 1) { x += 10; it.R = Rectangle.Empty; }
                    else if (it.Kind == 2) { int w = TextRenderer.MeasureText(it.Text, f).Width; it.R = new Rectangle(x, 3, w, Height - 6); TextRenderer.DrawText(g, it.Text, f, it.R, tt, TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding); x += w + 6; }
                    else if (it.Kind == 3) { it.R = Rectangle.Empty; if (box != null) { Point p = new Point(x, (Height - box.Height) / 2); if (box.Location != p) box.Location = p; x += box.Width + 6; } }
                    else
                    {
                        bool lp = it.Src is LoupeButton; int w = (it.Text == "▲" || it.Text == "↶" || it.Text == "↷" || lp) ? Height - 6 : it.Text == "↔" ? 36 : TextRenderer.MeasureText(it.Text, f).Width + 16; it.R = new Rectangle(x, 3, w, Height - 6);
                        Color tc = Looks.DrawButton(g, it.R, null, tt, hotItem == i, false, true, back);
                        if (lp) LoupeButton.Draw(g, it.R, tc); else if (it.Text == "↔") FitIcon(g, it.R, tc); else TextRenderer.DrawText(g, it.Text, f, it.R, tc, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding); x += w + 4;
                    }
                }
                int dz = Math.Min(22, Height - 6); zPlus = new Rectangle(Width - dz - 8, (Height - dz) / 2, dz, dz); zMinus = new Rectangle(zPlus.X - dz - 4, zPlus.Y, dz, dz); zReset = new Rectangle(zMinus.X - dz - 8, zPlus.Y, dz, dz);
                LogZoom.Circle(g, zMinus, false, zhot == 1); LogZoom.Circle(g, zPlus, true, zhot == 2); LogZoom.Reset(g, zReset, zhot == 3);
            }
        }

        // icône « <-> » : deux chevrons larges de part et d'autre d'un petit tiret central (ajuster la largeur des colonnes)
        internal static void FitIcon(Graphics g, Rectangle r, Color c)
        {
            System.Drawing.Drawing2D.SmoothingMode old = g.SmoothingMode; g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f, half = 9f, hh = 4.5f, ah = 4.5f;   // flèche double « ←→ » : un trait continu et deux pointes collées aux extrémités
            using (Pen p = new Pen(c, 1.8f)) { p.StartCap = System.Drawing.Drawing2D.LineCap.Round; p.EndCap = System.Drawing.Drawing2D.LineCap.Round; p.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                g.DrawLine(p, cx - half, cy, cx + half, cy);
                g.DrawLines(p, new PointF[] { new PointF(cx - half + ah, cy - hh), new PointF(cx - half, cy), new PointF(cx - half + ah, cy + hh) });
                g.DrawLines(p, new PointF[] { new PointF(cx + half - ah, cy - hh), new PointF(cx + half, cy), new PointF(cx + half - ah, cy + hh) }); }
            g.SmoothingMode = old;
        }
        int ZAt(Point p) { return zMinus.Contains(p) ? 1 : zPlus.Contains(p) ? 2 : zReset.Contains(p) ? 3 : 0; }
        int At(Point p) { for (int i = 0; i < rects.Count; i++) if (rects[i].Contains(p)) return i; return plus.Contains(p) ? -1 : -2; }
        int ItemAt(Point p) { for (int i = 0; i < items.Count; i++) if (items[i].Kind == 0 && items[i].R.Contains(p)) return i; return -1; }
        protected override void OnMouseMove(MouseEventArgs e) { int h = At(e.Location), hi = ItemAt(e.Location), zh = ZAt(e.Location); if (h != hot || hi != hotItem || zh != zhot) { hot = h; hotItem = hi; zhot = zh; Invalidate(); } Cursor = h > -2 || hi >= 0 || zh > 0 ? Cursors.Hand : Cursors.Default; }
        protected override void OnMouseLeave(EventArgs e) { hot = -2; hotItem = -1; zhot = 0; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            int ii = ItemAt(e.Location); if (e.Button == MouseButtons.Left && ZAt(e.Location) > 0) { int zz = ZAt(e.Location); if (zz == 3) { Action zr = ZoomReset; if (zr != null) zr(); return; } Action<int> za = ZoomStep; if (za != null) za(zz == 2 ? 1 : -1); return; }
            if (ii >= 0 && e.Button == MouseButtons.Left) { items[ii].Src.PerformClick(); return; }
            int i = At(e.Location);
            if (i == -1 && e.Button == MouseButtons.Left) { Action a = AddClicked; if (a != null) a(); return; }
            if (i < 0) return; LogRef l = App.Logs[i];
            if (e.Button == MouseButtons.Left) { Action<LogRef> h = TabSelected; if (h != null) h(l); }
            else if (e.Button == MouseButtons.Right)
            {
                ContextMenuStrip m = new ContextMenuStrip();
                m.Items.Add(Ui.French ? "Renommer..." : "Rename...", null, (s, a) => { Action<LogRef> h = RenameRequested; if (h != null) h(l); });
                m.Items.Add(Ui.French ? Tr.T("Retirer de la liste (la base n'est pas supprimée)") : Tr.T("Remove from list (database is kept)"), null, (s, a) => { Action<LogRef> h = RemoveRequested; if (h != null) h(l); });
                ToolStripMenuItem del = new ToolStripMenuItem(Ui.French ? Tr.T("Retirer de la liste ET SUPPRIMER la base...") : Tr.T("Remove from list AND DELETE database..."), null, (s, a) => { Action<LogRef> h = DeleteRequested; if (h != null) h(l); }) { ForeColor = Color.FromArgb(190, 0, 0), Font = new Font(Font, FontStyle.Bold) };
                m.Items.Add(new ToolStripSeparator()); m.Items.Add(del);
                m.Show(this, e.Location);
            }
        }
    }

    // Petite fenetre « veuillez patienter » : execute un travail long sur un autre thread et affiche son avancement.
    public static class Busy
    {
        public static void Run(IWin32Window owner, string title, Action<Action<string>> work)
        {
            Exception err = null; bool done = false;
            using (Form f = new Form { Text = title, ClientSize = new Size(520, 90), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, ControlBox = false, Font = Ui.Normal, ShowInTaskbar = false })
            {
                Label l = new Label { Left = 14, Top = 14, Width = 490, Height = 24, Text = "..." }; ProgressBar b = new ProgressBar { Left = 14, Top = 48, Width = 490, Height = 18, Style = ProgressBarStyle.Marquee }; f.Controls.Add(l); f.Controls.Add(b);
                Action<string> say = s => { try { if (f.IsHandleCreated) f.BeginInvoke(new Action(() => l.Text = s)); } catch { } };
                f.Shown += (s, e) => new Thread(() => { try { work(say); } catch (Exception ex) { err = ex; } done = true; try { f.BeginInvoke(new Action(f.Close)); } catch { } }) { IsBackground = true }.Start();
                f.FormClosing += (s, e) => { if (!done) e.Cancel = true; };
                f.ShowDialog(owner);
            }
            if (err != null) throw new Exception(err.Message, err);
        }
    }

    // « + » : creer un journal (vide, ou vide puis import ADIF) ou attacher une base existante.
    public class NewLogForm : Form
    {
        RadioButton rEmpty, rAdif, rAttach; TextBox tName, tHost, tPort, tUser, tPass, tSchema;
        public LogRef Result; public bool ThenImportAdif;

        public NewLogForm(string preset = "")
        {
            bool fr = Ui.French; Text = fr ? Tr.T("Nouveau journal") : Tr.T("New log"); Font = Ui.Normal; Icon = Ui.AppIcon; ClientSize = new Size(560, 400); StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
            Ui.Lbl(this, fr ? Tr.T("Nom du journal :") : Tr.T("Log name:"), 14, 16); tName = Ui.Txt(this, 130, 14, 250);
            rEmpty = new RadioButton { Left = 14, Top = 52, Width = 530, Text = fr ? Tr.T("Journal vide") : Tr.T("Empty log"), Checked = true }; Controls.Add(rEmpty);
            rAdif = new RadioButton { Left = 14, Top = 80, Width = 530, Text = fr ? Tr.T("Journal vide, puis importer un fichier ADIF (.adi)") : Tr.T("Empty log, then import an ADIF file") }; Controls.Add(rAdif);
            rAttach = new RadioButton { Left = 14, Top = 108, Width = 530, Text = fr ? Tr.T("Attacher une base MariaDB existante (journal au format compatible)") : Tr.T("Attach an existing MariaDB database (compatible format)") }; Controls.Add(rAttach);
            Ui.Lbl(this, fr ? "Serveur" : "Server", 34, 204); tHost = Ui.Txt(this, 100, 202, 130); tHost.Text = "127.0.0.1"; Ui.Lbl(this, Tr.T("Port"), 240, 204); tPort = Ui.Txt(this, 275, 202, 60); tPort.Text = "3306";
            Ui.Lbl(this, fr ? "Utilisateur" : "User", 34, 238); tUser = Ui.Txt(this, 100, 236, 130); tUser.Text = "root"; Ui.Lbl(this, fr ? Tr.T("Mot de passe") : "Password", 240, 238); tPass = Ui.Txt(this, 330, 236, 150); tPass.UseSystemPasswordChar = true;
            Ui.Lbl(this, fr ? Tr.T("Base (schema)") : "Database", 34, 272); tSchema = Ui.Txt(this, 130, 270, 200);
            Ui.Lbl(this, fr ? Tr.T("Le nouveau journal apparaît comme un onglet en haut du Logbook. Une base de plus est créée sur le serveur actuel : le journal en cours n'est pas touché.") : Tr.T("The new log appears as a tab at the top of the Logbook. A new database is created on the current server; the current log is not touched."), 14, 312, 530);
            Button ok = Ui.Btn(this, "OK", 370, 358, 80, 30, (s, e) => Validate2()); Ui.Btn(this, fr ? "Annuler" : "Cancel", 460, 358, 80, 30, (s, e) => { DialogResult = DialogResult.Cancel; Close(); }); AcceptButton = ok;
        }

        void Validate2()
        {
            string name = tName.Text.Trim(); if (name.Length == 0) { MessageBox.Show(this, Ui.French ? Tr.T("Donnez un nom au journal.") : Tr.T("Give the log a name."), "HamRadioLink"); return; }
            foreach (LogRef l in App.Logs) if (string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase)) { MessageBox.Show(this, Ui.French ? Tr.T("Ce nom existe déjà.") : Tr.T("This name already exists."), "HamRadioLink"); return; }
            LogRef r = new LogRef { Name = name };
            if (rAttach.Checked)
            {
                int p; if (tSchema.Text.Trim().Length == 0 || !int.TryParse(tPort.Text, out p)) { MessageBox.Show(this, Ui.French ? Tr.T("Indiquez le serveur, le port et la base.") : Tr.T("Enter server, port and database."), "HamRadioLink"); return; }
                r.Host = tHost.Text.Trim(); r.Port = p; r.User = tUser.Text.Trim(); r.Pass = tPass.Text; r.Schema = tSchema.Text.Trim();
            }
            else
            {
                string slug = Regex.Replace(name.ToLowerInvariant(), "[^a-z0-9]+", "_").Trim('_'); if (slug.Length == 0) slug = "log";
                r.Schema = "hl_" + slug; int n = 2; while (App.Logs.Exists(x => x.Schema == r.Schema)) r.Schema = "hl_" + slug + "_" + (n++);
                ThenImportAdif = rAdif.Checked;
            }
            Result = r; DialogResult = DialogResult.OK; Close();
        }
    }
}
