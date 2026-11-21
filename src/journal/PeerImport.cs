using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace HamRadioLink.Journal
{
    public class PeerDb
    {
        public string Title, Dsn, Host = "127.0.0.1", Database = "", User = "", Password = ""; public int Port = 3306;
        public bool IsMySql;
        public long Count = -1; public string Error = "";
        public override string ToString() { return Title + (IsMySql ? Tr.T("  (MariaDB ") + Host + ":" + Port + Tr.T(", base ") + Database + (Count >= 0 ? ", " + Count + " QSO" : "") + ")" : "");  }
    }

    // Tout ce que l'autre logiciel de journal a rangé sur cet ordinateur : station, comptes en ligne, cluster, radio, journaux.
    public class PeerProfile
    {
        public bool Found;
        public Dictionary<string, string> Station = new Dictionary<string, string>();
        public Dictionary<string, string> Settings = new Dictionary<string, string>();   // clés HamRadioLink (cfg) -> valeur
        public List<PeerDb> Dbs = new List<PeerDb>();
        public List<string> Notes = new List<string>();

        // Table des pays de l'autre logiciel (copiée une fois dans data\journal), ou null.
        public static string CountriesFile()
        {
            string pf86 = Environment.GetEnvironmentVariable(Tr.T("ProgramFiles(x86)")) ?? "";
            foreach (string s in new[] { Path.Combine(RigDir, Tr.T("HRD Countries.txt")), Path.Combine(pf86, Tr.T("HRD Software LLC\\Ham Radio Deluxe\\Defaults\\HRD Countries.txt")) }) if (File.Exists(s)) return s;
            return null;
        }
        static string Roaming { get { return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData); } }
        public static string LogbookDir { get { return Path.Combine(Roaming, Tr.T("HRDLLC\\HRD Logbook")); } }
        public static string RigDir { get { return Path.Combine(Roaming, Tr.T("HRDLLC\\Ham Radio Deluxe")); } }

        static XmlDocument Load(string f) { if (!File.Exists(f)) return null; try { XmlDocument d = new XmlDocument(); d.XmlResolver = null; d.Load(f); return d; } catch { return null; } }
        static string El(XmlDocument d, string name) { if (d == null) return ""; XmlNodeList l = d.GetElementsByTagName(name); return l.Count > 0 ? l[0].InnerText.Trim() : ""; }

        public static PeerProfile Read()
        {
            PeerProfile p = new PeerProfile();
            string pf86 = Environment.GetEnvironmentVariable(Tr.T("ProgramFiles(x86)")) ?? "";
            p.Found = Directory.Exists(LogbookDir) || Directory.Exists(RigDir) || Directory.Exists(Path.Combine(pf86, Tr.T("HRD Software LLC")));
            if (!p.Found) return p;
            Dictionary<string, string> s = p.Settings;

            // Station (My Station profile)
            foreach (string f in Directory.Exists(LogbookDir) ? Directory.GetFiles(LogbookDir, "ClientMyStationProfile_*.xml") : new string[0])
            {
                XmlDocument d = Load(f); if (d == null) continue; XmlNodeList fl = d.GetElementsByTagName("Fields"); if (fl.Count == 0) continue;
                foreach (XmlAttribute a in fl[0].Attributes) p.Station[a.Name] = a.Value;
                if (p.Station.ContainsKey("Callsign") && p.Station["Callsign"].Length > 0) break;
            }
            Func<string, string> st = k => { string v; return p.Station.TryGetValue(k, out v) ? v : ""; };
            if (st("Callsign").Length > 0) s["me.call"] = st("Callsign").ToUpperInvariant();
            if (st("Locator").Length > 0) s["me.grid"] = st("Locator").ToUpperInvariant();
            string nm = st("Operator").Length > 0 ? st("Operator") : st("Name"); if (nm.Length > 0) s["me.name"] = nm;
            if (st("City").Length > 0) s["me.city"] = st("City");
            if (st("Country").Length > 0) s["me.country"] = st("Country");
            if (st("Zip").Length > 0) s["me.zip"] = st("Zip");
            if (st("Equipment").Length > 0) s["me.rig"] = st("Equipment");
            if (st("cqzone").Length > 0) s["me.cqz"] = st("cqzone");
            if (st("ituzone").Length > 0) s["me.ituz"] = st("ituzone");
            if (st("Latitude").Length > 0 && st("Latitude") != "0.000000") { s["me.lat"] = st("Latitude"); s["me.lon"] = st("Longitude"); }

            // Recherche d'indicatifs (QRZ.com, HamQTH)
            XmlDocument lk = Load(Path.Combine(LogbookDir, "ClientLogbookCallsignLookup.xml"));
            if (lk != null)
            {
                XmlNodeList ol = lk.GetElementsByTagName("Options");
                if (ol.Count > 0)
                {
                    XmlElement o = (XmlElement)ol[0];
                    if (o.GetAttribute("QRZUsername").Length > 0) { s["lookup.service"] = "QRZ"; s["lookup.user"] = o.GetAttribute("QRZUsername"); s["lookup.pass"] = o.GetAttribute("QRZPassword"); }
                    else if (o.GetAttribute("HamQthUsername").Length > 0) { s["lookup.service"] = "HamQTH"; s["lookup.user"] = o.GetAttribute("HamQthUsername"); s["lookup.pass"] = o.GetAttribute("HamQthPassword"); }
                }
            }
            // DX Cluster et autres préférences
            XmlDocument us = Load(Path.Combine(LogbookDir, "LogbookUserSettings.xml"));
            if (us != null)
            {
                if (El(us, "DXCCallsign").Length > 0) s["cluster.call"] = El(us, "DXCCallsign");
                if (El(us, "DXCQTH").Length > 0 && !s.ContainsKey("me.city")) s["me.city"] = El(us, "DXCQTH");
                if (El(us, "DXCLocator").Length > 0 && !s.ContainsKey("me.grid")) s["me.grid"] = El(us, "DXCLocator").ToUpperInvariant();
                double la, lo; if (double.TryParse(El(us, "HomeMyLatitude"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out la) && double.TryParse(El(us, "HomeMyLongitude"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out lo) && (la != 0 || lo != 0)) { s["me.lat"] = la.ToString(System.Globalization.CultureInfo.InvariantCulture); s["me.lon"] = lo.ToString(System.Globalization.CultureInfo.InvariantCulture); }
            }
            // Journaux : LogbookDatabases.xml (DSN ODBC -> serveur, port, base, utilisateur, mot de passe)
            XmlDocument db = Load(Path.Combine(LogbookDir, "LogbookDatabases.xml"));
            if (db != null)
                foreach (XmlNode n in db.GetElementsByTagName("Database"))
                {
                    XmlElement e = (XmlElement)n; PeerDb i = new PeerDb { Title = e.GetAttribute("Title"), Dsn = e.GetAttribute("DSN"), User = e.GetAttribute("Username"), Password = e.GetAttribute("Password") };
                    ResolveDsn(i); p.Dbs.Add(i);
                }
            return p;
        }

        static void ResolveDsn(PeerDb i)
        {
            foreach (var root in new[] { Microsoft.Win32.Registry.CurrentUser, Microsoft.Win32.Registry.LocalMachine })
                foreach (string path in new[] { @"Software\ODBC\ODBC.INI\", @"SOFTWARE\ODBC\ODBC.INI\", @"SOFTWARE\WOW6432Node\ODBC\ODBC.INI\" })
                    using (Microsoft.Win32.RegistryKey k = root.OpenSubKey(path + i.Dsn))
                    {
                        if (k == null) continue;
                        string drv = (k.GetValue("Driver") as string ?? "").ToLowerInvariant();
                        if (drv.Contains("myodbc") || drv.Contains("maria") || drv.Contains("mysql"))
                        {
                            i.IsMySql = true; i.Host = k.GetValue("SERVER") as string ?? "127.0.0.1"; if (i.Host == "localhost") i.Host = "127.0.0.1"; i.Database = k.GetValue("DATABASE") as string ?? "mysql";
                            int pt; if (int.TryParse(k.GetValue("PORT") as string ?? "3306", out pt)) i.Port = pt;
                            string u = k.GetValue("UID") as string; if (!string.IsNullOrEmpty(u) && i.User.Length == 0) i.User = u;
                            string pw = k.GetValue("PWD") as string; if (!string.IsNullOrEmpty(pw) && i.Password.Length == 0) i.Password = pw;
                        }
                        return;
                    }
        }

        // Interroge chaque serveur (lecture seule) : le journal est-il là, combien de QSO ?
        public void Probe(int portOverride = 0)
        {
            foreach (PeerDb i in Dbs)
            {
                if (!i.IsMySql) continue;
                try
                {
                    using (MySqlClient c = new MySqlClient(i.Host, portOverride > 0 ? portOverride : i.Port, i.User, i.Password, i.Database))
                    {
                        c.Open(); string n = c.Scalar("SELECT COUNT(*) FROM " + MySqlClient.Ident(i.Database) + "." + MySqlClient.Ident(PeerLog.Table));
                        long v; i.Count = long.TryParse(n, out v) ? v : -1;
                    }
                }
                catch (Exception ex) { i.Error = ex.Message; i.Count = -1; }
            }
        }

        // Applique les réglages récupérés dans la configuration de HamRadioLink (sans écraser ce qui est déjà renseigné, sauf si force).
        public void Apply(Ini cfg, bool force)
        {
            foreach (KeyValuePair<string, string> kv in Settings)
                if (force || cfg.Get(kv.Key, "").Length == 0) cfg.Set(kv.Key, kv.Value);
        }
    }

    public static class Input
    {
        // Indicatifs radioamateurs (F5PBG, DL/F5PBG/P) et indicatifs non radioamateurs du type 14xx1234 : lettres/chiffres, au moins un chiffre, 3 à 16 caractères.
        public static bool Callsign(string c)
        {
            if (string.IsNullOrWhiteSpace(c)) return false; c = c.Trim().ToUpperInvariant();
            return c.Length >= 3 && c.Length <= 16 && Regex.IsMatch(c, @"^[A-Z0-9]+(/[A-Z0-9]+){0,2}$") && Regex.IsMatch(c, "[0-9]");
        }
        public static bool Locator(string g) { return !string.IsNullOrWhiteSpace(g) && Regex.IsMatch(g.Trim().ToUpperInvariant(), @"^[A-R]{2}[0-9]{2}([A-X]{2}([0-9]{2})?)?$"); }
    }

    // Création du journal et copie d'un journal existant.
    public static class LogSetup
    {
        // Crée la base et la table du journal (structure identique à celle de l'autre logiciel) si elles n'existent pas. Ne modifie jamais une table existante.
        public static void EnsureTable(MySqlClient db, string schema, Action<string> log)
        {
            db.Exec("CREATE DATABASE IF NOT EXISTS " + MySqlClient.Ident(schema) + " CHARACTER SET utf8mb4");
            string exists = db.Scalar("SELECT COUNT(*) FROM information_schema.tables WHERE table_schema=" + MySqlClient.Quote(schema) + " AND table_name=" + MySqlClient.Quote(PeerLog.Table));
            if (exists == "1") { log(Tr.T("Table du journal déjà présente : conservée telle quelle.")); return; }
            db.Exec(PeerSchema.CreateSql.Replace("{TABLE}", MySqlClient.Ident(schema) + "." + MySqlClient.Ident(PeerLog.Table)));
            log(Tr.T("Table du journal créée (structure compatible, 122 colonnes)."));
        }

        // Copie tous les QSO du journal source vers le nouveau journal (la source n'est que lue).
        public static long Copy(MySqlClient src, string srcSchema, MySqlClient dst, string dstSchema, Action<long, long> progress)
        {
            string st = MySqlClient.Ident(srcSchema) + "." + MySqlClient.Ident(PeerLog.Table), dt = MySqlClient.Ident(dstSchema) + "." + MySqlClient.Ident(PeerLog.Table);
            long total = long.Parse(src.Scalar("SELECT COUNT(*) FROM " + st));
            MySqlResult dc = dst.Query("SELECT * FROM " + dt + " LIMIT 0"); HashSet<string> dcols = new HashSet<string>(dc.Columns);
            long last = 0, done = 0; int batch = 250;
            while (true)
            {
                MySqlResult r = src.Query("SELECT * FROM " + st + " WHERE COL_PRIMARY_KEY > " + last + " ORDER BY COL_PRIMARY_KEY LIMIT " + batch);
                if (r.Rows.Count == 0) break;
                List<int> idx = new List<int>(); List<string> names = new List<string>();
                for (int i = 0; i < r.Columns.Length; i++) if (dcols.Contains(r.Columns[i])) { idx.Add(i); names.Add(MySqlClient.Ident(r.Columns[i])); }
                int pk = Array.IndexOf(r.Columns, "COL_PRIMARY_KEY");
                StringBuilder sb = new StringBuilder("INSERT INTO " + dt + " (" + string.Join(",", names.ToArray()) + Tr.T(") VALUES "));
                bool first = true;
                foreach (string[] row in r.Rows)
                {
                    if (!first) sb.Append(','); first = false; sb.Append('(');
                    for (int k = 0; k < idx.Count; k++) { if (k > 0) sb.Append(','); sb.Append(MySqlClient.Quote(row[idx[k]])); }
                    sb.Append(')'); last = Math.Max(last, long.Parse(row[pk]));
                }
                dst.Exec(sb.ToString()); done += r.Rows.Count; if (progress != null) progress(done, total);
            }
            return done;
        }
    }
}
