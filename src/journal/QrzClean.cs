using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace HamRadioLink.Journal
{
    // Outils > Nettoyer « Incoming » (QRZ.com) : fonction reprise de QRZLogExport (sans ses autres onglets).
    // Ouvre QRZ.com dans Edge (piloté par le protocole de débogage), puis rejette les demandes de la liste « Incoming » dont le QSO est plus ancien que N jours
    // (motif « Not in my logbook », comme le bouton Reject de QRZ). Le rejet ne peut pas être annulé : une confirmation est demandée.
    class QrzCdp : IDisposable
    {
        ClientWebSocket ws; int id; readonly JavaScriptSerializer ser = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
        public void Connect(string url) { ws = new ClientWebSocket(); ws.ConnectAsync(new Uri(url), CancellationToken.None).Wait(); }
        public string Eval(string js)
        {
            int my = ++id;
            string req = ser.Serialize(new Dictionary<string, object> { { "id", my }, { "method", "Runtime.evaluate" }, { "params", new Dictionary<string, object> { { "expression", js }, { "returnByValue", true }, { "awaitPromise", true } } } });
            ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(req)), WebSocketMessageType.Text, true, CancellationToken.None).Wait();
            byte[] buf = new byte[65536];
            while (true)
            {
                MemoryStream ms = new MemoryStream(); WebSocketReceiveResult r;
                do
                {
                    r = ws.ReceiveAsync(new ArraySegment<byte>(buf), CancellationToken.None).Result;
                    if (r.MessageType == WebSocketMessageType.Close) throw new Exception("Edge a fermé la connexion.");
                    ms.Write(buf, 0, r.Count);
                } while (!r.EndOfMessage);
                Dictionary<string, object> d = ser.DeserializeObject(Encoding.UTF8.GetString(ms.ToArray())) as Dictionary<string, object>;
                if (d == null || !d.ContainsKey("id") || Convert.ToInt32(d["id"]) != my) continue;
                if (d.ContainsKey("error")) throw new Exception("CDP : " + ser.Serialize(d["error"]));
                Dictionary<string, object> res = d["result"] as Dictionary<string, object>;
                if (res.ContainsKey("exceptionDetails")) throw new Exception("Erreur JavaScript dans la page : " + ser.Serialize(res["exceptionDetails"]));
                Dictionary<string, object> rr = res["result"] as Dictionary<string, object>; object v; return rr.TryGetValue("value", out v) && v != null ? v.ToString() : "";
            }
        }
        public void Call(string method, Dictionary<string, object> prm)
        {
            int my = ++id;
            string req = ser.Serialize(new Dictionary<string, object> { { "id", my }, { "method", method }, { "params", prm ?? new Dictionary<string, object>() } });
            ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(req)), WebSocketMessageType.Text, true, CancellationToken.None).Wait();
            byte[] buf = new byte[65536];
            for (int guard = 0; guard < 200; guard++)
            {
                MemoryStream ms = new MemoryStream(); WebSocketReceiveResult r;
                do { r = ws.ReceiveAsync(new ArraySegment<byte>(buf), CancellationToken.None).Result; ms.Write(buf, 0, r.Count); } while (!r.EndOfMessage);
                Dictionary<string, object> d = ser.DeserializeObject(Encoding.UTF8.GetString(ms.ToArray())) as Dictionary<string, object>;
                if (d != null && d.ContainsKey("id") && Convert.ToInt32(d["id"]) == my) return;
            }
        }
        public void Dispose() { if (ws != null) { try { ws.Dispose(); } catch { } } }
    }

    public class QrzCleanForm : Form
    {
        readonly NumericUpDown numDays; readonly Button btnOpen, btnClean, btnStop; readonly ProgressBar bar; readonly TextBox txt;
        readonly JavaScriptSerializer ser = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
        int port; Process edge; volatile bool stop, busy;
        static readonly string ProfileDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QRZLogExport", "edge-profile");   // même profil que QRZLogExport : la connexion à QRZ est conservée

        public QrzCleanForm()
        {
            AutoScaleMode = AutoScaleMode.None; Text = Tr.T("Nettoyer « Incoming » (QRZ.com)"); FormBorderStyle = FormBorderStyle.Sizable; StartPosition = FormStartPosition.CenterParent; Font = Ui.Normal; ClientSize = new Size(640, 420); MinimumSize = new Size(560, 360); Icon = Ui.AppIcon;
            Label h = new Label { Left = 12, Top = 10, Width = 616, Height = 52, Text = Tr.T("Rejette les demandes de la liste « Incoming » de QRZ.com dont le QSO est plus ancien que le nombre de jours choisi (motif « Not in my logbook »). Le rejet ne peut pas être annulé.\r\n1) Ouvrir QRZ dans Edge et vous connecter   2) Lancer le nettoyage."), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right }; Controls.Add(h);
            Label l = new Label { Left = 12, Top = 72, Width = 330, Height = 22, Text = Tr.T("Rejeter les QSO de plus de (jours) :") }; Controls.Add(l);
            int d = App.Cfg.GetInt("qrzclean.days", 30); numDays = new NumericUpDown { Left = 345, Top = 68, Width = 70, Minimum = 0, Maximum = 36500, Value = Math.Max(0, Math.Min(36500, d)) }; Controls.Add(numDays);
            btnOpen = new Button { Left = 12, Top = 100, Width = 210, Height = 32, Text = Tr.T("Ouvrir QRZ dans Edge") }; Controls.Add(btnOpen);
            btnClean = new Button { Left = 232, Top = 100, Width = 250, Height = 32, Text = Tr.T("Nettoyer la liste « Incoming »") }; Controls.Add(btnClean);
            btnStop = new Button { Left = 492, Top = 100, Width = 130, Height = 32, Text = Tr.T("Arrêter"), Enabled = false }; Controls.Add(btnStop);
            bar = new ProgressBar { Left = 12, Top = 142, Width = 610, Height = 16, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right }; Controls.Add(bar);
            txt = new TextBox { Left = 12, Top = 166, Width = 610, Height = 240, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Font = Ui.Mono, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right }; Controls.Add(txt);
            btnOpen.Click += (s, e) => Start(delegate { OpenEdge("https://logbook.qrz.com/"); });
            btnClean.Click += (s, e) => { int dd = (int)numDays.Value; App.Cfg.Set("qrzclean.days", dd.ToString()); App.Cfg.Save(); Start(delegate { RunClean(dd); }); };
            btnStop.Click += (s, e) => { stop = true; Log(Tr.T("Arrêt demandé...")); };
            FormClosing += (s, e) => { if (busy) { stop = true; } };
        }

        void Log(string s) { try { BeginInvoke((Action)delegate { if (!IsDisposed) txt.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + s + "\r\n"); }); } catch { } }
        void Prog(int v, int max) { try { BeginInvoke((Action)delegate { if (!IsDisposed) { bar.Maximum = Math.Max(1, max); bar.Value = Math.Max(0, Math.Min(v, bar.Maximum)); } }); } catch { } }
        void Busy(bool b) { busy = b; btnOpen.Enabled = btnClean.Enabled = numDays.Enabled = !b; btnStop.Enabled = b; }
        void Start(Action work)
        {
            if (busy) return; stop = false; Busy(true);
            Thread t = new Thread(delegate()
            {
                try { work(); } catch (Exception ex) { Log(Tr.T("ERREUR : ") + ex.Message); }
                try { BeginInvoke((Action)delegate { if (!IsDisposed) Busy(false); }); } catch { }
            }) { IsBackground = true, Name = "qrz-incoming" };
            t.Start();
        }

        static string FindEdge()
        {
            string[] c = {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Microsoft\Edge\Application\msedge.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Microsoft\Edge\Application\msedge.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Google\Chrome\Application\chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Google\Chrome\Application\chrome.exe") };
            foreach (string p in c) if (File.Exists(p)) return p;
            throw new Exception(Tr.T("Microsoft Edge (ou Chrome) est introuvable."));
        }
        static string HttpGet(string url) { using (WebClient w = new WebClient()) { w.Encoding = Encoding.UTF8; return w.DownloadString(url); } }
        bool EdgeAlive() { if (port == 0) return false; try { HttpGet("http://127.0.0.1:" + port + "/json/version"); return true; } catch { return false; } }

        void OpenEdge(string url)
        {
            if (EdgeAlive()) { Log(Tr.T("Edge est déjà ouvert (fenêtre QRZ).")); return; }
            // un Edge lancé auparavant avec ce profil tourne peut-être encore (par QRZLogExport par exemple) : on le réutilise
            try
            {
                using (System.Management.ManagementObjectSearcher q = new System.Management.ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE Name='msedge.exe' OR Name='chrome.exe'"))
                    foreach (System.Management.ManagementObject o in q.Get())
                    {
                        string cl = Convert.ToString(o["CommandLine"]);
                        if (cl.IndexOf("edge-profile", StringComparison.OrdinalIgnoreCase) < 0 || cl.Contains("--type=")) continue;
                        Match m = Regex.Match(cl, @"remote-debugging-port=(\d+)"); if (!m.Success) continue;
                        port = int.Parse(m.Groups[1].Value);
                        if (EdgeAlive()) { Log(Tr.T("Edge est déjà ouvert : connectez-vous à QRZ si besoin (fenêtre Edge).")); return; }
                        port = 0;
                    }
            }
            catch { }
            TcpListener l = new TcpListener(IPAddress.Loopback, 0); l.Start(); port = ((IPEndPoint)l.LocalEndpoint).Port; l.Stop();
            Directory.CreateDirectory(ProfileDir);
            ProcessStartInfo psi = new ProcessStartInfo(FindEdge(), "--remote-debugging-port=" + port + " --remote-allow-origins=* --disable-background-timer-throttling --disable-renderer-backgrounding --disable-backgrounding-occluded-windows --no-first-run --user-data-dir=\"" + ProfileDir + "\" \"" + url + "\"") { UseShellExecute = false };
            edge = Process.Start(psi);
            for (int i = 0; i < 40; i++) { if (EdgeAlive()) { Log(Tr.T("Edge ouvert : connectez-vous à QRZ, puis cliquez sur « Nettoyer la liste Incoming ».")); return; } Thread.Sleep(250); }
            throw new Exception(Tr.T("Edge n'a pas répondu."));
        }
        QrzCdp Attach()
        {
            if (!EdgeAlive()) throw new Exception(Tr.T("Ouvrez d'abord QRZ avec le bouton « Ouvrir QRZ dans Edge »."));
            object[] tabs = ser.DeserializeObject(HttpGet("http://127.0.0.1:" + port + "/json")) as object[]; Dictionary<string, object> pick = null;
            foreach (Dictionary<string, object> t in tabs)
            {
                if ((string)t["type"] != "page" || !t.ContainsKey("webSocketDebuggerUrl")) continue;
                string u = (string)t["url"];
                if (u.IndexOf("logbook.qrz.com", StringComparison.OrdinalIgnoreCase) >= 0) { pick = t; break; }
                if (pick == null && !u.StartsWith("devtools")) pick = t;
            }
            if (pick == null) throw new Exception(Tr.T("Aucun onglet Edge trouvé."));
            QrzCdp c = new QrzCdp(); c.Connect((string)pick["webSocketDebuggerUrl"]);
            try { c.Call("Page.bringToFront", null); } catch { }
            try { c.Call("Emulation.setFocusEmulationEnabled", new Dictionary<string, object> { { "enabled", true } }); } catch { }
            return c;
        }

        const string JsReqList =
            "(function(){var o=[];[].slice.call(document.querySelectorAll('tr[id^=rr-]')).forEach(function(tr){" +
            "var b=tr.querySelector('button[onclick^=\"lb_reject\"]');if(!b)return;" +
            "var m=/lb_reject\\('(\\d+)'\\)/.exec(b.getAttribute('onclick'));if(!m)return;var id=m[1];" +
            "var rd=document.getElementById('rdate-'+id),rc=document.getElementById('rcall-'+id);" +
            "o.push([id,rc?rc.value:'',rd?rd.value:'']);});" +
            "return JSON.stringify({page:!!document.querySelector('#lbrequests'),rows:o});})()";
        const string JsReject =
            "(async function(id){var f=document.getElementById('lbmenu');var fd=new FormData(f);" +
            "fd.set('op','reject');fd.set('reason','Not in my logbook');fd.set('comments','');fd.set('qso',id);" +
            "var r=await fetch(f.action,{method:'POST',body:new URLSearchParams(fd),credentials:'include'});" +
            "var h=await r.text();var ok=/rejected successfully/i.test(h);" +
            "if(ok){var b=document.querySelector('button[onclick=\"lb_reject(\\''+id+'\\')\"]');if(b){var tr=b.closest('tr');if(tr)tr.remove();}}" +
            "return JSON.stringify({ok:ok,status:r.status,snip:ok?'':h.replace(/<script[\\s\\S]*?<\\/script>/gi,'').replace(/<[^>]+>/g,' ').replace(/\\s+/g,' ').slice(0,200)});})(";

        class Req { public string id, call, date; }
        List<Req> ListRequests(QrzCdp c, string cutoff, out int total)
        {
            total = 0; Dictionary<string, object> d = ser.Deserialize<Dictionary<string, object>>(c.Eval(JsReqList)); List<Req> res = new List<Req>();
            if (!(bool)d["page"]) return null;
            foreach (object r in (System.Collections.IEnumerable)d["rows"])
            {
                string[] a = ((System.Collections.IEnumerable)r).Cast<object>().Select(x => Convert.ToString(x)).ToArray(); total++;
                if (a[2].Length == 10 && string.CompareOrdinal(a[2], cutoff) < 0) res.Add(new Req { id = a[0], call = a[1], date = a[2] });
            }
            return res;
        }
        static void GoRequests(QrzCdp c) { try { c.Eval("lb_go('requests','')"); } catch { } Thread.Sleep(4000); }

        void RunClean(int days)
        {
            string cutoff = DateTime.Today.AddDays(-days).ToString("yyyy-MM-dd");
            if (!EdgeAlive()) { Log(Tr.T("Edge n'est pas ouvert : cliquez d'abord sur « Ouvrir QRZ dans Edge » et connectez-vous.")); return; }
            using (QrzCdp c = Attach())
            {
                int total = 0; List<Req> old = null;
                for (int i = 0; i < 6 && !stop; i++)
                {
                    try { old = ListRequests(c, cutoff, out total); } catch (Exception ex) { old = null; Log(Tr.T("lecture : ") + ex.Message); }
                    if (old != null) break;
                    if (i == 0) Log(Tr.T("Ouverture de la liste « Incoming »..."));
                    GoRequests(c);
                }
                if (old == null) { Log(Tr.T("La liste « Incoming » n'a pas pu être ouverte. Connectez-vous à QRZ (« Ouvrir QRZ dans Edge ») puis recommencez.")); return; }
                Log(Tr.T("Liste « Incoming » : ") + total + Tr.T(" demandes, dont ") + old.Count + Tr.T(" avec un QSO antérieur au ") + cutoff + " (" + Tr.T("plus de ") + days + Tr.T(" jours).") );
                if (old.Count == 0) { Log(Tr.T("Rien à nettoyer.")); return; }
                string ex1 = old[0].call + " (" + old[0].date + ")", ex2 = old[old.Count - 1].call + " (" + old[old.Count - 1].date + ")"; int n0 = old.Count;
                DialogResult ans = (DialogResult)Invoke((Func<DialogResult>)delegate { return MessageBox.Show(this, n0 + Tr.T(" demande(s) vont être REJETÉES avec le motif « Not in my logbook ».\r\n\r\nPremière : ") + ex1 + Tr.T("\r\nDernière : ") + ex2 + Tr.T("\r\n\r\nLe rejet ne peut pas être annulé. Continuer ?"), Tr.T("Nettoyer Incoming"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning); });
                if (ans != DialogResult.Yes) { Log(Tr.T("Annulé, rien n'a été rejeté.")); return; }
                int done = 0, fails = 0, pass = 0, planned = old.Count; Prog(0, planned);
                while (old.Count > 0 && !stop && pass < 30)
                {
                    pass++; int before = done;
                    foreach (Req r in old)
                    {
                        if (stop) break;
                        bool ok = false; string why = "";
                        try { Dictionary<string, object> d = ser.Deserialize<Dictionary<string, object>>(c.Eval(JsReject + "'" + r.id + "')")); ok = (bool)d["ok"]; why = Convert.ToString(d["snip"]); }
                        catch (Exception ex) { why = ex.Message; }
                        if (ok) { done++; fails = 0; Log(Tr.T("Rejeté ") + done + " : " + r.call + Tr.T(" (QSO du ") + r.date + ")"); Prog(done, Math.Max(planned, done)); }
                        else
                        {
                            fails++; Log(Tr.T("Échec pour ") + r.call + " (" + r.date + ") : " + why);
                            if (fails >= 3) { Log(Tr.T("ARRÊT : 3 échecs de suite. Rien d'autre n'a été modifié.")); return; }
                        }
                        Thread.Sleep(400);
                    }
                    if (stop) break;
                    GoRequests(c);   // on recharge la liste : s'il reste des demandes anciennes, on recommence
                    try { old = ListRequests(c, cutoff, out total) ?? new List<Req>(); } catch { old = new List<Req>(); }
                    if (old.Count > 0 && done == before) { Log(Tr.T("Les demandes restantes ne partent pas : arrêt.")); break; }
                }
                GoRequests(c);
                Log(Tr.T("Terminé : ") + done + Tr.T(" demande(s) rejetée(s).") + (stop ? Tr.T(" (arrêté à la demande)") : ""));
            }
        }
    }
}
