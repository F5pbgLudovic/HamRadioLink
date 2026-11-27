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
        public static readonly string[] Names = { "Arrière-plan général", "Panneaux", "Boutons et champs", "Bordures", "Texte (avant-plan)", "Texte atténué", "Accent (titres, voyants)", "Cyan (infos)", "Rouge (émission, alertes)", "Vert (connexion, OK)", "Bleu", "Verrouillage (cadre rouge)", "(réservé)", "Touche Lock verrouillée", "Voile d'un organe verrouillé", "Touche active (fond)", "Bouton de connexion : connecté (OK)", "Bouton de connexion : perdue / absente", "Valeurs du S-mètre à aiguille (jaune)" };
        static readonly Color[] def; static Theme() { def = Snapshot(); }
        public static Color Get(int i)
        {
            switch (i) { case 0: return T.Bg; case 1: return T.Panel; case 2: return T.Panel2; case 3: return T.Edge; case 4: return T.Text; case 5: return T.Dim; case 6: return T.Amber; case 7: return T.Cyan; case 8: return T.Red; case 9: return T.Green; case 10: return T.Blue; case 11: return T.Lock; case 12: return T.PttBg; case 13: return T.LockBtn; case 14: return T.Veil; case 15: return T.OnBg; case 16: return T.ConnOk; case 17: return T.ConnKo; default: return T.SVal; }
        }
        public static void Set(int i, Color c)
        {
            c = Color.FromArgb(255, c);
            switch (i) { case 0: T.Bg = c; break; case 1: T.Panel = c; break; case 2: T.Panel2 = c; break; case 3: T.Edge = c; break; case 4: T.Text = c; break; case 5: T.Dim = c; break; case 6: T.Amber = c; break; case 7: T.Cyan = c; break; case 8: T.Red = c; break; case 9: T.Green = c; break; case 10: T.Blue = c; break; case 11: T.Lock = c; break; case 12: T.PttBg = c; break; case 13: T.LockBtn = c; break; case 14: T.Veil = c; break; case 15: T.OnBg = c; break; case 16: T.ConnOk = c; break; case 17: T.ConnKo = c; break; default: T.SVal = c; break; }
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
        public event Action<Dictionary<int, Color>> ThemeChanged; public event Action<bool> AllMeters; public event Action UnlockAll; public event Action OpenMonitor; public event Action ResetAllLocks, VeilChanged, RunUpdate;
        readonly Panel page = new Panel(); readonly Rig rig; readonly Func<int> lockCount;
        public OptionsDlg(Rig rig, Func<int> lockCount)
        {
            this.rig = rig; this.lockCount = lockCount;
            Text = "Paramètres"; BackColor = T.Bg; ForeColor = T.Text; Font = new Font("Segoe UI", 9f); ClientSize = new Size(880, 580); StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
            ListBox menu = new ListBox(); menu.SetBounds(0, 0, 250, 580); menu.BackColor = Color.FromArgb(14, 18, 26); menu.ForeColor = T.Text; menu.BorderStyle = BorderStyle.None; menu.Font = new Font("Segoe UI Semibold", 10f); menu.ItemHeight = 28; menu.IntegralHeight = false; Controls.Add(menu);
            foreach (string s in new string[] { "  Couleurs de l'interface", "  Vumètres", "  Réglage molette souris", "  Verrous", "  Commande manuelle", "  Réinitialiser les verrous", "  Journal des commandes", "  Réglages des ports", "  Micro vers le poste", "  Bouton TUNE", "  Alerte ROS", "  Mise à jour", "  Réglage S-mètre puissance", "  Réglage autres S-mètres", "  Réglage Width / CONT / RF PWR" }) menu.Items.Add(s);
            menu.SelectedIndexChanged += delegate { switch (menu.SelectedIndex) { case 7: PortsPage(); break; case 8: MicPage(); break; case 9: TunePage(); break; case 10: RosPage(); break; case 11: UpdatePage(); break; case 12: ScalePage(); break; case 13: OtherScalePage(); break; case 14: KnobScalePage(); break; case 1: Meters(); break; case 2: Wheel(); break; case 3: Locks(); break; case 4: Manual(); break; case 5: ResetLocks(); break; case 6: LogPage(); break; default: Colors(); break; } };
            FormClosed += delegate { Cfg.Save(); };
            page.SetBounds(260, 0, 620, 580); Controls.Add(page); menu.SelectedIndex = 0;
        }
        Label Head(string t, string sub) { Label h = new Label(); h.Text = t; h.Font = new Font("Segoe UI Semibold", 12f); h.ForeColor = T.Amber; h.SetBounds(6, 8, 400, 26); page.Controls.Add(h); Label d = new Label(); d.Text = sub; d.SetBounds(6, 38, 586, 52); page.Controls.Add(d); return d; }
        Button Mk(string t, int x, int y, int w) { Button b = new Button(); b.Text = t; b.SetBounds(x, y, w, 30); b.FlatStyle = FlatStyle.Flat; b.BackColor = T.Panel2; b.ForeColor = T.Text; b.FlatAppearance.BorderColor = T.Edge; page.Controls.Add(b); return b; }
        Color Ask(Color c, string title) { using (IC7760.ColorPickForm f = new IC7760.ColorPickForm(c, title)) return f.ShowDialog(this) == DialogResult.OK ? f.Result : c; }
        void MicPage()
        {
            page.Controls.Clear(); Head("Micro vers le poste", "Envoie en continu le micro du PC sur la carte son reliée au poste (aussi par clic droit sur la touche PTT). Réglage commun à tous les postes.");
            Button b = Mk("Choisir le micro, la carte son, activer…", 6, 100, 300); b.Click += delegate { ContextMenuStrip m = new ContextMenuStrip(); m.BackColor = T.Panel2; m.ForeColor = T.Text; MicRoute.AddItems(m); m.Items.RemoveAt(0); m.Show(b, new Point(0, b.Height)); };
        }
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
            Label lh = new Label(); lh.Text = "Maintien de la valeur maximale (S-mètre et autres vumètres) : le repère et la valeur lue restent affichés ce temps-là avant de redescendre."; lh.ForeColor = T.Text; lh.SetBounds(6, 170, 584, 36); page.Controls.Add(lh);
            ComboBox ch = new ComboBox(); ch.DropDownStyle = ComboBoxStyle.DropDownList; ch.FlatStyle = FlatStyle.Flat; ch.BackColor = T.Panel2; ch.ForeColor = T.Text; ch.SetBounds(6, 212, 160, 26); page.Controls.Add(ch);
            List<int> hv = new List<int>(); hv.Add(500); for (int s = 1; s <= 20; s++) hv.Add(s * 1000);
            foreach (int ms in hv) ch.Items.Add(ms == 500 ? "0,5 s" : (ms / 1000) + " s");
            int cur = HMeter.HoldMs, ix = hv.IndexOf(cur); if (ix < 0) { ix = 0; for (int i = 0; i < hv.Count; i++) if (Math.Abs(hv[i] - cur) < Math.Abs(hv[ix] - cur)) ix = i; }
            ch.SelectedIndex = ix; ch.SelectedIndexChanged += delegate { Cfg.SetInt("meter.hold", hv[ch.SelectedIndex]); Cfg.Save(); };
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
        void TunePage()
        {
            page.Controls.Clear(); Head("Bouton TUNE", "Émet une tonalité de 1500 Hz sur la carte son d'émission du poste (celle de « Micro vers le poste »), avec le PTT par CAT ou le VOX du poste. Réglages communs à tous les postes.");
            Label l1 = new Label(); l1.Text = "Niveau de la tonalité (%)"; l1.SetBounds(6, 104, 220, 24); page.Controls.Add(l1);
            NumericUpDown n1 = new NumericUpDown(); n1.Minimum = 1; n1.Maximum = 100; n1.Value = TuneMic.Level; n1.SetBounds(230, 101, 70, 24); n1.BackColor = T.Panel2; n1.ForeColor = T.Text; page.Controls.Add(n1);
            n1.ValueChanged += delegate { Cfg.SetInt("tune.niveau", (int)n1.Value); Cfg.Save(); };
            Label l2 = new Label(); l2.Text = "Durée de l'émission (5 à 60 s)"; l2.SetBounds(6, 138, 220, 24); page.Controls.Add(l2);
            NumericUpDown n2 = new NumericUpDown(); n2.Minimum = 5; n2.Maximum = 60; n2.Value = TuneMic.Seconds; n2.SetBounds(230, 135, 70, 24); n2.BackColor = T.Panel2; n2.ForeColor = T.Text; page.Controls.Add(n2);
            n2.ValueChanged += delegate { Cfg.SetInt("tune.duree", (int)n2.Value); Cfg.Save(); };
            CheckBox vx = new CheckBox(); vx.Text = "VOX du poste (tonalité seule, sans PTT par CAT)"; vx.SetBounds(6, 172, 560, 24); vx.ForeColor = T.Text; vx.Checked = TuneMic.Vox; page.Controls.Add(vx);
            vx.CheckedChanged += delegate { Cfg.SetBool("tune.vox", vx.Checked); Cfg.Save(); };
            CheckBox pw = new CheckBox(); pw.Text = "Changer la puissance pendant le TUNE, puis la remettre :"; pw.SetBounds(6, 204, 470, 24); pw.ForeColor = T.Text; pw.Checked = Cfg.GetBool("tune.pwron", false); page.Controls.Add(pw);
            NumericUpDown pn = Num(1, 100, 1, 0, Cfg.GetInt("tune.pwr", 10), 482, 202, 60); Lbl("W", 548, 204, 30);
            pw.CheckedChanged += delegate { Cfg.SetBool("tune.pwron", pw.Checked); Cfg.Save(); }; pn.ValueChanged += delegate { Cfg.SetInt("tune.pwr", (int)pn.Value); Cfg.Save(); };
            Button c1 = Mk("Couleur au repos…", 6, 250, 200), c2 = Mk("Couleur pendant le TUNE…", 216, 250, 220), c3 = Mk("Couleurs par défaut (vert / rouge)", 6, 290, 300);
            c1.BackColor = TuneMic.ColOff; c2.BackColor = TuneMic.ColOn;
            c1.Click += delegate { Color c = Ask(TuneMic.ColOff, "TUNE : couleur au repos"); Cfg.SetInt("tune.coff", Color.FromArgb(255, c).ToArgb()); Cfg.Save(); c1.BackColor = c; TuneMic.Refresh(); };
            c2.Click += delegate { Color c = Ask(TuneMic.ColOn, "TUNE : couleur pendant le TUNE"); Cfg.SetInt("tune.con", Color.FromArgb(255, c).ToArgb()); Cfg.Save(); c2.BackColor = c; TuneMic.Refresh(); };
            c3.Click += delegate { Cfg.SetInt("tune.coff", 0); Cfg.SetInt("tune.con", 0); Cfg.Save(); c1.BackColor = TuneMic.ColOff; c2.BackColor = TuneMic.ColOn; TuneMic.Refresh(); };
        }
        NumericUpDown Num(decimal min, decimal max, decimal step, int dec, decimal val, int x, int y, int w)
        {
            NumericUpDown n = new NumericUpDown(); n.Minimum = min; n.Maximum = max; n.Increment = step; n.DecimalPlaces = dec; n.Value = Math.Max(min, Math.Min(max, val)); n.SetBounds(x, y, w, 24); n.BackColor = T.Panel2; n.ForeColor = T.Text; page.Controls.Add(n); return n;
        }
        Label Lbl(string t, int x, int y, int w) { Label l = new Label(); l.Text = t; l.SetBounds(x, y, w, 24); l.ForeColor = T.Text; page.Controls.Add(l); return l; }
        // ---- Réglage S-mètre puissance : puissance max de l'émetteur et étalonnage du wattmètre
        void ScalePage()
        {
            page.Controls.Clear(); Head("Réglage S-mètre puissance", "Indiquez la puissance maximale de l'émetteur : les vumètres Po, Vdd, Id, SWR, ALC et COMP sont gradués en conséquence. Le wattmètre Po peut ensuite être étalonné palier par palier (10 W, 20 W…).");
            if (rig == null || rig.Def == null || !MeterCal.Active(rig.Def)) { Lbl("Ce poste n'a pas de vumètre d'émission (Po, SWR, ALC, COMP, Id, Vdd) à graduer.", 6, 100, 580); return; }
            string key = rig.Def.Key; Lbl("Puissance maximale de l'émetteur (W)", 6, 100, 280); NumericUpDown pm = Num(5m, 2000m, 5m, 0, MeterCal.PMax(key), 290, 98, 80);
            Lbl("Pleine déviation de l'échelle Po (W)", 6, 132, 280); NumericUpDown pfn = Num(5m, 2000m, 5m, 0, MeterCal.PFull(key), 290, 130, 80); Lbl("(sans nouvel étalonnage)", 380, 132, 200);
            Label vd = Lbl("", 6, 164, 580); Label st = Lbl("", 6, 262, 584); st.Height = 120; Button cal = Mk("Étalonner le wattmètre…", 6, 226, 240), clr = Mk("Effacer l'étalonnage", 256, 226, 200);
            Action show = delegate
            {
                int p = MeterCal.PMax(key); vd.Text = "Vdd : pleine échelle " + MeterCal.VddFull(p).ToString("0") + " V, valeur nominale " + MeterCal.VddNominal(p).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " V (200 W = 50 V, sinon 13,8 V). Id : pleine déviation 15 A. COMP : 0 / 10 / 30 dB. SWR : 1 à ∞ (3 au milieu). ALC : bleu puis rouge à partir du milieu.";
                List<double[]> pts = MeterCal.Points(key); StringBuilder sb = new StringBuilder();
                if (pts.Count == 0) sb.Append("Wattmètre non étalonné : l'échelle Po est linéaire de 0 à " + p + " W (approximation).");
                else { sb.Append("Wattmètre étalonné (" + pts.Count + " points) :\n"); foreach (double[] x in pts) sb.Append("lecture " + x[0].ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " = " + x[1].ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " W    "); }
                st.Text = sb.ToString(); clr.Enabled = pts.Count > 0;
            };
            vd.Height = 56; pm.ValueChanged += delegate { MeterCal.SetPMax(key, (int)pm.Value); pfn.Value = Math.Max(pfn.Minimum, Math.Min(pfn.Maximum, MeterCal.PFull(key))); show(); };
            pfn.ValueChanged += delegate { MeterCal.SetPFull(key, (int)pfn.Value); };
            cal.Click += delegate
            {
                if (rig.Link == null) { MessageBox.Show(this, "Connectez d'abord le poste.", "Étalonnage"); return; }
                MeterCal.SetPMax(key, (int)pm.Value);
                if (MessageBox.Show(this, "L'étalonnage va demander " + string.Join(", ", MeterCal.Steps((int)pm.Value).ConvertAll<string>(delegate(int w) { return w + " W"; }).ToArray()) + ".\nVous réglez la puissance du poste et vous émettez une porteuse à chaque palier ; l'antenne doit être adaptée (ou une charge fictive). Continuer ?", "Étalonnage du wattmètre", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;
                { CalDlg d = new CalDlg(rig); d.FormClosed += delegate { show(); }; d.Show(this); }
            };
            clr.Click += delegate { MeterCal.SavePoints(key, new List<double[]>()); show(); };
            show(); if (!MeterCal.Has(rig.Def, "po")) { cal.Enabled = false; pfn.Enabled = false; clr.Enabled = false; }
        }
        // ---- Réglage autres S-mètres : SWR, ALC, COMP, Id, Vdd (valeur lue sur le poste <-> lecture brute, puis extrapolation)
        void OtherScalePage()
        {
            page.Controls.Clear(); Head("Réglage autres S-mètres", "Pour chaque vumètre, vous émettez, vous lisez la valeur réelle sur le poste et vous la saisissez : le programme relie sa lecture à cette valeur puis extrapole toute l'échelle. Un point suffit, plusieurs sont plus justes.");
            if (rig == null || rig.Def == null || !MeterCal.Active(rig.Def)) { Lbl("Ce poste n'a pas de vumètre d'émission (Po, SWR, ALC, COMP, Id, Vdd) à graduer.", 6, 100, 580); return; }
            string key = rig.Def.Key; int y = 100;
            foreach (string id in MeterTestDlg.Ids)
            {
                if (!MeterCal.Has(rig.Def, id)) continue; string mid = id; Lbl(MeterTestDlg.Name(mid), 6, y + 3, 130); Label st = Lbl("", 140, y + 3, 190); Button b = Mk("Étalonner…", 336, y, 120), c = Mk("Effacer", 466, y, 100);
                Action show = delegate { int n = MeterCal.Points(key, mid).Count; st.Text = n == 0 ? "échelle par défaut" : "étalonné (" + n + " point" + (n > 1 ? "s" : "") + ")"; c.Enabled = n > 0; };
                b.Click += delegate
                {
                    if (rig.Link == null) { MessageBox.Show(this, "Connectez d'abord le poste.", "Étalonnage"); return; }
                    { MeterTestDlg d = new MeterTestDlg(rig, mid); d.FormClosed += delegate { show(); }; d.Show(this); }
                };
                c.Click += delegate { MeterCal.SavePoints(key, mid, new List<double[]>()); show(); }; show(); y += 40;
            }
            Label h = Lbl("Valeurs d'origine : SWR 1 à ∞ (3 au milieu), ALC 0 à 100 %, COMP 0 / 10 / 30 dB, Id jusqu'à 15 A, Vdd jusqu'à la pleine échelle réglée dans « Réglage S-mètre puissance ».", 6, y + 8, 584); h.Height = 50; h.ForeColor = T.Dim;
        }
        // ---- Réglage Width / RF PWR (FT-2000) : le bouton affiche des Hz / des watts au lieu de la valeur brute du poste
        void KnobScalePage()
        {
            page.Controls.Clear(); Head("Réglage Width / CONT / RF PWR", "Les boutons WIDTH et CONT affichent des fréquences en Hz et le bouton RF PWR la puissance en W, au lieu de la valeur interne du poste. Vous réglez le poste sur chaque valeur et le programme relève la position correspondante.");
            if (rig == null || rig.Def == null || !MeterCal.KnobAny(rig.Def)) { Lbl("Ce poste n'a pas de bouton Width, CONT ou RF PWR à graduer.", 6, 100, 580); return; }
            string key = rig.Def.Key; int y = 100;
            foreach (string kid in new string[] { "width", "contf", "rfpwr" })
            {
                if (!MeterCal.KnobActive(rig.Def, rig.Def.Find(kid == "rfpwr" ? "pwr" : kid))) continue;
                string id = kid; Lbl(KnobCalDlg.Title(id), 6, y + 3, 200); Label st = Lbl("", 210, y + 3, 150); Button b = Mk("Étalonner…", 366, y, 120), c = Mk("Effacer", 496, y, 90);
                Action show = delegate { int n = MeterCal.Points(key, id).Count; st.Text = n == 0 ? "valeur brute du poste" : "étalonné (" + n + " point" + (n > 1 ? "s" : "") + ")"; c.Enabled = n > 0; };
                b.Click += delegate
                {
                    if (rig.Link == null) { MessageBox.Show(this, "Connectez d'abord le poste.", "Étalonnage"); return; }
                    if (id == "rfpwr" && MessageBox.Show(this, "L'étalonnage de RF PWR demande " + string.Join(", ", MeterCal.Steps(MeterCal.PMax(key)).ConvertAll<string>(delegate(int w) { return w + " W"; }).ToArray()) + " (puissance max : " + MeterCal.PMax(key) + " W, réglable dans « Réglage S-mètre puissance »).\nVous réglez le bouton RF PWR du poste sur chaque valeur ; il n'y a pas d'émission. Continuer ?", "Étalonnage RF PWR", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;
                    { KnobCalDlg d = new KnobCalDlg(rig, id); d.FormClosed += delegate { show(); }; d.Show(this); }
                };
                c.Click += delegate { MeterCal.SavePoints(key, id, new List<double[]>()); show(); }; show(); y += 40;
            }
            Label h = Lbl("Width d'origine : position 1 = 200 Hz, 16 = 2400 Hz, 31 = 4000 Hz (droites entre ces points, jamais au-delà de 4000 Hz). CONT d'origine : position 1 = 100 Hz, 20 = 2000 Hz, 40 = 4000 Hz (valeurs supposées, à étalonner). RF PWR : tant qu'il n'est pas étalonné, la valeur brute du poste est affichée en W ; une fois étalonné, la pleine échelle du bouton est la puissance max.", 6, y + 8, 584); h.Height = 100; h.ForeColor = T.Dim;
        }
        void RosPage()
        {
            page.Controls.Clear(); Head("Alerte ROS", "En émission, si le ROS lu sur le poste dépasse le seuil, bips continus jusqu'à ce que vous les arrêtiez (bouton de la fenêtre d'alerte). Réglages communs à tous les postes ; sans lecture du ROS, rien ne se déclenche.");
            CheckBox on = new CheckBox(); on.Text = "Alerte ROS activée"; on.SetBounds(6, 96, 300, 24); on.ForeColor = T.Text; on.Checked = RosGuard.On; page.Controls.Add(on); on.CheckedChanged += delegate { Cfg.SetBool("ros.on", on.Checked); Cfg.Save(); };
            Lbl("Seuil de ROS", 6, 128, 200); NumericUpDown ns = Num(1.1m, 20m, 0.1m, 1, (decimal)RosGuard.Threshold, 230, 126, 70); ns.ValueChanged += delegate { Cfg.SetInt("ros.seuil", (int)Math.Round(ns.Value * 100)); Cfg.Save(); };
            Lbl("Délai avant déclenchement (s)", 6, 158, 220); NumericUpDown nd = Num(0m, 10m, 0.05m, 2, RosGuard.DelayMs / 1000m, 230, 156, 70); nd.ValueChanged += delegate { Cfg.SetInt("ros.delai", (int)Math.Round(nd.Value * 1000)); Cfg.Save(); };
            Lbl("0 = immédiat ; de 0,05 en 0,05 s jusqu'à 10 s", 310, 158, 280).ForeColor = T.Dim;
            Lbl("Fréquence des bips (Hz)", 6, 188, 220); NumericUpDown nf = Num(200m, 8000m, 50m, 0, RosGuard.Freq, 230, 186, 70); nf.ValueChanged += delegate { Cfg.SetInt("ros.freq", (int)nf.Value); Cfg.Save(); };
            Lbl("Volume des bips (%)", 6, 218, 220); NumericUpDown nv = Num(1m, 100m, 1m, 0, RosGuard.Volume, 230, 216, 70); nv.ValueChanged += delegate { Cfg.SetInt("ros.vol", (int)nv.Value); Cfg.Save(); };
            Lbl("Intervalle entre bips (s)", 6, 248, 220); NumericUpDown np = Num(0.05m, 2m, 0.05m, 2, RosGuard.IntervalMs / 1000m, 230, 246, 70); np.ValueChanged += delegate { Cfg.SetInt("ros.pause", (int)Math.Round(np.Value * 1000)); Cfg.Save(); };
            Lbl("Extinction seule après (s)", 330, 248, 170); NumericUpDown ne = Num(0.5m, 30m, 0.5m, 1, RosGuard.ClearMs / 1000m, 510, 246, 70); ne.ValueChanged += delegate { Cfg.SetInt("ros.eteint", (int)Math.Round(ne.Value * 1000)); Cfg.Save(); };
            CheckBox kp = new CheckBox(); kp.Text = "Laisser la puissance réduite de sécurité (sinon elle est rétablie quand l'alerte s'éteint)"; kp.SetBounds(6, 306, 584, 24); kp.ForeColor = T.Text; kp.Checked = RosGuard.KeepReduced; page.Controls.Add(kp); kp.CheckedChanged += delegate { Cfg.SetBool("ros.garder", kp.Checked); Cfg.Save(); };
            Button tb = Mk("Essai du bip", 320, 216, 130); tb.Click += delegate { RosGuard.TestBeep(); };
            CheckBox dr = new CheckBox(); dr.Text = "Faire chuter la puissance à (0 % = minimum du poste)"; dr.SetBounds(6, 280, 400, 24); dr.ForeColor = T.Text; dr.Checked = RosGuard.Drop; page.Controls.Add(dr); dr.CheckedChanged += delegate { Cfg.SetBool("ros.chute", dr.Checked); Cfg.Save(); };
            TrackBar npc = new TrackBar(); npc.Minimum = 0; npc.Maximum = 100; npc.TickFrequency = 5; npc.SmallChange = 1; npc.LargeChange = 5; npc.SetBounds(410, 272, 130, 40); npc.Value = Math.Max(0, Math.Min(100, RosGuard.DropPct)); page.Controls.Add(npc);
            Label npl = Lbl(npc.Value + " %", 544, 282, 50);
            npc.ValueChanged += delegate { Cfg.SetInt("ros.pct", npc.Value); Cfg.Save(); npl.Text = npc.Value + " %"; RosGuard.ReapplyDrop(); };
            CheckBox tv = new CheckBox(); tv.Text = "Test en VOX du poste (sinon PTT par CAT)"; tv.SetBounds(6, 332, 380, 24); tv.ForeColor = T.Text; tv.Checked = RosGuard.TestVox; page.Controls.Add(tv); tv.CheckedChanged += delegate { Cfg.SetBool("ros.vox", tv.Checked); Cfg.Save(); };
            Button tt = Mk("Test : TUNE 1500 Hz à la puissance de sécurité", 6, 360, 380); tt.Click += delegate { TuneMic.StartSafe(rig, tv.Checked); };
            // rouge pendant l'émission du test (puissance de sécurité), comme le bouton TUNE
            Action recolor = delegate { bool act = TuneMic.SafeActiveFor(rig); tt.BackColor = act ? TuneMic.ColOn : T.Panel2; tt.Text = act ? "● TEST EN COURS (" + TuneMic.Left + " s) — cliquer pour arrêter" : "Test : TUNE 1500 Hz à la puissance de sécurité"; };
            Action rc = delegate { if (!tt.IsDisposed) { try { tt.BeginInvoke(recolor); } catch { } } }; TuneMic.Changed += rc; tt.Disposed += delegate { TuneMic.Changed -= rc; }; recolor();
            Label hint = Lbl("Le test émet le TUNE à la puissance réduite (durée et niveau : onglet Bouton TUNE) puis remet la puissance d'origine. Second clic = arrêt.", 6, 394, 584); hint.Height = 34; hint.ForeColor = T.Dim;
            // ---- étalonnage du ROS (tous les postes qui lisent le ROS) : la lecture du poste est calée sur le ROS réel ; l'alerte et les vumètres s'en servent
            Label ch = Lbl("Étalonnage du ROS", 6, 436, 300); ch.Font = new Font("Segoe UI Semibold", 11f); ch.ForeColor = T.Amber;
            Label cdesc = Lbl("Émettez sur une charge de ROS connu (ou lisez le ROS sur un appareil de mesure) et saisissez la valeur réelle : le programme la fait coïncider avec la lecture du poste. Plusieurs mesures sont possibles, « Passer » saute une étape, le reste est extrapolé.", 6, 462, 584); cdesc.Height = 48; cdesc.ForeColor = T.Dim;
            Label cst = Lbl("", 6, 520, 250); Button cbtn = Mk("Étalonner le ROS…", 262, 514, 190), cclr = Mk("Effacer", 460, 514, 110);
            Ctl swc = rig != null && rig.Def != null ? rig.Def.Find("swr") : null; bool hasSwr = swc != null && swc.HasCat; string rk = rig != null && rig.Def != null ? rig.Def.Key : "";
            Action cshow = delegate { if (!hasSwr) { cst.Text = "ce poste ne donne pas le ROS"; cbtn.Enabled = cclr.Enabled = false; return; } int n = MeterCal.Points(rk, "swr").Count; cst.Text = n == 0 ? "échelle d'origine" : "étalonné (" + n + " point" + (n > 1 ? "s" : "") + ")"; cclr.Enabled = n > 0; };
            cbtn.Click += delegate { if (rig.Link == null) { MessageBox.Show(this, "Connectez d'abord le poste.", "Étalonnage du ROS"); return; } { SwrCalDlg d = new SwrCalDlg(rig); d.FormClosed += delegate { cshow(); }; d.Show(this); } };
            cclr.Click += delegate { MeterCal.SavePoints(rk, "swr", new List<double[]>()); cshow(); };
            cshow();
        }
        void UpdatePage()
        {
            page.Controls.Clear(); Head("Mise à jour", "Version installée : " + MainForm.Version + ". La dernière version publiée sur GitHub peut être installée en un clic ; seul le programme est remplacé (réglages, dispositions, commandes et journal restent en place).");
            CheckBox au = new CheckBox(); au.Text = "Mise à jour automatique : vérifier la dernière version au démarrage"; au.SetBounds(6, 110, 580, 24); au.ForeColor = T.Text; au.Checked = Updater.AutoOn; page.Controls.Add(au);
            au.CheckedChanged += delegate { Cfg.SetBool("maj.auto", au.Checked); Cfg.Save(); };
            Label ex = Lbl("À jour : rien n'est dit. Sinon une fenêtre propose la mise à jour (un clic), avec la liste de ce qui peut être conservé ou remis à zéro, et une copie de sécurité des réglages.", 6, 140, 584); ex.Height = 54; ex.ForeColor = T.Dim;
            Button go = Mk("Vérifier et mettre à jour maintenant", 6, 210, 320); go.Click += delegate { Cursor = Cursors.WaitCursor; try { if (RunUpdate != null) RunUpdate(); } finally { Cursor = Cursors.Default; } };
            Button ig = Mk("Proposer de nouveau la version ignorée", 6, 252, 320); ig.Enabled = Cfg.Get("maj.ignore", "").Length > 0; ig.Click += delegate { Cfg.Set("maj.ignore", ""); Cfg.Save(); ig.Enabled = false; };
        }
        void LogPage()
        {
            page.Controls.Clear(); Head("Journal des commandes", "Enregistre les commandes envoyées, les réponses du poste, les erreurs et les événements (connexion, partage, paires com0com, serveur) dans UN fichier par jour (dossier « logs » à côté du programme). Les fichiers plus vieux que la durée choisie sont supprimés.");
            CheckBox on = new CheckBox(); on.Text = "Écrire le journal"; on.SetBounds(6, 100, 400, 24); on.ForeColor = T.Text; on.Checked = Cfg.GetBool("log.on", true); page.Controls.Add(on);
            CheckBox det = new CheckBox(); det.Text = "Journal détaillé (toutes les relectures périodiques : fichier beaucoup plus gros)"; det.SetBounds(6, 130, 580, 24); det.ForeColor = T.Text; det.Checked = Cfg.GetBool("log.detail", false); page.Controls.Add(det);
            Label ld = new Label(); ld.Text = "Archives : 24 h au plus (réinitialisé à 25 Mo)"; ld.SetBounds(6, 166, 400, 24); page.Controls.Add(ld);
            NumericUpDown nd = new NumericUpDown(); nd.Minimum = 1; nd.Maximum = 60; nd.Value = Math.Max(1, Math.Min(60, Cfg.GetInt("log.days", 7))); nd.SetBounds(140, 163, 70, 24); nd.Visible = false; nd.BackColor = T.Panel2; nd.ForeColor = T.Text; page.Controls.Add(nd);
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

    // Export / import par parties : réglages du logiciel et dispositions, réglages du poste (relevés en direct), menu interne du poste (Yaesu à menus « EXnnn »)
    public static class Backup
    {
        static readonly string[] noRadio = { "ptt", "tune", "tunemic", "tuner", "poff", "pon", "power_on", "pwr_off" };
        // postes dont le menu interne se lit / s'écrit par EXnnn (3 chiffres), d'après les catalogues des manuels
        static readonly string[] menuRadios = { "ft2000", "ftdx5000", "ft450", "ft950", "ft991a", "ft991a-df5dp", "ftdx3000" };
        public static int MenuFrom = 1, MenuTo = 199;   // plage des menus lus (le test la réduit)
        public static bool HasMenu(RadioDef r) { return r != null && Array.IndexOf(menuRadios, r.Key) >= 0; }

        public static string Dump(RadioDef cur, Rig rig) { return Dump(cur, rig, true, true, false, null, null); }
        public static string Dump(RadioDef cur, Rig rig, bool app, bool radio, bool menu, Action<string> say, Func<bool> cancelled)
        {
            StringBuilder sb = new StringBuilder(); sb.AppendLine("# HamRadioLink " + MainForm.Version + " - sauvegarde " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + (cur != null ? " - poste " + cur.Key : ""));
            if (app)
            {
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
            }
            if (radio && rig.Link != null && cur != null)
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
            if (menu && rig.Link != null && HasMenu(cur)) { sb.AppendLine("[[radiomenu:" + cur.Key + "]]"); ReadMenu(rig, sb, say, cancelled ?? delegate { return false; }); }
            return sb.ToString();
        }

        // Lit les menus 001 à 199 : la réponse brute (ex. « EX0850; ») est gardée telle quelle et renvoyée telle quelle à la restauration. Arrêt après 20 numéros muets de suite (ou dès le menu 030 si le poste ne répond à aucun).
        static void ReadMenu(Rig rig, StringBuilder sb, Action<string> say, Func<bool> cancelled)
        {
            int miss = 0, got = 0;
            for (int n = MenuFrom; n <= MenuTo && !cancelled(); n++)
            {
                string pre = "EX" + n.ToString("000"); if (say != null) say("Menu " + n.ToString("000") + " (" + got + " lus)…");
                string a = rig.AskPrefix(pre + ";", pre, 700); if (a == null) a = rig.AskPrefix(pre + ";", pre, 700);
                if (a != null && a.StartsWith(pre) && a.Length > pre.Length) { sb.AppendLine(a.Trim()); got++; miss = 0; } else { miss++; if (got == 0 ? n - MenuFrom >= 29 : miss >= 20) break; }
            }
        }

        // Parties présentes dans un fichier : "app", "radio", "menu"
        public static List<string> Parts(string text)
        {
            List<string> l = new List<string>();
            foreach (string raw in text.Replace("\r\n", "\n").Split('\n')) { string s = raw.Trim(); if (!(s.StartsWith("[[") && s.EndsWith("]]"))) continue; string n = s.Substring(2, s.Length - 4); if (n.StartsWith("rig") && !n.EndsWith(".ini")) continue; string p =n.StartsWith("radiomenu:") ? "menu" : n.StartsWith("radio:") ? "radio" : "app"; if (!l.Contains(p)) l.Add(p); }
            return l;
        }
        // Poste d'origine du menu / des réglages (ex. « ft2000 »), vide si inconnu
        public static string RadioOf(string text) { System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(text, @"\[\[radio(?:menu)?:([^\]]+)\]\]"); return m.Success ? m.Groups[1].Value : ""; }

        // ----- Export / import d'UN poste : disposition (toutes fenêtres), commandes (CAT, personnalisées) et réglages d'affichage de ce poste -----
        static readonly string[] rigKinds = { "layout", "cat", "custom" };
        static readonly string[] rigPrefixes = { "analog.", "lock.", "cal." };
        public static string DumpRig(RadioDef r)
        {
            StringBuilder sb = new StringBuilder(); string k = r.Key;
            sb.AppendLine("# HamRadioLink " + MainForm.Version + " - poste seul " + k + " - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm")); sb.AppendLine("[[rig:" + k + "]]");
            foreach (string kind in rigKinds)
            {
                string f = Path.Combine(Cfg.Dir, k + "." + kind + ".ini"); if (!File.Exists(f)) continue;
                sb.AppendLine("[[rigfile:" + kind + "]]"); foreach (string l in File.ReadAllLines(f, Encoding.UTF8)) if (l.Trim().Length > 0) sb.AppendLine(l);
            }
            sb.AppendLine("[[rigcfg]]");
            foreach (string line in Cfg.Dump().Replace("\r\n", "\n").Split('\n')) foreach (string p in rigPrefixes) if (line.StartsWith(p + k + ".", StringComparison.Ordinal)) sb.AppendLine(line);
            return sb.ToString();
        }
        // Poste d'un fichier « poste seul » (vide si ce n'en est pas un)
        public static string RigOf(string text) { System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(text, @"^\[\[rig:([^\]]+)\]\]", System.Text.RegularExpressions.RegexOptions.Multiline); return m.Success ? m.Groups[1].Value.Trim() : ""; }
        // Restaure ce seul poste ; retourne le nombre de blocs écrits (0 = fichier sans rien d'utile). Le reste de la configuration n'est pas touché.
        public static int ImportRig(string text)
        {
            string k = RigOf(text); if (k.Length == 0 || k != Path.GetFileName(k) || k.IndexOfAny(new[] { '/', '\\', ':' }) >= 0) return 0;
            Dictionary<string, StringBuilder> files = new Dictionary<string, StringBuilder>(); StringBuilder cfg = null, cur = null;
            foreach (string raw in text.Replace("\r\n", "\n").Split('\n'))
            {
                string l = raw.TrimEnd();
                if (l.StartsWith("[[") && l.EndsWith("]]"))
                {
                    string n = l.Substring(2, l.Length - 4); cur = null;
                    if (n.StartsWith("rigfile:")) { string kind = n.Substring(8); if (Array.IndexOf(rigKinds, kind) >= 0) { cur = new StringBuilder(); files[kind] = cur; } }
                    else if (n == "rigcfg") { cfg = new StringBuilder(); cur = cfg; }
                }
                else if (cur != null && l.Length > 0 && l[0] != '#') cur.AppendLine(l);
            }
            int c = 0;
            foreach (string kind in rigKinds)
            {
                string f = Path.Combine(Cfg.Dir, k + "." + kind + ".ini");
                try { if (files.ContainsKey(kind)) { File.WriteAllText(f, files[kind].ToString().Replace("\n", "\r\n"), Encoding.UTF8); c++; } else if (File.Exists(f)) File.Delete(f); } catch { }
            }
            foreach (string p in rigPrefixes) Cfg.RemovePrefix(p + k + ".");
            if (cfg != null) foreach (string line in cfg.ToString().Replace("\r\n", "\n").Split('\n')) { int i = line.IndexOf('='); if (i <= 0) continue; string key = line.Substring(0, i).Trim(); foreach (string p in rigPrefixes) if (key.StartsWith(p + k + ".", StringComparison.Ordinal)) { Cfg.Set(key, line.Substring(i + 1)); c++; } }
            Cfg.Save(); return c;
        }

        public static int Import(string text) { return Import(text, true, true, true); }
        // écrit les fichiers (app) et garde les réglages / le menu du poste en attente (appliqués à la prochaine connexion, ou tout de suite par ApplyPending) ; retourne le nombre de blocs
        public static int Import(string text, bool app, bool radio, bool menu)
        {
            int n = 0; string name = null; StringBuilder body = new StringBuilder();
            Action flush = delegate
            {
                if (name == null) return;
                if (name.StartsWith("radiomenu:")) { if (menu) { Cfg.Set("pendingmenu." + name.Substring(10), Enc(body.ToString())); n++; } }
                else if (name.StartsWith("radio:")) { if (radio) { Cfg.Set("pending." + name.Substring(6), Enc(body.ToString())); n++; } }
                else if (name.StartsWith("rig") && !name.EndsWith(".ini")) { }
                else if (!app) { }
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
        // applique les réglages et le menu en attente du poste (après connexion) ; retourne le nombre de commandes envoyées
        public static int ApplyPending(RadioDef cur, Rig rig)
        {
            int n = 0; string k = "pending." + cur.Key, e = Cfg.Get(k, "");
            if (e.Length > 0)
            {
                Cfg.Set(k, ""); Cfg.Save();
                foreach (string l in Uri.UnescapeDataString(e).Replace("\r\n", "\n").Split('\n'))
                {
                    int i = l.IndexOf('='); if (i <= 0) continue; Ctl c = cur.Find(l.Substring(0, i)); if (c == null || !c.HasCat || c.Set.Length == 0 || Array.IndexOf(noRadio, c.Id) >= 0) continue; string v = l.Substring(i + 1);
                    if (c.K == Kind.Toggle) rig.Set(c, v == "1" ? c.OnSend : c.OffSend); else if (c.K == Kind.Knob || c.K == Kind.Choice) rig.Set(c, v); else continue; n++;
                }
            }
            string km = "pendingmenu." + cur.Key, em = Cfg.Get(km, "");
            if (em.Length > 0 && HasMenu(cur))
            {
                Cfg.Set(km, ""); Cfg.Save();
                foreach (string l in Uri.UnescapeDataString(em).Replace("\r\n", "\n").Split('\n')) { string s = l.Trim(); if (s.StartsWith("EX") && s.EndsWith(";") && s.Length > 6) { rig.Raw(s + "{wait:150}"); n++; } }
            }
            return n;
        }
    }

    // Petite fenêtre : cases à cocher des parties à exporter / importer
    public class PartsDlg : Form
    {
        readonly CheckBox[] cb;
        public PartsDlg(string title, string intro, string[] labels, bool[] on, bool[] enabled)
        {
            Text = title; BackColor = T.Bg; ForeColor = T.Text; Font = new Font("Segoe UI", 9f); FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false; StartPosition = FormStartPosition.CenterParent;
            Label l = new Label(); l.SetBounds(12, 10, 456, 44); l.Text = intro; Controls.Add(l); cb = new CheckBox[labels.Length]; int y = 60;
            for (int i = 0; i < labels.Length; i++) { cb[i] = new CheckBox(); cb[i].SetBounds(16, y, 450, 40); cb[i].Text = labels[i]; cb[i].Checked = on[i] && enabled[i]; cb[i].Enabled = enabled[i]; cb[i].ForeColor = enabled[i] ? T.Text : T.Dim; Controls.Add(cb[i]); y += 44; }
            Button ok = new Button(); ok.Text = "OK"; ok.SetBounds(262, y + 6, 100, 30); ok.FlatStyle = FlatStyle.Flat; ok.BackColor = T.Panel2; ok.ForeColor = T.Text; ok.DialogResult = DialogResult.OK; Controls.Add(ok); AcceptButton = ok;
            Button no = new Button(); no.Text = "Annuler"; no.SetBounds(370, y + 6, 100, 30); no.FlatStyle = FlatStyle.Flat; no.BackColor = T.Panel2; no.ForeColor = T.Text; no.DialogResult = DialogResult.Cancel; Controls.Add(no); CancelButton = no;
            ClientSize = new Size(480, y + 48);
        }
        public bool Checked(int i) { return cb[i].Checked; }
    }

    public class ExportDlg : Form
    {
        public bool Imported; readonly RadioDef cur; readonly Rig rig; readonly bool connected; Label status; Button b1, b2, b3; volatile bool cancel;
        public ExportDlg(RadioDef cur, Rig rig)
        {
            this.cur = cur; this.rig = rig; connected = rig != null && rig.Link != null; Text = "Export / import"; BackColor = T.Bg; ForeColor = T.Text; ClientSize = new Size(420, 170); StartPosition = FormStartPosition.CenterParent; Font = new Font("Segoe UI", 9f);
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
            Label l = new Label(); l.SetBounds(12, 10, 396, 70); Controls.Add(l);
            l.Text = "Exporter : choisissez les parties à enregistrer (logiciel et dispositions, réglages du poste" + (HasMenuPart() ? ", menu interne du poste" : "") + ").\nImporter : choisissez les parties à restaurer ; les réglages du poste sont envoyés au poste connecté (ou à la prochaine connexion).";
            b1 = B("Exporter…", 12, 96, 120); b1.Click += delegate { Export(); }; b2 = B("Importer…", 142, 96, 120); b2.Click += delegate { Open(); }; b3 = B("Annuler", 292, 96, 116); b3.Click += delegate { cancel = true; Close(); }; CancelButton = b3;
            Label l2 = new Label(); l2.SetBounds(12, 140, 396, 34); l2.ForeColor = T.Dim; l2.Text = "Un seul poste (" + CmdServer.ModelName(cur) + ") : sa disposition, ses commandes et ses réglages d'affichage, sans toucher au reste du logiciel."; Controls.Add(l2);
            Button b4 = B("Exporter ce poste…", 12, 178, 196), b5 = B("Importer un poste…", 214, 178, 194);
            b4.Click += delegate { ExportRig(); }; b5.Click += delegate { ImportRig(); };
            status = new Label(); status.SetBounds(12, 220, 396, 24); status.ForeColor = T.Dim; Controls.Add(status); ClientSize = new Size(420, 248);
        }
        void ExportRig()
        {
            using (SaveFileDialog d = new SaveFileDialog())
            {
                d.Filter = "Poste HamRadioLink (*.ini)|*.ini|Tous les fichiers|*.*"; d.FileName = "HamRadioLink-poste-" + cur.Key + "-" + DateTime.Now.ToString("yyyy-MM-dd") + ".ini"; if (d.ShowDialog(this) != DialogResult.OK) return;
                try { try { cur.SaveLayout(); cur.SaveCustom(); cur.SaveCat(); } catch { } File.WriteAllText(d.FileName, Backup.DumpRig(cur).Replace("\r\n", "\n").Replace("\n", "\r\n"), Encoding.UTF8); MessageBox.Show(this, "Poste exporté :\n" + d.FileName, "Export"); Close(); }
                catch (Exception ex) { MessageBox.Show(this, ex.Message, "Erreur"); }
            }
        }
        void ImportRig()
        {
            using (OpenFileDialog d = new OpenFileDialog())
            {
                d.Filter = "Poste HamRadioLink (*.ini)|*.ini|Tous les fichiers|*.*"; if (d.ShowDialog(this) != DialogResult.OK) return;
                string txt; try { txt = File.ReadAllText(d.FileName, Encoding.UTF8); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Erreur"); return; }
                string k = Backup.RigOf(txt); if (k.Length == 0) { MessageBox.Show(this, "Ce fichier n'est pas une sauvegarde d'un seul poste (utilisez « Importer… » pour une sauvegarde complète).", "Importer"); return; }
                RadioDef rd = null; try { rd = Radios.ByKey(k); } catch { }
                if (rd == null) { MessageBox.Show(this, "Le poste « " + k + " » de ce fichier n'existe pas dans cette version du logiciel : import impossible.", "Importer un poste"); return; }
                string nm = CmdServer.ModelName(rd) + " (" + k + ")";
                if (MessageBox.Show(this, "ATTENTION — ce fichier est la sauvegarde du poste :\n\n        " + nm + "\n\nSa disposition, ses commandes et ses réglages d'affichage vont ÉCRASER la configuration actuelle de ce poste (le poste est déterminé par le fichier, pas par la fenêtre en cours). Le reste du logiciel n'est pas touché. Le logiciel redémarre.\n\nContinuer ?", "Importer un poste", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.OK) return;
                if (Backup.ImportRig(txt) == 0) { MessageBox.Show(this, "Rien à restaurer dans ce fichier.", "Importer"); return; }
                Imported = true; DialogResult = DialogResult.OK; Close();
            }
        }
        bool HasMenuPart() { return Backup.HasMenu(cur); }
        Button B(string t, int x, int y, int w) { Button b = new Button(); b.Text = t; b.SetBounds(x, y, w, 34); b.FlatStyle = FlatStyle.Flat; b.BackColor = T.Panel2; b.ForeColor = T.Text; b.FlatAppearance.BorderColor = T.Edge; Controls.Add(b); return b; }
        void Export()
        {
            bool menu = HasMenuPart() && connected;
            using (PartsDlg p = new PartsDlg("Exporter", "Parties à enregistrer dans le fichier :", new[] { "Réglages du logiciel et dispositions de tous les postes", "Réglages du poste connecté (relevés en direct)" + (connected ? "" : " — poste non connecté"), "Menu interne du poste (EXnnn, une minute environ)" + (!HasMenuPart() ? " — non géré pour ce poste" : connected ? "" : " — poste non connecté") },
                new[] { true, true, false }, new[] { true, connected, menu })) { if (p.ShowDialog(this) != DialogResult.OK) return; bool a = p.Checked(0), r = p.Checked(1), m = p.Checked(2); if (!a && !r && !m) return; Save(a, r, m); }
        }
        void Save(bool a, bool r, bool m)
        {
            string file; using (SaveFileDialog d = new SaveFileDialog())
            {
                d.Filter = "Sauvegarde HamRadioLink (*.ini)|*.ini|Tous les fichiers|*.*"; d.FileName = "HamRadioLink-config-" + DateTime.Now.ToString("yyyy-MM-dd") + ".ini"; if (d.ShowDialog(this) != DialogResult.OK) return; file = d.FileName;
            }
            b1.Enabled = b2.Enabled = false; cancel = false;
            System.Threading.Thread th = new System.Threading.Thread(delegate()
            {
                string s = null, err = null;
                try { s = Backup.Dump(cur, rig, a, r, m, delegate(string t) { Say(t); }, delegate { return cancel; }); File.WriteAllText(file, s.Replace("\r\n", "\n").Replace("\n", "\r\n"), Encoding.UTF8); } catch (Exception ex) { err = ex.Message; }
                try { BeginInvoke(new Action(delegate { if (err != null) MessageBox.Show(this, err, "Erreur"); else { MessageBox.Show(this, "Configuration exportée :\n" + file, "Export"); Close(); } })); } catch { }
            }); th.IsBackground = true; th.Start();
        }
        void Say(string t) { try { BeginInvoke(new Action(delegate { status.Text = t; })); } catch { } }
        void Open()
        {
            using (OpenFileDialog d = new OpenFileDialog())
            {
                d.Filter = "Sauvegarde HamRadioLink (*.ini)|*.ini|Tous les fichiers|*.*"; if (d.ShowDialog(this) != DialogResult.OK) return;
                string txt; try { txt = File.ReadAllText(d.FileName, Encoding.UTF8); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Erreur"); return; }
                List<string> parts = Backup.Parts(txt); if (parts.Count == 0) { MessageBox.Show(this, "Aucun réglage valide dans ce fichier.", "Importer"); return; }
                string from = Backup.RadioOf(txt); bool same = from.Length == 0 || (cur != null && from == cur.Key);
                using (PartsDlg p = new PartsDlg("Importer", "Parties à restaurer depuis ce fichier" + (from.Length > 0 ? " (poste d'origine : " + from + ")" : "") + " :",
                    new[] { "Réglages du logiciel et dispositions (le logiciel redémarre)", "Réglages du poste" + (same ? "" : " — fichier d'un autre poste : non proposé"), "Menu interne du poste (EXnnn)" + (same ? "" : " — fichier d'un autre poste : non proposé") },
                    new[] { true, true, false }, new[] { parts.Contains("app"), parts.Contains("radio") && same, parts.Contains("menu") && same && Backup.HasMenu(cur) }))
                {
                    if (p.ShowDialog(this) != DialogResult.OK) return; bool a = p.Checked(0), r = p.Checked(1), m = p.Checked(2); if (!a && !r && !m) return;
                    if (m && MessageBox.Show(this, "Le menu interne du poste va être réécrit (valeurs relevées lors de l'export). Continuer ?", "Importer", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK) return;
                    if (Backup.Import(txt, a, r, m) == 0) { MessageBox.Show(this, "Rien à restaurer dans les parties choisies.", "Importer"); return; }
                    if (a) { Imported = true; DialogResult = DialogResult.OK; Close(); return; }   // le logiciel redémarre (les réglages du poste suivent à la prochaine connexion)
                    int sent = connected && cur != null ? Backup.ApplyPending(cur, rig) : 0;
                    MessageBox.Show(this, sent > 0 ? sent + " commandes envoyées au poste." : "Les réglages du poste seront envoyés à la prochaine connexion.", "Importer"); Close();
                }
            }
        }
    }
}
