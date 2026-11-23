using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace HamRadioLink.Journal
{
    // Petits outils pour construire les fenêtres rapidement.
    public static class Ui
    {
        // Fenêtre secondaire NON bloquante : le journal reste utilisable ; une seule fenêtre de chaque sorte à la fois (une deuxième demande ramène la première devant).
        static readonly Dictionary<Type, Form> freeWins = new Dictionary<Type, Form>();
        public static void ShowFree(Form f, IWin32Window owner)
        {
            Type ty = f.GetType(); Form ex;
            if (freeWins.TryGetValue(ty, out ex) && ex != null && !ex.IsDisposed) { try { ex.Activate(); } catch { } f.Dispose(); return; }
            freeWins[ty] = f; f.ShowInTaskbar = false; f.FormClosed += (s, e) => { Form cur; if (freeWins.TryGetValue(ty, out cur) && cur == f) freeWins.Remove(ty); };
            f.Show(owner);
        }

        public static Font Mono = new Font("Consolas", 9.5f);
        public static Font Normal = new Font("Segoe UI", 9f);
        public static Icon AppIcon { get { try { return Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { return null; } } }

        // ---------------- langue : anglais (d'origine) ou français pour les menus et barres d'outils ----------------
        public static bool French { get { return Tr.Code == "fr"; } }
        static readonly Dictionary<string, string> Fr = MakeFr();
        static Dictionary<string, string> MakeFr()
        {
            string[] p = {
                "File|Fichier", "Edit|Édition", "View|Affichage", "Help|Aide", "Tools|Outils", "Remote|Télécommande", "Bands|Bandes", "Favorites|Favoris", "Quick Save|Mémoire rapide", "Macros|Macros", "Logbook|Journal", "Radio Options|Options radio", "Tuning|Accord",
                "Connect|Connecter", "Disconnect|Déconnecter", "Exit|Quitter", "Copy Freq|Copier la fréquence", "Copy Mode|Copier le mode", "Customize Layout (edit mode)|Personnaliser la disposition (mode édition)", "Import layout from l'autre logiciel...|Importer la disposition de l'autre logiciel...",
                "Color Schemes|Jeux de couleurs", "New remote...|Nouvelle télécommande...", "Full Screen|Plein écran", "Band Up|Bande supérieure", "Band Down|Bande inférieure", "Add|Ajouter", "Manager|Gestionnaire", "Next|Suivant", "Previous|Précédent",
                "CAT Command Manager|Gestionnaire de commandes CAT", "Display|Afficher", "Options|Options", "Enter Frequency|Saisir une fréquence", "Split on/off|Split marche/arrêt", "VFO A = B|VFO A = B", "Swap VFO A/B|Échanger les VFO A/B", "Scan...|Balayage (scan)...", "Spectrum (sweep)...|Spectre (balayage)...",
                "Command Tester|Testeur de commandes", "Rotator|Rotor", "Satellite Tracking|Suivi des satellites", "Mapper|Carte", "Sync (two logbooks)...|Synchroniser (deux journaux)...", "IP Server / UDP / serial sharing...|Serveur IP / UDP / partage série...", "Online services...|Services en ligne...", "Hardware|Matériel", "About|À propos",
                "Import l'autre logiciel themes (.opt)...|Importer des thèmes l'autre logiciel (.opt)...", "Language|Langue", "New log...|Nouveau journal...", "New Rig window (another transceiver)|Nouvelle fenetre Rig (autre emetteur-recepteur)", "Colors...|Couleurs...", "Band map|Carte de bande", "Look|Allure des boutons", "Setup assistant...|Assistant de configuration...", "MariaDB engine...|Moteur MariaDB...",
                "Open database...|Ouvrir une base...", "Import ADIF...|Importer un ADIF...", "Export ADIF (selection or all)...|Exporter en ADIF (sélection ou tout)...", "View / Edit|Voir / Modifier", "Delete|Supprimer", "Edit selections...|Modifier la sélection...", "Copy|Copier", "Cut|Couper", "Paste|Coller", "Cut|Couper", "Paste|Coller",
                "Refresh|Actualiser", "Show all|Tout afficher", "Columns (Layout)...|Colonnes (disposition)...", "DX Cluster|DX Cluster", "Calendar|Calendrier", "Calendar...|Calendrier...", "Countries|Pays", "Countries / DXCC...|Pays / DXCC...",
                "Awards tracking...|Suivi des diplômes...", "Statistics...|Statistiques...", "Find duplicates...|Chercher les doublons...", "Backup (ADIF)...|Sauvegarde (ADIF)...", "Update countries from QRZ/HamQTH data|Mettre à jour les pays",
                "Upload selection to QRZ.com Logbook|Envoyer la sélection au journal QRZ.com", "Upload selection to Club Log|Envoyer la sélection à Club Log", "Upload selection to eQSL.cc|Envoyer la sélection à eQSL.cc", "Upload selection to LoTW (TQSL)|Envoyer la sélection à LoTW (TQSL)",
                "Contest|Concours", "Layout|Disposition", "Edit Selections|Modifier la sélection", "Awards Tracking|Suivi des diplômes", "Backup|Sauvegarde", "Filter|Filtre", "QSL|QSL", "LOTW Upload|Envoi LoTW", "Search:|Recherche :", "Go|Aller", "All|Tout", "Grey line|Ligne grise", "Grid|Grille", "Contacted countries|Pays contactés" };
            Dictionary<string, string> d = new Dictionary<string, string>(); foreach (string x in p) { int k = x.IndexOf('|'); d[x.Substring(0, k)] = x.Substring(k + 1); }
            return d;
        }
        static readonly Dictionary<ToolStripItem, string> orig = new Dictionary<ToolStripItem, string>();
        // Traduit (ou remet en anglais) tous les éléments d'un menu ou d'une barre d'outils.
        public static void Translate(ToolStrip ts)
        {
            if (ts == null) return; string lg = Tr.Code;
            foreach (ToolStripItem it in ts.Items) TranslateItem(it, lg);
        }
        static void TranslateItem(ToolStripItem it, string lg)
        {
            if (!(it is ToolStripSeparator) && !(it is ToolStripControlHost))
            {
                string o; if (!orig.TryGetValue(it, out o)) { o = it.Text; orig[it] = o; }
                string key = o, tail = ""; int t = o.IndexOf('\t'); if (t >= 0) { key = o.Substring(0, t); tail = o.Substring(t); }
                string tr = null; if (lg != "en") { Fr.TryGetValue(key, out tr); if (lg != "fr") tr = Tr.T(tr ?? key); } string full = tr != null ? tr + tail : o; ToolStripMenuItem mi = it as ToolStripMenuItem; int ft = full.IndexOf('\t');
                if (mi != null && ft >= 0) { mi.Text = full.Substring(0, ft); mi.ShortcutKeyDisplayString = full.Substring(ft + 1); } else it.Text = full;   // le raccourci s'affiche dans sa colonne, séparé du libellé
            }
            ToolStripDropDownItem dd = it as ToolStripDropDownItem; if (dd != null) foreach (ToolStripItem c in dd.DropDownItems) TranslateItem(c, lg);
        }

        // Mise à l'échelle d'une fenêtre à positions fixes : quand la fenêtre est agrandie, boutons, champs et textes s'agrandissent avec elle.
        public static void AutoScale(Form f)
        {
            Dictionary<Control, Rectangle> rec = new Dictionary<Control, Rectangle>(); Dictionary<Control, Font> fonts = new Dictionary<Control, Font>(); Size[] baseSize = { Size.Empty };
            Action<Control> walk = null;
            walk = c => { foreach (Control k in c.Controls) { if (k.Dock == DockStyle.None && k.Anchor == (AnchorStyles.Top | AnchorStyles.Left)) { rec[k] = k.Bounds; fonts[k] = k.Font; } walk(k); } };
            f.Shown += (s, e) => { baseSize[0] = f.ClientSize; walk(f); };
            f.Resize += (s, e) =>
            {
                if (baseSize[0].IsEmpty || f.WindowState == FormWindowState.Minimized || f.ClientSize.Width < 50) return;
                double sx = f.ClientSize.Width / (double)baseSize[0].Width, sy = f.ClientSize.Height / (double)baseSize[0].Height, k = Math.Max(0.9, Math.Min(2.4, Math.Min(sx, sy)));
                f.SuspendLayout();
                foreach (KeyValuePair<Control, Rectangle> kv in rec)
                {
                    Control c = kv.Key; Rectangle r = kv.Value;
                    c.Bounds = new Rectangle((int)Math.Round(r.X * sx), (int)Math.Round(r.Y * sy), (int)Math.Round(r.Width * sx), (int)Math.Round(r.Height * (c is TextBox && !((TextBox)c).Multiline ? 1 : sy) ));
                    Font bf = fonts[c]; float ns = bf.Size * (float)k; if (Math.Abs(ns - c.Font.Size) > 0.4f && !(c is ListView)) { try { c.Font = new Font(bf.FontFamily, ns, bf.Style); } catch { } }
                }
                f.ResumeLayout();
            };
        }

        // Couleurs du Logbook sur une fenetre de saisie (Add...) : fond et textes (les champs de saisie restent clairs).
        public static void ApplyLogTheme(Control root)
        {
            Color back = Theme.Get("lb.back"), tt = Theme.Get("lb.tool.text");
            Action<Control> walk = null;
            walk = c => { foreach (Control k in c.Controls) { if (k is Label || k is CheckBox || k is RadioButton || k is GroupBox) k.ForeColor = tt; if (k is Panel || k is GroupBox || k is TabPage) k.BackColor = back; walk(k); } };
            root.BackColor = back; walk(root);
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool PrintWindow(IntPtr h, IntPtr dc, uint flags);
        // Photo fidele d'une de NOS fenetres (controles natifs compris) : pour les essais, jamais de capture de l'ecran.
        public static void Snap(Form f, string path)
        {
            using (Bitmap b = new Bitmap(f.Width, f.Height)) { using (Graphics g = Graphics.FromImage(b)) { IntPtr dc = g.GetHdc(); PrintWindow(f.Handle, dc, 2); g.ReleaseHdc(dc); } b.Save(path); }
        }

        // Position, taille et etat (agrandi) d'une fenetre : retrouves au prochain demarrage (win.<cle> dans les reglages).
        public static void RememberWindow(Form f, string key, bool sizeOnly = false)
        {
            if (App.OffScreen) return;
            f.Load += (s, e) =>
            {
                string[] p = App.Cfg.Get("win." + key, "").Split(','); int x, y, w, h;
                if (p.Length < 5 || !int.TryParse(p[0], out x) || !int.TryParse(p[1], out y) || !int.TryParse(p[2], out w) || !int.TryParse(p[3], out h) || w < 200 || h < 120) return;
                Rectangle r = new Rectangle(x, y, w, h); bool on = false; foreach (Screen sc in Screen.AllScreens) if (sc.WorkingArea.IntersectsWith(Rectangle.Inflate(r, -40, -20))) on = true;
                if (sizeOnly) { f.Size = new Size(w, h); }
                else if (on) { f.StartPosition = FormStartPosition.Manual; f.Bounds = r; }
                if (!sizeOnly && on && p[4] == "1") f.WindowState = FormWindowState.Maximized;
            };
            f.FormClosing += (s, e) =>
            {
                try
                {
                    if (f.WindowState == FormWindowState.Minimized) return; Rectangle b = f.WindowState == FormWindowState.Normal ? f.Bounds : f.RestoreBounds;
                    App.Cfg.Set("win." + key, b.X + "," + b.Y + "," + b.Width + "," + b.Height + "," + (f.WindowState == FormWindowState.Maximized ? "1" : "0")); App.Cfg.Save();
                }
                catch { }
            };
        }

        // Petite fenetre de saisie d'un texte ; renvoie null si annulee.
        public static string Prompt(IWin32Window owner, string text, string def)
        {
            using (Form f = new Form { Text = "HamRadioLink", ClientSize = new Size(480, 110), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, ShowInTaskbar = false, Font = Normal })
            {
                Label l = new Label { Left = 12, Top = 10, Width = 456, Height = 36, Text = text }; f.Controls.Add(l); TextBox t = Txt(f, 12, 50, 456); t.Text = def ?? "";
                Button ok = Btn(f, "OK", 300, 76, 80, 28, (s, e) => f.DialogResult = DialogResult.OK); Btn(f, "Annuler", 388, 76, 80, 28, (s, e) => f.DialogResult = DialogResult.Cancel); f.AcceptButton = ok;
                f.Shown += (s, e) => { t.Focus(); t.SelectAll(); };
                return f.ShowDialog(owner) == DialogResult.OK ? t.Text : null;
            }
        }

        public static Label Lbl(Control p, string text, int x, int y, int w = 0)
        {
            Label l = new Label { Text = text, Left = x, Top = y + 3, AutoSize = w == 0, Font = Normal };
            if (w > 0) l.Width = w;
            p.Controls.Add(l); return l;
        }

        public static TextBox Txt(Control p, int x, int y, int w, int h = 0)
        {
            TextBox t = new TextBox { Left = x, Top = y, Width = w, Font = Normal };
            if (h > 0) { t.Multiline = true; t.Height = h; t.ScrollBars = ScrollBars.Vertical; }
            p.Controls.Add(t); return t;
        }

        public static ComboBox Combo(Control p, int x, int y, int w, IEnumerable<string> items, bool editable = false)
        {
            ComboBox c = new ComboBox { Left = x, Top = y, Width = w, Font = Normal, DropDownStyle = editable ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList };
            if (items != null) foreach (string s in items) c.Items.Add(s);
            p.Controls.Add(c); return c;
        }

        public static Button Btn(Control p, string text, int x, int y, int w, int h, EventHandler click)
        {
            // Texte toujours lisible : un bouton trop étroit est élargi (et le texte est de toute façon ajusté au dessin).
            int need = TextRenderer.MeasureText(text ?? "", Normal, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width + 22;
            if (need > w) w = need;
            if (h < 24) h = 24;
            Button b = new Button { Text = text, Left = x, Top = y, Width = w, Height = h, Font = Normal, FlatStyle = FlatStyle.System };
            Looks.Hook(b);
            if (click != null) b.Click += click;
            p.Controls.Add(b); return b;
        }

        public static CheckBox Chk(Control p, string text, int x, int y, int w = 0)
        {
            CheckBox c = new CheckBox { Text = text, Left = x, Top = y, Font = Normal, AutoSize = w == 0 };
            if (w > 0) c.Width = w;
            p.Controls.Add(c); return c;
        }

        public static void Err(IWin32Window o, Exception e) { try { System.IO.File.AppendAllText(System.IO.Path.Combine(App.DataDir ?? ".", "crash.log"), DateTime.Now + "\r\n" + e + "\r\n\r\n"); } catch { } MessageBox.Show(o, e.Message, "HamRadioLink", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        public static bool Ask(IWin32Window o, string q) { return MessageBox.Show(o, q, "HamRadioLink", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes; }

        // Fréquence en Hz -> "7.049.130" comme l'autre logiciel
        public static string FreqText(string hz)
        {
            long f; if (!long.TryParse(hz, out f) || f <= 0) return "";
            string s = f.ToString().PadLeft(7, '0');
            string r = s.Substring(s.Length - 3);
            s = s.Substring(0, s.Length - 3);
            r = s.Substring(s.Length - 3) + "." + r;
            s = s.Substring(0, s.Length - 3);
            return s.Length > 0 ? s + "." + r : r;
        }

        public static string BandOf(long hz)
        {
            double m = hz / 1e6;
            if (m >= 0.1357 && m <= 0.1378) return "2190m"; if (m >= 0.472 && m <= 0.479) return "630m";
            if (m >= 1.8 && m <= 2.0) return "160m"; if (m >= 3.5 && m <= 4.0) return "80m"; if (m >= 5.06 && m <= 5.45) return "60m";
            if (m >= 7.0 && m <= 7.3) return "40m"; if (m >= 10.1 && m <= 10.15) return "30m"; if (m >= 14.0 && m <= 14.35) return "20m";
            if (m >= 18.068 && m <= 18.168) return "17m"; if (m >= 21.0 && m <= 21.45) return "15m"; if (m >= 24.89 && m <= 24.99) return "12m";
            if (m >= 28.0 && m <= 29.7) return "10m"; if (m >= 50 && m <= 54) return "6m"; if (m >= 70 && m <= 71) return "4m";
            if (m >= 144 && m <= 148) return "2m"; if (m >= 220 && m <= 225) return "1.25m"; if (m >= 420 && m <= 450) return "70cm";
            if (m >= 902 && m <= 928) return "33cm"; if (m >= 1240 && m <= 1300) return "23cm"; if (m >= 2300 && m <= 2450) return "13cm";
            return "";
        }

        public static readonly string[] Bands = { "2190m", "630m", "160m", "80m", "60m", "40m", "30m", "20m", "17m", "15m", "12m", "10m", "6m", "4m", "2m", "1.25m", "70cm", "33cm", "23cm", "13cm", "9cm", "6cm", "3cm" };
        public static readonly string[] Modes = { "AM", "CW", "FM", "LSB", "USB", "SSB", "RTTY", "PSK", "FT8", "FT4", "MFSK", "JT65", "JT9", "JS8", "OLIVIA", "SSTV", "DIGITALVOICE", "DSTAR", "C4FM", "DMR", "HELL", "WSPR", "PKT", "THOR", "CONTESTIA", "FAX" };
    }
}


