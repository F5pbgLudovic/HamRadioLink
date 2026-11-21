using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace HamRadioLink
{
    // Menu interne du FT-2000 / FT-2000D (menus 001 à 149), lu et écrit avec la commande CAT « EX » : EX P1P1P1 P2…  (P1 = numéro à 3 chiffres, P2 = valeur).
    // Les valeurs lues sont mémorisées (menu-ft2000.ini) ; les noms et les listes de valeurs possibles se corrigent par clic droit (menu-ft2000-defs.ini),
    // car certaines valeurs du manuel ne sont pas à jour.
    public static class RigMenu
    {
        public static string Key = "ft2000";                          // poste dont le menu est ouvert
        static string[] Tbl { get { string[] t; return RigMenuTables.Tab.TryGetValue(Key.StartsWith("ft991a") ? "ft991a" : Key, out t) ? t : null; } }
        static int tmax = -1; static string tmaxKey = "";
        // nombre de menus : 149 pour le FT-2000, le plus grand numéro du manuel pour les autres postes à table, sinon 199 (lecture jusqu'à 20 absences de réponse)
        public static int Count { get { if (Key == "ft2000") return 149; string[] t = Tbl; if (t == null) return 199; if (tmaxKey != Key) { tmax = 0; foreach (string l in t) { int n; if (int.TryParse(l.Substring(0, 3), out n) && n > tmax) tmax = n; } tmaxKey = Key; } return tmax; } }
        public static bool HasTable { get { return Key == "ft2000" || Tbl != null; } }
        public static string Dir;                                   // dossier des fichiers (par défaut celui des réglages)
        static string D { get { return Dir ?? Cfg.Dir ?? "."; } }
        public static string FileVals { get { return Path.Combine(D, "menu-" + Key + ".ini"); } }
        public static string FileDefs { get { return Path.Combine(D, "menu-" + Key + "-defs.ini"); } }
        public static bool Applies(RadioDef d) { return Backup.HasMenu(d); }
        static readonly HashSet<int> critical = new HashSet<int> { 28, 29, 30, 143, 144, 147 };   // liaison CAT, polarisation, puissance maximale, puissance du tuner
        public static bool Critical(int n) { if (Key == "ft2000") return critical.Contains(n); return System.Text.RegularExpressions.Regex.IsMatch(ManualName(n), @"bCATb|RTS|BAUD|POWER|TUNER|BIAS|PTT|TX GND|TOT|TX TIME"); }

        // ---- valeurs lues (numéro -> texte tel que le poste le renvoie, signe et zéros compris)
        public static Dictionary<int, string> LoadVals()
        {
            Dictionary<int, string> d = new Dictionary<int, string>();
            try { if (File.Exists(FileVals)) foreach (string l in File.ReadAllLines(FileVals, Encoding.UTF8)) { int k = l.IndexOf('='), n; if (k > 0 && int.TryParse(l.Substring(0, k), out n) && n >= 1 && n <= Count) d[n] = l.Substring(k + 1).Trim(); } } catch { }
            return d;
        }
        public static void SaveVals(Dictionary<int, string> d)
        {
            try { List<string> l = new List<string>(); for (int n = 1; n <= Count; n++) { string v; if (d.TryGetValue(n, out v)) l.Add(n.ToString("000") + "=" + v); } Directory.CreateDirectory(D); File.WriteAllLines(FileVals, l.ToArray(), Encoding.UTF8); } catch { }
        }

        static readonly string[] DefaultDefsFt2000 = { "010.name=DISP BAR SEL", "012.name=DISP RTR STU", "013.name=DISP RTR ADJ", "016.name=DUS RX LVL", "017.name=DUS TX LEVEL" };
        // ---- noms et listes de valeurs personnalisés : « 001.name=… » et « 001.vals=0=OFF;1=ON »
        public static Dictionary<string, string> LoadDefs()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            if (Key == "ft2000") foreach (string kv in DefaultDefsFt2000) { int q = kv.IndexOf('='); d[kv.Substring(0, q)] = kv.Substring(q + 1); }   // noms d'origine relevés sur l'afficheur du poste
            try { if (File.Exists(FileDefs)) foreach (string l in File.ReadAllLines(FileDefs, Encoding.UTF8)) { int k = l.IndexOf('='); if (k > 0) d[l.Substring(0, k)] = l.Substring(k + 1).Replace("\\n", "\n"); } } catch { }
            return d;
        }
        public static void SaveDefs(Dictionary<string, string> d)
        {
            try { List<string> l = new List<string>(); foreach (KeyValuePair<string, string> kv in d) if (kv.Value.Length > 0) l.Add(kv.Key + "=" + kv.Value.Replace("\r", "").Replace("\n", "\\n")); l.Sort(StringComparer.Ordinal); Directory.CreateDirectory(D); File.WriteAllLines(FileDefs, l.ToArray(), Encoding.UTF8); } catch { }
        }
        static string TabField(int n, int f) { string[] t = Tbl; if (t == null) return ""; string p = n.ToString("000") + "|"; foreach (string l in t) if (l.StartsWith(p)) { string[] q = l.Split(new char[] { '|' }, 3); return q.Length > f ? q[f] : ""; } return ""; }
        public static string ManualName(int n) { if (Key == "ft2000") return n >= 1 && n <= 149 ? Ft2000MenuNames.Names[n - 1] : ""; return TabField(n, 1); }
        public static string Name(Dictionary<string, string> defs, int n) { string s; if (defs.TryGetValue(n.ToString("000") + ".name", out s) && s.Length > 0) return s; s = ManualName(n); return s.Length > 0 ? s : "Menu " + n.ToString("000"); }
        // valeurs possibles : « 0=OFF;1=ON;2=… » -> liste (valeur, libellé)
        public static List<KeyValuePair<string, string>> Choices(Dictionary<string, string> defs, int n) { return Choices(defs, n, false); }
        // texte des valeurs possibles du manuel (FT-2000D : liste propre à ce modèle quand elle diffère)
        public static string ManualP2(int n, bool d) { if (n < 1 || n > Count) return ""; if (Key != "ft2000") return TabField(n, 2); string s = d ? Ft2000MenuInfo.P2D(n) : null; return s ?? Ft2000MenuInfo.P2[n - 1]; }
        public static int Bytes(int n) { return Key == "ft2000" && n >= 1 && n <= 149 ? Ft2000MenuInfo.Bytes[n - 1] : 0; }
        // liste « valeur: libellé » du manuel (vide quand le menu a une plage et non une liste)
        public static List<KeyValuePair<string, string>> ManualChoices(int n, bool d)
        {
            List<KeyValuePair<string, string>> l = new List<KeyValuePair<string, string>>(); string t = ManualP2(n, d); if (!System.Text.RegularExpressions.Regex.IsMatch(t, @"^\d{1,4}:")) return l;
            System.Text.RegularExpressions.MatchCollection mc = System.Text.RegularExpressions.Regex.Matches(t, @"(?:(?<=\s)|^)(\d{1,4}):\s*");
            for (int i = 0; i < mc.Count; i++) { int s = mc[i].Index + mc[i].Length, e = i + 1 < mc.Count ? mc[i + 1].Index : t.Length; l.Add(new KeyValuePair<string, string>(mc[i].Groups[1].Value, t.Substring(s, e - s).Trim())); }
            return l;
        }
        public static List<KeyValuePair<string, string>> Choices(Dictionary<string, string> defs, int n, bool d)
        {
            List<KeyValuePair<string, string>> l = new List<KeyValuePair<string, string>>(); string s;
            if (!defs.TryGetValue(n.ToString("000") + ".vals", out s)) return ManualChoices(n, d);
            foreach (string p in s.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)) { int k = p.IndexOf('='); if (k > 0) l.Add(new KeyValuePair<string, string>(p.Substring(0, k).Trim(), p.Substring(k + 1).Trim())); }
            return l;
        }
        public static bool SameNumPublic(string a, string b) { return SameNum(a, b); }
        static bool SameNum(string a, string b) { long x, y; if (long.TryParse(a, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out x) && long.TryParse(b, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out y)) return x == y; return a == b; }
        public static string Meaning(Dictionary<string, string> defs, int n, string val) { return Meaning(defs, n, val, false); }
        public static string Meaning(Dictionary<string, string> defs, int n, string val, bool d)
        {
            if (val == null) return ""; foreach (KeyValuePair<string, string> c in Choices(defs, n, d)) if (SameNum(c.Key, val)) return c.Value; return "";
        }

        // Met la saisie au format du poste : même nombre de chiffres que la valeur lue, même présence d'un signe. null = saisie invalide.
        public static string Format(string current, string input)
        {
            if (current == null || current.Length == 0 || input == null) return null; input = input.Trim().Replace(',', '.'); long v; bool signed = current[0] == '+' || current[0] == '-';
            if (!long.TryParse(input, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out v)) return null;
            int width = current.Length - (signed ? 1 : 0); string digits = Math.Abs(v).ToString(CultureInfo.InvariantCulture).PadLeft(width, '0');
            if (digits.Length > width) return null; if (!signed && v < 0) return null;
            return signed ? (v < 0 ? "-" : "+") + digits : digits;
        }

        // ---- échanges avec le poste
        public static string Read(Rig rig, int n)
        {
            string pre = "EX" + n.ToString("000"); string a = rig.AskPrefix(pre + ";", pre, 700); if (a == null) a = rig.AskPrefix(pre + ";", pre, 700);
            if (a == null) return null; a = a.Trim(); if (!a.StartsWith(pre) || a.Length <= pre.Length) return null; return a.Substring(pre.Length).TrimEnd(';');
        }
        // envoie la valeur puis la relit : renvoie la valeur lue (null = pas de réponse)
        public static string Write(Rig rig, int n, string val)
        {
            rig.Raw("EX" + n.ToString("000") + val + ";"); Thread.Sleep(250); return Read(rig, n);
        }

        // ouvre la fenêtre (une seule à la fois)
        static MenuDlg open;
        public static void Open(Rig rig, IWin32Window owner)
        {
            if (open != null && !open.IsDisposed) { open.Activate(); return; } Key = rig.Def.Key;
            open = new MenuDlg(rig, owner); open.Show(owner);
        }
    }

    public class MenuDlg : Form
    {
        readonly Rig rig; readonly DataGridView grid; bool IsD { get { return rig != null && rig.SeenId != null && rig.SeenId.Contains("0252"); } }
        // valeurs possibles affichées : celles de l'utilisateur, sinon celles du manuel
        string Possible(int n) { string s; if (defs.TryGetValue(n.ToString("000") + ".vals", out s) && s.Length > 0) return s.Replace(";", "   "); return RigMenu.ManualP2(n, IsD); }
        readonly Label status; readonly Button btnDl, btnStop; readonly TextBox find;
        Dictionary<int, string> vals; Dictionary<string, string> defs; readonly Dictionary<int, string> prev = new Dictionary<int, string>();
        volatile bool stop, busy; bool editing;

        public const int DefW = 1366, DefH = 700;   // taille d'origine : celle de la fenêtre de l'utilisateur ; réduite si l'écran est plus petit
        public MenuDlg(Rig r, IWin32Window owner = null)
        {
            rig = r; RigMenu.Key = r.Def.Key; vals = RigMenu.LoadVals(); defs = RigMenu.LoadDefs();
            Text = "Menu du " + CmdServer.ModelName(r.Def) + " — menus 001 à " + RigMenu.Count.ToString("000"); ClientSize = FitScreen(owner); StartPosition = FormStartPosition.CenterParent; BackColor = T.Bg; ForeColor = T.Text; Font = new Font("Segoe UI", 9.5f); MinimumSize = new Size(560, 360);
            Panel top = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = T.Panel };
            btnDl = Mk("Télécharger les valeurs du poste", 8, 8, 270, delegate { Download(); }); btnStop = Mk("Arrêter", 286, 8, 90, delegate { stop = true; }); btnStop.Visible = false;
            Label lf = new Label { Text = "Chercher :", AutoSize = true, Left = 400, Top = 14, ForeColor = T.Dim };
            find = new TextBox { Left = 478, Top = 10, Width = 220, BackColor = T.Panel2, ForeColor = T.Text }; find.TextChanged += delegate { Filter(); };
            top.Controls.AddRange(new Control[] { btnDl, btnStop, lf, find });
            status = new Label { Dock = DockStyle.Bottom, Height = 46, ForeColor = T.Dim, Padding = new Padding(8, 4, 8, 0) };
            grid = new DataGridView
            {
                Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
                BackgroundColor = T.Panel, GridColor = T.Edge, BorderStyle = BorderStyle.None, EnableHeadersVisualStyles = false, ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing, ColumnHeadersHeight = 28, EditMode = DataGridViewEditMode.EditOnEnter
            };
            grid.DefaultCellStyle.BackColor = T.Panel; grid.DefaultCellStyle.ForeColor = T.Text; grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(60, 90, 140); grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.BackColor = T.Panel2; grid.ColumnHeadersDefaultCellStyle.ForeColor = T.Text; grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = T.Panel2; grid.RowTemplate.Height = 24;
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "n", HeaderText = "N°", Width = 50, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "name", HeaderText = "Menu", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 36, MinimumWidth = 120, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "val", HeaderText = "Valeur (modifiable)", Width = 120 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "mean", HeaderText = "Signification", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 22, MinimumWidth = 90, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "poss", HeaderText = "Valeurs possibles", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 42, MinimumWidth = 120, ReadOnly = true });
            foreach (string cn in new string[] { "name", "mean", "poss" }) grid.Columns[cn].DefaultCellStyle.WrapMode = DataGridViewTriState.True;   // retour à la ligne : tout le texte reste visible
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells; grid.ScrollBars = ScrollBars.Vertical;   grid.DefaultCellStyle.Padding = new Padding(2, 2, 2, 2);   // seulement l'ascenseur haut / bas : les colonnes s'adaptent à la largeur de la fenêtre
            grid.Columns["val"].DefaultCellStyle.ForeColor = T.Amber; grid.Columns["val"].DefaultCellStyle.Font = new Font("Consolas", 10f, FontStyle.Bold);
            Label warn = new Label { Dock = DockStyle.Top, Height = 46, BackColor = Color.FromArgb(150, 0, 0), ForeColor = Color.White, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter, Text = "⚠  ATTENTION : chaque valeur modifiée est envoyée IMMÉDIATEMENT au poste." + Environment.NewLine + "Une erreur peut dérégler le poste (liaison CAT, puissance, émission). Ne modifiez que ce que vous maîtrisez." };
            Controls.Add(grid); Controls.Add(top); Controls.Add(status); Controls.Add(warn);
            for (int n = 1; n <= RigMenu.Count; n++) grid.Rows.Add(n.ToString("000"), "", "", "", "");
            RefreshAll();
            grid.CellBeginEdit += delegate(object s, DataGridViewCellCancelEventArgs e)
            {
                if (busy || rig.Link == null) { e.Cancel = true; if (rig.Link == null) Say("Poste non connecté : connectez-le pour modifier une valeur."); return; }
                List<KeyValuePair<string, string>> ch = RigMenu.Choices(defs, e.RowIndex + 1, IsD);
                if (e.ColumnIndex == 2 && ch.Count > 0) { e.Cancel = true; ChoiceMenu(e.RowIndex, ch, Cursor.Position); return; }   // menu à liste : on choisit dans la liste
                editing = true;
            };
            grid.CellEndEdit += delegate(object s, DataGridViewCellEventArgs e) { if (!editing) return; editing = false; if (e.ColumnIndex == 2) ApplyEdit(e.RowIndex, Convert.ToString(grid[2, e.RowIndex].Value)); };
            grid.CellMouseDown += delegate(object s, DataGridViewCellMouseEventArgs e) { if (e.Button == MouseButtons.Right && e.RowIndex >= 0) { grid.ClearSelection(); grid.Rows[e.RowIndex].Selected = true; grid.CurrentCell = grid[Math.Max(e.ColumnIndex, 0), e.RowIndex]; RowMenu(e.RowIndex, Cursor.Position); } };
            Shown += delegate
            {
                if (vals.Count == 0) Say("Aucune valeur mémorisée : cliquez sur « Télécharger les valeurs du poste » (première connexion).");
                else Say(vals.Count + " valeurs mémorisées. Clic droit sur une ligne : renommer, définir les valeurs possibles. Double-clic sur une valeur : la modifier.");
                if (vals.Count == 0 && rig.Link != null && MessageBox.Show(this, "C'est la première fois : lire maintenant les " + RigMenu.Count + " menus du poste ?\n(1 à 2 minutes ; vous pouvez arrêter à tout moment)", "Menu du poste", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) Download();
            };
            FormClosing += delegate(object s, FormClosingEventArgs e) { if (busy) { stop = true; } };
        }
        public int RowsShown { get { return grid.Rows.Count; } }
        public string TestValue(int n) { return Convert.ToString(grid[2, n - 1].Value); }
        public string TestMean(int n) { return Convert.ToString(grid[3, n - 1].Value); }
        public string TestName(int n) { return Convert.ToString(grid[1, n - 1].Value); }
        public void TestApply(int n, string s) { ApplyEdit(n - 1, s); }
        public string TestStatus { get { return status.Text; } }
        // 1366 x 700, sauf si la zone utile de l'écran est plus petite (bordures et barre de titre déduites)
        static Size FitScreen(IWin32Window owner)
        {
            Rectangle wa; try { wa = owner != null ? Screen.FromHandle(owner.Handle).WorkingArea : Screen.PrimaryScreen.WorkingArea; } catch { wa = Screen.PrimaryScreen.WorkingArea; }
            return new Size(Math.Max(560, Math.Min(DefW, wa.Width - 24)), Math.Max(360, Math.Min(DefH, wa.Height - 80)));
        }
        public Size TestFit(int sw, int sh) { return new Size(Math.Max(560, Math.Min(DefW, sw - 24)), Math.Max(360, Math.Min(DefH, sh - 80))); }
        Button Mk(string t, int x, int y, int w, Action a) { Button b = new Button { Text = t, Left = x, Top = y, Width = w, Height = 30, FlatStyle = FlatStyle.Flat, BackColor = T.Panel2, ForeColor = T.Text }; b.FlatAppearance.BorderColor = T.Edge; b.Click += delegate { a(); }; return b; }
        void Say(string s) { if (IsDisposed) return; status.Text = s; }

        void RefreshRow(int i)
        {
            int n = i + 1; string v; vals.TryGetValue(n, out v);
            grid[1, i].Value = RigMenu.Name(defs, n) + (RigMenu.Critical(n) ? "   ⚠" : ""); grid[2, i].Value = v ?? "—"; grid[3, i].Value = RigMenu.Meaning(defs, n, v, IsD); grid[4, i].Value = Possible(n); grid[4, i].Style.ForeColor = defs.ContainsKey(n.ToString("000") + ".vals") ? T.Cyan : T.Dim;
            grid[1, i].Style.ForeColor = defs.ContainsKey(n.ToString("000") + ".name") ? T.Cyan : T.Text;
        }
        void RefreshAll() { for (int i = 0; i < RigMenu.Count; i++) RefreshRow(i); Filter(); }
        void Filter()
        {
            string q = find.Text.Trim().ToLowerInvariant();
            foreach (DataGridViewRow r in grid.Rows) r.Visible = q.Length == 0 || (r.Cells[0].Value + " " + r.Cells[1].Value + " " + r.Cells[3].Value).ToString().ToLowerInvariant().Contains(q);
        }

        // ---- téléchargement des 149 menus (fil séparé, avec progression et arrêt)
        void Download()
        {
            if (busy) return; if (rig.Link == null) { MessageBox.Show(this, "Connectez d'abord le poste.", "Menu du poste"); return; }
            busy = true; stop = false; btnDl.Enabled = false; btnStop.Visible = true; int got = 0, miss = 0, consec = 0, total = RigMenu.Count; bool tbl = RigMenu.HasTable;
            Thread th = new Thread(delegate()
            {
                for (int n = 1; n <= total && !stop; n++)
                {
                    int nn = n; string v = RigMenu.Read(rig, n);
                    if (v != null) { got++; consec = 0; lock (vals) vals[n] = v; } else { miss++; consec++; if (!tbl && got > 0 && consec >= 20) { stop = true; } if (!tbl && got == 0 && consec >= 30) { stop = true; } }
                    try { BeginInvoke(new Action(delegate { Say("Lecture du menu " + nn.ToString("000") + " / " + total + " …  (" + got + " lus)"); if (v != null) RefreshRow(nn - 1); })); } catch { return; }
                }
                try
                {
                    BeginInvoke(new Action(delegate
                    {
                        lock (vals) RigMenu.SaveVals(vals); busy = false; btnDl.Enabled = true; btnStop.Visible = false;
                        Say((stop ? "Arrêté : " : "Terminé : ") + got + " valeurs lues" + (miss > 0 ? ", " + miss + " sans réponse" : "") + ". Enregistrées dans " + Path.GetFileName(RigMenu.FileVals) + ".");
                    }));
                }
                catch { }
            });
            th.IsBackground = true; th.Start();
        }

        // ---- modification d'une valeur : mise au format du poste, confirmation si critique, envoi puis relecture
        void ApplyEdit(int i, string input)
        {
            int n = i + 1; string old; vals.TryGetValue(n, out old);
            if (old == null) { old = rig.Link != null ? RigMenu.Read(rig, n) : null; if (old == null) { Say("Valeur actuelle inconnue : téléchargez d'abord les valeurs."); RefreshRow(i); return; } vals[n] = old; }
            string nv = RigMenu.Format(old, input);
            if (nv == null) { Say("Valeur refusée : " + n.ToString("000") + " attend " + old.Length + " caractère(s) (" + old + "). Saisie : « " + input + " »."); RefreshRow(i); return; }
            if (nv == old) { RefreshRow(i); return; }
            if (RigMenu.Critical(n) && MessageBox.Show(this, "Le menu " + n.ToString("000") + " (" + RigMenu.Name(defs, n) + ") agit sur la liaison CAT, la puissance ou l'émission.\nChanger " + old + " en " + nv + " ?", "Menu critique", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) { RefreshRow(i); return; }
            Cursor = Cursors.WaitCursor; string back; try { back = RigMenu.Write(rig, n, nv); } finally { Cursor = Cursors.Default; }
            if (back != null && RigMenu.Format(nv, back) == nv) { prev[n] = old; vals[n] = back; RigMenu.SaveVals(vals); Say("Menu " + n.ToString("000") + " : " + old + " → " + back + " (accepté par le poste)."); }
            else { Say("Menu " + n.ToString("000") + " : le poste n'a pas accepté " + nv + (back != null ? " (valeur lue : " + back + ")" : " (pas de réponse)") + "."); if (back != null) { vals[n] = back; RigMenu.SaveVals(vals); } }
            RefreshRow(i);
        }

        // ---- clic droit : noms et valeurs possibles
        void RowMenu(int i, Point screen)
        {
            int n = i + 1; string k = n.ToString("000"); ContextMenuStrip m = new ContextMenuStrip { BackColor = T.Panel2, ForeColor = T.Text };
            m.Items.Add("Modifier le nom…", null, delegate
            {
                string s = Ask("Nom du menu " + k, "Nom affiché (le nom du manuel est : " + RigMenu.ManualName(n) + ")", RigMenu.Name(defs, n)); if (s == null) return;
                if (s.Trim().Length == 0 || s.Trim() == RigMenu.ManualName(n)) defs.Remove(k + ".name"); else defs[k + ".name"] = s.Trim(); RigMenu.SaveDefs(defs); RefreshRow(i);
            });
            m.Items.Add("Définir les valeurs possibles…", null, delegate
            {
                string cur = defs.ContainsKey(k + ".vals") ? defs[k + ".vals"] : string.Join(";", RigMenu.ManualChoices(n, IsD).ConvertAll<string>(delegate(KeyValuePair<string, string> c) { return c.Key + "=" + c.Value; }).ToArray());
                string s = Ask("Valeurs possibles du menu " + k, "Une valeur par élément : valeur=libellé, séparés par des points-virgules.\nExemple :  0=OFF;1=ON;2=AUTO   ou   0300=300 ms;0400=400 ms", cur); if (s == null) return;
                if (s.Trim().Length == 0) defs.Remove(k + ".vals"); else defs[k + ".vals"] = s.Trim(); RigMenu.SaveDefs(defs); RefreshRow(i);
            });
            if (defs.ContainsKey(k + ".vals")) m.Items.Add("Rétablir les valeurs possibles du manuel", null, delegate { defs.Remove(k + ".vals"); RigMenu.SaveDefs(defs); RefreshRow(i); });
            List<KeyValuePair<string, string>> ch = RigMenu.Choices(defs, n, IsD);
            if (ch.Count > 0)
            {
                m.Items.Add(new ToolStripSeparator());
                foreach (KeyValuePair<string, string> c in ch) { KeyValuePair<string, string> cc = c; m.Items.Add("Choisir : " + c.Key + " = " + c.Value, null, delegate { if (rig.Link == null) { Say("Poste non connecté."); return; } ApplyEdit(i, cc.Key); }); }
            }
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add("Relire ce menu dans le poste", null, delegate { if (rig.Link == null) { Say("Poste non connecté."); return; } string v = RigMenu.Read(rig, n); if (v == null) Say("Menu " + k + " : pas de réponse."); else { vals[n] = v; RigMenu.SaveVals(vals); RefreshRow(i); Say("Menu " + k + " relu : " + v); } });
            ToolStripItem undo = m.Items.Add("Remettre la valeur précédente", null, delegate { string pv; if (prev.TryGetValue(n, out pv)) ApplyEdit(i, pv); }); undo.Enabled = prev.ContainsKey(n) && rig.Link != null;
            m.Show(screen);
        }
        // menu à liste de valeurs : un clic sur la valeur ouvre la liste (celle du manuel ou celle définie par l'utilisateur)
        void ChoiceMenu(int i, List<KeyValuePair<string, string>> ch, Point screen)
        {
            int n = i + 1; string cur; vals.TryGetValue(n, out cur); ContextMenuStrip m = new ContextMenuStrip { BackColor = T.Panel2, ForeColor = T.Text };
            foreach (KeyValuePair<string, string> c in ch) { KeyValuePair<string, string> cc = c; ToolStripMenuItem it = new ToolStripMenuItem(c.Key + " : " + c.Value) { Checked = cur != null && RigMenu.SameNumPublic(c.Key, cur) }; it.Click += delegate { ApplyEdit(i, cc.Key); }; m.Items.Add(it); }
            m.Items.Add(new ToolStripSeparator()); m.Items.Add("Autre valeur…", null, delegate { string s = Ask("Menu " + n.ToString("000"), "Valeur à envoyer (hors liste ; actuelle : " + cur + ")", cur); if (s != null) ApplyEdit(i, s); });
            m.Show(screen);
        }
        string Ask(string title, string prompt, string cur)
        {
            using (Form f = new Form { Text = title, ClientSize = new Size(520, 190), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, ShowInTaskbar = false, BackColor = T.Bg, ForeColor = T.Text })
            {
                Label l = new Label { Left = 12, Top = 10, Width = 496, Height = 70, Text = prompt }; TextBox t = new TextBox { Left = 12, Top = 88, Width = 496, Text = cur ?? "", BackColor = T.Panel2, ForeColor = T.Text };
                Button ok = new Button { Text = "OK", Left = 330, Top = 140, Width = 85, Height = 30, DialogResult = DialogResult.OK }, ca = new Button { Text = "Annuler", Left = 423, Top = 140, Width = 85, Height = 30, DialogResult = DialogResult.Cancel };
                f.Controls.AddRange(new Control[] { l, t, ok, ca }); f.AcceptButton = ok; f.CancelButton = ca;
                return f.ShowDialog(this) == DialogResult.OK ? t.Text : null;
            }
        }
    }
}
