using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace HamRadioLink
{
    // Fenêtre « Serveur de commande » : le premier serveur reste sur le premier poste ; un second serveur (autre port) peut piloter un second poste
    public class ServerDlg : Form
    {
        CheckBox chOn, chNet, ch2; TextBox tbPort, tb2; DarkCombo cb2; Label lbState;
        public static Func<string> State; public static List<string[]> Others = new List<string[]>(), AllRadios = new List<string[]>(); public static string First = ""; DarkCombo cb1; List<string[]> keys1 = new List<string[]>();
        public ServerDlg()
        {
            Text = "Serveur de commande"; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = MinimizeBox = false; StartPosition = FormStartPosition.CenterParent; BackColor = T.Panel; ClientSize = new Size(620, 560); ShowInTaskbar = false;
            Label l = new Label(); l.Text = "HamRadioLink ouvre un serveur de commande IP : WSJT-X, JTDX, JS8Call, SDRLink et les autres logiciels qui savent piloter un poste par un serveur IP pilotent alors le poste choisi dans HamRadioLink (fréquence, mode, émission, split, volume), quel que soit son modèle.\n\nDans le logiciel : choisissez le type de poste qui se connecte à un serveur IP (son nom exact dépend du logiciel), serveur = 127.0.0.1 (ou l'adresse de ce PC), port = celui ci-dessous (7809 par défaut). Si un autre programme occupe déjà ce port, fermez-le ou changez de port.";
            l.SetBounds(14, 10, 592, 120); l.ForeColor = T.Dim; l.BackColor = Color.Transparent; Controls.Add(l);
            chOn = new CheckBox(); chOn.Text = "Activer le serveur de commande"; chOn.SetBounds(14, 136, 580, 24); chOn.ForeColor = T.Text; chOn.BackColor = Color.Transparent; chOn.Checked = Cfg.GetBool("srv.on", false); Controls.Add(chOn);
            Label lp = new Label(); lp.Text = "Port TCP"; lp.SetBounds(34, 168, 70, 24); lp.ForeColor = T.Dim; lp.BackColor = Color.Transparent; Controls.Add(lp);
            tbPort = new TextBox(); tbPort.SetBounds(108, 166, 80, 24); tbPort.BackColor = T.Panel2; tbPort.ForeColor = T.Text; tbPort.BorderStyle = BorderStyle.FixedSingle; tbPort.Text = Cfg.GetInt("srv.port", 7809).ToString(); Controls.Add(tbPort);
            Label n1 = new Label(); n1.Text = "Poste"; n1.SetBounds(206, 168, 46, 24); n1.ForeColor = T.Dim; n1.BackColor = Color.Transparent; Controls.Add(n1);
            cb1 = new DarkCombo(); cb1.SetBounds(254, 166, 340, 26); Controls.Add(cb1); keys1 = AllRadios.Count > 0 ? AllRadios : AllRadiosFallback(); string sv1 = Cfg.Get("srv.radio", First); int sel1 = -1;
            for (int i = 0; i < keys1.Count; i++) { cb1.Items.Add(keys1[i][1]); if (keys1[i][0] == sv1) sel1 = i; } if (sel1 >= 0) cb1.SelectedIndex = sel1; else if (cb1.Items.Count > 0) cb1.SelectedIndex = 0;
            ch2 = new CheckBox(); ch2.Text = "Second serveur (autre port) pour un second poste"; ch2.SetBounds(14, 208, 580, 24); ch2.ForeColor = T.Text; ch2.BackColor = Color.Transparent; ch2.Checked = Cfg.GetBool("srv2.on", false); Controls.Add(ch2);
            Label lr = new Label(); lr.Text = "Poste"; lr.SetBounds(34, 242, 56, 24); lr.ForeColor = T.Dim; lr.BackColor = Color.Transparent; Controls.Add(lr);
            cb2 = new DarkCombo(); cb2.SetBounds(94, 240, 330, 26); Controls.Add(cb2); string saved = Cfg.Get("srv2.radio", ""); int sel = -1;
            List<string[]> list = Others.Count > 0 ? Others : AllRadiosList(); for (int i = 0; i < list.Count; i++) { cb2.Items.Add(list[i][1]); if (list[i][0] == saved) sel = i; } keys = list; if (sel >= 0) cb2.SelectedIndex = sel; else if (cb2.Items.Count > 0) cb2.SelectedIndex = 0;
            Label lp2 = new Label(); lp2.Text = "Port TCP"; lp2.SetBounds(440, 242, 70, 24); lp2.ForeColor = T.Dim; lp2.BackColor = Color.Transparent; Controls.Add(lp2);
            tb2 = new TextBox(); tb2.SetBounds(514, 240, 80, 24); tb2.BackColor = T.Panel2; tb2.ForeColor = T.Text; tb2.BorderStyle = BorderStyle.FixedSingle; tb2.Text = Cfg.GetInt("srv2.port", 7810).ToString(); Controls.Add(tb2);
            Label n2 = new Label(); n2.Text = Others.Count > 0 ? "Choisissez le second poste parmi les postes ouverts. Le logiciel connecté sur ce port pilote ce poste." : "Aucun second poste ouvert pour l'instant : choisissez-le ici, le serveur le pilotera dès qu'il sera connecté (liste Poste, puis CONNECTER).";
            n2.SetBounds(34, 274, 560, 40); n2.ForeColor = T.Dim; n2.BackColor = Color.Transparent; Controls.Add(n2);
            chNet = new CheckBox(); chNet.Text = "Accessible depuis le réseau local (sinon : cet ordinateur seulement)"; chNet.SetBounds(14, 326, 580, 24); chNet.ForeColor = T.Text; chNet.BackColor = Color.Transparent; chNet.Checked = Cfg.GetBool("srv.net", false); Controls.Add(chNet);
            lbState = new Label(); lbState.SetBounds(14, 366, 592, 90); lbState.ForeColor = T.Cyan; lbState.BackColor = Color.Transparent; lbState.Text = State != null ? State() : ""; Controls.Add(lbState);
            Label lk = new Label(); lk.Text = "Les verrous (clic droit) et la touche Lock protègent seulement l'interface : le logiciel connecté garde la main."; lk.SetBounds(14, 466, 592, 22); lk.ForeColor = T.Dim; lk.BackColor = Color.Transparent; Controls.Add(lk);
            Btn ok = new Btn(); ok.Text = "OK"; ok.Accent = T.Green; ok.SetBounds(400, 516, 100, 30); ok.Click += delegate { Save(); }; Controls.Add(ok);
            Btn ca = new Btn(); ca.Text = "Annuler"; ca.SetBounds(510, 516, 100, 30); ca.Click += delegate { DialogResult = DialogResult.Cancel; Close(); }; Controls.Add(ca);
        }
        List<string[]> keys = new List<string[]>();
        static List<string[]> AllRadiosFallback() { return AllRadiosList(); }
        static List<string[]> AllRadiosList() { List<string[]> l = new List<string[]>(); foreach (RadioDef r in Radios.All()) l.Add(new string[] { r.Key, CmdServer.ModelName(r) }); return l; }
        void Save()
        {
            int port, port2; if (!int.TryParse(tbPort.Text.Trim(), out port) || port < 1024 || port > 65535) { MessageBox.Show(this, "Port TCP invalide (1024 à 65535).", "Serveur de commande"); return; }
            if (!int.TryParse(tb2.Text.Trim(), out port2) || port2 < 1024 || port2 > 65535) { MessageBox.Show(this, "Port TCP du second serveur invalide (1024 à 65535).", "Serveur de commande"); return; }
            if (ch2.Checked && port2 == port) { MessageBox.Show(this, "Les deux serveurs doivent avoir des ports différents.", "Serveur de commande"); return; }
            if (ch2.Checked && cb2.SelectedIndex < 0) { MessageBox.Show(this, "Choisissez le poste du second serveur.", "Serveur de commande"); return; }
            if (cb1.SelectedIndex >= 0 && cb1.SelectedIndex < keys1.Count) { string k1 = keys1[cb1.SelectedIndex][0]; if (ch2.Checked && cb2.SelectedIndex >= 0 && cb2.SelectedIndex < keys.Count && keys[cb2.SelectedIndex][0] == k1) { MessageBox.Show(this, "Les deux serveurs doivent piloter des postes différents.", "Serveur de commande"); return; } Cfg.Set("srv.radio", k1); }
            Cfg.SetBool("srv.on", chOn.Checked); Cfg.SetInt("srv.port", port); Cfg.SetBool("srv.net", chNet.Checked);
            Cfg.SetBool("srv2.on", ch2.Checked); Cfg.SetInt("srv2.port", port2); if (cb2.SelectedIndex >= 0 && cb2.SelectedIndex < keys.Count) Cfg.Set("srv2.radio", keys[cb2.SelectedIndex][0]); Cfg.Save(); DialogResult = DialogResult.OK; Close();
        }
    }
}
