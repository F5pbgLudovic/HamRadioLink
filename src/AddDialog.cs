using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace HamRadioLink
{
    // Fenêtre « Ajouter » de la vue config : choisir un type d'élément et une commande CAT parmi toutes celles connues du programme
    // (éléments de tous les postes + catalogues des manuels). Le résultat est un Ctl prêt à être placé ; le clic droit permet ensuite de l'ajuster.
    public class AddDialog : Form
    {
        public class Entry
        {
            public string Origin, Name, Set = "", Read = "", Parse = "", Items = "", On = "1", Off = "0", Fmt = "raw", Hint = ""; public int Min, Max = 255, Step = 1; public bool FromCtl, Meter; public override string ToString() { return FromCtl ? Name : Name + "   [" + (Set.Length > 0 ? Set : Read) + "]"; }
        }
        public Ctl Result;
        readonly List<Entry> all = new List<Entry>(); readonly List<Entry> shown = new List<Entry>();
        ComboBox cbType; TextBox tbName, tbFind; ListView lv; Label lbInfo;
        static readonly string[] TypeNames = { "Bouton (envoie une commande)", "Touche de fonction (allumé / éteint)", "Potentiomètre (réglage)", "Choix (rangée de touches)", "Vu-mètre (barre lue sur le poste)", "Voyant (lu sur le poste)" };
        static readonly Kind[] TypeKinds = { Kind.Action, Kind.Toggle, Kind.Knob, Kind.Choice, Kind.Meter, Kind.Led };

        public AddDialog(RadioDef current)
        {
            Text = "Ajouter un élément — " + current.Name; FormBorderStyle = FormBorderStyle.FixedDialog; StartPosition = FormStartPosition.CenterParent; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
            BackColor = T.Panel; ForeColor = T.Text; Font = new Font("Segoe UI", 9f); ClientSize = new Size(940, 640);
            Lab("1. Type d'élément", 12, 10, 300); cbType = new ComboBox(); cbType.DropDownStyle = ComboBoxStyle.DropDownList; cbType.Items.AddRange(TypeNames); cbType.SelectedIndex = 0; cbType.SetBounds(12, 30, 380, 26); Skin(cbType); Controls.Add(cbType);
            Lab("2. Nom affiché", 410, 10, 300); tbName = new TextBox(); tbName.SetBounds(410, 30, 300, 26); Skin(tbName); Controls.Add(tbName);
            Lab("3. Commande CAT de " + current.Name + " (éléments du poste, puis catalogue de son manuel) — filtre :", 12, 66, 600); tbFind = new TextBox(); tbFind.SetBounds(12, 88, 420, 26); Skin(tbFind); Controls.Add(tbFind);
            lv = new ListView(); lv.View = View.Details; lv.FullRowSelect = true; lv.HideSelection = false; lv.MultiSelect = false; lv.SetBounds(12, 122, 916, 430); lv.BackColor = T.Bg; lv.ForeColor = T.Text; lv.BorderStyle = BorderStyle.FixedSingle; lv.Font = new Font("Consolas", 9f);
            lv.Columns.Add("Origine", 190); lv.Columns.Add("Nom", 250); lv.Columns.Add("Envoi", 230); lv.Columns.Add("Lecture", 230); Controls.Add(lv);
            lbInfo = Lab("", 12, 558, 916); lbInfo.ForeColor = T.Dim;
            Button ok = new Button(); ok.Text = "Ajouter"; ok.SetBounds(760, 598, 80, 30); Skin(ok); ok.Click += delegate { Make(); };
            Button ca = new Button(); ca.Text = "Annuler"; ca.SetBounds(848, 598, 80, 30); Skin(ca); ca.DialogResult = DialogResult.Cancel; CancelButton = ca;
            Controls.Add(ok); Controls.Add(ca);
            Collect(current, all); Fill(); tbFind.TextChanged += delegate { Fill(); };
            lv.SelectedIndexChanged += delegate { Entry e = Sel(); if (e != null) { lbInfo.Text = (e.FromCtl ? "Élément existant : tout est repris (envoi, lecture, réponse, valeurs)." : "Modèle fabriqué d'après le format du manuel : à vérifier / ajuster par clic droit (valeur = dernier groupe de paramètres).") + (e.Hint.Length > 0 ? "\n" + e.Hint : ""); if (tbName.Text.Length == 0 || tbName.Tag as string == "auto") { tbName.Text = e.Name; tbName.Tag = "auto"; } } };
            tbName.KeyPress += delegate { tbName.Tag = "user"; };
        }
        Label Lab(string t, int x, int y, int w) { Label l = new Label(); l.Text = t; l.SetBounds(x, y, w, 20); l.ForeColor = T.Dim; if (t.Length == 0) l.Height = 36; Controls.Add(l); return l; }
        void Skin(Control c) { c.BackColor = T.Panel2; c.ForeColor = T.Text; if (c is Button) { Button b = (Button)c; b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderColor = T.Edge; } else if (c is TextBox) ((TextBox)c).BorderStyle = BorderStyle.FixedSingle; else if (c is ComboBox) ((ComboBox)c).FlatStyle = FlatStyle.Flat; }
        Entry Sel() { return lv.SelectedIndices.Count > 0 ? shown[lv.SelectedIndices[0]] : null; }

        // ---- les commandes du poste choisi seulement : ses éléments, puis le catalogue de son manuel (ou du modèle voisin)
        static string Neighbour(RadioDef d)
        {
            if (d.Catalog != null) return d.Catalog;
            switch (d.Key) { case "ftdx10": return "ftdx101"; case "ft2000": return "ft950"; case "ftdx5000": return "ftdx3000"; }
            return null;
        }
        // commande de mesure du signal reçu (S-mètre) : d'après son nom au manuel
        public static bool IsSName(Entry e) { return Regex.IsMatch(e.Name ?? "", @"(?i)\bs[-\s]?m[eè]tre?\b|\bs[-\s]?meter|\bsmeter|\bs[-\s]?level|niveau s\b"); }
        // un S-mètre s'affiche avec l'échelle S1…S9+60 : format « s »
        public static bool IsS(Entry e) { return e.Meter && (e.Fmt == "s" || IsSName(e) || e.FromCtl && Regex.IsMatch(e.Name ?? "", @"(?i)^s\b|\bs[-\s]?m")); }
        public static List<Entry> Entries(RadioDef cur) { List<Entry> l = new List<Entry>(); Collect(cur, l); return l; }
        static void Collect(RadioDef cur, List<Entry> all)
        {
            HashSet<string> seen = new HashSet<string>();
            foreach (Ctl c in cur.C)
            {
                if (!c.HasCat || c.Hidden || (c.Set.Length == 0 && c.Read.Length == 0)) continue; string k = c.Set + "|" + c.Read; if (!seen.Add(k)) continue;
                Entry e = new Entry(); e.FromCtl = true; e.Origin = cur.Name + " · " + c.K; e.Name = c.Label; e.Set = c.Set; e.Read = c.Read; e.Parse = c.Parse; e.Items = c.Items; e.On = c.OnVal; e.Off = c.OffVal; e.Min = c.Min; e.Max = c.Max; e.Fmt = c.Fmt; e.Step = c.Step; e.Hint = c.Hint; e.Meter = c.K == Kind.Meter; all.Add(e);
            }
            if (cur.Civ >= 0)
            {
                string key = cur.CivCat ?? "*"; string txt = Civ.CatText(key);
                string org = key == "*" ? "Catalogue CI-V générique (8 manuels Icom ; pas de manuel analysé pour ce poste)" : "Catalogue CI-V du manuel " + cur.Name;
                foreach (string line in txt.Split('\n'))
                {
                    string[] p = line.TrimEnd('\r').Split('|'); if (p.Length < 4 || p[0].Length != 2) continue;
                    string k = p[0] + p[1].Replace(" ", ""); if (!seen.Add("V|" + k)) continue;
                    Entry e = new Entry(); e.Origin = org; e.Name = k + " — " + p[3]; CivTemplate(e, k, p[2], cur.FreqDigits); e.Meter = IsSName(e); all.Add(e);
                }
            }
            else
            {
                string cat = Neighbour(cur); string txt;
                if (cat != null && Catalogs.D.TryGetValue(cat, out txt))
                    foreach (string line in txt.Split('\n'))
                    {
                        string[] p = line.TrimEnd('\r').Split('|'); if (p.Length < 4 || p[2].Length == 0 && p[3].Length == 0) continue;
                        string k = "C|" + p[2] + "|" + p[3]; if (!seen.Add(k)) continue;
                        Entry e = new Entry(); e.Origin = "Catalogue " + cat.ToUpperInvariant() + (cat == cur.Catalog ? "" : " (modèle voisin)"); e.Name = p[0] + " — " + p[1]; Template(e, p[2], p[3]); e.Meter = IsSName(e); all.Add(e);
                    }
            }
            all.Sort(delegate(Entry a, Entry b) { if (a.FromCtl != b.FromCtl) return a.FromCtl ? -1 : 1; return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase); });
        }
        // modèle CI-V d'après la ligne du manuel : « 1401 / BCD 2 bytes 0000~0255 » -> envoi 1401{v:4}; lecture 1401; réponse ^1401(?<v>\d{4});
        static void CivTemplate(Entry e, string key, string spec, int digits)
        {
            e.Hint = spec; Match m = Regex.Match(spec, @"BCD (\d+) bytes?", RegexOptions.IgnoreCase); string s = spec.Trim();
            if (s == "none") { e.Set = key + ";"; return; }
            if (m.Success && m.Groups[1].Value == "5") { e.Set = key + "{v:" + digits + "};"; e.Read = key + ";"; e.Parse = "^" + key + @"(?<v>\d{" + digits + "});$"; e.Fmt = "hz"; e.Max = 100000; return; }
            if (m.Success && m.Groups[1].Value == "2") { e.Set = key + "{v:4};"; e.Read = key + ";"; e.Parse = "^" + key + @"(?<v>\d{4});$"; e.Fmt = "pct"; e.Max = 255; return; }
            if (Regex.IsMatch(s, @"^1 byte")) { e.Set = key + "{v};"; e.Read = key + ";"; e.Parse = "^" + key + "(?<v>[0-9A-F]{2});$"; e.On = "01"; e.Off = "00"; e.Items = "OFF=00,ON=01"; return; }
            e.Set = key + "{v};"; e.Read = key + ";"; e.Parse = "^" + key + "(?<v>[0-9A-F]+);$"; e.On = "01"; e.Off = "00";
        }
        // modèle d'après le format du manuel : XXP1P2P2P2; -> « XX0{v:3}; » (dernier groupe = valeur, autres paramètres à 0)
        static void Template(Entry e, string setF, string readF)
        {
            List<string> toks = Toks(setF); int last = -1; for (int i = toks.Count - 1; i >= 0; i--) if (toks[i].StartsWith("P")) { last = i; break; }
            string name = last >= 0 ? toks[last] : ""; int n = 0; int first = last; while (first >= 0 && toks[first] == name) { n++; first--; } first++;
            StringBuilder set = new StringBuilder(), prs = new StringBuilder("^");
            for (int i = 0; i < toks.Count; i++)
            {
                string t = toks[i];
                if (last >= 0 && i == first) { set.Append("{v:" + n + "}"); prs.Append("(?<v>\\d{" + n + "})"); }
                if (last >= 0 && i >= first && i <= last) continue;
                if (t.StartsWith("P")) { set.Append('0'); prs.Append('0'); } else if (t == "S") { set.Append('+'); prs.Append("[+-]"); } else { set.Append(t); prs.Append(Regex.Escape(t)); }
            }
            if (last < 0) { e.Set = setF; } else { e.Set = set.ToString() + ";"; e.Parse = prs.ToString() + ";$"; }
            if (last >= 0) e.Fmt = "raw";
            StringBuilder rd = new StringBuilder(); foreach (string t in Toks(readF)) rd.Append(t.StartsWith("P") ? "0" : (t == "S" ? "+" : t)); e.Read = readF.Length > 0 ? rd.ToString() + ";" : "";
            if (e.Read.Length == 0) e.Parse = "";
            e.Max = n >= 3 ? 255 : (n == 2 ? 99 : 9);
        }
        static List<string> Toks(string f)
        {
            List<string> l = new List<string>(); if (f.EndsWith(";")) f = f.Substring(0, f.Length - 1); int i = 0;
            while (i < f.Length)
            {
                if (f[i] == 'P' && i + 1 < f.Length && char.IsDigit(f[i + 1])) { int j = i + 1; while (j < f.Length && char.IsDigit(f[j])) j++; l.Add(f.Substring(i, j - i)); i = j; continue; }
                if (f.Substring(i).StartsWith("-/+")) { l.Add("S"); i += 3; continue; }
                if (f[i] == '~' || f[i] == ' ') { i++; continue; }
                l.Add(f[i].ToString()); i++;
            }
            return l;
        }

        void Fill()
        {
            string f = tbFind.Text.Trim().ToLowerInvariant(); shown.Clear(); lv.BeginUpdate(); lv.Items.Clear();
            foreach (Entry e in all)
            {
                if (f.Length > 0 && (e.Origin + " " + e.Name + " " + e.Set + " " + e.Read).ToLowerInvariant().IndexOf(f) < 0) continue;
                shown.Add(e); ListViewItem it = new ListViewItem(e.Origin); it.SubItems.Add(e.Name); it.SubItems.Add(e.Set); it.SubItems.Add(e.Read); lv.Items.Add(it);
                if (shown.Count >= 3000) break;
            }
            lv.EndUpdate(); Text = Text.Split('(')[0].TrimEnd() + " (" + shown.Count + " commandes)";
        }

        void Make()
        {
            Entry e = Sel(); if (e == null) { MessageBox.Show(this, "Choisissez une commande dans la liste.", "Ajouter"); return; }
            Kind k = TypeKinds[cbType.SelectedIndex]; Ctl c = new Ctl(); c.K = k; c.Custom = true; c.Label = tbName.Text.Trim().Length > 0 ? tbName.Text.Trim() : e.Name;
            c.Set = e.Set; c.Read = e.Read; c.Parse = e.Parse; c.Items = e.Items; c.OnVal = e.On; c.OffVal = e.Off; c.Min = e.Min; c.Max = e.Max; c.Fmt = e.Fmt; c.Step = Math.Max(1, e.Step); c.Hint = e.Hint;
            switch (k)
            {
                case Kind.Action: c.Read = ""; c.Parse = ""; c.W = 120; c.H = 40; if (c.Set.Contains("{")) c.Set = Tpl.Fill(c.Set, c.OnSend); break;
                case Kind.Toggle: c.W = 110; c.H = 40; c.Period = 2200; break;
                case Kind.Knob: c.W = 90; c.H = 110; c.Period = 2200; if (c.Max <= c.Min) { c.Min = 0; c.Max = 255; } break;
                case Kind.Choice: c.Row = true; c.Rows = 1; c.W = 400; c.H = 44; c.Period = 2200; if (c.Items.Length == 0) c.Items = "OFF=0,ON=1"; break;
                case Kind.Meter: c.W = 480; c.H = 34; c.Period = 300; c.Set = ""; c.Fmt = "pct"; c.Max = e.Max > 1 ? e.Max : 255; break;
                case Kind.Led: c.W = 110; c.H = 26; c.Set = ""; c.Period = 500; c.OnVal = "1"; c.Accent = "green"; break;
            }
            if ((k == Kind.Meter || k == Kind.Led || k == Kind.Toggle || k == Kind.Knob || k == Kind.Choice) && c.Read.Length == 0) { MessageBox.Show(this, "Cette commande n'a pas de lecture : choisissez « Bouton », ou ajoutez une commande de lecture après coup (clic droit).", "Ajouter"); if (k == Kind.Meter || k == Kind.Led) return; }
            Result = c; DialogResult = DialogResult.OK; Close();
        }
    }
}
