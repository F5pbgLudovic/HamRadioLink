using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace HamRadioLink.Journal
{
    // Fenêtre de progression NON bloquante d'un traitement long du journal (le reste du programme, télécommande comprise, reste utilisable).
    public class BgTaskForm : Form
    {
        readonly Label lbl; readonly ProgressBar bar; readonly Button btn; volatile bool cancel, running = true; readonly SynchronizationContext ui;
        public BgTaskForm(string title)
        {
            ui = SynchronizationContext.Current; Text = title; ClientSize = new Size(480, 150); StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedSingle; MaximizeBox = false; Font = Ui.Normal; Icon = Ui.AppIcon;
            lbl = Ui.Lbl(this, Tr.T("Traitement en cours…"), 12, 12, 456); lbl.Height = 72; bar = new ProgressBar { Left = 12, Top = 88, Width = 456, Height = 14, Style = ProgressBarStyle.Marquee }; Controls.Add(bar);
            btn = Ui.Btn(this, Tr.T("Annuler"), 12, 112, 120, 28, (s, e) => { if (running) { cancel = true; btn.Enabled = false; lbl.Text = Tr.T("Annulation demandée…"); } else Close(); });
            FormClosing += (s, e) => { if (running) { e.Cancel = true; if (!cancel) { cancel = true; btn.Enabled = false; lbl.Text = Tr.T("Annulation demandée…"); } } };
        }
        public bool Cancelled { get { return cancel; } }
        void Post(Action a) { try { if (ui != null) ui.Post(delegate { try { if (!IsDisposed) a(); } catch { } }, null); } catch { } }
        public void Status(string text, int pct) { Post(delegate { lbl.Text = text; if (pct >= 0) { if (bar.Style != ProgressBarStyle.Continuous) bar.Style = ProgressBarStyle.Continuous; bar.Value = Math.Min(100, pct); } }); }
        public void Finish(string msg) { running = false; Post(delegate { lbl.Text = msg; bar.Style = ProgressBarStyle.Continuous; bar.Value = 100; btn.Enabled = true; btn.Text = Tr.T("Fermer"); }); }
    }

    public static class Bg
    {
        // Lance « work » sur un autre fil avec SA PROPRE connexion à la base du journal courant (la connexion de l'interface n'est pas partagée).
        // work(log, db, say(texte, pourcentage ou -1), annulé?) -> message final. done() est appelé dans le fil de l'interface à la fin.
        public static void Run(Form owner, string title, Func<PeerLog, MySqlClient, Action<string, int>, Func<bool>, string> work, Action done)
        {
            LogRef r = App.CurrentLog; string host, user, pass, schema; int port;
            if (r != null) { App.ServerOf(r, out host, out port, out user, out pass); schema = r.Schema; }
            else { host = App.Cfg.Get("db.host", "127.0.0.1"); port = App.Cfg.GetInt("db.port", 3306); user = App.Cfg.Get("db.user", "root"); pass = App.Cfg.Get("db.pass", ""); schema = App.Cfg.Get("db.schema", "mysql"); }
            BgTaskForm f = new BgTaskForm(title); Ui.CenterOn(f, owner); f.Show(owner); SynchronizationContext ui = SynchronizationContext.Current;
            Thread th = new Thread(delegate()
            {
                string msg; MySqlClient db = null;
                try { db = new MySqlClient(host, port, user, pass, schema); db.Open(); PeerLog lg = new PeerLog(db, schema); msg = work(lg, db, f.Status, delegate { return f.Cancelled; }); }
                catch (Exception ex) { msg = Tr.T("Erreur : ") + ex.Message; }
                finally { try { if (db != null) db.Dispose(); } catch { } }
                f.Finish(msg); if (ui != null) ui.Post(delegate { try { if (done != null) done(); } catch { } }, null);
            }) { IsBackground = true, Name = "journal-traitement", Priority = ThreadPriority.BelowNormal };
            th.Start();
        }
    }
}
