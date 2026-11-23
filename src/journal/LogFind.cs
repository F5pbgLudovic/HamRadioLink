using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace HamRadioLink.Journal
{
    // Bouton « loupe » de la barre d'outils : image dessinée dans la couleur du thème (même taille que le bouton voisin « Refaire »).
    public class LoupeButton : ToolStripButton
    {
        public ToolStripItem Like;   // le bouton dont on reprend la taille
        public LoupeButton() { DisplayStyle = ToolStripItemDisplayStyle.None; ToolTipText = "Rechercher (date, indicatif, texte)"; }
        public void SetColor(Color c) { ForeColor = c; Invalidate(); }
        // dessin direct, centré dans le bouton quelle que soit sa forme (pas d'image : rien à cadrer)
        // dessinée par la ligne d'onglets du journal (LogTabs) : même carré que les autres petits boutons
        public static void Draw(Graphics g, Rectangle r, Color c)
        {
            int s = Math.Max(10, Math.Min(r.Width, r.Height) - 12); float x = r.X + (r.Width - s) / 2f, y = r.Y + (r.Height - s) / 2f;
            SmoothingMode old = g.SmoothingMode;
            using (Pen p = new Pen(c, Math.Max(1.8f, s / 9f)))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias; p.StartCap = LineCap.Round; p.EndCap = LineCap.Round;
                float d = s * 0.68f; g.DrawEllipse(p, x, y, d, d); g.DrawLine(p, x + d * 0.88f, y + d * 0.88f, x + s, y + s);
            }
            g.SmoothingMode = old;
        }
    }
    // Fenêtre de recherche : NON bloquante (on peut naviguer et modifier le journal pendant qu'elle est ouverte) ; « Suivant » / « Précédent » montent et descendent dans le tableau.
    public class SearchForm : Form
    {
        readonly LogbookForm owner; readonly ComboBox cf; readonly TextBox tb; readonly Label st;
        public static readonly string[] Fields = { "Date du QSO", "Indicatif", "Texte (nom, QTH, commentaire…)" };
        public SearchForm(LogbookForm o)
        {
            owner = o; AutoScaleMode = AutoScaleMode.None; Text = Tr.T("Rechercher"); FormBorderStyle = FormBorderStyle.FixedToolWindow; StartPosition = FormStartPosition.CenterParent; Font = Ui.Normal; ClientSize = new Size(470, 150); ShowInTaskbar = false;
            Ui.Lbl(this, Tr.T("Chercher dans :"), 12, 12, 110); cf = Ui.Combo(this, 130, 10, 328, new string[] { Tr.T(Fields[0]), Tr.T(Fields[1]), Tr.T(Fields[2]) }); cf.DropDownStyle = ComboBoxStyle.DropDownList; cf.SelectedIndex = Math.Max(0, Math.Min(2, App.Cfg.GetInt("find.field", 1)));
            Ui.Lbl(this, Tr.T("Valeur :"), 12, 46, 110); tb = Ui.Txt(this, 130, 44, 328); tb.Text = App.Cfg.Get("find.text", "");
            Label hint = Ui.Lbl(this, Tr.T("Date : 22/11/2026, 2026-11-22, 2026-11 ou 2026. Indicatif : une partie suffit."), 12, 74, 446); hint.ForeColor = SystemColors.GrayText;
            Ui.Btn(this, Tr.T("▲ Début"), 12, 100, 90, 28, (s, e) => { try { st.Text = owner.GoTop(); } catch (Exception ex) { st.Text = ex.Message; } }); Ui.Btn(this, Tr.T("◀ Précédent"), 110, 100, 105, 28, (s, e) => Go(false)); Button nx = Ui.Btn(this, Tr.T("Suivant ▶"), 223, 100, 105, 28, (s, e) => Go(true)); Ui.Btn(this, Tr.T("Fermer"), 366, 100, 92, 28, (s, e) => Close());
            st = Ui.Lbl(this, "", 12, 130, 446); st.ForeColor = SystemColors.GrayText; AcceptButton = nx;
            tb.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { Go(!e.Shift); e.SuppressKeyPress = true; } };
            FormClosing += (s, e) => { App.Cfg.Set("find.field", cf.SelectedIndex.ToString()); App.Cfg.Set("find.text", tb.Text); App.Cfg.Save(); };
            Shown += (s, e) => { tb.Focus(); tb.SelectAll(); };
        }
        void Go(bool fwd)
        {
            try { st.Text = owner.FindNext(cf.SelectedIndex, tb.Text, fwd); } catch (Exception ex) { st.Text = ex.Message; }
        }
    }
}
