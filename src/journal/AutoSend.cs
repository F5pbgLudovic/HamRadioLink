using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace HamRadioLink.Journal
{
    // Fenêtre « Envoyer à tous les logs en ligne » : choix des services (coche) et date de départ (jusqu'à aujourd'hui).
    public class SendAllDlg : Form
    {
        readonly List<CheckBox> boxes = new List<CheckBox>(); readonly List<string> keys = new List<string>();
        readonly DateTimePicker dp; readonly CheckBox cSkip, cSel;
        public List<string> Keys = new List<string>(); public DateTime From; public bool SkipSent, OnlySel;

        public SendAllDlg(List<SendSvc> avail, List<string> notSet, int selCount)
        {
            AutoScaleMode = AutoScaleMode.None; Text = Tr.T("Envoyer à tous les logs en ligne"); FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; StartPosition = FormStartPosition.CenterParent; Font = Ui.Normal; ShowInTaskbar = false;
            int y = 12; Ui.Lbl(this, Tr.T("Services à utiliser (décochez ceux à ignorer) :"), 12, y, 420); y += 28;
            foreach (SendSvc s in avail) { CheckBox c = new CheckBox { Text = s.Name, Left = 28, Top = y, Width = 380, Checked = true }; Controls.Add(c); boxes.Add(c); keys.Add(s.Key); y += 24; }
            foreach (string n in notSet) { CheckBox c = new CheckBox { Text = n + Tr.T(" (non réglé : Outils > Online services)"), Left = 28, Top = y, Width = 400, Checked = false, Enabled = false }; Controls.Add(c); y += 24; }
            y += 10; Ui.Lbl(this, Tr.T("Envoyer les QSO à partir du :"), 12, y, 200);
            dp = new DateTimePicker { Left = 216, Top = y, Width = 130, Format = DateTimePickerFormat.Short }; Controls.Add(dp);
            DateTime d; if (!DateTime.TryParse(App.Cfg.Get("sendall.date", ""), out d)) d = DateTime.Today.AddDays(-7); if (d > DateTime.Today) d = DateTime.Today; dp.Value = d; dp.MaxDate = DateTime.Today;
            Ui.Lbl(this, Tr.T("jusqu'à aujourd'hui"), 356, y, 120); y += 32;
            cSkip = new CheckBox { Text = Tr.T("Ignorer les QSO déjà marqués envoyés à chaque service"), Left = 12, Top = y, Width = 460, Checked = true }; Controls.Add(cSkip); y += 26;
            cSel = new CheckBox { Text = Tr.T("Seulement les ") + selCount + Tr.T(" QSO sélectionnés (ignorer la date)"), Left = 12, Top = y, Width = 460, Enabled = selCount > 0 }; Controls.Add(cSel); y += 36;
            Button ok = Ui.Btn(this, Tr.T("Envoyer"), 290, y, 90, 28, (s, e) => Go()); Ui.Btn(this, Tr.T("Annuler"), 390, y, 90, 28, (s, e) => { DialogResult = DialogResult.Cancel; Close(); });
            ClientSize = new Size(492, y + 42); AcceptButton = ok;
        }
        void Go()
        {
            for (int i = 0; i < boxes.Count; i++) if (boxes[i].Checked) Keys.Add(keys[i]);
            if (Keys.Count == 0) { MessageBox.Show(this, Tr.T("Cochez au moins un service."), Tr.T("Envoi")); return; }
            From = dp.Value.Date; SkipSent = cSkip.Checked; OnlySel = cSel.Checked;
            App.Cfg.Set("sendall.date", From.ToString("yyyy-MM-dd")); App.Cfg.Save();
            DialogResult = DialogResult.OK; Close();
        }
    }

    // Fenêtre « Envoi automatique » : activation, services, délai.
    public class AutoSendDlg : Form
    {
        readonly CheckBox on; readonly List<CheckBox> boxes = new List<CheckBox>(); readonly List<string> keys = new List<string>(); readonly ComboBox cd;
        public AutoSendDlg(LogbookForm owner)
        {
            AutoScaleMode = AutoScaleMode.None; Text = Tr.T("Envoi automatique"); FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; StartPosition = FormStartPosition.CenterParent; Font = Ui.Normal; ShowInTaskbar = false;
            int y = 12;
            on = new CheckBox { Text = Tr.T("Activer l'envoi automatique des nouveaux contacts (saisis ici ou reçus de WSJT-X…)"), Left = 12, Top = y, Width = 520, Checked = App.Cfg.Get("auto.on", "0") == "1" }; Controls.Add(on); y += 32;
            Ui.Lbl(this, Tr.T("Services :"), 12, y, 200); y += 24;
            List<string> sk = new List<string>(); List<SendSvc> avail = owner.BuildSvcs(null, sk, true); List<string> sel = new List<string>(App.Cfg.Get("auto.svc", "").Split(','));
            bool none = App.Cfg.Get("auto.svc", "") == "";
            foreach (SendSvc s in avail) { CheckBox c = new CheckBox { Text = s.Name, Left = 28, Top = y, Width = 380, Checked = none || sel.Contains(s.Key) }; Controls.Add(c); boxes.Add(c); keys.Add(s.Key); y += 24; }
            foreach (string n in sk) { CheckBox c = new CheckBox { Text = n + Tr.T(" (non réglé)"), Left = 28, Top = y, Width = 400, Enabled = false }; Controls.Add(c); y += 24; }
            y += 10; Ui.Lbl(this, Tr.T("Envoyer :"), 12, y, 70);
            cd = Ui.Combo(this, 90, y - 2, 330, AutoSend.DelayNames); cd.DropDownStyle = ComboBoxStyle.DropDownList; cd.SelectedIndex = AutoSend.DelayIndex(App.Cfg.GetInt("auto.delay", 0)); y += 36;
            string last = App.Cfg.Get("auto.last", ""); string st = App.Cfg.Get("auto.status", "");
            Label info = Ui.Lbl(this, Tr.T("Dernier envoi automatique : ") + (last.Length > 0 ? last : Tr.T("jamais")) + (st.Length > 0 ? "\r\n" + st : "") + "\r\n" + Tr.T("Seuls les nouveaux contacts sont envoyés ; un envoi interrompu reprend au prochain démarrage, sans repartir du début du journal."), 12, y, 520); info.Height = 80; y += 88;
            Button ok = Ui.Btn(this, "OK", 340, y, 90, 28, (s, e) => Save()); Ui.Btn(this, Tr.T("Annuler"), 440, y, 90, 28, (s, e) => { DialogResult = DialogResult.Cancel; Close(); });
            ClientSize = new Size(544, y + 42); AcceptButton = ok;
        }
        void Save()
        {
            List<string> k = new List<string>(); for (int i = 0; i < boxes.Count; i++) if (boxes[i].Checked) k.Add(keys[i]);
            App.Cfg.Set("auto.on", on.Checked ? "1" : "0"); App.Cfg.Set("auto.svc", k.Count > 0 ? string.Join(",", k.ToArray()) : "-"); App.Cfg.Set("auto.delay", AutoSend.DelayVals[Math.Max(0, cd.SelectedIndex)].ToString()); App.Cfg.Save();
            AutoSend.Changed(); DialogResult = DialogResult.OK; Close();
        }
    }

    // Envoi automatique : surveille la table du journal (nouveaux contacts, quelle qu'en soit l'origine) et les envoie aux services cochés.
    // Mémoire : n° du dernier contact traité (par journal) + heure du premier contact en attente + heure du dernier envoi, dans settings.ini :
    // un redémarrage reprend où l'on s'était arrêté, sans repartir du début du journal.
    public static class AutoSend
    {
        public static readonly string[] DelayNames = { "Envoi instantané", "Envoi après 30 minutes", "Envoi après 1 heure", "Envoi après 2 heures", "Envoi à la fermeture du log" };
        public static readonly int[] DelayVals = { 0, 30, 60, 120, -1 };   // minutes ; -1 = à la fermeture du log
        public static int DelayIndex(int v) { for (int i = 0; i < DelayVals.Length; i++) if (DelayVals[i] == v) return i; return 0; }

        static System.Windows.Forms.Timer tm; static LogbookForm owner; static volatile bool busy; static DateTime nextTry = DateTime.MinValue; static bool startup = true;

        public static void Attach(LogbookForm f)
        {
            owner = f;
            if (tm == null) { tm = new System.Windows.Forms.Timer(); tm.Interval = 15000; tm.Tick += delegate { Tick(false, false); }; tm.Start(); }
            startup = true;
        }
        public static void ShowDialog(LogbookForm f) { Ui.ShowFree(new AutoSendDlg(f), f); }
        public static void Changed() { nextTry = DateTime.MinValue; }
        public static void OnLogClosed() { Tick(true, false); }     // fenêtre du journal fermée
        public static void OnQuit() { Tick(true, true); }           // arrêt du programme : on attend la fin (25 s au plus)

        static string Nm() { return App.CurrentLog != null ? App.CurrentLog.Name : "log"; }

        static void Tick(bool trigger, bool wait)
        {
            try
            {
                if (busy || owner == null || owner.IsDisposed || App.Log == null || App.Db == null || App.Cfg.Get("auto.on", "0") != "1") return;
                if (DateTime.UtcNow < nextTry) return;
                string wk = "auto.wm." + Nm(), sk = "auto.since." + Nm(); long wm;
                if (!long.TryParse(App.Cfg.Get(wk, ""), out wm))
                {   // première activation sur ce journal : on part du dernier contact existant (on n'envoie pas tout l'historique)
                    string mx = App.Db.Scalar("SELECT MAX(COL_PRIMARY_KEY) FROM " + App.Log.TableRef()); long.TryParse(mx ?? "", out wm);
                    App.Cfg.Set(wk, wm.ToString()); App.Cfg.Save(); return;
                }
                List<Qso> L = new List<Qso>();
                for (int off = 0; off < 5000; off += 1000) { List<Qso> p = App.Log.Fetch("COL_PRIMARY_KEY > " + wm, "COL_PRIMARY_KEY ASC", off, 1000); L.AddRange(p); if (p.Count < 1000) break; }
                if (L.Count == 0) { if (App.Cfg.Get(sk, "") != "") { App.Cfg.Set(sk, ""); App.Cfg.Save(); } startup = false; return; }
                long sinceTicks; DateTime since;
                if (!long.TryParse(App.Cfg.Get(sk, ""), out sinceTicks)) { since = DateTime.UtcNow; App.Cfg.Set(sk, since.Ticks.ToString()); App.Cfg.Save(); } else since = new DateTime(sinceTicks, DateTimeKind.Utc);
                int delay = App.Cfg.GetInt("auto.delay", 0);
                bool due = delay == 0 || (delay > 0 && (DateTime.UtcNow - since).TotalMinutes >= delay) || (delay < 0 && (trigger || startup));   // « à la fermeture » : aussi au démarrage, pour reprendre un envoi interrompu
                startup = false;
                if (!due) return;
                Run(L, wk, sk, wait);
            }
            catch (Exception ex) { try { App.Cfg.Set("auto.status", DateTime.Now.ToString("dd/MM HH:mm") + " : " + ex.Message); } catch { } nextTry = DateTime.UtcNow.AddMinutes(5); }
        }

        static void Run(List<Qso> L, string wk, string sk, bool wait)
        {
            List<string> skipped = new List<string>(); List<SendSvc> avail = owner.BuildSvcs(null, skipped, true);
            List<string> keys = new List<string>(App.Cfg.Get("auto.svc", "").Split(',')); bool all = App.Cfg.Get("auto.svc", "") == "";
            List<SendSvc> sv = avail.FindAll(x => all || keys.Contains(x.Key));
            if (sv.Count == 0) { App.Cfg.Set("auto.status", DateTime.Now.ToString("dd/MM HH:mm") + Tr.T(" : aucun service coché et réglé")); App.Cfg.Save(); nextTry = DateTime.UtcNow.AddMinutes(5); return; }
            foreach (SendSvc x in sv) { string k = x.Key; x.Skip = q => LogbookForm.IsSentTo(k, q); }
            long maxId = 0; foreach (Qso q in L) if (q.Id > maxId) maxId = q.Id;
            if (!SendAllForm.TryAcquire()) return;   // un envoi manuel (ou un autre) est en cours : l'envoi automatique est simplement décalé, il sera retenté au prochain passage (15 s)
            busy = true; LogbookForm o = owner; bool quit = wait;
            Thread th = new Thread(delegate()
            {
                string err = null; int sent = 0;
                try
                {
                    foreach (SendSvc s in sv)
                    {
                        List<Qso> mine = L.FindAll(q => !s.Skip(q)); int bsz = Math.Max(1, Math.Min(s.Batch, SendAllForm.SessionSize));
                        for (int i = 0; i < mine.Count; i += bsz)
                        {
                            if (i > 0) { int pause = s.PauseMs; if (i % SendAllForm.SessionSize == 0) pause = Math.Max(pause, SendAllForm.SessionPauseMs); if (quit && pause > 10000) { err = s.Name + Tr.T(" : reste à envoyer (reprise au prochain démarrage)"); break; } Thread.Sleep(pause); }
                            List<Qso> part = mine.GetRange(i, Math.Min(bsz, mine.Count - i)); SendSvc ss = s;
                            try { s.Fn(part); }
                            catch (Exception ex) { err = s.Name + " : " + ex.Message; break; }
                            sent += part.Count;
                            try { o.Invoke(new Action(() => o.MarkSentTo(ss, part))); } catch { err = err ?? Tr.T("marquage impossible"); break; }
                        }
                    }
                }
                catch (Exception ex) { err = ex.Message; }
                string e2 = err; int n = sent;
                try { o.BeginInvoke(new Action(() => Done(e2, n, maxId, wk, sk))); } catch { busy = false; SendAllForm.Release(); }
            });
            th.IsBackground = true; th.Name = "envoi-auto"; th.Start();
            if (wait) { DateTime t0 = DateTime.UtcNow; while (busy && (DateTime.UtcNow - t0).TotalSeconds < 25) { Application.DoEvents(); Thread.Sleep(50); } }
        }

        static void Done(string err, int sent, long maxId, string wk, string sk)
        {
            busy = false; SendAllForm.Release();
            try
            {
                string now = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                if (err == null) { App.Cfg.Set(wk, maxId.ToString()); App.Cfg.Set(sk, ""); App.Cfg.Set("auto.last", now); App.Cfg.Set("auto.status", sent + Tr.T(" envoi(s) réussi(s) à ") + now); nextTry = DateTime.MinValue; }
                else { App.Cfg.Set("auto.status", now + " : " + err + Tr.T(" (nouvel essai dans 5 minutes)")); nextTry = DateTime.UtcNow.AddMinutes(5); }
                App.Cfg.Save(); if (owner != null && !owner.IsDisposed) owner.AfterAutoSend();
            }
            catch { }
        }
    }
}
