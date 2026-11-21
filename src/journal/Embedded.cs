using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace HamRadioLink.Journal
{
    // MariaDB intégré : le moteur (data\mariadb\engine) et les données (data\mariadb\data) sont séparés,
    // ce qui permet de mettre le moteur à jour sans toucher au journal.
    // Il n'est utilisé que si aucun MariaDB / MySQL n'est déjà installé sur l'ordinateur.
    public static class Embedded
    {
        public static string BaseDir { get { return Path.Combine(App.DataDir, "mariadb"); } }
        public static string EngineDir { get { return Path.Combine(BaseDir, "engine"); } }
        public static string DataDir { get { return Path.Combine(BaseDir, "data"); } }
        public static string BackupDir { get { return Path.Combine(BaseDir, "backups"); } }
        public static string Bin(string exe) { return Path.Combine(EngineDir, "bin", exe); }
        public static bool EnginePresent { get { return File.Exists(Bin("mariadbd.exe")) && File.Exists(Bin("server.dll")); } }
        public static bool DataReady { get { return File.Exists(Path.Combine(DataDir, "my.ini")) && Directory.Exists(Path.Combine(DataDir, "mysql")); } }

        // Fichiers du moteur conservés (le reste de la distribution MariaDB — outils, symboles de débogage — ne sert pas).
        public static readonly string[] EngineFiles = { "mariadbd.exe", "mysqld.exe", "server.dll", "mariadb-install-db.exe", "mariadb-upgrade.exe", "mariadb-admin.exe", "mariadb.exe", "mariadb-check.exe", "mariadb.exe",
            "concrt140.dll", "libcurl.dll", "msvcp140_1.dll", "msvcp140_2.dll", "msvcp140_atomic_wait.dll", "msvcp140_codecvt_ids.dll", "msvcp140.dll", "vcruntime140_1.dll", "vcruntime140.dll", "zlib1.dll" };

        static Process proc; static bool weStarted;

        // ---------------- détection d'un MariaDB / MySQL déjà installé ----------------
        // Renvoie une description (service, dossier, port) ou null s'il n'y en a pas. Le moteur intégré lui-même ne compte pas.
        public static string SystemServer()
        {
            if (App.Cfg != null && App.Cfg.GetBool("test.nosystem", false)) return null;   // essais : fait comme si aucun MariaDB n'était installé
            try
            {
                using (Microsoft.Win32.RegistryKey svc = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services"))
                    if (svc != null)
                        foreach (string n in svc.GetSubKeyNames())
                        {
                            if (!Regex.IsMatch(n, Tr.T("maria|mysql"), RegexOptions.IgnoreCase)) continue;
                            using (Microsoft.Win32.RegistryKey k = svc.OpenSubKey(n))
                            {
                                string img = (k == null ? null : k.GetValue("ImagePath") as string) ?? "";
                                if (Regex.IsMatch(img, Tr.T("mariadbd|mysqld"), RegexOptions.IgnoreCase) && img.IndexOf(EngineDir, StringComparison.OrdinalIgnoreCase) < 0) return Tr.T("service Windows « ") + n + " »";
                            }
                        }
            }
            catch { }
            foreach (string pf in new[] { Environment.GetEnvironmentVariable("ProgramFiles"), Environment.GetEnvironmentVariable(Tr.T("ProgramFiles(x86)")) })
            {
                if (string.IsNullOrEmpty(pf) || !Directory.Exists(pf)) continue;
                try { foreach (string d in Directory.GetDirectories(pf, "MariaDB*")) if (File.Exists(Path.Combine(d, "bin", "mariadbd.exe"))) return Tr.T("dossier ") + d; } catch { }
                try { foreach (string d in Directory.GetDirectories(pf, "MySQL*")) if (File.Exists(Path.Combine(d, "bin", "mysqld.exe"))) return Tr.T("dossier ") + d; } catch { }
            }
            if (PortOpen(3306)) return Tr.T("serveur à l'écoute sur le port 3306");
            return null;
        }

        public static bool PortOpen(int port)
        {
            try { using (TcpClient c = new TcpClient()) { IAsyncResult r = c.BeginConnect("127.0.0.1", port, null, null); bool ok = r.AsyncWaitHandle.WaitOne(400) && c.Connected; return ok; } } catch { return false; }
        }

        public static int FreePort(int start)
        {
            for (int p = start; p < start + 200; p++) { try { TcpListener l = new TcpListener(IPAddress.Loopback, p); l.Start(); l.Stop(); return p; } catch { } }
            return start;
        }

        public static string RandomPassword()
        {
            byte[] b = new byte[18]; using (RandomNumberGenerator g = RandomNumberGenerator.Create()) g.GetBytes(b);
            return Convert.ToBase64String(b).Replace('+', 'a').Replace('/', 'b').Replace('=', 'c');
        }

        // ---------------- initialisation et démarrage ----------------
        static string Run(string exe, string args, int timeoutMs, out int code)
        {
            ProcessStartInfo si = new ProcessStartInfo(exe, args) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = Path.GetDirectoryName(exe) };
            using (Process p = Process.Start(si))
            {
                StringBuilder sb = new StringBuilder(); p.OutputDataReceived += (s, e) => { if (e.Data != null) sb.AppendLine(e.Data); }; p.ErrorDataReceived += (s, e) => { if (e.Data != null) sb.AppendLine(e.Data); };
                p.BeginOutputReadLine(); p.BeginErrorReadLine();
                if (!p.WaitForExit(timeoutMs)) { try { p.Kill(); } catch { } code = -1; return sb + "\r\n(délai dépassé)"; }
                code = p.ExitCode; return sb.ToString();
            }
        }

        // Crée les données du serveur (mot de passe root aléatoire, écoute locale uniquement).
        public static void InitData(string rootPassword)
        {
            if (!EnginePresent) throw new Exception(Tr.T("Le moteur MariaDB est absent (") + EngineDir + ").");
            Directory.CreateDirectory(DataDir);
            int code; string o = Run(Bin("mariadb-install-db.exe"), "--datadir=\"" + DataDir + "\" --password=" + rootPassword, 120000, out code);
            if (code != 0 || !DataReady) throw new Exception(Tr.T("Création des données MariaDB impossible :\r\n") + o);
        }

        // Démarre le serveur sur 127.0.0.1:port (ou le réutilise s'il répond déjà avec ces identifiants).
        public static void Start(int port, string rootPassword)
        {
            if (TryConnect(port, rootPassword)) return;
            if (!EnginePresent || !DataReady) throw new Exception(Tr.T("MariaDB intégré non installé."));
            if (PortOpen(port)) throw new Exception(Tr.T("Le port ") + port + Tr.T(" est occupé par un autre programme."));
            string args = "--defaults-file=\"" + Path.Combine(DataDir, "my.ini") + "\" --datadir=\"" + DataDir + "\" --basedir=\"" + EngineDir + "\" --port=" + port + " --bind-address=127.0.0.1 --skip-name-resolve"
                + " --character-set-server=utf8mb4 --log-error=\"" + Path.Combine(DataDir, "mariadb.err") + "\" --innodb-buffer-pool-size=128M --max-allowed-packet=64M";
            ProcessStartInfo si = new ProcessStartInfo(Bin("mariadbd.exe"), args) { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Path.Combine(EngineDir, "bin") };
            proc = Process.Start(si); weStarted = true;
            DateTime end = DateTime.UtcNow.AddSeconds(90);
            while (DateTime.UtcNow < end)
            {
                if (proc.HasExited) throw new Exception(Tr.T("MariaDB s'est arrêté au démarrage (voir ") + Path.Combine(DataDir, "mariadb.err") + ").");
                if (TryConnect(port, rootPassword)) return;
                Thread.Sleep(400);
            }
            throw new Exception(Tr.T("MariaDB ne répond pas après 90 s."));
        }

        static bool TryConnect(int port, string pass)
        {
            try { using (MySqlClient c = new MySqlClient("127.0.0.1", port, "root", pass, "mysql")) { c.Open(); return true; } } catch { return false; }
        }

        // Arrêt propre (commande SHUTDOWN) ; ne touche qu'au serveur lancé par ce programme.
        public static void Stop()
        {
            if (!weStarted && App.Cfg.Get("db.mode", "") != "embedded") return;
            if (App.OtherInstancesAlive()) return;     // une autre fenetre HamRadioLink utilise encore le journal : le dernier qui part arrete le serveur
            try { using (MySqlClient c = new MySqlClient("127.0.0.1", App.Cfg.GetInt("emb.port", 3307), "root", App.Cfg.Get("emb.pass", ""), "mysql")) { c.Open(); try { c.Exec("SHUTDOWN"); } catch { } } } catch { }
            try { if (proc != null) { if (!proc.WaitForExit(30000)) proc.Kill(); } else { for (int i = 0; i < 60 && PortOpen(App.Cfg.GetInt("emb.port", 3307)); i++) Thread.Sleep(500); } } catch { }
            proc = null; weStarted = false;
        }

        // Lancé au démarrage du programme quand db.mode=embedded.
        public static void EnsureRunning()
        {
            // Mise à jour interrompue (arrêt du PC en plein remplacement) : on remet l'ancien moteur.
            if (!EnginePresent && Directory.Exists(EngineDir + ".old")) { try { if (Directory.Exists(EngineDir)) Directory.Delete(EngineDir, true); Directory.Move(EngineDir + ".old", EngineDir); } catch { } }
            int port = App.Cfg.GetInt("emb.port", 3307); string pass = App.Cfg.Get("emb.pass", "");
            if (!TryConnect(port, pass) && PortOpen(port)) { port = FreePort(3307); App.Cfg.Set("emb.port", port); App.Cfg.Set("db.port", port); App.Cfg.Save(); }
            Start(port, pass);
            App.Cfg.Set("db.host", "127.0.0.1"); App.Cfg.Set("db.port", port); App.Cfg.Set("db.user", "root"); App.Cfg.Set("db.pass", pass);
        }

        public static string InstalledVersion()
        {
            try { int code; string o = Run(Bin("mariadbd.exe"), "--version", 15000, out code); Match m = Regex.Match(o, @"Ver\s+(\d+\.\d+\.\d+)"); return m.Success ? m.Groups[1].Value : ""; } catch { return ""; }
        }

        // ---------------- mise à jour du moteur ----------------
        public class Release { public string Version, FileName, Url, Sha256; }

        static string Http(string url)
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | (SecurityProtocolType)768 | SecurityProtocolType.Tls;
            using (WebClient w = new WebClient { Encoding = Encoding.UTF8 }) { w.Headers[HttpRequestHeader.UserAgent] = "HamRadioLink/" + App.Version; return w.DownloadString(url); }
        }

        static int[] Ver(string v) { string[] p = v.Split('.'); int[] r = new int[3]; for (int i = 0; i < 3 && i < p.Length; i++) int.TryParse(p[i], out r[i]); return r; }
        public static int CompareVersions(string a, string b) { int[] x = Ver(a), y = Ver(b); for (int i = 0; i < 3; i++) if (x[i] != y[i]) return x[i].CompareTo(y[i]); return 0; }

        // Dernière version de la même branche (ex. 12.3.x) pour Windows 64 bits, avec la somme de contrôle officielle.
        public static Release LatestInBranch(string installed)
        {
            string[] p = installed.Split('.'); if (p.Length < 2) return null; string branch = p[0] + "." + p[1];
            string json = Http("https://downloads.mariadb.org/rest-api/mariadb/" + branch + "/");
            Release best = null;
            foreach (Match m in Regex.Matches(json, "\"file_name\": \"(mariadb-(\\d+\\.\\d+\\.\\d+)-winx64\\.zip)\".*?\"sha256sum\": \"([0-9a-f]{64})\".*?\"file_download_url\": \"([^\"]+)\"", RegexOptions.Singleline))
            {
                Release r = new Release { FileName = m.Groups[1].Value, Version = m.Groups[2].Value, Sha256 = m.Groups[3].Value, Url = m.Groups[4].Value.Replace("http://", "https://") };
                if (best == null || CompareVersions(r.Version, best.Version) > 0) best = r;
            }
            return best;
        }

        // Branche stable plus récente (information seulement : un changement de branche demande une décision de l'utilisateur).
        public static string NewerBranch(string installed)
        {
            try
            {
                string json = Http("https://downloads.mariadb.org/rest-api/mariadb/"); string best = null;
                foreach (Match m in Regex.Matches(json, "\"release_id\": \"(\\d+\\.\\d+)\".*?\"release_status\": \"Stable\".*?\"release_support_type\": \"Long Term Support\"", RegexOptions.Singleline))
                {
                    string[] p = installed.Split('.'); string cur = p[0] + "." + p[1];
                    if (CompareVersions(m.Groups[1].Value + ".0", cur + ".0") > 0 && (best == null || CompareVersions(m.Groups[1].Value + ".0", best + ".0") > 0)) best = m.Groups[1].Value;
                }
                return best;
            }
            catch { return null; }
        }

        // Déplacement avec nouvelles tentatives : l'antivirus ou l'explorateur peuvent verrouiller un instant des fichiers tout juste créés.
        static void MoveRetry(string src, string dst)
        {
            for (int i = 0; ; i++)
            {
                try { Directory.Move(src, dst); return; }
                catch (IOException) { if (i >= 20) throw; Thread.Sleep(1000); }
                catch (UnauthorizedAccessException) { if (i >= 20) throw; Thread.Sleep(1000); }
            }
        }

        static void CopyDir(string src, string dst)
        {
            Directory.CreateDirectory(dst);
            foreach (string f in Directory.GetFiles(src)) File.Copy(f, Path.Combine(dst, Path.GetFileName(f)), true);
            foreach (string d in Directory.GetDirectories(src)) CopyDir(d, Path.Combine(dst, Path.GetFileName(d)));
        }

        // Mise à jour complète : téléchargement + vérification SHA-256, sauvegarde des données, remplacement du moteur,
        // mariadb-upgrade, et retour automatique à l'ancien moteur et aux données sauvegardées si le redémarrage échoue.
        public static void UpdateEngine(Release r, Action<string> log)
        {
            string tmp = Path.Combine(Path.GetTempPath(), "hrl-mariadb-" + r.Version); Directory.CreateDirectory(tmp);
            string zip = Path.Combine(tmp, r.FileName);
            log(Tr.T("Téléchargement de ") + r.FileName + " ...");
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | (SecurityProtocolType)768 | SecurityProtocolType.Tls;
            using (WebClient w = new WebClient()) { w.Headers[HttpRequestHeader.UserAgent] = "HamRadioLink/" + App.Version; w.DownloadFile(r.Url, zip); }
            log(Tr.T("Vérification de la somme de contrôle SHA-256 ..."));
            string sha; using (FileStream fs = File.OpenRead(zip)) using (SHA256 h = SHA256.Create()) sha = BitConverter.ToString(h.ComputeHash(fs)).Replace("-", "").ToLowerInvariant();
            if (sha != r.Sha256) { File.Delete(zip); throw new Exception(Tr.T("Somme de contrôle incorrecte : fichier rejeté.")); }

            string neu = EngineDir + ".new", old = EngineDir + ".old";
            if (Directory.Exists(neu)) Directory.Delete(neu, true);
            log(Tr.T("Extraction du moteur ..."));
            using (System.IO.Compression.ZipArchive za = System.IO.Compression.ZipFile.OpenRead(zip))
            {
                Directory.CreateDirectory(Path.Combine(neu, "bin"));
                foreach (System.IO.Compression.ZipArchiveEntry e in za.Entries)
                {
                    int i = e.FullName.IndexOf('/'); if (i < 0 || e.Length == 0) continue; string rel = e.FullName.Substring(i + 1);
                    bool keep = rel.StartsWith("share/") || rel == "COPYING" || rel == "THIRDPARTY" || rel == "README.md" || (rel.StartsWith("bin/") && Array.IndexOf(EngineFiles, rel.Substring(4)) >= 0);
                    if (!keep) continue;
                    string dst = Path.Combine(neu, rel.Replace('/', '\\')); Directory.CreateDirectory(Path.GetDirectoryName(dst)); System.IO.Compression.ZipFileExtensions.ExtractToFile(e, dst, true);
                }
            }
            if (!File.Exists(Path.Combine(neu, "bin", "mariadbd.exe")) || !File.Exists(Path.Combine(neu, "bin", "server.dll"))) throw new Exception(Tr.T("Archive inattendue : moteur introuvable."));
            if (!File.Exists(Path.Combine(neu, "bin", "mysqld.exe"))) File.Copy(Path.Combine(neu, "bin", "mariadbd.exe"), Path.Combine(neu, "bin", "mysqld.exe"));

            string before = InstalledVersion(); int port = App.Cfg.GetInt("emb.port", 3307); string pass = App.Cfg.Get("emb.pass", "");
            log(Tr.T("Arrêt du serveur ..."));
            if (weStarted) Stop(); else if (TryConnect(port, pass)) { try { using (MySqlClient c = new MySqlClient("127.0.0.1", port, "root", pass, "mysql")) { c.Open(); try { c.Exec("SHUTDOWN"); } catch { } } } catch { } Thread.Sleep(4000); }
            string bak = Path.Combine(BackupDir, "data-" + DateTime.Now.ToString("yyyy-MM-dd_HHmm") + "-" + before);
            log(Tr.T("Sauvegarde des données dans ") + bak + " ...");
            CopyDir(DataDir, bak);
            log(Tr.T("Remplacement du moteur ..."));
            if (Directory.Exists(old)) Directory.Delete(old, true);
            MoveRetry(EngineDir, old);
            try { MoveRetry(neu, EngineDir); }
            catch { try { MoveRetry(old, EngineDir); } catch { } throw new Exception(Tr.T("Impossible d'installer le nouveau moteur (fichiers verrouillés ?) : ancien moteur conservé.")); }
            try
            {
                log(Tr.T("Démarrage avec le nouveau moteur ..."));
                Start(port, pass);
                int code; string o = Run(Bin("mariadb-upgrade.exe"), "--host=127.0.0.1 --port=" + port + " --user=root --password=" + pass, 300000, out code);
                log(Tr.T("mariadb-upgrade : ") + (code == 0 ? "terminé" : Tr.T("code ") + code + " " + o.Trim()));
                if (InstalledVersion() == "") throw new Exception(Tr.T("Le nouveau moteur ne répond pas."));
                log(Tr.T("Mise à jour terminée : ") + before + " -> " + InstalledVersion());
            }
            catch (Exception ex)
            {
                log(Tr.T("ÉCHEC (") + ex.Message + Tr.T(") : retour à l'ancien moteur et aux données sauvegardées."));
                Stop(); try { if (proc != null) proc.Kill(); } catch { } Thread.Sleep(2000);
                if (Directory.Exists(EngineDir)) Directory.Delete(EngineDir, true);
                Directory.Move(old, EngineDir);
                Directory.Delete(DataDir, true); CopyDir(bak, DataDir);
                Start(port, pass);
                throw new Exception(Tr.T("Mise à jour annulée, ancien moteur remis en service : ") + ex.Message);
            }
            try { File.Delete(zip); } catch { }
        }
    }
}
