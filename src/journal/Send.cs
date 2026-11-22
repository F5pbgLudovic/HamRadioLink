using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace HamRadioLink.Journal
{
    // Un service en ligne à alimenter (envoi par lots)
    public class SendSvc
    {
        public string Key, Name; public Func<List<Qso>, string> Fn; public int Batch, PauseMs; public Func<Qso, bool> Skip; public string Mark;   // Mark : "eqsl" / "lotw" / "clublog" = état « envoyé » à mettre à jour
        public Action<SendSvc, string, List<Qso>> OnFail;   // OnFail(service, message d'erreur, QSO restant à envoyer) : appelé dans le fil de l'interface
        public List<Qso> Done = new List<Qso>(); public bool Failed; public ProgressBar Bar; public Label Lbl;
    }

    // « Envoyer à tous les logs en ligne » : fenêtre NON bloquante qui montre la progression de chaque service ; l'envoi tourne sur un autre fil.
    public class SendAllForm : Form
    {
        readonly List<SendSvc> svcs; readonly List<Qso> qsos; volatile bool cancel, running; readonly TextBox log; readonly Button bCancel; readonly Label head;
        readonly SynchronizationContext ui; public Action<List<SendSvc>> Finished; public Action<SendSvc, List<Qso>> BatchDone;   // appelé dans le fil de l'interface, même si la fenêtre est fermée

        public SendAllForm(List<SendSvc> s, List<Qso> q)
        {
            svcs = s; qsos = q; ui = SynchronizationContext.Current;
            Text = Tr.T("Envoi aux logs en ligne"); ClientSize = new Size(560, 150 + 34 * s.Count + 170); StartPosition = FormStartPosition.CenterParent; Font = Ui.Normal; Icon = Ui.AppIcon; FormBorderStyle = FormBorderStyle.FixedSingle; MaximizeBox = false; ShowInTaskbar = true;
            head = Ui.Lbl(this, qsos.Count + Tr.T(" QSO à envoyer. Vous pouvez continuer à utiliser le journal pendant l'envoi."), 12, 12, 536); head.Height = 36;
            int y = 56;
            foreach (SendSvc v in svcs)
            {
                v.Lbl = Ui.Lbl(this, v.Name + " : en attente", 12, y, 536); v.Bar = new ProgressBar { Left = 12, Top = y + 20, Width = 536, Height = 12, Minimum = 0, Maximum = Math.Max(1, qsos.Count) }; Controls.Add(v.Bar); y += 42;
            }
            log = new TextBox { Left = 12, Top = y + 4, Width = 536, Height = 150, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Font = Ui.Mono }; Controls.Add(log);
            bCancel = Ui.Btn(this, Tr.T("Annuler l'envoi"), 12, y + 164, 150, 28, (a, b) => { if (running) { if (MessageBox.Show(this, Tr.T("Des envois sont encore en cours. Interrompre les transferts non terminés ?"), Tr.T("Envoi"), MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return; cancel = true; bCancel.Enabled = false; Say(Tr.T("Annulation demandée : le lot en cours se termine…")); } else Close(); });
            ClientSize = new Size(560, y + 204);
            FormClosing += (a, b) => { if (running) { if (MessageBox.Show(this, Tr.T("Des envois sont encore en cours. Interrompre les transferts non terminés ?"), Tr.T("Envoi"), MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) { b.Cancel = true; return; } cancel = true; } };
        }

        public void Start() { running = true; Thread th = new Thread(Run) { IsBackground = true, Name = "envoi-en-ligne", Priority = ThreadPriority.BelowNormal }; th.Start(); }

        void Post(Action a) { try { if (ui != null) ui.Post(delegate { try { a(); } catch { } }, null); } catch { } }
        void Say(string t) { Post(delegate { if (!IsDisposed) log.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + t + "\r\n"); }); }

        static readonly object gate = new object(); static bool gateBusy;
        public static bool TryAcquire() { lock (gate) { if (gateBusy) return false; gateBusy = true; return true; } }
        public static void Release() { lock (gate) gateBusy = false; }
        public static bool InUse { get { lock (gate) return gateBusy; } }
        public const int SessionSize = 100, SessionPauseMs = 3000;   // envoi par sessions de 100 QSO, 3 s de pause entre deux sessions
        void Run()
        {
            bool acquired = false; bool told = false;   // un seul envoi à la fois (manuel ou automatique) : les autres attendent leur tour, sans conflit
            while (!cancel) { if (TryAcquire()) { acquired = true; break; } if (!told) { told = true; Say(Tr.T("Un autre envoi (automatique ou manuel) est en cours : celui-ci démarrera ensuite.")); Post(delegate { if (!IsDisposed) head.Text = Tr.T("En attente de la fin d'un autre envoi…"); }); } Thread.Sleep(1000); }
            if (acquired && told) Post(delegate { if (!IsDisposed) head.Text = qsos.Count + Tr.T(" QSO à envoyer. Vous pouvez continuer à utiliser le journal pendant l'envoi."); });
            foreach (SendSvc s in svcs)
            {
                if (cancel) break; SendSvc sv = s; Post(delegate { if (!IsDisposed) sv.Lbl.Text = sv.Name + " : envoi en cours…"; }); Say(s.Name + " : début");
                List<Qso> mine = s.Skip == null ? qsos : qsos.FindAll(x => !s.Skip(x));
                int tot = Math.Max(1, mine.Count); Post(delegate { if (!IsDisposed) { sv.Bar.Maximum = tot; sv.Bar.Value = 0; } });   // la barre de chaque service est sur SES QSO à envoyer
                int bsz = Math.Max(1, Math.Min(s.Batch, SessionSize));   // un lot ne dépasse jamais une session
                for (int i = 0; i < mine.Count && !cancel; i += bsz)
                {
                    int pause = s.PauseMs; if (i > 0 && i % SessionSize == 0) pause = Math.Max(pause, SessionPauseMs);   // nouvelle session de 100 : pause pour respecter les limites des services en ligne (PauseMs : délai imposé par le service entre deux lots)
                    if (i > 0 && pause > 0)
                    {
                        Say(s.Name + " : lot terminé, pause de " + (pause / 1000) + " s avant le suivant");
                        for (int w = 0; w < pause && !cancel; w += 250) Thread.Sleep(250);
                        if (cancel) break;
                    }
                    List<Qso> part = mine.GetRange(i, Math.Min(bsz, mine.Count - i));
                    try { string m = s.Fn(part); s.Done.AddRange(part); Say(m); List<Qso> pp = part; Post(delegate { Action<SendSvc, List<Qso>> bd = BatchDone; if (bd != null) bd(sv, pp); }); }
                    catch (Exception ex) { s.Failed = true; Say(s.Name + " : ERREUR : " + ex.Message); string em = ex.Message; List<Qso> rest = mine.GetRange(i, mine.Count - i); Post(delegate { Action<SendSvc, string, List<Qso>> h = sv.OnFail; if (h != null) h(sv, em, rest); }); break; }
                    int n = s.Done.Count; Post(delegate { if (!IsDisposed) { sv.Bar.Value = Math.Min(sv.Bar.Maximum, n); sv.Lbl.Text = sv.Name + " : " + n + " / " + mine.Count; } });
                }
                string state = s.Failed ? Tr.T("arrêté sur erreur") : (cancel ? "interrompu" : "terminé"); int dn = s.Done.Count;
                Post(delegate { if (!IsDisposed) { sv.Lbl.Text = sv.Name + " : " + state + " (" + dn + " / " + mine.Count + ")"; if (!sv.Failed && !cancel) sv.Bar.Value = sv.Bar.Maximum; } }); Say(s.Name + " : " + state + " (" + dn + " / " + mine.Count + ")");
            }
            if (acquired) Release();
            running = false;
            Post(delegate { if (!IsDisposed) { bCancel.Enabled = true; bCancel.Text = Tr.T("Fermer"); head.Text = cancel ? Tr.T("Envoi interrompu.") : Tr.T("Envoi terminé."); } Action<List<SendSvc>> f = Finished; if (f != null) f(svcs); });
        }
    }

    // État « envoyé » pour les services sans champ dans la table du journal (Club Log, QRZ.com) : la table est partagée avec d'autres logiciels, on ne la modifie pas ;
    // la liste des QSO envoyés est gardée dans data\journal\<service>-<base>.txt (un numéro de QSO par ligne).
    public static class SentState
    {
        static readonly Dictionary<string, HashSet<int>> sets = new Dictionary<string, HashSet<int>>();
        static string Path_(string svc) { string sch = App.Log != null ? App.Log.Schema : "journal"; foreach (char c in System.IO.Path.GetInvalidFileNameChars()) sch = sch.Replace(c, '_'); return System.IO.Path.Combine(App.DataDir ?? ".", svc + "-" + sch + ".txt"); }
        static HashSet<int> Cur(string svc)
        {
            string p = Path_(svc); HashSet<int> h;
            if (!sets.TryGetValue(p, out h)) { h = new HashSet<int>(); try { if (System.IO.File.Exists(p)) foreach (string l in System.IO.File.ReadAllLines(p)) { int n; if (int.TryParse(l.Trim(), out n)) h.Add(n); } } catch { } sets[p] = h; }
            return h;
        }
        public static bool IsSent(string svc, int id) { lock (sets) return Cur(svc).Contains(id); }
        public static void Unmark(string svc, IEnumerable<int> ids)
        {
            lock (sets)
            {
                HashSet<int> h = Cur(svc); bool ch = false; foreach (int i in ids) if (h.Remove(i)) ch = true;
                if (ch) try { List<string> l = new List<string>(); foreach (int i in h) l.Add(i.ToString()); System.IO.File.WriteAllLines(Path_(svc), l.ToArray()); } catch { }
            }
        }
        public static void Mark(string svc, IEnumerable<int> ids)
        {
            lock (sets)
            {
                HashSet<int> h = Cur(svc); List<string> add = new List<string>(); foreach (int i in ids) if (h.Add(i)) add.Add(i.ToString());
                if (add.Count > 0) try { System.IO.File.AppendAllLines(Path_(svc), add.ToArray()); } catch { }
            }
        }
    }
    public static class ClubLogState { public static bool IsSent(int id) { return SentState.IsSent("clublog", id); } public static void Mark(IEnumerable<int> ids) { SentState.Mark("clublog", ids); } }
    public static class QrzState { public static bool IsSent(int id) { return SentState.IsSent("qrz", id); } public static void Mark(IEnumerable<int> ids) { SentState.Mark("qrz", ids); } }
}