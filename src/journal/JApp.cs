using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace HamRadioLink.Journal
{
    // Le gestionnaire de journal (logbook) : un journal de trafic dans une base MariaDB / MySQL (la sienne, intégrée, ou celle d'un autre logiciel).
    // Tout ce qui est dans ce dossier vit dans l'espace de noms HamRadioLink.Journal ; ses réglages sont dans settings.ini sous « jn.* ».

    // Un journal : nom affiché (onglet) et base. Host vide = le serveur principal (réglages db.* / MariaDB intégré).
    public class LogRef
    {
        public string Name = "", Host = "", User = "", Pass = "", Schema = ""; public int Port;
        public static string FilePath { get { return Path.Combine(App.DataDir, "logs.txt"); } }
        static string Esc(string s) { return (s ?? "").Replace("|", "/").Replace("\r", " ").Replace("\n", " "); }
        public string Serialize() { return string.Join("|", new[] { Esc(Name), Esc(Host), Port.ToString(), Esc(User), Esc(Pass), Esc(Schema) }); }
        public static LogRef Parse(string l) { string[] p = l.Split('|'); if (p.Length < 6) return null; int port; int.TryParse(p[2], out port); return new LogRef { Name = p[0], Host = p[1], Port = port, User = p[3], Pass = p[4], Schema = p[5] }; }
        public static List<LogRef> LoadAll()
        {
            List<LogRef> l = new List<LogRef>();
            if (File.Exists(FilePath)) foreach (string x in File.ReadAllLines(FilePath, Encoding.UTF8)) { if (x.Trim().Length == 0) continue; LogRef r = Parse(x); if (r != null) l.Add(r); }
            return l;
        }
        public static void SaveAll(List<LogRef> l) { List<string> o = new List<string>(); foreach (LogRef r in l) o.Add(r.Serialize()); File.WriteAllLines(FilePath, o.ToArray(), new UTF8Encoding(false)); }
    }

    // Réglages du journal : rangés dans les réglages de HamRadioLink avec le préfixe « jn. » (la langue est toujours le français).
    public class Ini
    {
        const string P = "jn.";
        public string Get(string k, string def) { if (k == "lang") return Tr.Code; return HamRadioLink.Cfg.Get(P + k, def); }
        public int GetInt(string k, int def) { return HamRadioLink.Cfg.GetInt(P + k, def); }
        public bool GetBool(string k, bool def) { return HamRadioLink.Cfg.GetBool(P + k, def); }
        public void Set(string k, object v) { string s = v is bool ? ((bool)v ? "1" : "0") : Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture); HamRadioLink.Cfg.Set(P + k, s ?? ""); }
        public void Save() { HamRadioLink.Cfg.Save(); }
    }

    public class InstInfo { public int N; public long Hwnd; public string Name = ""; public bool Online; public bool Alive = true; public long Hz; public string Mode = ""; }

    // Le poste actuellement choisi dans HamRadioLink : fréquence et mode proposés dans la fenêtre d'un QSO (branchés par la fenêtre principale).
    public static class RigState
    {
        public static Func<long> HzFn; public static Func<string> ModeFn; public static Func<string> NameFn;
        public static long Hz { get { try { return Enabled && HzFn != null ? HzFn() : 0; } catch { return 0; } } }
        public static bool Enabled = true;   // faux quand la télécommande est fermée : plus aucun lien avec le poste
        public static Func<long> HzBFn; public static Action<long, string> TuneFn;   // TuneFn(Hz, « mode1|mode2 »)
        public static long HzB { get { try { return Enabled && HzBFn != null ? HzBFn() : 0; } catch { return 0; } } }
        public static string Mode { get { try { return Enabled && ModeFn != null ? ModeFn() : ""; } catch { return ""; } } }
        public static string Radio { get { try { return Enabled && NameFn != null ? NameFn() : ""; } catch { return ""; } } }
    }

    public static class App
    {
        public static string Root = AppDomain.CurrentDomain.BaseDirectory;
        public static string DataDir;
        public static Ini Cfg = new Ini();
        public static MySqlClient Db;
        public static PeerLog Log;
        public static string Version { get { return MainForm.Version; } }
        public static bool OffScreen;
        public static int Instance = 1;
        public static Action<long, string> Tune;
        public static List<LogRef> Logs = new List<LogRef>();
        public static LogRef CurrentLog;

        public static void Init()
        {
            if (DataDir != null) return;
            DataDir = Path.Combine(HamRadioLink.Cfg.Dir ?? ".", "journal"); Directory.CreateDirectory(DataDir);
            try { Countries.Load(); } catch { } try { Skin.Install(); } catch { }
        }
        public static bool OtherInstancesAlive() { return false; }
        public static List<InstInfo> Instances() { List<InstInfo> l = new List<InstInfo>(); string n = RigState.Radio; if (n.Length > 0) l.Add(new InstInfo { N = 1, Name = n, Online = true, Hz = RigState.Hz, Mode = RigState.Mode }); return l; }

        // Charge la liste des journaux ; la crée à partir des réglages db.* si elle n'existe pas encore.
        public static void LoadLogs()
        {
            Logs = LogRef.LoadAll();
            if (Logs.Count == 0 && Cfg.Get("db.host", "").Length > 0) { Logs.Add(new LogRef { Name = Cfg.Get("db.name", "Log"), Schema = Cfg.Get("db.schema", "mysql") }); LogRef.SaveAll(Logs); }
            string cur = Cfg.Get("log.current", ""); CurrentLog = Logs.Find(x => x.Name == cur) ?? (Logs.Count > 0 ? Logs[0] : null);
        }

        public static void ServerOf(LogRef r, out string host, out int port, out string user, out string pass)
        {
            if (r.Host.Length > 0) { host = r.Host; port = r.Port; user = r.User; pass = r.Pass; }
            else { host = Cfg.Get("db.host", "127.0.0.1"); port = Cfg.GetInt("db.port", 3306); user = Cfg.Get("db.user", "root"); pass = Cfg.Get("db.pass", ""); }
        }

        public static void ConnectDb() { if (Logs.Count == 0) LoadLogs(); ConnectTo(CurrentLog); }

        public static void ConnectTo(LogRef r)
        {
            if (Db != null) { try { Db.Dispose(); } catch { } }
            string schema = r == null ? Cfg.Get("db.schema", "mysql") : r.Schema, host, user, pass; int port;
            if (r != null) ServerOf(r, out host, out port, out user, out pass); else { host = Cfg.Get("db.host", "127.0.0.1"); port = Cfg.GetInt("db.port", 3306); user = Cfg.Get("db.user", "root"); pass = Cfg.Get("db.pass", ""); }
            Db = new MySqlClient(host, port, user, pass, schema);
            Db.Open();
            Log = new PeerLog(Db, schema);
            if (r != null) { CurrentLog = r; Cfg.Set("log.current", r.Name); Cfg.Set("db.name", r.Name); Cfg.Set("db.schema", schema); Cfg.Save(); }
        }

        public static void Shutdown()
        {
            try { if (Db != null) Db.Dispose(); } catch { }
            Db = null; Log = null;
            try { if (DataDir != null) Embedded.Stop(); } catch { }
        }
    }

    // Point d'entrée : le bouton LOG de la fenêtre principale.
    public static class Hub
    {
        static LogbookForm win;
        public static Action ShowMain, LogClosed;   // fournis par la fenêtre de télécommande
        public static bool IsOpen { get { return win != null && !win.IsDisposed && win.Visible; } }
        static bool quit;
        // Le journal vit dès le démarrage, fenêtre masquée : il reçoit les QSO des logiciels (WSJT-WWA…) et le cluster tourne.
        // La croix de la fenêtre la masque seulement ; le bouton LOG la rouvre.
        public static void Prepare()
        {
            try
            {
                if (WsjtListener.Disabled || (win != null && !win.IsDisposed)) return;
                App.Init();
                if (!App.Cfg.GetBool("setup.done", false) && App.Cfg.Get("db.host", "").Length == 0) return;
                if (!Connect(null)) return;
                Create(); DxCluster.AutoStart();
            }
            catch { }
        }
        static void Create()
        {
            win = new LogbookForm();
            win.FormClosing += (s, e) => { if (!quit && e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; win.Hide(); Action a = LogClosed; if (a != null) a(); } };
            win.FormClosed += delegate { Action a = LogClosed; if (a != null) a(); };
            IntPtr h = win.Handle;      // crée la fenêtre sans l'afficher
        }
        public static void Open(IWin32Window owner)
        {
            try
            {
                if (win != null && !win.IsDisposed) { bool was = win.Visible; win.Show(); if (!was) win.Reload(); if (win.WindowState == FormWindowState.Minimized) win.WindowState = FormWindowState.Normal; win.BringToFront(); win.Activate(); return; }
                App.Init();
                if (!App.Cfg.GetBool("setup.done", false))
                {
                    if (App.Cfg.Get("db.host", "").Length > 0) { App.Cfg.Set("setup.done", true); App.Cfg.Save(); }
                    else { using (SetupForm f = new SetupForm()) f.ShowDialog(owner); if (!App.Cfg.GetBool("setup.done", false)) return; }
                }
                Connect(owner);
                Create(); win.Show();
            }
            catch (Exception ex) { MessageBox.Show(owner, ex.Message, Tr.T("Journal"), MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        // Connexion à la base du journal (MariaDB intégré démarré au besoin). owner = null : en silence (réception WSJT-X sans fenêtre).
        public static bool Connect(IWin32Window owner)
        {
            App.Init();
            if (App.Db != null && App.Log != null) return true;
            if (!App.Cfg.GetBool("setup.done", false) && App.Cfg.Get("db.host", "").Length == 0) return false;
            if (App.Cfg.Get("db.mode", "") == "embedded") { try { Embedded.EnsureRunning(); } catch (Exception ex) { if (owner != null) MessageBox.Show(owner, Tr.T("MariaDB intégré : ") + ex.Message, "Journal"); else return false; } }
            if (App.Cfg.Get("db.host", "").Length > 0) { try { App.ConnectDb(); } catch (Exception ex) { if (owner != null) MessageBox.Show(owner, Tr.T("Connexion au journal impossible : ") + ex.Message + "\r\n\r\nChoisissez la base dans la fenêtre suivante.", "Journal"); return false; } }
            return App.Log != null;
        }

        // Après un QSO reçu : rafraîchit la fenêtre du journal si elle est ouverte.
        public static void Refresh() { try { if (win != null && !win.IsDisposed) win.Reload(); } catch { } }
        public static void ClusterChanged() { try { if (win != null && !win.IsDisposed) win.ApplyCluster(); } catch { } }
        public static void Shutdown() { quit = true; WsjtListener.Stop(); try { DxCluster.Disconnect(); } catch { } try { if (win != null && !win.IsDisposed) win.Close(); } catch { } try { if (win != null && !win.IsDisposed) win.Dispose(); } catch { } try { List<Form> all = new List<Form>(); foreach (Form x in Application.OpenForms) all.Add(x); foreach (Form f in all) if (f != null && !f.IsDisposed && (f.GetType().Namespace ?? "").StartsWith("HamRadioLink.Journal")) f.Dispose(); } catch { } App.Shutdown(); }
    }

}
