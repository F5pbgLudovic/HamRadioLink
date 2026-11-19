using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace HamRadioLink
{
    // Couleurs de l'interface (reprise du principe d'IC7760Link) : modifiables, mémorisées, appliquées tout de suite
    public static class Theme
    {
        public static readonly string[] Names = { "Arrière-plan général", "Panneaux", "Boutons et champs", "Bordures", "Texte (avant-plan)", "Texte atténué", "Accent (titres, voyants)", "Cyan (infos)", "Rouge (émission, alertes)", "Vert (connexion, OK)", "Bleu", "Verrouillage (cadre rouge)", "(réservé)", "Touche Lock verrouillée", "Voile d'un organe verrouillé", "Touche active (fond)", "Bouton de connexion : connecté (OK)", "Bouton de connexion : perdue / absente" };
        static readonly Color[] def; static Theme() { def = Snapshot(); }
        public static Color Get(int i)
        {
            switch (i) { case 0: return T.Bg; case 1: return T.Panel; case 2: return T.Panel2; case 3: return T.Edge; case 4: return T.Text; case 5: return T.Dim; case 6: return T.Amber; case 7: return T.Cyan; case 8: return T.Red; case 9: return T.Green; case 10: return T.Blue; case 11: return T.Lock; case 12: return T.PttBg; case 13: return T.LockBtn; case 14: return T.Veil; case 15: return T.OnBg; case 16: return T.ConnOk; default: return T.ConnKo; }
        }
        public static void Set(int i, Color c)
        {
            c = Color.FromArgb(255, c);
            switch (i) { case 0: T.Bg = c; break; case 1: T.Panel = c; break; case 2: T.Panel2 = c; break; case 3: T.Edge = c; break; case 4: T.Text = c; break; case 5: T.Dim = c; break; case 6: T.Amber = c; break; case 7: T.Cyan = c; break; case 8: T.Red = c; break; case 9: T.Green = c; break; case 10: T.Blue = c; break; case 11: T.Lock = c; break; case 12: T.PttBg = c; break; case 13: T.LockBtn = c; break; case 14: T.Veil = c; break; case 15: T.OnBg = c; break; case 16: T.ConnOk = c; break; default: T.ConnKo = c; break; }
        }
        public static Color Default(int i) { return def[i]; }
        public static Color[] Snapshot() { Color[] a = new Color[Names.Length]; for (int i = 0; i < a.Length; i++) a[i] = Get(i); return a; }
        public static void Load() { for (int i = 0; i < Names.Length; i++) { int v = Cfg.GetInt("theme." + i, 0); if (i == 14 && (v == Color.FromArgb(255, 255, 140, 20).ToArgb() || v == Color.FromArgb(255, 52, 56, 64).ToArgb())) continue; if (v != 0) Set(i, Color.FromArgb(v)); } }      // 14 : anciens voiles orange / gris, remplacés par le rouge foncé      // 12 : emplacement réservé (non affiché), pour ne pas décaler les couleurs enregistrées
        public static void Save() { for (int i = 0; i < Names.Length; i++) Cfg.SetInt("theme." + i, Get(i).ToArgb()); Cfg.Save(); }
        public static Dictionary<int, Color> Map(Color[] before, Color[] after)
        {
            Dictionary<int, Color> m = new Dictionary<int, Color>(); for (int i = 0; i < before.Length; i++) if (before[i].ToArgb() != after[i].ToArgb() && !m.ContainsKey(before[i].ToArgb())) m[before[i].ToArgb()] = after[i]; return m;
        }
        static Color M(Dictionary<int, Color> m, Color c) { Color r; return m.TryGetValue(c.ToArgb(), out r) ? r : c; }
        public static void Recolor(Control c, Dictionary<int, Color> m)
        {
            if (c.BackColor.A == 255) { Color nb = M(m, c.BackColor); if (nb.ToArgb() != c.BackColor.ToArgb()) c.BackColor = nb; }
            Color nf = M(m, c.ForeColor); if (nf.ToArgb() != c.ForeColor.ToArgb()) c.ForeColor = nf;
            Btn b = c as Btn; if (b != null) b.Accent = M(m, b.Accent);
            foreach (Control ch in c.Controls) Recolor(ch, m);
            c.Invalidate();
        }
    }

    // Fenêtre des paramètres (menu à gauche, page à droite)
    public class OptionsDlg : Form
    {
        public event Action<Dictionary<int, Color>> ThemeChanged; public event Action<bool> AllMeters; public event Action UnlockAll; public event Action OpenMonitor; public event Action ResetAllLocks, VeilChanged;
        readonly Panel page = new Panel(); readonly Rig rig; readonly Func<int> lockCount;
        public OptionsDlg(Rig rig, Func<int> lockCount)
        {
            this.rig = rig; this.lockCount = lockCount;
            Text = "Paramètres"; BackColor = T.Bg; ForeColor = T.Text; Font = new Font("Segoe UI", 9f); ClientSize = new Size(800, 430); StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
            ListBox menu = new ListBox(); menu.SetBounds(0, 0, 190, 430); menu.BackColor = Color.FromArgb(14, 18, 26); menu.ForeColor = T.Text; menu.BorderStyle = BorderStyle.None; menu.Font = new Font("Segoe UI Semibold", 10f); menu.ItemHeight = 28; menu.IntegralHeight = false; Controls.Add(menu);
            foreach (string s in new string[] { "  Couleurs de l'interface", "  Vumètres", "  Réglage molette souris", "  Verrous", "  Commande manuelle", "  Réinitialiser les verrous", "  Journal des commandes", "  Réglages des ports" }) menu.Items.Add(s);
            menu.SelectedIndexChanged += delegate { switch (menu.SelectedIndex) { case 7: PortsPage(); break; case 1: Meters(); break; case 2: Wheel(); break; case 3: Locks(); break; case 4: Manual(); break; case 5: ResetLocks(); break; case 6: LogPage(); break; default: Colors(); break; } };
            FormClosed += delegate { Cfg.Save(); };
            page.SetBounds(200, 0, 600, 430); Controls.Add(page); menu.SelectedIndex = 0;
        }
        Label Head(string t, string sub) { Label h = new Label(); h.Text = t; h.Font = new Font("Segoe UI Semibold", 12f); h.ForeColor = T.Amber; h.SetBounds(6, 8, 400, 26); page.Controls.Add(h); Label d = new Label(); d.Text = sub; d.SetBounds(6, 38, 586, 52); page.Controls.Add(d); return d; }
        Button Mk(string t, int x, int y, int w) { Button b = new Button(); b.Text = t; b.SetBounds(x, y, w, 30); b.FlatStyle = FlatStyle.Flat; b.BackColor = T.Panel2; b.ForeColor = T.Text; b.FlatAppearance.BorderColor = T.Edge; page.Controls.Add(b); return b; }
        Color Ask(Color c, string title) { using (IC7760.ColorPickForm f = new IC7760.ColorPickForm(c, title)) return f.ShowDialog(this) == DialogResult.OK ? f.Result : c; }
        void Colors()
        {
            page.Controls.Clear(); Head("Couleurs de l'interface", "Cliquez sur une couleur pour la modifier (palette 64 couleurs ou RVB). Le changement est immédiat et mémorisé.");
            for (int i = 0; i < Theme.Names.Length; i++)
            {
                if (i == 12) continue; int k = i; int j = i > 12 ? i - 1 : i; int x = 6 + (j / 9) * 296, y = 92 + (j % 9) * 32;
                Panel p = new Panel(); p.SetBounds(x, y, 60, 28); p.BorderStyle = BorderStyle.FixedSingle; p.Cursor = Cursors.Hand; p.BackColor = Theme.Get(k); page.Controls.Add(p);
                Label l = new Label(); l.Text = Theme.Names[k]; l.SetBounds(x + 66, y + 4, 222, 20); page.Controls.Add(l);
                p.Click += delegate
                {
                    Color[] before = Theme.Snapshot(); Color c = Ask(Theme.Get(k), Theme.Names[k]); if (c.ToArgb() == before[k].ToArgb()) return;
                    Theme.Set(k, c); Theme.Save(); p.BackColor = c; if (ThemeChanged != null) ThemeChanged(Theme.Map(before, Theme.Snapshot()));
                };
            }
            Button rs = Mk("Rétablir les couleurs par défaut", 6, 392, 230); rs.Click += delegate
            {
                Color[] before = Theme.Snapshot(); for (int i = 0; i < Theme.Names.Length; i++) Theme.Set(i, Theme.Default(i)); Theme.Save();
                if (ThemeChanged != null) ThemeChanged(Theme.Map(before, Theme.Snapshot())); Colors();
            };
            Button cl = Mk("Fermer", 500, 380, 90); cl.Click += delegate { Close(); };
        }
        void Meters()
        {
            page.Controls.Clear(); Head("Vumètres", "Clic droit sur un vumètre : bascule entre barres (bargraphe) et aiguille, mémorisé pour ce vumètre. Ces boutons changent tous les vumètres du poste d'un coup.");
            Button a = Mk("Tous en aiguille", 6, 110, 200); a.Click += delegate { if (AllMeters != null) AllMeters(true); };
            Button b = Mk("Tous en barres", 216, 110, 200); b.Click += delegate { if (AllMeters != null) AllMeters(false); };
            Button cl = Mk("Fermer", 500, 380, 90); cl.Click += delegate { Close(); };
        }
        void Wheel()
        {
            page.Controls.Clear(); Head("Réglage molette souris", "Variation d'un potentiomètre pour un cran de molette, en % de sa course (1 à 10 %, 2 % par défaut). Maj + molette = pas plus fin.");
            Label lv = new Label(); lv.Font = new Font("Segoe UI Semibold", 11f); lv.ForeColor = T.Cyan; lv.SetBounds(500, 104, 90, 24); page.Controls.Add(lv);
            TrackBar tb = new TrackBar(); tb.Minimum = 1; tb.Maximum = 10; tb.TickFrequency = 1; tb.LargeChange = 1; tb.SetBounds(6, 96, 480, 45); tb.Value = Math.Max(1, Math.Min(10, HW.WheelPct)); page.Controls.Add(tb);
            lv.Text = tb.Value + " % / cran";
            tb.ValueChanged += delegate { HW.WheelPct = tb.Value; Cfg.SetInt("wheelpct", tb.Value); Cfg.Save(); lv.Text = tb.Value + " % / cran"; };
            Button df = Mk("Valeur par défaut (2 %)", 6, 160, 200); df.Click += delegate { tb.Value = 2; };
            Button cl = Mk("Fermer", 500, 380, 90); cl.Click += delegate { Close(); };
        }
        void Locks()
        {
            page.Controls.Clear(); Head("Verrous", "Clic droit sur un bouton ou un potentiomètre : il se verrouille (cadre rouge) et ne répond plus ; nouveau clic droit pour le libérer. Pour un choix à liste : Ctrl + clic droit. Mémorisé par poste.");
            Label n = new Label(); n.Font = new Font("Segoe UI Semibold", 11f); n.ForeColor = T.Cyan; n.SetBounds(6, 110, 400, 24); n.Text = lockCount() + " élément(s) verrouillé(s) sur ce poste"; page.Controls.Add(n);
            Button u = Mk("Tout déverrouiller", 6, 146, 200); u.Click += delegate { if (UnlockAll != null) UnlockAll(); n.Text = lockCount() + " élément(s) verrouillé(s) sur ce poste"; };
            // niveau du « flou » (voile) posé sur un organe verrouillé par clic droit
            Label lv0 = new Label(); lv0.Text = "Niveau du voile sur un organe verrouillé (sa couleur : Couleurs de l'interface)"; lv0.SetBounds(6, 196, 580, 22); page.Controls.Add(lv0);
            Label lv = new Label(); lv.Font = new Font("Segoe UI Semibold", 11f); lv.ForeColor = T.Cyan; lv.SetBounds(500, 224, 90, 24); page.Controls.Add(lv);
            TrackBar vt = new TrackBar(); vt.Minimum = 10; vt.Maximum = 100; vt.TickFrequency = 10; vt.SmallChange = 5; vt.LargeChange = 10; vt.SetBounds(6, 218, 480, 45); vt.Value = Math.Max(10, Math.Min(100, HW.VeilPct)); page.Controls.Add(vt); lv.Text = vt.Value + " %";
            vt.ValueChanged += delegate { HW.VeilPct = vt.Value; Cfg.SetInt("veil.pct", vt.Value); lv.Text = vt.Value + " %"; if (VeilChanged != null) VeilChanged(); };
            Button vr = Mk("Valeur par défaut (67 %)", 6, 268, 220); vr.Click += delegate { vt.Value = 67; };
            Button cl = Mk("Fermer", 500, 380, 90); cl.Click += delegate { Close(); };
        }
        // réinitialisation générale des verrous (tous les postes), après confirmation
        void ResetLocks()
        {
            page.Controls.Clear(); Head("Réinitialiser les verrous", "Supprime TOUS les verrous posés par clic droit (organes entiers et touches d'une rangée de modes ou de bandes), sur TOUS les postes, après confirmation. La touche Lock du haut de la fenêtre n'est pas concernée.");
            Label n = new Label(); n.Font = new Font("Segoe UI Semibold", 11f); n.ForeColor = T.Cyan; n.SetBounds(6, 110, 560, 24); n.Text = Cfg.CountPrefix("lock.") + " verrou(s) enregistré(s), tous postes confondus"; page.Controls.Add(n);
            Button b = Mk("Réinitialiser tous les verrous…", 6, 146, 280);
            b.Click += delegate
            {
                int c = Cfg.CountPrefix("lock."); if (c == 0) { MessageBox.Show(this, "Aucun verrou n'est posé.", "Réinitialiser les verrous"); return; }
                if (MessageBox.Show(this, "Supprimer les " + c + " verrou(s) de tous les postes ?\n\nToutes les touches verrouillées répondront de nouveau. Cette action est immédiate.", "Réinitialiser les verrous", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
                Cfg.RemovePrefix("lock."); Cfg.Save(); CatLog.Event("programme", c + " verrou(s) réinitialisé(s)"); if (ResetAllLocks != null) ResetAllLocks(); n.Text = "0 verrou enregistré, tous postes confondus";
            };
            Button cl = Mk("Fermer", 500, 380, 90); cl.Click += delegate { Close(); };
        }
        // journal des commandes : un fichier par jour, les plus vieux sont supprimés
        void LogPage()
        {
            page.Controls.Clear(); Head("Journal des commandes", "Enregistre les commandes envoyées, les réponses du poste, les erreurs et les événements (connexion, partage, paires com0com, serveur) dans UN fichier par jour (dossier « logs » à côté du programme). Les fichiers plus vieux que la durée choisie sont supprimés.");
            CheckBox on = new CheckBox(); on.Text = "Écrire le journal"; on.SetBounds(6, 100, 400, 24); on.ForeColor = T.Text; on.Checked = Cfg.GetBool("log.on", true); page.Controls.Add(on);
            CheckBox det = new CheckBox(); det.Text = "Journal détaillé (toutes les relectures périodiques : fichier beaucoup plus gros)"; det.SetBounds(6, 130, 580, 24); det.ForeColor = T.Text; det.Checked = Cfg.GetBool("log.detail", false); page.Controls.Add(det);
            Label ld = new Label(); ld.Text = "Conserver (jours)"; ld.SetBounds(6, 166, 130, 24); page.Controls.Add(ld);
            NumericUpDown nd = new NumericUpDown(); nd.Minimum = 1; nd.Maximum = 60; nd.Value = Math.Max(1, Math.Min(60, Cfg.GetInt("log.days", 7))); nd.SetBounds(140, 163, 70, 24); nd.BackColor = T.Panel2; nd.ForeColor = T.Text; page.Controls.Add(nd);
            Label info = new Label(); info.SetBounds(6, 204, 584, 60); info.ForeColor = T.Cyan; page.Controls.Add(info);
            Action upd = delegate { string f = CatLog.TodayFile; info.Text = "Fichier du jour : " + f + (System.IO.File.Exists(f) ? "  (" + (new System.IO.FileInfo(f).Length / 1024) + " Ko)" : "  (pas encore créé)"); };
            Action save = delegate { Cfg.SetBool("log.on", on.Checked); Cfg.SetBool("log.detail", det.Checked); Cfg.SetInt("log.days", (int)nd.Value); Cfg.Save(); CatLog.Reload(); upd(); };
            on.CheckedChanged += delegate { save(); }; det.CheckedChanged += delegate { save(); }; nd.ValueChanged += delegate { save(); }; upd();
            Button op = Mk("Ouvrir le dossier du journal", 6, 270, 260); op.Click += delegate { try { System.IO.Directory.CreateDirectory(CatLog.Dir); System.Diagnostics.Process.Start(CatLog.Dir); } catch { } };
            Button cl = Mk("Fermer", 500, 380, 90); cl.Click += delegate { Close(); };
        }
        // ports de réception des QSO du journal (WSJT-X, N1MM, ADIF) : mêmes ports par défaut que le logiciel remplacé
        void PortsPage()
        {
            page.Controls.Clear(); Head("Réglages des ports", "Ports UDP où HamRadioLink reçoit les QSO enregistrés par WSJT-X (0 = inutilisé). Un même port ne peut être écouté que par un seul programme à la fois.");
            Journal.PortDef[] ps = Journal.WsjtListener.Ports; TextBox[] tb = new TextBox[ps.Length]; int y = 98;
            for (int i = 0; i < ps.Length; i++)
            {
                Label l = new Label(); l.Text = ps[i].Label; l.SetBounds(6, y + 4, 250, 34); page.Controls.Add(l);
                tb[i] = new TextBox(); tb[i].SetBounds(260, y, 64, 24); tb[i].Text = ps[i].Value.ToString(); tb[i].BackColor = T.Panel2; tb[i].ForeColor = T.Text; page.Controls.Add(tb[i]);
                Label e = new Label(); e.Text = "Défaut " + ps[i].Default + " — " + ps[i].Example; e.ForeColor = T.Dim; e.SetBounds(332, y + 4, 262, 34); page.Controls.Add(e); y += 40;
            }
            Label o = new Label(); o.Text = "Autres ports : serveur de commande IP 7809 (bouton Serveur) ; MariaDB installé 3306 (bouton Base… du journal) ; MariaDB intégré 3307."; o.ForeColor = T.Cyan; o.SetBounds(6, y + 4, 584, 36); page.Controls.Add(o);
            Label st = new Label(); st.SetBounds(6, y + 44, 584, 36); st.ForeColor = T.Cyan; page.Controls.Add(st);
            Action show = delegate { st.Text = Journal.WsjtListener.Open.Count == 0 ? "Aucun port ouvert. " + Journal.WsjtListener.Error.Replace("\r\n", " ") : "Ports ouverts : " + string.Join(", ", Journal.WsjtListener.Open.ConvertAll(x => x.ToString()).ToArray()) + (Journal.WsjtListener.Error.Length > 0 ? " — occupés : " + Journal.WsjtListener.Error.Replace("\r\n", " ") : ""); }; show();
            Button ap = Mk("Appliquer", 6, 380, 110); ap.Click += delegate
            {
                for (int i = 0; i < tb.Length; i++) { int v; if (!int.TryParse(tb[i].Text.Trim(), out v) || v < 0 || v > 65535) { MessageBox.Show(this, "Port invalide : " + ps[i].Label, "Ports"); return; } }
                for (int i = 0; i < tb.Length; i++) ps[i].Value = int.Parse(tb[i].Text.Trim());
                Cfg.Save(); Journal.WsjtListener.Restart(); show();
            };
            Button df = Mk("Valeurs par défaut", 126, 380, 150); df.Click += delegate { for (int i = 0; i < tb.Length; i++) tb[i].Text = ps[i].Default.ToString(); };
            Button cl = Mk("Fermer", 500, 380, 90); cl.Click += delegate { Close(); };
        }
        void Manual()
        {
            page.Controls.Clear(); Head("Commande manuelle", "Envoie une commande brute au poste (CAT texte, ex. FA;  — pour un poste CI-V : octets en hexadécimal, ex. 03). Les réponses apparaissent dans le moniteur. À manier avec précaution : la commande agit sur le vrai poste.");
            TextBox t = new TextBox(); t.SetBounds(6, 100, 400, 24); t.BackColor = T.Panel2; t.ForeColor = T.Text; t.BorderStyle = BorderStyle.FixedSingle; page.Controls.Add(t);
            Button s = Mk("Envoyer", 416, 96, 110); s.Click += delegate { if (rig.Link == null) { MessageBox.Show(this, "Le poste n'est pas connecté.", "Commande"); return; } if (t.Text.Trim().Length > 0) rig.Raw(t.Text.Trim()); };
            Button m = Mk("Ouvrir le moniteur CAT", 6, 140, 220); m.Click += delegate { if (OpenMonitor != null) OpenMonitor(); };
            Button cl = Mk("Fermer", 500, 380, 90); cl.Click += delegate { Close(); };
        }
    }

    // Export / import : réglages du logiciel, dispositions de tous les postes et réglages du poste courant (relevés en direct)
    public static class Backup
    {
        static readonly string[] noRadio = { "ptt", "tune", "tuner", "poff", "pon", "power_on", "pwr_off" };
        public static string Dump(RadioDef cur, Rig rig)
        {
            StringBuilder sb = new StringBuilder(); sb.AppendLine("# HamRadioLink " + MainForm.Version + " - sauvegarde " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            sb.AppendLine("[[settings.ini]]"); sb.Append(Cfg.Dump());
            try
            {
                foreach (string f in Directory.GetFiles(Cfg.Dir, "*.ini"))
                {
                    string n = Path.GetFileName(f); if (n.Equals("settings.ini", StringComparison.OrdinalIgnoreCase)) continue;
                    sb.AppendLine("[[" + n + "]]"); foreach (string l in File.ReadAllLines(f, Encoding.UTF8)) if (l.Trim().Length > 0) sb.AppendLine(l);
                }
            }
            catch { }
            if (rig.Link != null && cur != null)
            {
                sb.AppendLine("[[radio:" + cur.Key + "]]");
                foreach (Ctl c in cur.C)
                {
                    if (!c.HasCat || c.Set.Length == 0 || Array.IndexOf(noRadio, c.Id) >= 0) continue; string v = rig.Get(c.Id); if (v == null) continue;
                    if (c.K == Kind.Toggle) sb.AppendLine(c.Id + "=" + (rig.IsOn(c) ? "1" : "0"));
                    else if (c.K == Kind.Knob) sb.AppendLine(c.Id + "=" + v);
                    else if (c.K == Kind.Choice) { foreach (Item it in c.ItemList) if (it.Match(v)) { sb.AppendLine(c.Id + "=" + it.Code); break; } }
                }
            }
            return sb.ToString();
        }
        // écrit les fichiers, retourne le nombre de blocs ; les réglages du poste sont gardés en attente (appliqués à la prochaine connexion)
        public static int Import(string text)
        {
            int n = 0; string name = null; StringBuilder body = new StringBuilder();
            Action flush = delegate
            {
                if (name == null) return;
                if (name.StartsWith("radio:")) { Cfg.Set("pending." + name.Substring(6), Enc(body.ToString())); n++; }
                else if (name == "settings.ini") { if (Cfg.Import(body.ToString()) > 0) n++; }
                else if (name.EndsWith(".ini") && name == Path.GetFileName(name)) { try { File.WriteAllText(Path.Combine(Cfg.Dir, name), body.ToString().Replace("\n", "\r\n"), Encoding.UTF8); n++; } catch { } }
                body.Length = 0;
            };
            foreach (string raw in text.Replace("\r\n", "\n").Split('\n'))
            {
                string l = raw.TrimEnd();
                if (l.StartsWith("[[") && l.EndsWith("]]")) { flush(); name = l.Substring(2, l.Length - 4); }
                else if (name != null && l.Length > 0 && l[0] != '#') body.AppendLine(l);
            }
            flush(); Cfg.Save(); return n;
        }
        static string Enc(string s) { return Uri.EscapeDataString(s); }
        // applique les réglages en attente du poste (après connexion) ; retourne le nombre envoyé
        public static int ApplyPending(RadioDef cur, Rig rig)
        {
            string k = "pending." + cur.Key, e = Cfg.Get(k, ""); if (e.Length == 0) return 0; Cfg.Set(k, ""); Cfg.Save(); int n = 0;
            foreach (string l in Uri.UnescapeDataString(e).Replace("\r\n", "\n").Split('\n'))
            {
                int i = l.IndexOf('='); if (i <= 0) continue; Ctl c = cur.Find(l.Substring(0, i)); if (c == null || !c.HasCat || c.Set.Length == 0 || Array.IndexOf(noRadio, c.Id) >= 0) continue; string v = l.Substring(i + 1);
                if (c.K == Kind.Toggle) rig.Set(c, v == "1" ? c.OnSend : c.OffSend); else if (c.K == Kind.Knob || c.K == Kind.Choice) rig.Set(c, v); else continue; n++;
            }
            return n;
        }
    }

    public class ExportDlg : Form
    {
        public bool Imported; readonly string dump;
        public ExportDlg(string dump, bool connected)
        {
            this.dump = dump; Text = "Export / import"; BackColor = T.Bg; ForeColor = T.Text; ClientSize = new Size(400, 150); StartPosition = FormStartPosition.CenterParent; Font = new Font("Segoe UI", 9f);
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
            Label l = new Label(); l.SetBounds(12, 10, 376, 54); Controls.Add(l);
            l.Text = "Exporter : enregistre les réglages du logiciel, la disposition de tous les postes" + (connected ? " et les réglages du poste connecté" : " (poste non connecté : réglages du poste non inclus)") + ".\nImporter : restaure un fichier (le logiciel redémarre).";
            Button b1 = B("Exporter…", 12, 80, 120); b1.Click += delegate { Export(); }; Button b2 = B("Importer…", 142, 80, 120); b2.Click += delegate { Open(); }; Button b3 = B("Annuler", 272, 80, 116); b3.Click += delegate { Close(); }; CancelButton = b3;
        }
        Button B(string t, int x, int y, int w) { Button b = new Button(); b.Text = t; b.SetBounds(x, y, w, 34); b.FlatStyle = FlatStyle.Flat; b.BackColor = T.Panel2; b.ForeColor = T.Text; b.FlatAppearance.BorderColor = T.Edge; Controls.Add(b); return b; }
        void Export()
        {
            using (SaveFileDialog d = new SaveFileDialog())
            {
                d.Filter = "Sauvegarde HamRadioLink (*.ini)|*.ini|Tous les fichiers|*.*"; d.FileName = "HamRadioLink-config-" + DateTime.Now.ToString("yyyy-MM-dd") + ".ini";
                if (d.ShowDialog(this) != DialogResult.OK) return;
                try { File.WriteAllText(d.FileName, dump.Replace("\r\n", "\n").Replace("\n", "\r\n"), Encoding.UTF8); MessageBox.Show(this, "Configuration exportée :\n" + d.FileName, "Export"); Close(); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Erreur"); }
            }
        }
        void Open()
        {
            using (OpenFileDialog d = new OpenFileDialog())
            {
                d.Filter = "Sauvegarde HamRadioLink (*.ini)|*.ini|Tous les fichiers|*.*"; if (d.ShowDialog(this) != DialogResult.OK) return;
                string txt; try { txt = File.ReadAllText(d.FileName, Encoding.UTF8); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Erreur"); return; }
                if (MessageBox.Show(this, "Remplacer la configuration par ce fichier et redémarrer le logiciel ?\nLes réglages du poste (s'il y en a) seront renvoyés dès la prochaine connexion.", "Importer", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;
                if (Backup.Import(txt) == 0) { MessageBox.Show(this, "Aucun réglage valide dans ce fichier.", "Importer"); return; }
                Imported = true; DialogResult = DialogResult.OK; Close();
            }
        }
    }
}
