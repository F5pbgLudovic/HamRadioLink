using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace HamRadioLink.Journal
{
    // Petit carré de couleur (dessiné par lui-même : le thème ne le repeint pas).
    public class Swatch : Control
    {
        public Color Col = Color.Gray; public bool Sel;
        public Swatch() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true); Cursor = Cursors.Hand; Size = new Size(28, 28); }
        protected override void OnPaint(PaintEventArgs e)
        {
            using (SolidBrush b = new SolidBrush(Col)) e.Graphics.FillRectangle(b, 0, 0, Width, Height);
            using (Pen p = new Pen(Color.FromArgb(110, 110, 110))) e.Graphics.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
            if (Sel) { using (Pen w = new Pen(Color.White, 2)) e.Graphics.DrawRectangle(w, 1, 1, Width - 3, Height - 3); using (Pen k = new Pen(Color.Black)) e.Graphics.DrawRectangle(k, 3, 3, Width - 7, Height - 7); }
        }
    }

    // Sélecteur : tablette de 64 couleurs, curseurs Rouge / Vert / Bleu, code #RRGGBB, couleurs personnalisées récentes.
    public class ColorPicker : Form
    {
        public Color Result;
        readonly Swatch pNew = new Swatch { Size = new Size(70, 40) }, pOld = new Swatch { Size = new Size(70, 40) };
        readonly TrackBar[] tr = new TrackBar[3]; readonly NumericUpDown[] nu = new NumericUpDown[3]; TextBox hex; bool busy; readonly List<Swatch> all = new List<Swatch>();
        const int Cell = 30;

        static Color Hsl(double h, double s, double l)
        {
            double c = (1 - Math.Abs(2 * l - 1)) * s, x = c * (1 - Math.Abs((h / 60) % 2 - 1)), m = l - c / 2, r = 0, g = 0, b = 0;
            if (h < 60) { r = c; g = x; } else if (h < 120) { r = x; g = c; } else if (h < 180) { g = c; b = x; } else if (h < 240) { g = x; b = c; } else if (h < 300) { r = x; b = c; } else { r = c; b = x; }
            return Color.FromArgb((int)Math.Round((r + m) * 255), (int)Math.Round((g + m) * 255), (int)Math.Round((b + m) * 255));
        }

        public ColorPicker(string title, Color start)
        {
            Text = title; ClientSize = new Size(600, 400); StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; Font = Ui.Normal; Icon = Ui.AppIcon; Result = start;
            Ui.Lbl(this, "Tablette de 64 couleurs :", 12, 10);
            // 8 colonnes : ligne des gris, puis 7 lignes (clair -> sombre) pour 8 teintes
            double[] hues = { 0, 28, 52, 120, 172, 215, 270, 320 }; double[] lights = { 0.86, 0.74, 0.62, 0.5, 0.4, 0.3, 0.2 };
            for (int col = 0; col < 8; col++) { int g = (int)Math.Round(col * 255.0 / 7); AddSwatch(12 + col * Cell, 34, Color.FromArgb(g, g, g)); }
            for (int row = 0; row < 7; row++) for (int col = 0; col < 8; col++) AddSwatch(12 + col * Cell, 34 + (row + 1) * Cell, Hsl(hues[col], 0.78, lights[row]));
            Ui.Lbl(this, "Récentes (personnalisées) :", 12, 34 + 8 * Cell + 8);
            List<Color> rec = Recents(); for (int i = 0; i < 8; i++) { Swatch s = new Swatch { Location = new Point(12 + i * Cell, 34 + 8 * Cell + 32), Col = i < rec.Count ? rec[i] : Color.FromArgb(60, 60, 60), Tag = i < rec.Count ? (object)1 : null }; s.Click += delegate { if (s.Tag != null) Set(s.Col); }; Controls.Add(s); }
            int x = 290; Ui.Lbl(this, "Couleur personnalisée (RVB) :", x, 10);
            string[] nm = { "Rouge", "Vert", "Bleu" };
            for (int i = 0; i < 3; i++)
            {
                int k = i, y = 40 + i * 44; Ui.Lbl(this, nm[i], x, y + 4);
                tr[i] = new TrackBar { Left = x + 50, Top = y, Width = 170, Minimum = 0, Maximum = 255, TickStyle = TickStyle.None, Value = 0 }; Controls.Add(tr[i]);
                nu[i] = new NumericUpDown { Left = x + 228, Top = y + 3, Width = 52, Minimum = 0, Maximum = 255 }; Controls.Add(nu[i]);
                tr[i].ValueChanged += delegate { if (!busy) FromRgb(tr[k].Value, -1, -1, k); };
                nu[i].ValueChanged += delegate { if (!busy) { busy = true; tr[k].Value = (int)nu[k].Value; busy = false; FromSliders(); } };
            }
            Ui.Lbl(this, "Code :", x, 180); hex = Ui.Txt(this, x + 50, 178, 90); hex.TextChanged += delegate { if (busy) return; Color c; if (TryHex(hex.Text, out c)) { busy = true; Apply(c, false); busy = false; } };
            Ui.Lbl(this, "Ancienne", x + 50, 222); pOld.Location = new Point(x + 50, 244); pOld.Col = start; Controls.Add(pOld);
            Ui.Lbl(this, "Nouvelle", x + 140, 222); pNew.Location = new Point(x + 140, 244); Controls.Add(pNew);
            Button ok = Ui.Btn(this, "OK", 404, 356, 90, 28, (s, e) => { AddRecent(Result); DialogResult = DialogResult.OK; Close(); }); Ui.Btn(this, "Annuler", 502, 356, 86, 28, (s, e) => { DialogResult = DialogResult.Cancel; Close(); }); AcceptButton = ok;
            Set(start);
        }

        void AddSwatch(int x, int y, Color c) { Swatch s = new Swatch { Location = new Point(x, y), Col = c, Size = new Size(Cell - 2, Cell - 2) }; s.Click += delegate { Set(s.Col); }; Controls.Add(s); all.Add(s); }
        static bool TryHex(string t, out Color c)
        {
            c = Color.Black; t = (t ?? "").Trim().TrimStart('#'); int v; if (t.Length != 6 || !int.TryParse(t, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out v)) return false;
            c = Color.FromArgb((v >> 16) & 255, (v >> 8) & 255, v & 255); return true;
        }
        void Set(Color c) { busy = true; Apply(c, true); busy = false; }
        void Apply(Color c, bool updateHex)
        {
            Result = c; pNew.Col = c; pNew.Invalidate();
            tr[0].Value = c.R; tr[1].Value = c.G; tr[2].Value = c.B; nu[0].Value = c.R; nu[1].Value = c.G; nu[2].Value = c.B;
            if (updateHex) hex.Text = "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
            foreach (Swatch s in all) { bool sel = s.Col.ToArgb() == c.ToArgb(); if (s.Sel != sel) { s.Sel = sel; s.Invalidate(); } }
        }
        void FromRgb(int v, int a, int b, int k) { FromSliders(); }
        void FromSliders() { busy = true; Apply(Color.FromArgb(tr[0].Value, tr[1].Value, tr[2].Value), true); busy = false; }

        static List<Color> Recents()
        {
            List<Color> l = new List<Color>(); foreach (string s in App.Cfg.Get("theme.recent", "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) { Color c; if (TryHex(s, out c)) l.Add(c); } return l;
        }
        static void AddRecent(Color c)
        {
            List<Color> l = Recents(); l.RemoveAll(x => x.ToArgb() == c.ToArgb()); l.Insert(0, c); if (l.Count > 8) l.RemoveRange(8, l.Count - 8);
            List<string> s = new List<string>(); foreach (Color k in l) s.Add(k.R.ToString("X2") + k.G.ToString("X2") + k.B.ToString("X2")); App.Cfg.Set("theme.recent", string.Join(",", s.ToArray())); try { App.Cfg.Save(); } catch { }
        }
    }

    // Menu Thèmes > Personnaliser : chaque couleur du thème en cours, changement en direct, mémorisé pour chaque thème.
    public class ColorsForm : Form
    {
        readonly Swatch[] sw = new Swatch[16]; readonly Label[] hx = new Label[16]; ComboBox scope; Label title;
        public ColorsForm()
        {
            Text = "Couleurs du thème"; ClientSize = new Size(560, 560); StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; Font = Ui.Normal; Icon = Ui.AppIcon;
            title = Ui.Lbl(this, "", 12, 12, 536); title.Font = new Font(Font, FontStyle.Bold);
            scope = Ui.Combo(this, 12, 40, 536, new[] { "Fenêtre principale du journal (et fenêtres ouvertes depuis elle)", "Fiches et fenêtres secondaires" }); scope.DropDownStyle = ComboBoxStyle.DropDownList; scope.SelectedIndex = 0; scope.SelectedIndexChanged += delegate { Fill(); };
            int y = 78;
            for (int i = 0; i < 16; i++)
            {
                int k = i; Label l = Ui.Lbl(this, ThemeDef.FieldNames[i], 12, y + 4, 280); l.Cursor = Cursors.Hand; l.Click += delegate { Pick(k); };
                sw[i] = new Swatch { Location = new Point(300, y), Size = new Size(80, 26) }; sw[i].Click += delegate { Pick(k); }; Controls.Add(sw[i]);
                hx[i] = Ui.Lbl(this, "", 392, y + 4, 90); y += 28;
            }
            Ui.Btn(this, "Rétablir les couleurs d'origine de ce thème", 12, 524, 300, 28, (s, e) => { if (MessageBox.Show(this, "Rétablir toutes les couleurs d'origine du thème « " + Theme.Cur.Name + " » ?", "Thèmes", MessageBoxButtons.YesNo) == DialogResult.Yes) { Theme.ResetTheme(Theme.Index); Fill(); } });
            Ui.Btn(this, "Fermer", 458, 524, 90, 28, (s, e) => Close());
            Theme.Changed += Fill; Disposed += delegate { Theme.Changed -= Fill; }; Fill();
        }
        ThemeDef Def() { ThemeDef t = Theme.Cur; return scope.SelectedIndex == 1 && t.Dlg != null ? t.Dlg : t; }
        bool Dlg() { return scope.SelectedIndex == 1 && Theme.Cur.Dlg != null; }
        void Fill()
        {
            title.Text = Theme.Cur.Name + " : cliquez sur une couleur pour la changer";
            scope.Enabled = Theme.Cur.Dlg != null; if (Theme.Cur.Dlg == null && scope.SelectedIndex != 0) scope.SelectedIndex = 0;
            ThemeDef d = Def();
            for (int i = 0; i < 16; i++) { Color c = Theme.C(d[i]); sw[i].Col = c; sw[i].Invalidate(); hx[i].Text = "#" + d[i].ToString("X6") + (d[i] != d.Orig[i] ? "  *" : ""); }
        }
        void Pick(int k)
        {
            ThemeDef d = Def(); using (ColorPicker p = new ColorPicker(ThemeDef.FieldNames[k], Theme.C(d[k])))
                if (p.ShowDialog(this) == DialogResult.OK) { Theme.SetColor(Theme.Index, Dlg(), k, (p.Result.R << 16) | (p.Result.G << 8) | p.Result.B); }
        }
    }
}
