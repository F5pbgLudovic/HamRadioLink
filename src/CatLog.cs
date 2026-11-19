using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace HamRadioLink
{
    // Journal des commandes et des réponses : UN fichier par jour (logs\hamradiolink-AAAA-MM-JJ.log), les plus anciens sont supprimés
    // (7 jours par défaut). Par défaut le journal est « compact » : les ordres envoyés, les réponses qui CHANGENT, les erreurs et les événements
    // (connexion, partage, paires com0com, serveur) ; les relectures périodiques identiques sont comptées, pas écrites. Le mode « détaillé » écrit tout.
    // Un plafond de taille par jour évite qu'un fichier devienne énorme.
    public static class CatLog
    {
        public static bool On, Detail; public static int KeepDays = 7; public static long MaxBytes = 25L * 1024 * 1024;
        static StreamWriter w; static string day, dir; static long size; static readonly object lk = new object(); static Timer flush; static bool capped;
        static readonly Dictionary<string, string> lastRx = new Dictionary<string, string>(); static readonly Dictionary<string, int> omitted = new Dictionary<string, int>();
        public static string Dir { get { return dir ?? Path.Combine(Cfg.Dir ?? ".", "logs"); } }
        public static string TodayFile { get { return Path.Combine(Dir, "hamradiolink-" + DateTime.Now.ToString("yyyy-MM-dd") + ".log"); } }

        public static void Init(string folder)
        {
            lock (lk) { Close(); dir = folder; }
            On = Cfg.GetBool("log.on", true); Detail = Cfg.GetBool("log.detail", false); KeepDays = Math.Max(1, Math.Min(60, Cfg.GetInt("log.days", 7)));
            if (On) { Purge(); Event("programme", "démarrage de HamRadioLink " + MainForm.Version); }
        }
        public static void Reload() { On = Cfg.GetBool("log.on", true); Detail = Cfg.GetBool("log.detail", false); KeepDays = Math.Max(1, Math.Min(60, Cfg.GetInt("log.days", 7))); if (On) Purge(); }
        public static void Purge()
        {
            try
            {
                if (!Directory.Exists(Dir)) return; DateTime limit = DateTime.Now.Date.AddDays(-(KeepDays - 1));
                foreach (string f in Directory.GetFiles(Dir, "hamradiolink-*.log")) { try { DateTime d; if (DateTime.TryParseExact(Path.GetFileNameWithoutExtension(f).Substring(13), "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out d) && d < limit) File.Delete(f); } catch { } }
            }
            catch { }
        }
        static void Open()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (w != null && day == today) return;
            if (w != null) { Summary(); try { w.Flush(); w.Dispose(); } catch { } w = null; Purge(); }
            Directory.CreateDirectory(Dir); string p = TodayFile; size = File.Exists(p) ? new FileInfo(p).Length : 0; capped = false;
            w = new StreamWriter(new FileStream(p, FileMode.Append, FileAccess.Write, FileShare.ReadWrite), new UTF8Encoding(false)); day = today;
            if (flush == null) flush = new Timer(delegate { lock (lk) { try { if (w != null) w.Flush(); } catch { } } }, null, 1000, 1000);
        }
        static void Summary()
        {
            if (w == null) return; foreach (KeyValuePair<string, int> kv in omitted) if (kv.Value > 0) w.WriteLine("# " + kv.Key + " : " + kv.Value + " réponse(s) identique(s) non écrite(s) (journal compact)"); omitted.Clear();
        }
        static void Put(string radio, string kind, string text)
        {
            if (!On) return;
            lock (lk)
            {
                try
                {
                    Open(); if (capped) return;
                    string line = DateTime.Now.ToString("HH:mm:ss.fff") + "  " + (radio ?? "").PadRight(10) + " " + kind.PadRight(3) + "  " + text.Replace("\r", " ").Replace("\n", " ");
                    if (size + line.Length > MaxBytes) { capped = true; w.WriteLine("# plafond de " + (MaxBytes / (1024 * 1024)) + " Mo atteint : le reste de la journée n'est pas écrit"); return; }
                    w.WriteLine(line); size += line.Length + 2;
                }
                catch { }
            }
        }
        public static void Event(string radio, string text) { Put(radio, "EVT", text); }
        // ordre envoyé au poste (poll = relecture périodique du moteur : écrite seulement en mode détaillé)
        public static void Tx(string radio, string text, bool poll) { if (!On || (poll && !Detail)) return; Put(radio, "TX", text); }
        // réponse du poste : en mode compact, seulement si elle diffère de la dernière du même genre
        public static void Rx(string radio, string text)
        {
            if (!On) return;
            if (!Detail && text != "?;" && text != "E;" && text != "O;")
            {
                string key = (radio ?? "") + "|" + (text.Length >= 4 ? text.Substring(0, 4) : text); string last;
                lock (lk) { if (lastRx.TryGetValue(key, out last) && last == text) { int n; omitted.TryGetValue(radio ?? "", out n); omitted[radio ?? ""] = n + 1; return; } lastRx[key] = text; if (lastRx.Count > 2000) lastRx.Clear(); }
            }
            Put(radio, "RX", text);
        }
        // commande reçue d'un logiciel client du serveur de commande : ordres et erreurs toujours, relectures seulement quand la réponse CHANGE (ou en mode détaillé)
        static readonly Dictionary<string, string> lastSrv = new Dictionary<string, string>();
        public static void Server(string who, string cmd, string reply)
        {
            if (!On) return; string c = System.Text.RegularExpressions.Regex.Replace(cmd, @"^\[\d+\]\s*", "");
            bool order = c.StartsWith("set ", StringComparison.OrdinalIgnoreCase);
            if (!Detail && !order && reply != "ERROR") { string last; lock (lk) { if (lastSrv.TryGetValue(who + "|" + c, out last) && last == reply) return; lastSrv[who + "|" + c] = reply; if (lastSrv.Count > 500) lastSrv.Clear(); } }
            Put(who, "EVT", cmd + " -> " + reply);
        }
        public static void Close() { lock (lk) { try { if (w != null) { Summary(); w.Flush(); w.Dispose(); } } catch { } w = null; day = null; if (flush != null) { flush.Dispose(); flush = null; } } }
    }
}
