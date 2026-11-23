using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace HamRadioLink
{
    // Réglages détaillés du réducteur de bruit numérique (DNR) et du blanker (NB) du FTdx10, ouverts par un clic droit sur DNR ou NB (Ctrl + clic droit : verrou).
    //   NB  : NB (marche / arrêt), NL0 niveau 0-10, menu EX030101 « NB WIDTH » (1 / 3 / 10 ms), menu EX030102 « NB REJECTION » (10 / 30 / 50 dB).
    //   DNR : NR0 (marche / arrêt), RL0 niveau 1-15.
    // Fenêtre non bloquante ; les valeurs changées sur le poste sont relues et affichées.
    public class RxFxForm : Form
    {
        static readonly Dictionary<string, RxFxForm> inst = new Dictionary<string, RxFxForm>();
        public static void Open(Rig rig, Form owner, string kind)
        {
            RxFxForm f; if (inst.TryGetValue(kind, out f) && !f.IsDisposed) { f.WindowState = FormWindowState.Normal; f.BringToFront(); f.Activate(); return; }
            f = new RxFxForm(rig, kind); inst[kind] = f; f.Show(owner);
        }

        readonly Rig rig; readonly bool nb; readonly Ctl onCtl, lvCtl; readonly CheckBox on; readonly TrackBar tb; readonly Label val, msg;
        readonly RadioButton[] width = new RadioButton[3], rej = new RadioButton[3];
        bool loading = true; DateTime lastEdit = DateTime.MinValue; volatile bool busy; System.Windows.Forms.Timer poll;
        static readonly Color Back = Color.FromArgb(58, 58, 58), Txt = Color.FromArgb(225, 225, 225), Pan = Color.FromArgb(64, 64, 64);

        RxFxForm(Rig rig, string kind)
        {
            this.rig = rig; nb = kind == "nb"; onCtl = rig.Def.Find(nb ? "nb" : "dnr"); lvCtl = rig.Def.Find(nb ? "nbl" : "dnrl");
            AutoScaleMode = AutoScaleMode.None; Text = (nb ? "Noise Blanker (NB)" : "Digital Noise Reduction (DNR)") + " — " + rig.Def.Name; FormBorderStyle = FormBorderStyle.FixedSingle; MaximizeBox = false; StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(440, nb ? 380 : 230); BackColor = Back; ForeColor = Txt; Font = new Font("Segoe UI", 9f); Icon = Application.OpenForms.Count > 0 ? Application.OpenForms[0].Icon : null;
            int y = 14;
            on = new CheckBox { Appearance = Appearance.Button, Text = "", Left = 16, Top = y, Width = 408, Height = 40, TextAlign = ContentAlignment.MiddleCenter, FlatStyle = FlatStyle.Flat, Font = new Font(Font.FontFamily, 11f, FontStyle.Bold), UseVisualStyleBackColor = false };
            on.FlatAppearance.BorderColor = Color.FromArgb(130, 130, 130); on.CheckedChanged += delegate { Style(); if (!loading) { lastEdit = DateTime.Now; rig.Set(onCtl, on.Checked ? onCtl.OnSend : onCtl.OffSend); } }; Controls.Add(on);
            y += 56; Panel pl = new Panel { Left = 16, Top = y, Width = 408, Height = 96, BackColor = Pan }; Controls.Add(pl);
            pl.Controls.Add(new Label { Text = nb ? "Niveau du blanker (NL, 0 à 10)" : "Niveau de réduction (RL, 1 à 15)", Left = 12, Top = 8, Width = 380, ForeColor = Color.White, Font = new Font(Font, FontStyle.Bold) });
            int lo = nb ? 0 : 1, hi = nb ? 10 : 15;
            tb = new TrackBar { Left = 8, Top = 34, Width = 330, Minimum = lo, Maximum = hi, TickFrequency = 1, LargeChange = 1, SmallChange = 1, BackColor = Pan }; pl.Controls.Add(tb);
            val = new Label { Left = 346, Top = 38, Width = 50, Height = 30, Font = new Font(Font.FontFamily, 14f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.FromArgb(255, 160, 40) }; pl.Controls.Add(val);
            tb.ValueChanged += delegate { val.Text = tb.Value.ToString(); if (!loading) { lastEdit = DateTime.Now; rig.Set(lvCtl, tb.Value.ToString()); } };
            y += 108;
            if (nb)
            {
                Panel pw = new Panel { Left = 16, Top = y, Width = 408, Height = 62, BackColor = Pan }; Controls.Add(pw);
                pw.Controls.Add(new Label { Text = "Largeur du blanker (menu NB WIDTH)", Left = 12, Top = 6, Width = 380, ForeColor = Color.White, Font = new Font(Font, FontStyle.Bold) });
                string[] wn = { "1 ms", "3 ms", "10 ms" }; for (int i = 0; i < 3; i++) { int k = i; RadioButton r = new RadioButton { Text = wn[i], Left = 20 + i * 120, Top = 30, Width = 100, ForeColor = Txt }; r.CheckedChanged += delegate { if (!loading && r.Checked) SendMenu("EX030101", k); }; pw.Controls.Add(r); width[i] = r; }
                y += 70; Panel pr = new Panel { Left = 16, Top = y, Width = 408, Height = 62, BackColor = Pan }; Controls.Add(pr);
                pr.Controls.Add(new Label { Text = "Réjection du blanker (menu NB REJECTION)", Left = 12, Top = 6, Width = 380, ForeColor = Color.White, Font = new Font(Font, FontStyle.Bold) });
                string[] rn = { "10 dB", "30 dB", "50 dB" }; for (int i = 0; i < 3; i++) { int k = i; RadioButton r = new RadioButton { Text = rn[i], Left = 20 + i * 120, Top = 30, Width = 100, ForeColor = Txt }; r.CheckedChanged += delegate { if (!loading && r.Checked) SendMenu("EX030102", k); }; pr.Controls.Add(r); rej[i] = r; }
                y += 70;
            }
            msg = new Label { Left = 16, Top = y, Width = 408, Height = nb ? 44 : 40, ForeColor = Color.FromArgb(170, 170, 170) }; Controls.Add(msg);
            msg.Text = nb ? "Les réglages sont envoyés au poste dès qu'on les change ; ceux faits sur le poste apparaissent ici." : "Le DNR du FTdx10 n'a qu'un niveau (1 à 15) ; les changements faits sur le poste apparaissent ici.";
            Refresh2(); loading = false; Shown += delegate { ReadNow(); };
            poll = new System.Windows.Forms.Timer { Interval = 1000 }; poll.Tick += delegate { Tick(); }; poll.Start(); FormClosed += delegate { poll.Stop(); poll.Dispose(); };
        }

        // images des deux fenêtres (--rxfxshot=dossier)
        public static void Shot(string dir)
        {
            foreach (string k in new[] { "nb", "dnr" })
            {
                Rig r = new Rig(); r.Def = Radios.ByKey("ftdx10"); r.V["nb"] = "1"; r.V["nbl"] = "6"; r.V["dnr"] = "1"; r.V["dnrl"] = "7";
                RxFxForm f = new RxFxForm(r, k); f.StartPosition = FormStartPosition.Manual; f.Location = new Point(-3000, 0); f.Show(); Application.DoEvents(); System.Threading.Thread.Sleep(300); Application.DoEvents();
                using (Bitmap b = new Bitmap(f.Width, f.Height)) { f.DrawToBitmap(b, new Rectangle(0, 0, f.Width, f.Height)); b.Save(System.IO.Path.Combine(dir, k + ".png"), System.Drawing.Imaging.ImageFormat.Png); }
                f.Close();
            }
        }

        // lecture directe du poste à l'ouverture : marche / arrêt, niveau et (NB) largeur et réjection, sans attendre la lecture périodique
        void ReadNow()
        {
            new Thread(delegate()
            {
                string pre = nb ? "NB0" : "NR0", lpre = nb ? "NL0" : "RL0"; int o = -1, l = -1, w = -1, r = -1;
                try
                {
                    string a = null; for (int i = 0; i < 3 && a == null; i++) a = rig.AskPrefix(pre + ";", pre, 900); int v; if (a != null && int.TryParse(a.Substring(a.IndexOf(pre) + pre.Length).TrimEnd(';'), out v)) o = v;
                    a = null; for (int i = 0; i < 3 && a == null; i++) a = rig.AskPrefix(lpre + ";", lpre, 900); if (a != null && int.TryParse(a.Substring(a.IndexOf(lpre) + lpre.Length).TrimEnd(';'), out v)) l = v;
                    if (nb) { w = Menu("EX030101"); r = Menu("EX030102"); }
                }
                catch { }
                try
                {
                    BeginInvoke(new Action(delegate
                    {
                        bool was = loading; loading = true;
                        if (o >= 0) { on.Checked = o == 1; Style(); rig.ShowLocal(onCtl.Id, o.ToString()); }
                        if (l >= 0) { tb.Value = Math.Max(tb.Minimum, Math.Min(tb.Maximum, l)); val.Text = tb.Value.ToString(); rig.ShowLocal(lvCtl.Id, l.ToString()); }
                        if (nb) { if (w >= 0 && w < 3) width[w].Checked = true; if (r >= 0 && r < 3) rej[r].Checked = true; }
                        loading = was; Msg(o < 0 && l < 0 ? "Poste non connecté ou sans réponse : valeurs non lues." : "Valeurs lues dans le poste.", o < 0 && l < 0);
                    }));
                }
                catch { }
            }) { IsBackground = true }.Start();
        }
        void Style()
        {
            on.BackColor = on.Checked ? Color.FromArgb(40, 130, 60) : Color.FromArgb(90, 90, 90); on.ForeColor = Color.White; on.Text = (nb ? "NB " : "DNR ") + (on.Checked ? "— ACTIVÉ" : "— désactivé");
            on.FlatAppearance.CheckedBackColor = on.BackColor;
        }
        // valeurs déjà lues par la boucle de lecture du programme (marche / arrêt, niveau)
        void Refresh2()
        {
            bool was = loading; loading = true;
            try
            {
                string o = rig.Get(onCtl.Id), l = rig.Get(lvCtl.Id); int n;
                on.Checked = onCtl.IsOn(o); Style();
                if (l != null && int.TryParse(l, out n)) { tb.Value = Math.Max(tb.Minimum, Math.Min(tb.Maximum, n)); val.Text = tb.Value.ToString(); }
            }
            finally { loading = was; }
        }
        int mw = -1, mr = -1;
        void Tick()
        {
            if (IsDisposed) return;
            if ((DateTime.Now - lastEdit).TotalSeconds >= 2) Refresh2();
            if (!nb || busy || rig.Loading || (DateTime.Now - lastEdit).TotalSeconds < 3) return; busy = true;
            new Thread(delegate()
            {
                int w = Menu("EX030101"), r = Menu("EX030102");
                try { BeginInvoke(new Action(delegate { busy = false; bool was = loading; loading = true; if (w >= 0 && w < 3) { width[w].Checked = true; mw = w; } if (r >= 0 && r < 3) { rej[r].Checked = true; mr = r; } loading = was; })); } catch { }
            }) { IsBackground = true }.Start();
        }
        int Menu(string a)
        {
            string ans = null; for (int i = 0; i < 3 && ans == null; i++) ans = rig.AskPrefix(a + ";", a, 900); int v;
            return ans != null && int.TryParse(ans.Substring(ans.IndexOf(a) + a.Length).TrimEnd(';').Trim(), out v) ? v : -1;
        }
        void SendMenu(string a, int v)
        {
            if (rig.Loading) { Msg("Le poste est encore en cours de lecture : réglage non envoyé.", false); return; }
            lastEdit = DateTime.Now; rig.Raw(a + v + ";");
            new Thread(delegate()
            {
                Thread.Sleep(700); int r = Menu(a);
                if (r >= 0 && r != v) try { BeginInvoke(new Action(delegate { Msg("Commande non acceptée par le poste (menu " + a + ") — entrez la valeur par le menu du poste : OPERATION SETTING > GENERAL.", true); })); } catch { }
            }) { IsBackground = true }.Start();
        }
        void Msg(string t, bool bad) { msg.Text = t; msg.ForeColor = bad ? Color.FromArgb(255, 90, 90) : Color.FromArgb(170, 170, 170); msg.Font = new Font(msg.Font.FontFamily, 9f, bad ? FontStyle.Bold : FontStyle.Regular); }
    }
}
