using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace HamRadioLink
{
    // Fenêtre « Partage du poste » : port COM virtuel (paires com0com) et / ou serveur TCP
    public class ShareDlg : Form
    {
        readonly string[] radioPorts; readonly string rk, rn; CheckBox chOn, chCom, chTcp, chNet; DarkCombo cbPair, cbBaud, cbStop; TextBox tbPort; Label lbInfo, lbCom; Btn btnCreate, btnDelete; List<Com0Com.Pair> pairs = new List<Com0Com.Pair>(); bool loading = true, offered;
        static Label L(Form f, string t, int x, int y, int w, int h, Color c) { Label l = new Label(); l.Text = t; l.SetBounds(x, y, w, h); l.ForeColor = c; l.BackColor = Color.Transparent; f.Controls.Add(l); return l; }
        CheckBox C(string t, int x, int y, int w) { CheckBox c = new CheckBox(); c.Text = t; c.SetBounds(x, y, w, 24); c.ForeColor = T.Text; c.BackColor = Color.Transparent; Controls.Add(c); return c; }

        public ShareDlg(string radioKey, string radioName, string[] radioPorts)
        {
            this.radioPorts = radioPorts; rk = radioKey; rn = radioName;
            Text = "Partage du poste — " + radioName; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = MinimizeBox = false; StartPosition = FormStartPosition.CenterParent; BackColor = T.Panel; ClientSize = new Size(620, 470); ShowInTaskbar = false;
            L(this, "HamRadioLink garde la liaison complète avec le poste ; un autre logiciel (OmniRig, OpsLog, log, WSJT-X, wfview…) peut se brancher en même temps sur un port COM virtuel ou sur un port TCP. Les commandes passent sans aucun filtre. Les réglages s'appliquent à la prochaine connexion.", 14, 10, 592, 62, T.Dim);
            chOn = C("Activer le partage à la connexion", 14, 78, 400); chOn.Checked = Cfg.GetBool(ShareOpts.K(rk, "on"), false);
            chCom = C("Port COM virtuel (paire com0com)", 30, 110, 500); chCom.Checked = Cfg.GetBool(ShareOpts.K(rk, "comOn"), false);
            lbCom = L(this, "", 50, 138, 540, 20, T.Amber);
            L(this, "Paire", 50, 168, 50, 24, T.Dim); cbPair = new DarkCombo(); cbPair.SetBounds(104, 166, 210, 26); Controls.Add(cbPair); cbPair.SelectedIndexChanged += delegate { UpdateInfo(); };
            btnCreate = new Btn(); btnCreate.Text = "Créer une paire…"; btnCreate.SetBounds(326, 164, 150, 30); btnCreate.Click += delegate { Create(); }; Controls.Add(btnCreate);
            btnDelete = new Btn(); btnDelete.Text = "Supprimer la paire…"; btnDelete.Accent = T.Red; btnDelete.SetBounds(484, 164, 120, 30); btnDelete.Click += delegate { Delete(); }; Controls.Add(btnDelete);
            L(this, "Vitesse", 50, 204, 56, 24, T.Dim); cbBaud = new DarkCombo(); foreach (string s in new string[] { "1200", "4800", "9600", "19200", "38400", "57600", "115200" }) cbBaud.Items.Add(s); cbBaud.SetBounds(108, 202, 90, 26); Controls.Add(cbBaud); cbBaud.Text = Cfg.GetInt(ShareOpts.K(rk, "baud"), 9600).ToString();
            L(this, "Arrêt", 212, 204, 46, 24, T.Dim); cbStop = new DarkCombo(); cbStop.Items.Add("1"); cbStop.Items.Add("2"); cbStop.SetBounds(260, 202, 56, 26); Controls.Add(cbStop); cbStop.Text = Cfg.GetInt(ShareOpts.K(rk, "stop"), 1).ToString();
            lbInfo = L(this, "", 50, 238, 550, 78, T.Cyan);
            chTcp = C("Serveur TCP", 30, 332, 200); chTcp.Checked = Cfg.GetBool(ShareOpts.K(rk, "tcpOn"), false);
            L(this, "Port TCP", 50, 362, 70, 24, T.Dim); tbPort = new TextBox(); tbPort.SetBounds(124, 360, 80, 24); tbPort.BackColor = T.Panel2; tbPort.ForeColor = T.Text; tbPort.BorderStyle = BorderStyle.FixedSingle; Controls.Add(tbPort); tbPort.Text = Cfg.GetInt(ShareOpts.K(rk, "tcpPort"), 4540).ToString();
            chNet = C("Accessible depuis le réseau local (sinon : cet ordinateur seulement)", 50, 392, 540); chNet.Checked = Cfg.GetBool(ShareOpts.K(rk, "net"), false);
            Btn ok = new Btn(); ok.Text = "OK"; ok.SetBounds(390, 426, 100, 30); ok.Accent = T.Green; ok.Click += delegate { Save(); }; Controls.Add(ok);
            Btn ca = new Btn(); ca.Text = "Annuler"; ca.SetBounds(500, 426, 100, 30); ca.Click += delegate { DialogResult = DialogResult.Cancel; Close(); }; Controls.Add(ca);
            chOn.CheckedChanged += delegate { if (!loading && chOn.Checked && !Com0Com.Installed) AskInstall(true); };
            chCom.CheckedChanged += delegate { if (loading) return; if (chCom.Checked) { if (!AskInstall(false)) chCom.Checked = false; else RefreshPairs(true); } else RefreshPairs(false); };
            Shown += delegate { loading = false; RefreshPairs(chCom.Checked); };
        }

        // com0com absent : explique et propose l'installation silencieuse
        bool AskInstall(bool fromActivation)
        {
            if (Com0Com.Installed) return true;
            string q = "Le partage par port COM virtuel a besoin de com0com : un pilote gratuit qui crée des paires de ports COM reliés entre eux (HamRadioLink prend une extrémité, OmniRig, OpsLog ou un autre logiciel prend l'autre).\n\ncom0com n'est pas installé sur cet ordinateur. HamRadioLink contient son installateur signé et peut l'installer maintenant, sans rien à régler : Windows demandera seulement l'autorisation d'administrateur (et peut-être de confirmer le pilote).\n\nInstaller com0com maintenant ?" + (fromActivation ? "\n\n(Non = vous n'utiliserez que le serveur TCP.)" : "");
            if (MessageBox.Show(this, q, "Partage du poste — com0com", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return false;
            string err; Cursor = Cursors.WaitCursor; bool ok; try { ok = Com0Com.Install(out err); } finally { Cursor = Cursors.Default; }
            if (!ok) { MessageBox.Show(this, "Installation de com0com impossible : " + err + "\n\nVous pouvez l'installer à la main (sourceforge.net/projects/com0com, version « signed ») ou n'utiliser que le serveur TCP.", "Partage du poste", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
            MessageBox.Show(this, "com0com est installé.", "Partage du poste"); return true;
        }

        void RefreshPairs(bool enabled)
        {
            cbPair.Items.Clear(); pairs.Clear(); cbPair.Enabled = btnCreate.Enabled = btnDelete.Enabled = enabled; cbBaud.Enabled = cbStop.Enabled = enabled;
            if (!enabled) { lbCom.Text = ""; lbInfo.Text = ""; return; }
            if (!Com0Com.Installed) { lbCom.Text = "com0com n'est pas installé."; lbInfo.Text = ""; return; }
            Cursor = Cursors.WaitCursor;
            try { Dictionary<string, string> hd; lock (ShareLink.Held) hd = new Dictionary<string, string>(ShareLink.Held, StringComparer.OrdinalIgnoreCase); pairs = Com0Com.AllComPairs(radioPorts, hd, rn); } finally { Cursor = Cursors.Default; }
            string saved = Cfg.Get(ShareOpts.K(rk, "com"), ""); int sel = -1, firstFree = -1, nFree = 0, nOwn = 0, nBusy = 0;
            for (int i = 0; i < pairs.Count; i++)
            {
                cbPair.Items.Add(pairs[i].ToString()); if (pairs[i].Status == 0) { nFree++; if (firstFree < 0) firstFree = i; } else if (pairs[i].Status == 1 || pairs[i].Status == 3) nOwn++; else nBusy++;
                if (pairs[i].A.Equals(saved, StringComparison.OrdinalIgnoreCase)) sel = i;
            }
            if (sel < 0) sel = firstFree >= 0 ? firstFree : 0;
            foreach (Com0Com.Pair q in pairs) CatLog.Event("partage", "paire " + q.A + " <-> " + q.B + " : " + (q.Status == 0 ? "libre" : (q.Status == 1 ? "tenue par ce poste" : (q.Status == 3 ? "utilisée par un autre poste (" + q.Owner + ")" : "occupée par un autre programme"))));
            if (pairs.Count > 0) { lbCom.Text = pairs.Count + " paire(s) : " + nFree + " libre(s), " + nOwn + " tenue(s) par HamRadioLink, " + nBusy + " occupée(s) par un autre programme."; cbPair.SelectedIndex = sel; }
            else
            {
                lbCom.Text = "Aucune paire de ports COM virtuels."; UpdateInfo();
                if (!offered) { offered = true; BeginInvoke(new Action(delegate { Create(); })); }
            }
        }
        Com0Com.Pair Current { get { int i = cbPair.SelectedIndex; return i >= 0 && i < pairs.Count ? pairs[i] : null; } }
        void UpdateInfo()
        {
            Com0Com.Pair p = Current;
            if (p != null && p.Status == 2) { lbInfo.Text = "La paire " + p.A + " \u2194 " + p.B + " existe toujours, mais le port " + p.A + " est ouvert par un autre programme (ExpertSDR3, un logiciel de log…) : choisissez une autre paire, ou fermez ce programme. Rien n'a été supprimé."; return; }
            if (p != null && p.Status == 1) { lbInfo.Text = "Cette paire est en ce moment tenue ouverte par le partage de CE poste (" + p.Owner + ") : elle le reste tant qu'il est connecté.\n\nDans l'autre logiciel : port " + p.B + "."; return; }
            if (p != null && p.Status == 3) { lbInfo.Text = "Cette paire est déjà utilisée par le partage d'un AUTRE poste de HamRadioLink (" + p.Owner + ") : choisissez une autre paire pour " + rn + ", ou déconnectez " + p.Owner + "."; return; }
            lbInfo.Text = p == null ? "Cliquez sur « Créer une paire… » : HamRadioLink vous proposera deux numéros de ports COM libres."
                : "HamRadioLink utilisera " + p.A + ".\nDans OmniRig, OpsLog ou tout autre logiciel à brancher sur le poste, choisissez le port " + p.B + " (même vitesse que ci-dessus, 8 bits, sans parité).\nLes deux logiciels pilotent alors le même poste en même temps.";
        }
        void Delete()
        {
            Com0Com.Pair p = Current; if (p == null) { MessageBox.Show(this, "Choisissez d'abord la paire à supprimer dans la liste.", "Partage du poste"); return; }
            if (p.Status != 0) { MessageBox.Show(this, "Cette paire est utilisée (" + (p.Status == 2 ? "par un autre programme : fermez-le d'abord" : "par HamRadioLink : déconnectez d'abord le poste " + p.Owner) + ") : elle ne peut pas être supprimée maintenant.", "Partage du poste"); return; }
            if (MessageBox.Show(this, "Supprimer la paire " + p.A + " \u2194 " + p.B + " ?\n\nLes deux ports COM disparaissent de Windows. Tout logiciel configuré sur ces ports (OmniRig, OpsLog…) devra être reconfiguré. Fermez-les avant de continuer.\n\nWindows demandera l'autorisation d'administrateur.", "Supprimer la paire", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            string err; Cursor = Cursors.WaitCursor; bool ok; try { ok = Com0Com.RemovePair(p, out err); } finally { Cursor = Cursors.Default; }
            if (!ok) { MessageBox.Show(this, "Suppression impossible : " + err, "Partage du poste", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (Cfg.Get(ShareOpts.K(rk, "com"), "").Equals(p.A, StringComparison.OrdinalIgnoreCase)) Cfg.Set(ShareOpts.K(rk, "com"), "");
            RefreshPairs(true); MessageBox.Show(this, "Paire " + p.A + " \u2194 " + p.B + " supprimée.", "Partage du poste");
        }
        void Create()
        {
            if (!Com0Com.Installed && !AskInstall(false)) return;
            int[] n = Com0Com.FreeNumbers(); string a = "COM" + n[0], b = "COM" + n[1];
            using (PairDlg d = new PairDlg(a, b))
            {
                if (d.ShowDialog(this) != DialogResult.OK) return; a = d.A; b = d.B;
            }
            string err; Cursor = Cursors.WaitCursor; bool ok; try { ok = Com0Com.CreatePair(a, b, out err); } finally { Cursor = Cursors.Default; }
            if (!ok) { MessageBox.Show(this, "Création de la paire impossible : " + err, "Partage du poste", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            Cfg.Set(ShareOpts.K(rk, "com"), a); RefreshPairs(true);
            MessageBox.Show(this, "Paire créée : " + a + " ↔ " + b + ".\n\nHamRadioLink utilisera " + a + ".\nDans OmniRig, OpsLog ou tout autre logiciel, choisissez " + b + ".", "Partage du poste");
        }

        void Save()
        {
            int port; if (!int.TryParse(tbPort.Text.Trim(), out port) || port < 1024 || port > 65535) { MessageBox.Show(this, "Port TCP invalide (1024 à 65535).", "Partage"); return; }
            int baud; if (!int.TryParse(cbBaud.Text.Trim(), out baud)) baud = 9600;
            if (chOn.Checked && chCom.Checked && Current != null && Current.Status == 3) { MessageBox.Show(this, "La paire " + Current.A + " est déjà utilisée par le poste « " + Current.Owner + " » : choisissez-en une autre.", "Partage"); return; }
            if (chOn.Checked) foreach (RadioDef o in Radios.All())
                {
                    string ok = CmdServer.ModelName(o); if (o.Key == rk || !ShareOpts.IsOn(o.Key)) continue;
                    if (chCom.Checked && Current != null && Cfg.GetBool(ShareOpts.K(o.Key, "comOn"), false) && Cfg.Get(ShareOpts.K(o.Key, "com"), "").Equals(Current.A, StringComparison.OrdinalIgnoreCase)) { MessageBox.Show(this, "La paire " + Current.A + " \u2194 " + Current.B + " est déjà attribuée au partage du poste « " + ok + " » : choisissez une autre paire (ou changez celle de ce poste).", "Partage"); return; }
                    int tp; if (chTcp.Checked && int.TryParse(tbPort.Text.Trim(), out tp) && Cfg.GetBool(ShareOpts.K(o.Key, "tcpOn"), false) && Cfg.GetInt(ShareOpts.K(o.Key, "tcpPort"), 4540) == tp) { MessageBox.Show(this, "Le port TCP " + tp + " est déjà attribué au partage du poste « " + ok + " » : choisissez un autre port.", "Partage"); return; }
                }
            if (chOn.Checked && chCom.Checked && Current != null && Current.Status == 2) { MessageBox.Show(this, "Le port " + Current.A + " est occupé par un autre programme : choisissez une autre paire.", "Partage"); return; }
            if (chOn.Checked && chCom.Checked && Current == null) { MessageBox.Show(this, "Choisissez une paire de ports COM virtuels (ou créez-en une).", "Partage"); return; }
            if (chOn.Checked && !chCom.Checked && !chTcp.Checked) { MessageBox.Show(this, "Cochez le port COM virtuel et / ou le serveur TCP.", "Partage"); return; }
            Cfg.SetBool(ShareOpts.K(rk, "on"), chOn.Checked); Cfg.SetBool(ShareOpts.K(rk, "comOn"), chCom.Checked); if (Current != null) Cfg.Set(ShareOpts.K(rk, "com"), Current.A); Cfg.SetInt(ShareOpts.K(rk, "baud"), baud); Cfg.SetInt(ShareOpts.K(rk, "stop"), cbStop.Text == "2" ? 2 : 1);
            Cfg.SetBool(ShareOpts.K(rk, "tcpOn"), chTcp.Checked); Cfg.SetInt(ShareOpts.K(rk, "tcpPort"), port); Cfg.SetBool(ShareOpts.K(rk, "net"), chNet.Checked); Cfg.Save();
            if (chOn.Checked && chCom.Checked && Current != null) MessageBox.Show(this, "Au prochain CONNECTER, HamRadioLink ouvrira " + Current.A + ".\nDans OmniRig, OpsLog ou tout autre logiciel, choisissez " + Current.B + ".", "Partage du poste");
            DialogResult = DialogResult.OK; Close();
        }
    }

    // choix des deux numéros de ports de la nouvelle paire
    public class PairDlg : Form
    {
        TextBox ta, tb; public string A, B;
        public PairDlg(string a, string b)
        {
            Text = "Créer une paire de ports COM"; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = MinimizeBox = false; StartPosition = FormStartPosition.CenterParent; BackColor = T.Panel; ClientSize = new Size(520, 250); ShowInTaskbar = false;
            Label l = new Label(); l.Text = "Aucune paire libre. HamRadioLink propose de créer deux ports COM reliés entre eux (modifiez les numéros si besoin). Windows demandera l'autorisation d'administrateur.\n\nHamRadioLink utilisera le premier ; OmniRig, OpsLog ou tout autre logiciel utilisera le second."; l.SetBounds(14, 10, 492, 100); l.ForeColor = T.Dim; l.BackColor = Color.Transparent; Controls.Add(l);
            Label la = new Label(); la.Text = "Port pour HamRadioLink"; la.SetBounds(14, 122, 200, 24); la.ForeColor = T.Text; la.BackColor = Color.Transparent; Controls.Add(la);
            ta = new TextBox(); ta.SetBounds(220, 120, 100, 24); ta.BackColor = T.Panel2; ta.ForeColor = T.Text; ta.BorderStyle = BorderStyle.FixedSingle; ta.Text = a; Controls.Add(ta);
            Label lb = new Label(); lb.Text = "Port pour l'autre logiciel"; lb.SetBounds(14, 156, 200, 24); lb.ForeColor = T.Text; lb.BackColor = Color.Transparent; Controls.Add(lb);
            tb = new TextBox(); tb.SetBounds(220, 154, 100, 24); tb.BackColor = T.Panel2; tb.ForeColor = T.Text; tb.BorderStyle = BorderStyle.FixedSingle; tb.Text = b; Controls.Add(tb);
            Btn ok = new Btn(); ok.Text = "Créer"; ok.Accent = T.Green; ok.SetBounds(290, 204, 100, 30); ok.Click += delegate { Ok(); }; Controls.Add(ok);
            Btn ca = new Btn(); ca.Text = "Annuler"; ca.SetBounds(400, 204, 100, 30); ca.Click += delegate { DialogResult = DialogResult.Cancel; Close(); }; Controls.Add(ca);
        }
        void Ok()
        {
            string a = ta.Text.Trim().ToUpperInvariant(), b = tb.Text.Trim().ToUpperInvariant();
            if (!Com0Com.IsComName(a) || !Com0Com.IsComName(b) || a == b) { MessageBox.Show(this, "Indiquez deux noms de ports différents, de la forme COM22.", "Créer une paire"); return; }
            List<string> used = new List<string>(Com0Com.PortsNow()); foreach (Com0Com.Pair p in Com0Com.Pairs()) { used.Add(p.A); used.Add(p.B); }
            foreach (string u in used) if (u.Equals(a, StringComparison.OrdinalIgnoreCase) || u.Equals(b, StringComparison.OrdinalIgnoreCase)) { MessageBox.Show(this, "Le port " + u + " existe déjà : choisissez d'autres numéros.", "Créer une paire"); return; }
            A = a; B = b; DialogResult = DialogResult.OK; Close();
        }
    }
}
