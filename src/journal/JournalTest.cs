using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace HamRadioLink.Journal
{
    // Essai du journal (HRL_ONLY=journal) : un MariaDB intégré ISOLÉ (moteur copié dans le dossier d'essai, données vierges, port libre).
    // Aucune base réelle n'est touchée. Variable HRL_ENGINE = dossier d'un MariaDB installé dont on copie seulement bin et share.
    public static class JournalTest
    {
        public static void Run(Action<bool, string> check, Action<string> info, string tmp)
        {
            string src = Environment.GetEnvironmentVariable("HRL_ENGINE") ?? "";
            App.Init(); App.Cfg.Set("test.nosystem", true);
            check(Embedded.SystemServer() == null, "Journal : un MariaDB déjà installé n'est pas vu (réglage d'essai)");
            check(!Embedded.EnginePresent, "Journal : moteur intégré absent au départ");
            if (!Directory.Exists(Path.Combine(src, "bin"))) { info("Journal : moteur d'essai introuvable (HRL_ENGINE) : essai de la base ignoré."); return; }
            foreach (string f in Embedded.EngineFiles) { string s = Path.Combine(src, "bin", f); if (File.Exists(s)) { Directory.CreateDirectory(Path.Combine(Embedded.EngineDir, "bin")); File.Copy(s, Embedded.Bin(f), true); } }
            if (!File.Exists(Embedded.Bin("mysqld.exe"))) File.Copy(Embedded.Bin("mariadbd.exe"), Embedded.Bin("mysqld.exe"));
            CopyDir(Path.Combine(src, "share"), Path.Combine(Embedded.EngineDir, "share"));
            check(Embedded.EnginePresent, "Journal : moteur intégré présent après copie");
            int port = Embedded.FreePort(3317); string pass = Embedded.RandomPassword();
            Embedded.InitData(pass); check(Embedded.DataReady, "Journal : données MariaDB intégrées créées");
            Embedded.Start(port, pass); check(Embedded.PortOpen(port), "Journal : serveur intégré démarré (port " + port + ")");
            info("Journal : version du moteur " + Embedded.InstalledVersion());
            App.Cfg.Set("db.mode", "embedded"); App.Cfg.Set("emb.port", port); App.Cfg.Set("emb.pass", pass); App.Cfg.Set("db.host", "127.0.0.1"); App.Cfg.Set("db.port", port); App.Cfg.Set("db.user", "root"); App.Cfg.Set("db.pass", pass);
            App.Cfg.Set("me.call", "F5TEST"); App.Cfg.Set("me.grid", "IN78TI");
            try
            {
                using (MySqlClient c = new MySqlClient("127.0.0.1", port, "root", pass, "mysql")) { c.Open(); LogSetup.EnsureTable(c, "hamradiolink", delegate(string s) { }); LogSetup.EnsureTable(c, "hamradiolink", delegate(string s) { }); }
                App.Logs = new List<LogRef> { new LogRef { Name = "F5TEST", Schema = "hamradiolink" } }; LogRef.SaveAll(App.Logs); App.CurrentLog = App.Logs[0]; App.ConnectDb();
                check(App.Log != null && App.Log.Count("") == 0, "Journal : base et table créées, vides");
                Qso q = new Qso(); q["COL_CALL"] = "DL1ABC"; q["COL_TIME_ON"] = "2026-11-18 10:00:00"; q["COL_FREQ"] = "7074000"; q["COL_MODE"] = "FT8"; q["COL_BAND"] = "40m"; q["COL_RST_SENT"] = "-10"; q["COL_RST_RCVD"] = "-12"; q["COL_COUNTRY"] = "Germany"; q["COL_COMMENT"] = "é à ç essai";
                App.Log.ApplyDefaults(q); int id = App.Log.Insert(q); check(id > 0 && App.Log.Count("") == 1, "Journal : un QSO enregistré");
                Qso b = App.Log.Get(id); check(b != null && b.S("COL_CALL") == "DL1ABC" && b.S("COL_COMMENT") == "é à ç essai", "Journal : QSO relu (accents conservés)");
                string adi = Path.Combine(tmp, "essai.adi"); File.WriteAllText(adi, "ADIF test\r\n<ADIF_VER:5>3.1.0 <EOH>\r\n<CALL:5>F6ABC <QSO_DATE:8>20261101 <TIME_ON:4>1230 <FREQ:5>14.07 <MODE:3>CW <BAND:3>20m <EOR>\r\n<CALL:5>F6ABD <QSO_DATE:8>20261102 <TIME_ON:4>0815 <FREQ:5>7.074 <MODE:3>FT8 <BAND:3>40m <EOR>\r\n");
                int a, du; AdifImport.Run(App.Log, adi, delegate(string s) { }, out a, out du); check(a == 2 && App.Log.Count("") == 3, "Journal : import ADIF (2 QSO ajoutés, total " + App.Log.Count("") + ")");
                AdifImport.Run(App.Log, adi, delegate(string s) { }, out a, out du); check(du == 2 && App.Log.Count("") == 3, "Journal : réimport ADIF : doublons ignorés (" + du + ")");
                string ex = Adif.Export(App.Log.Fetch("", "COL_PRIMARY_KEY ASC", 0, 100)); check(ex.Contains("DL1ABC") && ex.Contains("F6ABC") && ex.Contains("<EOH>"), "Journal : export ADIF");
                check(App.Log.Count(PeerLog.QuickWhere("F6AB")) == 2, "Journal : recherche rapide");
                // fenêtres : construction hors écran, photographie
                string dir = Environment.GetEnvironmentVariable("HRL_SHOTS") ?? Path.Combine(tmp, "journal-shots"); Directory.CreateDirectory(dir);
                using (LogbookForm lb = new LogbookForm())
                {
                    lb.StartPosition = FormStartPosition.Manual; lb.Location = new Point(-3000, 0); lb.Show(); Pump(1500); Snap(lb, Path.Combine(dir, "journal.png"));
                    DataGridView g = (DataGridView)typeof(LogbookForm).GetField("grid", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(lb); check(g.RowCount == 3, "Journal : la liste affiche 3 QSO (" + g.RowCount + ")");
                    {
                        bool bgDone = false; long bgCount = -1; MethodInfo uc = typeof(LogbookForm).GetMethod("UpdateChunked", BindingFlags.NonPublic | BindingFlags.Static);
                        Bg.Run(lb, "Essai traitement", (lg, dbx, say, canc) => { say("en cours", 50); bgCount = lg.Count(""); uc.Invoke(null, new object[] { dbx, lg, "COL_COMMENT='essai-bg'", "COL_CALL='F6ABC'", canc, say }); return "ok"; }, () => { bgDone = true; });
                        for (int w = 0; w < 50 && !bgDone; w++) Pump(100); Pump(200);
                        check(bgDone && bgCount == 3 && App.Log.Count("COL_COMMENT='essai-bg'") == 1, "Traitement long : exécuté sur un autre fil avec sa propre connexion, modification par paquets (" + bgCount + " QSO vus)");
                        List<Form> bgf = new List<Form>(); foreach (Form bf in Application.OpenForms) if (bf is BgTaskForm) bgf.Add(bf); foreach (Form bf in bgf) bf.Dispose();
                    }
                    g.CurrentCell = g[0, 2]; typeof(LogbookForm).GetMethod("ReloadKeep", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(lb, null); Pump(200);
                    check(g.RowCount == 3 && g.CurrentCell != null && g.CurrentCell.RowIndex == 2, "Journal : après un envoi, la liste garde sa position (ligne 3 toujours courante)");
                    MethodInfo add = typeof(LogbookForm).GetMethod("AddQso", BindingFlags.NonPublic | BindingFlags.Instance); add.Invoke(lb, null); Pump(800);
                    DxCluster.RaiseSpotClicked(new Spot { Call = "DL9XYZ", Khz = 14074 }); Pump(400);
                    foreach (Form f in Application.OpenForms) if (f is QsoForm) { TextBox tc = (TextBox)typeof(QsoForm).GetField("tCall", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(f); check(tc.Text == "DL9XYZ", "Cluster : un clic sur un indicatif remplit la fenêtre Ajouter ouverte"); f.Location = new Point(-3000, 100); Snap(f, Path.Combine(dir, "qso.png")); f.Close(); break; }
                    for (int ti = 0; ti < Theme.All.Count; ti++)
                    {
                        Theme.Set(ti); Skin.ApplyAll(); Pump(400); Snap(lb, Path.Combine(dir, "theme-" + ti + "-journal.png"));
                        add.Invoke(lb, null); Pump(500); Skin.ApplyAll(); Pump(300);
                        foreach (Form f in Application.OpenForms) if (f is QsoForm) { f.Location = new Point(-3000, 100); Snap(f, Path.Combine(dir, "theme-" + ti + "-qso.png")); f.Close(); break; }
                    }
                    Theme.Set(1); int orig = Theme.All[1].Back; Theme.SetColor(1, false, 0, 0x112233); bool saved = App.Cfg.Get(Theme.Key(1, false, 0), "") == "112233" && Theme.Get("lb.back").ToArgb() == Theme.C(0x112233).ToArgb();
                    using (ColorsForm cf = new ColorsForm()) { cf.StartPosition = FormStartPosition.Manual; cf.Location = new Point(-3000, 0); cf.Show(); Skin.ApplyAll(); Pump(400); Snap(cf, Path.Combine(dir, "couleurs.png")); cf.Close(); }
                    using (ColorPicker cp = new ColorPicker("Fond des fenêtres", Theme.C(0x20C997))) { cp.StartPosition = FormStartPosition.Manual; cp.Location = new Point(-3000, 0); cp.Show(); Skin.ApplyAll(); Pump(400); Snap(cp, Path.Combine(dir, "selecteur.png")); cp.Close(); }
                    Theme.ResetTheme(1); check(saved && Theme.All[1].Back == orig && App.Cfg.Get(Theme.Key(1, false, 0), "") == "", "Couleurs du thème : personnalisée, mémorisée par thème, puis rétablie");
                    Theme.Set(0); Skin.ApplyAll(); check(Theme.All.Count >= 28, "Thèmes : " + Theme.All.Count + " thèmes disponibles, appliqués aux fenêtres sans erreur");
                    lb.Close();
                }
                using (DbForm f = new DbForm())
                {
                    f.StartPosition = FormStartPosition.Manual; f.Location = new Point(-3000, 0); f.Show(); Pump(4000); Snap(f, Path.Combine(dir, "base.png"));
                    string rep = ((TextBox)typeof(DbForm).GetField("report", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(f)).Text; info("Journal : essais de la fenêtre Base :\r\n" + rep);
                    check(rep.Contains("4. Connexion : réussie") && rep.Contains("6. Table du journal : présente — 3 QSO") && rep.Contains("Tout est prêt"), "Journal : les essais de la fenêtre Base voient la base et ses 3 QSO");
                    f.Close();
                }
                using (EngineForm f = new EngineForm()) { f.StartPosition = FormStartPosition.Manual; f.Location = new Point(-3000, 0); f.Show(); Pump(500); Snap(f, Path.Combine(dir, "moteur.png")); f.Close(); }
                using (SetupForm f = new SetupForm(true))
                {
                    f.StartPosition = FormStartPosition.Manual; f.Location = new Point(-3000, 0); f.Show(); Pump(500); Snap(f, Path.Combine(dir, "assistant-1.png"));
                    MethodInfo sh = typeof(SetupForm).GetMethod("Show", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(int) }, null); sh.Invoke(f, new object[] { 2 }); Pump(500); Snap(f, Path.Combine(dir, "assistant-3.png")); f.Close();
                }
                // l'assistant de bout en bout : aucun journal d'un autre logiciel ici (essai isolé), un fichier ADIF devient un nouvel onglet
                App.Cfg.Set("test.adif", adi); App.Cfg.Set("test.peerport", 1);
                using (SetupForm f = new SetupForm(true))
                {
                    f.StartPosition = FormStartPosition.Manual; f.Location = new Point(-3000, 0); bool done = f.AutoRun("empty"); check(done, "Assistant : installation terminée (nouvel onglet depuis un fichier ADIF)"); f.Close();
                }
                check(App.Log != null && App.Log.Count("") == 2 && App.Logs.Count >= 1 && App.CurrentLog.Name == "Import test", "Assistant : l'onglet « Import test » contient les 2 QSO du fichier ADIF");
                // réception WSJT-X : un paquet UDP « Logged ADIF » (type 12) devient un QSO du journal courant
                int up = Embedded.FreePort(23237); int un = Embedded.FreePort(up + 1); HamRadioLink.Cfg.Set("jn.port.wsjt", up.ToString()); HamRadioLink.Cfg.Set("jn.port.n1mm", un.ToString()); HamRadioLink.Cfg.Set("jn.port.adif", "0");
                WsjtListener.Disabled = false; WsjtListener.Start(); check(WsjtListener.Open.Contains(up) && WsjtListener.Open.Contains(un), "WSJT-X : écoute UDP sur les ports " + up + " et " + un + " " + WsjtListener.Error);
                List<byte> pk = new List<byte>(); Action<uint> w32 = delegate(uint v) { pk.Add((byte)(v >> 24)); pk.Add((byte)(v >> 16)); pk.Add((byte)(v >> 8)); pk.Add((byte)v); };
                w32(0xadbccbda); w32(3); w32(12); byte[] idb = Encoding.UTF8.GetBytes("WSJT-X"); w32((uint)idb.Length); pk.AddRange(idb);
                byte[] ab = Encoding.UTF8.GetBytes("\n<adif_ver:5>3.1.0\n<programid:6>WSJT-X\n<EOH>\n<call:6>DL9XYZ <gridsquare:4>JO62 <mode:3>FT8 <rst_sent:3>-05 <rst_rcvd:3>-07 <qso_date:8>20261118 <time_on:6>153000 <band:3>20m <freq:9>14.074000 <station_callsign:6>F5TEST <EOR>\n"); w32((uint)ab.Length); pk.AddRange(ab);
                using (System.Net.Sockets.UdpClient uc = new System.Net.Sockets.UdpClient()) uc.Send(pk.ToArray(), pk.Count, "127.0.0.1", up);
                Pump(1500); check(App.Log.Count("COL_CALL='DL9XYZ'") == 1, "WSJT-X : le QSO reçu en UDP est dans le journal");
                string nx = "<?xml version=\"1.0\"?><contactinfo><app>WSJT-X</app><call>OK1ABC</call><timestamp>2026-11-18 16:10:00</timestamp><mycall>F5TEST</mycall><rxfreq>1407400</rxfreq><txfreq>1407400</txfreq><mode>FT8</mode><gridsquare>JN79</gridsquare><snt>-03</snt><rcv>-11</rcv></contactinfo>";
                using (System.Net.Sockets.UdpClient uc = new System.Net.Sockets.UdpClient()) { byte[] nb = Encoding.UTF8.GetBytes(nx); uc.Send(nb, nb.Length, "127.0.0.1", un); }
                Pump(1500); check(App.Log.Count("COL_CALL='OK1ABC' AND COL_BAND='20m'") == 1, "N1MM : le QSO reçu en XML est dans le journal (bande déduite de la fréquence)");
                List<Qso> dq = App.Log.Fetch("COL_CALL='DL9XYZ'", "COL_PRIMARY_KEY", 0, 1); double dkm; double.TryParse(dq.Count > 0 ? dq[0].S("COL_DISTANCE") : "", System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out dkm);
                check(dkm > 1250 && dkm < 1380, "Distance IN78TI -> JO62 calculée à la réception (" + dkm + " km)");
                typeof(DxCluster).GetMethod("Line", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { "DX de F5ABC:    14074.0  DL1XX        FT8 -10 dB                   1530Z JO62" });
                check(DxCluster.Spots.Count == 1 && DxCluster.Spots[0].Call == "DL1XX" && DxCluster.Spots[0].Band == "20m" && DxCluster.Frames.Count == 1, "DX Cluster : trame lue, spot DL1XX sur 20m");
                List<DxCluster.AlertLine> al = new List<DxCluster.AlertLine> { new DxCluster.AlertLine { Op = "ou", Text = "dl1" }, new DxCluster.AlertLine { Op = "et", Text = "ft8" } };
                List<DxCluster.AlertLine> al2 = new List<DxCluster.AlertLine> { new DxCluster.AlertLine { Op = "ou", Text = "dl1" }, new DxCluster.AlertLine { Op = "et", Text = "cw" } };
                check(DxCluster.Matches(DxCluster.Spots[0], al) && !DxCluster.Matches(DxCluster.Spots[0], al2), "Alertes : « dl1 et ft8 » correspond, « dl1 et cw » non");
                using (Form f = new Form { ClientSize = new Size(220, 420), StartPosition = FormStartPosition.Manual, Location = new Point(-3000, 0) }) { f.Controls.Add(new ClusterStrip()); f.Show(); Pump(900); Snap(f, Path.Combine(dir, "cluster.png")); f.Close(); }
                using (Form f = new Form { ClientSize = new Size(700, 150), StartPosition = FormStartPosition.Manual, Location = new Point(-3000, 0) }) { f.Controls.Add(new ClusterFrames()); f.Show(); Pump(600); Snap(f, Path.Combine(dir, "frames.png")); f.Close(); }
                using (AwardsForm f = new AwardsForm()) { f.StartPosition = FormStartPosition.Manual; f.Location = new Point(-3000, 0); f.Show(); Pump(500); Snap(f, Path.Combine(dir, "diplomes.png")); check(f.Controls.Count > 0 && ((TabControl)f.Controls[0]).TabPages.Count == 5, "Suivi des diplômes : 5 onglets (DXCC, WAZ, WAC, WAS, IOTA) construits sans erreur"); f.Close(); }
                {
                    List<Qso> ql = App.Log.Fetch("", "COL_PRIMARY_KEY ASC", 0, 100);
                    SendSvc sa = new SendSvc { Name = "Service A", Batch = 2, Fn = delegate(List<Qso> p) { System.Threading.Thread.Sleep(120); return "A : " + p.Count + " envoyés"; } };
                    int restN = -1; SendSvc sb2 = new SendSvc { Name = "Service B (erreur simulée)", Batch = 2, Fn = delegate(List<Qso> p) { throw new Exception("refus simulé"); }, OnFail = delegate(SendSvc sx, string em, List<Qso> rest) { restN = rest.Count; } };
                    bool fin = false; SendAllForm sf = new SendAllForm(new List<SendSvc> { sa, sb2 }, ql) { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, 0) }; sf.Finished = delegate(List<SendSvc> d) { fin = true; };
                    sf.Show(); sf.Start(); for (int w = 0; w < 40 && !fin; w++) Pump(200); Pump(300); Skin.ApplyAll(); Snap(sf, Path.Combine(dir, "envoi.png"));
                    int rc; string big = Online.RunTqsl("cmd.exe", "/c for /l %i in (1,1,4000) do @echo ligne %i de sortie volumineuse", 30000, out rc);
                    check(rc == 0 && big.Length > 60000, "Lancement de TQSL : sortie volumineuse lue sans blocage (" + big.Length + " caractères)");
                    int cl0 = ql[0].Id; bool before = ClubLogState.IsSent(cl0); ClubLogState.Mark(new[] { cl0 }); check(!before && ClubLogState.IsSent(cl0), "Club Log : état « envoyé » mémorisé localement (sans toucher à la table du journal)"); bool q0 = QrzState.IsSent(cl0); QrzState.Mark(new[] { cl0 }); check(!q0 && QrzState.IsSent(cl0) && !SentState.IsSent("autre", cl0), "QRZ.com : état « envoyé » mémorisé localement, séparé de Club Log"); SentState.Unmark("qrz", new[] { cl0 }); check(!QrzState.IsSent(cl0) && ClubLogState.IsSent(cl0), "Marquer « non envoyé » : QRZ.com retiré, Club Log inchangé");
                    check(fin && sa.Done.Count == ql.Count && sb2.Failed && sb2.Done.Count == 0 && restN == ql.Count, "Envoi aux logs en ligne : fenêtre non bloquante, service A complet, service B arrêté sur erreur avec la liste des QSO restants (repli QRZ)"); sf.Close();
                }
                using (AlertForm f = new AlertForm()) { f.StartPosition = FormStartPosition.Manual; f.Location = new Point(-3000, 0); f.Show(); Pump(300); Snap(f, Path.Combine(dir, "alertes.png")); f.Close(); }
                using (PortsForm f = new PortsForm()) { f.StartPosition = FormStartPosition.Manual; f.Location = new Point(-3000, 0); f.Show(); Pump(300); Snap(f, Path.Combine(dir, "ports.png")); f.Close(); }
                using (OptionsDlg f = new OptionsDlg(null, delegate { return 0; })) { f.StartPosition = FormStartPosition.Manual; f.Location = new Point(-3000, 0); f.Show(); typeof(OptionsDlg).GetMethod("PortsPage", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(f, null); Pump(300); Snap(f, Path.Combine(dir, "ports-param.png")); f.Close(); }
                WsjtListener.Stop();
                info("Journal : photos dans " + dir);
            }
            finally { App.Shutdown(); check(!Embedded.PortOpen(port), "Journal : serveur intégré arrêté proprement"); }
        }

        static void Pump(int ms) { DateTime end = DateTime.UtcNow.AddMilliseconds(ms); while (DateTime.UtcNow < end) { Application.DoEvents(); Thread.Sleep(30); } }
        static void Snap(Form f, string path) { try { using (Bitmap b = new Bitmap(f.Width, f.Height)) { f.DrawToBitmap(b, new Rectangle(0, 0, f.Width, f.Height)); b.Save(path); } } catch { } }
        static void CopyDir(string s, string d) { Directory.CreateDirectory(d); foreach (string f in Directory.GetFiles(s)) File.Copy(f, Path.Combine(d, Path.GetFileName(f)), true); foreach (string x in Directory.GetDirectories(s)) CopyDir(x, Path.Combine(d, Path.GetFileName(x))); }
    }
}
