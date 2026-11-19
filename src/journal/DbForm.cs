using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace HamRadioLink.Journal
{
    // « Ouvrir une base » : choix du serveur MariaDB / MySQL et de la base du journal, avec tous les essais utiles
    // (un MariaDB est-il installé ? le moteur intégré est-il là et démarré ? le port répond-il ? la base et la table du journal existent-elles ?).
    public class DbForm : Form
    {
        TextBox host, port, user, pass, schema, name, call, grid, report; Button btnTest, btnGo; volatile bool busy;

        public DbForm()
        {
            Text = "Ouvrir une base de journal"; ClientSize = new Size(560, 600); StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; Font = Ui.Normal; Icon = Ui.AppIcon;
            int y = 14;
            Ui.Lbl(this, "Serveur MariaDB / MySQL :", 12, y); host = Ui.Txt(this, 190, y - 2, 200); host.Text = App.Cfg.Get("db.host", "127.0.0.1");
            Ui.Lbl(this, "Port :", 400, y); port = Ui.Txt(this, 440, y - 2, 60); port.Text = App.Cfg.Get("db.port", "3306"); y += 32;
            Ui.Lbl(this, "Utilisateur :", 12, y); user = Ui.Txt(this, 190, y - 2, 310); user.Text = App.Cfg.Get("db.user", "root"); y += 32;
            Ui.Lbl(this, "Mot de passe :", 12, y); pass = Ui.Txt(this, 190, y - 2, 310); pass.UseSystemPasswordChar = true; pass.Text = App.Cfg.Get("db.pass", ""); y += 32;
            Ui.Lbl(this, "Base (schéma) :", 12, y); schema = Ui.Txt(this, 190, y - 2, 310); schema.Text = App.Cfg.Get("db.schema", "hamradiolink"); y += 32;
            Ui.Lbl(this, "Nom du journal :", 12, y); name = Ui.Txt(this, 190, y - 2, 310); name.Text = App.Cfg.Get("db.name", App.Cfg.Get("me.call", "Journal")); y += 36;
            Ui.Lbl(this, "Mon indicatif :", 12, y); call = Ui.Txt(this, 190, y - 2, 110); call.Text = App.Cfg.Get("me.call", "");
            Ui.Lbl(this, "Mon locator :", 320, y); grid = Ui.Txt(this, 410, y - 2, 90); grid.Text = App.Cfg.Get("me.grid", ""); y += 38;
            btnTest = Ui.Btn(this, "Tester", 12, y, 100, 28, (s, e) => Test());
            Ui.Btn(this, "Moteur intégré…", 120, y, 130, 28, (s, e) => { using (EngineForm f = new EngineForm()) f.ShowDialog(this); });
            y += 36;
            report = new TextBox { Left = 12, Top = y, Width = 536, Height = 600 - y - 50, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Font = Ui.Mono }; Controls.Add(report);
            btnGo = Ui.Btn(this, "Se connecter", 340, 600 - 40, 110, 28, (s, e) => Go());
            Ui.Btn(this, "Annuler", 458, 600 - 40, 90, 28, (s, e) => { DialogResult = DialogResult.Cancel; Close(); });
            AcceptButton = btnGo;
            Shown += (s, e) => Test();
        }

        void Say(string t) { try { BeginInvoke(new Action(() => report.AppendText(t + "\r\n"))); } catch { } }

        // Les essais : un à un, chacun dit ce qu'il a trouvé. Rien n'est modifié (lecture seule).
        void Test()
        {
            if (busy) return; busy = true; btnTest.Enabled = false; report.Text = "";
            string h = host.Text.Trim(), u = user.Text.Trim(), p = pass.Text, sc = schema.Text.Trim(); int pt; if (!int.TryParse(port.Text.Trim(), out pt)) pt = 3306;
            new Thread(() =>
            {
                try
                {
                    string sys = Embedded.SystemServer();
                    Say("1. MariaDB / MySQL installé sur cet ordinateur : " + (sys ?? "non (HamRadioLink peut utiliser son propre MariaDB intégré)"));
                    bool eng = Embedded.EnginePresent;
                    Say("2. Moteur MariaDB intégré à HamRadioLink : " + (eng ? "présent" + (Embedded.DataReady ? ", journal intégré créé" : ", données pas encore créées") : "absent") + (App.Cfg.Get("db.mode", "") == "embedded" ? "  (c'est le mode actuellement utilisé)" : ""));
                    bool open = Embedded.PortOpen(pt) && (h == "127.0.0.1" || h.ToLowerInvariant() == "localhost");
                    Say("3. Port " + pt + " sur " + h + " : " + (h == "127.0.0.1" || h.ToLowerInvariant() == "localhost" ? (open ? "un serveur répond" : "personne n'écoute (serveur arrêté ou autre port ?)") : "serveur distant, essai de connexion directement"));
                    if (!open && (h == "127.0.0.1" || h.ToLowerInvariant() == "localhost")) { Say("→ Pas de serveur sur ce port : démarrez MariaDB, changez le port, ou utilisez l'assistant (bouton « Importer / partager… » du journal) pour installer le MariaDB intégré."); return; }
                    using (MySqlClient c = new MySqlClient(h, pt, u, p, "mysql"))
                    {
                        try { c.Open(); } catch (Exception ex) { Say("4. Connexion : ÉCHEC — " + ex.Message + "\r\n→ Vérifiez l'utilisateur et le mot de passe."); return; }
                        string ver = ""; try { ver = c.Scalar("SELECT VERSION()"); } catch { }
                        Say("4. Connexion : réussie — serveur " + ver);
                        if (sc.Length == 0) { Say("5. Indiquez le nom de la base (schéma)."); return; }
                        string ex2 = c.Scalar("SELECT COUNT(*) FROM information_schema.schemata WHERE schema_name=" + MySqlClient.Quote(sc));
                        if (ex2 != "1") { Say("5. Base « " + sc + " » : n'existe pas encore — elle sera créée à la connexion (avec la table du journal)."); return; }
                        Say("5. Base « " + sc + " » : existe.");
                        string tb = c.Scalar("SELECT COUNT(*) FROM information_schema.tables WHERE table_schema=" + MySqlClient.Quote(sc) + " AND table_name=" + MySqlClient.Quote(PeerLog.Table));
                        if (tb != "1") { Say("6. Table du journal : absente — elle sera créée à la connexion (structure compatible, 122 colonnes)."); return; }
                        string n = c.Scalar("SELECT COUNT(*) FROM " + MySqlClient.Ident(sc) + "." + MySqlClient.Ident(PeerLog.Table));
                        string cols = c.Scalar("SELECT COUNT(*) FROM information_schema.columns WHERE table_schema=" + MySqlClient.Quote(sc) + " AND table_name=" + MySqlClient.Quote(PeerLog.Table));
                        string pk = c.Scalar("SELECT COUNT(*) FROM information_schema.columns WHERE table_schema=" + MySqlClient.Quote(sc) + " AND table_name=" + MySqlClient.Quote(PeerLog.Table) + " AND column_name='COL_PRIMARY_KEY'");
                        Say("6. Table du journal : présente — " + n + " QSO, " + cols + " colonnes" + (pk == "1" ? " (structure reconnue)." : " : STRUCTURE NON RECONNUE (colonne COL_PRIMARY_KEY absente)."));
                        Say("\r\nTout est prêt : cliquez sur « Se connecter ».");
                    }
                }
                catch (Exception ex) { Say("Erreur : " + ex.Message); }
                finally { busy = false; try { BeginInvoke(new Action(() => btnTest.Enabled = true)); } catch { } }
            }) { IsBackground = true }.Start();
        }

        void Go()
        {
            App.Cfg.Set("db.host", host.Text.Trim()); App.Cfg.Set("db.port", port.Text.Trim()); App.Cfg.Set("db.user", user.Text.Trim()); App.Cfg.Set("db.pass", pass.Text);
            App.Cfg.Set("db.schema", schema.Text.Trim()); App.Cfg.Set("db.name", name.Text.Trim()); App.Cfg.Set("me.call", call.Text.Trim().ToUpperInvariant()); App.Cfg.Set("me.grid", grid.Text.Trim().ToUpperInvariant());
            if (App.Cfg.Get("db.mode", "") == "") App.Cfg.Set("db.mode", "external");
            try
            {
                Cursor = Cursors.WaitCursor;
                using (MySqlClient c = new MySqlClient(host.Text.Trim(), int.Parse(port.Text.Trim()), user.Text.Trim(), pass.Text, "mysql")) { c.Open(); LogSetup.EnsureTable(c, schema.Text.Trim(), delegate(string s) { Say(s); }); }
                if (App.Logs.Count == 0) App.LoadLogs(); string nm = name.Text.Trim().Length > 0 ? name.Text.Trim() : schema.Text.Trim(); LogRef r = App.Logs.Find(x => x.Name == nm); if (r == null) { r = new LogRef { Name = nm }; App.Logs.Add(r); }
                r.Host = host.Text.Trim(); r.Port = int.Parse(port.Text.Trim()); r.User = user.Text.Trim(); r.Pass = pass.Text; r.Schema = schema.Text.Trim(); LogRef.SaveAll(App.Logs);
                App.ConnectTo(r); Cursor = Cursors.Default; App.Cfg.Set("setup.done", true); App.Cfg.Save(); DialogResult = DialogResult.OK; Close();
            }
            catch (Exception ex) { Cursor = Cursors.Default; Ui.Err(this, ex); }
        }
    }
}
