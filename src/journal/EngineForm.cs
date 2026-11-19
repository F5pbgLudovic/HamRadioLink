using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace HamRadioLink.Journal
{
    // Fenêtre « MariaDB engine » : version du moteur intégré, recherche et installation des mises à jour (moteur séparé des données).
    public class EngineForm : Form
    {
        readonly Label lblVer; readonly TextBox log; readonly Button bCheck, bUpdate, bBackups; Embedded.Release latest; volatile bool busy;

        public EngineForm()
        {
            Text = "MariaDB engine"; Font = Ui.Normal; Icon = Ui.AppIcon; ClientSize = new Size(620, 420); StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
            bool emb = App.Cfg.Get("db.mode", "") == "embedded";
            lblVer = new Label { Left = 14, Top = 12, Width = 590, Height = 70 }; Controls.Add(lblVer);
            bCheck = Ui.Btn(this, "Check for updates", 14, 90, 150, 28, (s, e) => Check()); bUpdate = Ui.Btn(this, "Update now", 172, 90, 120, 28, (s, e) => Update()); bUpdate.Enabled = false;
            bBackups = Ui.Btn(this, "Backups folder", 300, 90, 120, 28, (s, e) => { try { System.IO.Directory.CreateDirectory(Embedded.BackupDir); System.Diagnostics.Process.Start("explorer.exe", Embedded.BackupDir); } catch { } });
            log = new TextBox { Left = 14, Top = 128, Width = 590, Height = 280, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Font = Ui.Mono }; Controls.Add(log);
            if (!emb) { lblVer.Text = "Ce journal utilise le MariaDB installé sur l'ordinateur (" + (Embedded.SystemServer() ?? "serveur externe") + ").\r\nSa mise à jour se fait avec son propre installeur : HamRadioLink n'y touche pas."; bCheck.Enabled = false; bBackups.Enabled = false; }
            else { string v = Embedded.InstalledVersion(); lblVer.Text = "MariaDB intégré à HamRadioLink — version installée : " + (v.Length > 0 ? v : "inconnue") + "\r\nMoteur : " + Embedded.EngineDir + "\r\nDonnées : " + Embedded.DataDir + "  (jamais touchées par une mise à jour sans sauvegarde)"; }
        }

        void L(string s) { try { BeginInvoke(new Action(() => log.AppendText(s + "\r\n"))); } catch { } }

        void Check()
        {
            if (busy) return; busy = true; bCheck.Enabled = false; L("Recherche de la dernière version sur mariadb.org ...");
            new Thread(() =>
            {
                try
                {
                    string v = Embedded.InstalledVersion(); latest = Embedded.LatestInBranch(v);
                    if (latest == null) L("Aucune information reçue.");
                    else if (Embedded.CompareVersions(latest.Version, v) > 0) { L("Mise à jour disponible : " + v + " -> " + latest.Version + " (SHA-256 officiel vérifié au téléchargement)."); BeginInvoke(new Action(() => bUpdate.Enabled = true)); }
                    else L("Vous avez la dernière version de la branche (" + v + ").");
                    string nb = Embedded.NewerBranch(v); if (nb != null) L("Une branche stable plus récente existe (" + nb + ") : elle n'est pas installée automatiquement (changement de format possible).");
                    App.Cfg.Set("emb.lastcheck", DateTime.UtcNow.ToString("yyyy-MM-dd")); App.Cfg.Set("emb.update.available", latest != null && Embedded.CompareVersions(latest.Version, v) > 0 ? latest.Version : ""); App.Cfg.Save();
                }
                catch (Exception ex) { L("Erreur : " + ex.Message); }
                busy = false; try { BeginInvoke(new Action(() => bCheck.Enabled = true)); } catch { }
            }) { IsBackground = true }.Start();
        }

        void Update()
        {
            if (busy || latest == null) return;
            if (MessageBox.Show(this, "Le journal va être fermé quelques minutes. Une sauvegarde complète des données est faite avant (dossier backups), et l'ancien moteur est remis automatiquement en cas de problème.\r\n\r\nMettre à jour vers MariaDB " + latest.Version + " ?", "HamRadioLink", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            busy = true; bUpdate.Enabled = false; bCheck.Enabled = false;
            new Thread(() =>
            {
                try
                {
                    try { if (App.Db != null) App.Db.Dispose(); } catch { }
                    Embedded.UpdateEngine(latest, L);
                    App.ConnectDb(); L("Journal reconnecté."); App.Cfg.Set("emb.update.available", ""); App.Cfg.Save();
                }
                catch (Exception ex) { L("ERREUR : " + ex.Message); try { App.ConnectDb(); } catch { } }
                busy = false; try { BeginInvoke(new Action(() => { bCheck.Enabled = true; lblVer.Text = lblVer.Text.Split('\r')[0].Split('—')[0] + "— version installée : " + Embedded.InstalledVersion(); })); } catch { }
            }) { IsBackground = true }.Start();
        }
    }
}
