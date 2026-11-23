using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace HamRadioLink.Journal
{
    public class LookupResult
    {
        public string Call, Name, Qth, Country, Grid, Email, Web, Iota, State, County, Address, Zip, QslVia, Source;
        public int Dxcc, Cq, Itu; public double Lat, Lon; public bool HasLatLon;
    }

    // Accès aux services en ligne : recherche d'indicatif (HamQTH, QRZ.com), envoi des QSO (QRZ Logbook, Club Log, eQSL, LoTW via TQSL).
    public static class Online
    {
        static Online() { ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | SecurityProtocolType.Tls; }

        public static string Http(string url, string postData = null, int timeout = 15000)
        {
            HttpWebRequest r = (HttpWebRequest)WebRequest.Create(url);
            r.UserAgent = "HamRadioLink/" + App.Version; r.Timeout = timeout;
            if (postData != null)
            {
                byte[] b = Encoding.UTF8.GetBytes(postData);
                r.Method = "POST"; r.ContentType = "application/x-www-form-urlencoded"; r.ContentLength = b.Length;
                using (Stream s = r.GetRequestStream()) s.Write(b, 0, b.Length);
            }
            try
            {
                using (HttpWebResponse resp = (HttpWebResponse)r.GetResponse()) using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8)) return sr.ReadToEnd();
            }
            catch (WebException ex)
            {
                if (ex.Response != null) using (StreamReader sr = new StreamReader(ex.Response.GetResponseStream(), Encoding.UTF8)) return sr.ReadToEnd();
                throw;
            }
        }

        static string Enc(string s)
        {
            s = s ?? ""; if (s.Length <= 20000) return Uri.EscapeDataString(s);
            StringBuilder sb = new StringBuilder(); int i = 0;   // Uri.EscapeDataString refuse plus de ~65 000 caractères : on découpe (sans couper une paire de substitution)
            while (i < s.Length) { int n = Math.Min(20000, s.Length - i); if (n < s.Length - i && char.IsHighSurrogate(s[i + n - 1])) n--; sb.Append(Uri.EscapeDataString(s.Substring(i, n))); i += n; }
            return sb.ToString();
        }
        static string Tag(string xml, string tag) { Match m = Regex.Match(xml, "<" + tag + "(?:\\s[^>]*)?>(.*?)</" + tag + ">", RegexOptions.Singleline | RegexOptions.IgnoreCase); return m.Success ? System.Net.WebUtility.HtmlDecode(m.Groups[1].Value.Trim()) : ""; }
        static int ToInt(string s) { int i; return int.TryParse(s, out i) ? i : 0; }

        // ---------- recherche d'indicatif ----------
        public static LookupResult Lookup(string call)
        {
            string svc = App.Cfg.Get("lookup.service", "HamQTH");
            if (svc == "QRZ.com") return LookupQrz(call);
            if (svc == "HamQTH") return LookupHamQth(call);
            return null;
        }

        static string hamQthSession; static DateTime hamQthTime; static string qrzKey; static DateTime qrzTime;

        static LookupResult LookupHamQth(string call)
        {
            string u = App.Cfg.Get("lookup.user", ""), p = App.Cfg.Get("lookup.pass", ""); if (u.Length == 0) throw new Exception(Tr.T("Renseignez votre compte HamQTH (Tools > Online services)."));
            for (int attempt = 0; attempt < 2; attempt++)
            {
                if (hamQthSession == null || (DateTime.Now - hamQthTime).TotalMinutes > 50)
                {
                    string x = Http(Tr.T("https://www.hamqth.com/xml.php?u=") + Enc(u) + "&p=" + Enc(p));
                    hamQthSession = Tag(x, "session_id"); hamQthTime = DateTime.Now;
                    if (hamQthSession.Length == 0) throw new Exception(Tr.T("HamQTH : ") + Tag(x, "error"));
                }
                string r = Http(Tr.T("https://www.hamqth.com/xml.php?id=") + hamQthSession + "&callsign=" + Enc(call) + "&prg=HamRadioLink");
                string err = Tag(r, "error");
                if (err.Length > 0) { if (err.IndexOf("expired", StringComparison.OrdinalIgnoreCase) >= 0 || err.IndexOf("Session", StringComparison.OrdinalIgnoreCase) >= 0) { hamQthSession = null; continue; } return null; }
                LookupResult l = new LookupResult { Source = "HamQTH", Call = Tag(r, "callsign").ToUpperInvariant(), Name = Tag(r, "adr_name"), Qth = Tag(r, "qth"), Country = Tag(r, "country"), Grid = Tag(r, "grid"), Email = Tag(r, "email"), Web = Tag(r, "web"),
                    Iota = Tag(r, "iota"), State = Tag(r, "us_state"), County = Tag(r, "us_county"), Address = Tag(r, "adr_street1"), Zip = Tag(r, "adr_zip"), QslVia = Tag(r, "qsl_via"), Cq = ToInt(Tag(r, "cq")), Itu = ToInt(Tag(r, "itu")), Dxcc = ToInt(Tag(r, "adif")) };
                if (l.Name.Length == 0) l.Name = Tag(r, "nick");
                double la, lo; if (double.TryParse(Tag(r, "latitude"), NumberStyles.Float, CultureInfo.InvariantCulture, out la) && double.TryParse(Tag(r, "longitude"), NumberStyles.Float, CultureInfo.InvariantCulture, out lo)) { l.Lat = la; l.Lon = lo; l.HasLatLon = true; }
                return l;
            }
            return null;
        }

        static LookupResult LookupQrz(string call)
        {
            string u = App.Cfg.Get("lookup.user", ""), p = App.Cfg.Get("lookup.pass", ""); if (u.Length == 0) throw new Exception(Tr.T("Renseignez votre compte QRZ.com (Tools > Online services)."));
            for (int attempt = 0; attempt < 2; attempt++)
            {
                if (qrzKey == null || (DateTime.Now - qrzTime).TotalMinutes > 50)
                {
                    string x = Http(Tr.T("https://xmldata.qrz.com/xml/current/?username=") + Enc(u) + Tr.T(";password=") + Enc(p) + Tr.T(";agent=HamRadioLink") + App.Version);
                    qrzKey = Tag(x, "Key"); qrzTime = DateTime.Now; if (qrzKey.Length == 0) throw new Exception(Tr.T("QRZ.com : ") + Tag(x, "Error"));
                }
                string r = Http(Tr.T("https://xmldata.qrz.com/xml/current/?s=") + qrzKey + Tr.T(";callsign=") + Enc(call));
                string err = Tag(r, "Error");
                if (err.Length > 0) { if (err.IndexOf("Session", StringComparison.OrdinalIgnoreCase) >= 0 || err.IndexOf("Invalid", StringComparison.OrdinalIgnoreCase) >= 0) { qrzKey = null; continue; } return null; }
                string fn = Tag(r, "fname"), nm = Tag(r, "name");
                LookupResult l = new LookupResult { Source = "QRZ.com", Call = Tag(r, "call").ToUpperInvariant(), Name = (fn + " " + nm).Trim(), Qth = Tag(r, "addr2"), Country = Tag(r, "country"), Grid = Tag(r, "grid"), Email = Tag(r, "email"), Web = Tag(r, "url"),
                    Iota = Tag(r, "iota"), State = Tag(r, "state"), County = Tag(r, "county"), Address = Tag(r, "addr1"), Zip = Tag(r, "zip"), QslVia = Tag(r, "mqsl") == "1" ? "" : Tag(r, "qslmgr"), Cq = ToInt(Tag(r, "cqzone")), Itu = ToInt(Tag(r, "ituzone")), Dxcc = ToInt(Tag(r, "dxcc")) };
                double la, lo; if (double.TryParse(Tag(r, "lat"), NumberStyles.Float, CultureInfo.InvariantCulture, out la) && double.TryParse(Tag(r, "lon"), NumberStyles.Float, CultureInfo.InvariantCulture, out lo)) { l.Lat = la; l.Lon = lo; l.HasLatLon = true; }
                return l;
            }
            return null;
        }

        // ---------- envoi des QSO ----------
        // ADIF des envois en ligne : sans le commentaire si la case « Ne pas transmettre les commentaires » est cochée (Services en ligne)
        public static string ExportOnline(IEnumerable<Qso> qsos)
        {
            if (!App.Cfg.GetBool("online.nocomment", false)) return Adif.Export(qsos);
            List<Qso> c = new List<Qso>(); foreach (Qso q in qsos) { Qso n = new Qso(); foreach (KeyValuePair<string, string> kv in q) if (kv.Key != "COL_COMMENT") n[kv.Key] = kv.Value; c.Add(n); }
            return Adif.Export(c);
        }
        static string OneAdif(List<Qso> q) { return ExportOnline(q); }

        public static string UploadQrz(List<Qso> qsos)
        {
            string key = App.Cfg.Get("qrzlog.key", ""); if (key.Length == 0) throw new Exception(Tr.T("QRZ Logbook : clé API à renseigner (Tools > Online services)."));
            int ok = 0, dup = 0; string msg = "";
            foreach (Qso q in qsos)
            {
                string adif = OneAdif(new List<Qso> { q }); adif = adif.Substring(adif.IndexOf("<EOH>") + 5).Trim();
                string r = Http("https://logbook.qrz.com/api", Tr.T("KEY=") + Enc(key) + "&ACTION=INSERT&ADIF=" + Enc(adif));
                if (r.Contains("RESULT=OK")) ok++; else if (r.Contains("duplicate")) dup++; else msg = r;
            }
            if (ok + dup == 0 && msg.Length > 0) throw new Exception(Tr.T("QRZ Logbook : ") + msg);
            return Tr.T("QRZ Logbook : ") + ok + " envoyés, " + dup + Tr.T(" déjà présents.");
        }

        public static string UploadClubLog(List<Qso> qsos)
        {
            // Club Log (API REST) : un jeton personnel « cl_… » créé dans Club Log (Settings > API) ; plus d'e-mail, de mot de passe ni de clé de logiciel.
            string call = App.Cfg.Get("clublog.call", App.Cfg.Get("me.call", "")), tok = App.Cfg.Get("clublog.token", "").Trim();
            if (tok.Length == 0) throw new Exception(Tr.T("Club Log : jeton à renseigner (Outils > Online services ; à créer sur clublog.org > Settings > API)."));
            if (call.Length == 0) throw new Exception(Tr.T("Club Log : indicatif à renseigner (Outils > Online services)."));
            int ok = 0, dup = 0;
            for (int i = 0; i < qsos.Count; i += 100)   // une requête par lot de 100 (maximum de l API)
            {
                StringBuilder sb = new StringBuilder("{\"qsos\":[");
                for (int j = i; j < Math.Min(i + 100, qsos.Count); j++)
                {
                    string adif = OneAdif(new List<Qso> { qsos[j] }); adif = adif.Substring(adif.IndexOf("<EOH>") + 5).Trim();
                    if (j > i) sb.Append(',');
                    sb.Append("{\"call\":\"" + JEsc(call) + "\",\"adif\":\"" + JEsc(adif) + "\"}");
                }
                sb.Append("]}");
                int code; string r = HttpBearer("https://clublog.org/api/v1/qso/batch", sb.ToString(), tok, out code);
                if (code == 401) throw new Exception(Tr.T("Club Log : jeton refusé ou révoqué (arrêt). Créez un nouveau jeton sur clublog.org > Settings > API."));
                if (code == 403) throw new Exception(Tr.T("Club Log : cet indicatif n'appartient pas au compte du jeton (arrêt)."));
                if (code == 429) throw new Exception(Tr.T("Club Log : trop de requêtes, réessayez dans 5 minutes."));
                if (code != 200) throw new Exception(Tr.T("Club Log : ") + code + " " + r.Trim());
                foreach (Match m in Regex.Matches(r, "\"status\"\\s*:\\s*\"(\\w+)\""))
                {
                    string s = m.Groups[1].Value; if (s == "created" || s == "updated") ok++; else if (s == "duplicate") dup++;
                }
            }
            if (ok + dup == 0 && qsos.Count > 0) throw new Exception(Tr.T("Club Log : aucun QSO accepté (") + (qsos.Count) + Tr.T(" refusés par Club Log)."));   // doublons ou refus isolés : on continue
            return Tr.T("Club Log : ") + ok + " envoyés, " + dup + Tr.T(" déjà présents.");
        }

        static string JEsc(string s) { return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t"); }

        static string HttpBearer(string url, string json, string token, out int code)
        {
            HttpWebRequest r = (HttpWebRequest)WebRequest.Create(url);
            r.UserAgent = "HamRadioLink/" + App.Version + " (F5PBG)"; r.Timeout = 60000; r.Method = "POST"; r.ContentType = "application/json"; if (token != null) r.Headers["Authorization"] = "Bearer " + token;
            byte[] b = Encoding.UTF8.GetBytes(json); r.ContentLength = b.Length;
            using (Stream s = r.GetRequestStream()) s.Write(b, 0, b.Length);
            try
            {
                using (HttpWebResponse resp = (HttpWebResponse)r.GetResponse()) using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8)) { code = (int)resp.StatusCode; return sr.ReadToEnd(); }
            }
            catch (WebException ex)
            {
                HttpWebResponse er = ex.Response as HttpWebResponse; if (er == null) throw;
                code = (int)er.StatusCode; using (StreamReader sr = new StreamReader(er.GetResponseStream(), Encoding.UTF8)) return sr.ReadToEnd();
            }
        }

        static string AdifBody(Qso q) { string adif = OneAdif(new List<Qso> { q }); return adif.Substring(adif.IndexOf("<EOH>") + 5).Trim(); }

        // HRDLog.net : « code d'envoi » reçu par e-mail (pas le mot de passe). Format du service non publié : à confirmer par un essai réel.
        public static string UploadHrdlog(List<Qso> qsos)
        {
            string call = App.Cfg.Get("hrdlog.call", App.Cfg.Get("me.call", "")), code = App.Cfg.Get("hrdlog.code", "").Trim();
            if (code.Length == 0 || call.Length == 0) throw new Exception(Tr.T("HRDLog.net : indicatif et code d'envoi à renseigner (Outils > Online services)."));
            int ok = 0, dup = 0;
            foreach (Qso q in qsos)
            {
                string r = Http("https://robot.hrdlog.net/NewEntry.aspx", "Callsign=" + Enc(call) + "&Code=" + Enc(code) + "&App=" + Enc("HamRadioLink") + "&ADIFData=" + Enc(AdifBody(q)), 30000);
                if (r.IndexOf("dup", StringComparison.OrdinalIgnoreCase) >= 0) { dup++; continue; }   // doublon : on continue
                if (r.IndexOf("<error", StringComparison.OrdinalIgnoreCase) >= 0 || r.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0 && r.IndexOf("<insert", StringComparison.OrdinalIgnoreCase) < 0) throw new Exception(Tr.T("HRDLog.net : ") + Regex.Replace(r, "<[^>]+>", " ").Trim());
                ok++;
            }
            return Tr.T("HRDLog.net : ") + ok + " envoyés, " + dup + Tr.T(" déjà présents.");
        }

        static DateTime lastQrzcq = DateTime.MinValue;
        // QRZCQ.com : API JSON (indicatif + clé API), « pas plus d'un envoi par minute » : on attend avant chaque requête si besoin.
        public static string UploadQrzcq(List<Qso> qsos)
        {
            string call = App.Cfg.Get("qrzcq.call", App.Cfg.Get("me.call", "")), key = App.Cfg.Get("qrzcq.key", "").Trim();
            if (key.Length == 0 || call.Length == 0) throw new Exception(Tr.T("QRZCQ.com : indicatif et clé API à renseigner (Outils > Online services)."));
            double wait = 61 - (DateTime.UtcNow - lastQrzcq).TotalSeconds; if (wait > 0) System.Threading.Thread.Sleep((int)(wait * 1000));
            string adif = ExportOnline(qsos); adif = adif.Substring(adif.IndexOf("<EOH>") + 5).Trim();
            int code; string r = HttpBearer("https://ssl.qrzcq.com/api/logupload", "{\"auth\":{\"call\":\"" + JEsc(call) + "\",\"key\":\"" + JEsc(key) + "\"},\"data\":{\"adif\":\"" + JEsc(adif) + "\"}}", null, out code);
            lastQrzcq = DateTime.UtcNow;
            if (code != 200 || !Regex.IsMatch(r, "\"status\"\\s*:\\s*\"OK\"", RegexOptions.IgnoreCase)) throw new Exception(Tr.T("QRZCQ.com : ") + code + " " + r.Trim());
            return Tr.T("QRZCQ.com : ") + qsos.Count + " QSO transmis (en file d'attente chez QRZCQ).";
        }

        // HamQTH : envoi en temps réel, un QSO par requête (le service déconseille l'envoi de milliers de QSO ainsi) ; identifiant et mot de passe de HamQTH.
        public static string UploadHamqth(List<Qso> qsos)
        {
            string u = App.Cfg.Get("hamqth.user", App.Cfg.Get("lookup.user", "")), p = App.Cfg.Get("hamqth.pass", App.Cfg.Get("lookup.pass", ""));
            if (u.Length == 0 || p.Length == 0) throw new Exception(Tr.T("HamQTH : identifiant et mot de passe à renseigner (Outils > Online services)."));
            int ok = 0, rej = 0;
            foreach (Qso q in qsos)
            {
                string r = Http("https://www.hamqth.com/qso_realtime.php", "u=" + Enc(u) + "&p=" + Enc(p) + "&prg=HamRadioLink&cmd=insert&adif=" + Enc(AdifBody(q)), 30000);
                if (r.IndexOf("QSO OK", StringComparison.OrdinalIgnoreCase) >= 0 || r.IndexOf("inserted", StringComparison.OrdinalIgnoreCase) >= 0 || r.IndexOf("updated", StringComparison.OrdinalIgnoreCase) >= 0) ok++;   // HamQTH répond « QSO inserted » (la doc annonçait « QSO OK »)
                else if (r.IndexOf("Forbidden", StringComparison.OrdinalIgnoreCase) < 0 && r.IndexOf("Access denied", StringComparison.OrdinalIgnoreCase) < 0 && r.IndexOf("Internal error", StringComparison.OrdinalIgnoreCase) < 0 && r.IndexOf("password", StringComparison.OrdinalIgnoreCase) < 0 && r.IndexOf("user", StringComparison.OrdinalIgnoreCase) < 0 && r.Trim().Length > 0) rej++;   // tout refus de ce QSO (déjà présent, bande inconnue…) : on continue ; seules les erreurs d'accès arrêtent   // doublon, bande inconnue ou champ manquant : on continue
                else throw new Exception(Tr.T("HamQTH : ") + Regex.Replace(r, "<[^>]+>", " ").Trim());
            }
            return Tr.T("HamQTH : ") + ok + " envoyés" + (rej > 0 ? ", " + rej + Tr.T(" refusés par HamQTH (doublons ou fiches incomplètes).") : ".");
        }

        static string HttpReq(string method, string url, string body, string contentType, string bearer, out int code)
        {
            HttpWebRequest r = (HttpWebRequest)WebRequest.Create(url);
            r.UserAgent = "HamRadioLink/" + App.Version + " (F5PBG)"; r.Timeout = 60000; r.Method = method;
            if (bearer != null) r.Headers["Authorization"] = "Bearer " + bearer;
            if (body != null) { r.ContentType = contentType; byte[] b = Encoding.UTF8.GetBytes(body); r.ContentLength = b.Length; using (Stream s = r.GetRequestStream()) s.Write(b, 0, b.Length); }
            try
            {
                using (HttpWebResponse resp = (HttpWebResponse)r.GetResponse()) using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8)) { code = (int)resp.StatusCode; return sr.ReadToEnd(); }
            }
            catch (WebException ex)
            {
                HttpWebResponse er = ex.Response as HttpWebResponse; if (er == null) throw;
                code = (int)er.StatusCode; using (StreamReader sr = new StreamReader(er.GetResponseStream(), Encoding.UTF8)) return sr.ReadToEnd();
            }
        }

        // World Radio League : liste des journaux du compte (id, nom) et identifiant du journal par défaut (null si aucun choisi).
        public static List<string[]> WrlLogbooks(string key, out string defId)
        {
            defId = null; List<string[]> l = new List<string[]>(); int code;
            string me = HttpReq("GET", "https://api.worldradioleague.com/v1/me", null, null, key, out code);
            if (code == 401 || code == 403) throw new Exception(Tr.T("World Radio League : clé API refusée ou révoquée."));
            Match md = Regex.Match(me, "\"defaultLogbook\"\\s*:\\s*\\{[^{}]*?\"logbookId\"\\s*:\\s*\"([^\"]+)\""); if (md.Success) defId = md.Groups[1].Value;
            string r = HttpReq("GET", "https://api.worldradioleague.com/v1/logbooks?limit=100", null, null, key, out code);
            if (code != 200) throw new Exception(Tr.T("World Radio League : ") + code + " " + Regex.Replace(r, "\\s+", " ").Trim());
            foreach (Match m in Regex.Matches(r, "\"id\"\\s*:\\s*\"([0-9a-fA-F\\-]{36})\"[^{}]*?\"name\"\\s*:\\s*\"([^\"]*)\"")) l.Add(new string[] { m.Groups[1].Value, m.Groups[2].Value });
            return l;
        }

        // Journal de destination : celui réglé (wrl.logbook), sinon le journal par défaut du compte, sinon l'unique journal ; plusieurs sans défaut = erreur explicite.
        static string WrlLogbook(string key)
        {
            string lb = App.Cfg.Get("wrl.logbook", "").Trim(); if (lb.Length > 0) return lb;
            string def; List<string[]> l = WrlLogbooks(key, out def);
            if (def != null) return def;
            if (l.Count == 1) return l[0][0];
            if (l.Count == 0) throw new Exception(Tr.T("World Radio League : aucun journal (logbook) trouvé sur votre compte."));
            throw new Exception(Tr.T("World Radio League : plusieurs journaux et aucun par défaut. Choisissez-en un dans Online services (bouton « Mes journaux »)."));
        }
        // World Radio League : clé API (Integrations > Developer API), envoi d'un fichier ADIF en 3 étapes (démarrer l'import, déposer le fichier, suivre le traitement).
        public static string UploadWrl(List<Qso> qsos)
        {
            string key = App.Cfg.Get("wrl.key", "").Trim();
            if (key.Length == 0) throw new Exception(Tr.T("World Radio League : clé API à renseigner (Outils > Online services)."));
            string adif = ExportOnline(qsos); int code; string r = ""; string lb = WrlLogbook(key);
            for (int t = 0; t < 12; t++)   // un seul import à la fois par compte : on attend si un autre est en cours
            {
                r = HttpReq("POST", "https://api.worldradioleague.com/v1/imports", "{\"programId\":\"HamRadioLink\"" + (lb.Length > 0 ? ",\"logbookId\":\"" + lb + "\"" : "") + "}", "application/json", key, out code);
                if (code == 200 || code == 201) break;
                if (code == 409 && t < 11) { System.Threading.Thread.Sleep(10000); continue; }
                if (code == 401 || code == 403) throw new Exception(Tr.T("World Radio League : clé API refusée ou révoquée (arrêt)."));
                if (code == 429) throw new Exception(Tr.T("World Radio League : trop de requêtes ou quota d'import du jour atteint, réessayez plus tard."));
                throw new Exception(Tr.T("World Radio League : ") + code + " " + Regex.Replace(r, "\\s+", " ").Trim());
            }
            Match mid = Regex.Match(r, "\"id\"\\s*:\\s*\"([^\"]+)\""), mu = Regex.Match(r, "\"url\"\\s*:\\s*\"([^\"]+)\"");
            if (!mid.Success || !mu.Success) throw new Exception(Tr.T("World Radio League : réponse inattendue ") + Regex.Replace(r, "\\s+", " ").Trim());
            string up = mu.Groups[1].Value.Replace("\\u0026", "&").Replace("\\/", "/");
            HttpReq("PUT", up, adif, "text/plain", null, out code);   // dépôt du fichier : sans clé API
            if (code < 200 || code >= 300) throw new Exception(Tr.T("World Radio League : dépôt du fichier refusé (") + code + ").");
            for (int i = 0; i < 60; i++)   // suivi : toutes les 3 s, 3 minutes au plus
            {
                System.Threading.Thread.Sleep(3000);
                r = HttpReq("GET", "https://api.worldradioleague.com/v1/imports/" + mid.Groups[1].Value, null, null, key, out code);
                Match ms = Regex.Match(r, "\"status\"\\s*:\\s*\"(\\w+)\""); string st = ms.Success ? ms.Groups[1].Value : "";
                if (st == "complete" || st == "complete_with_errors") return Tr.T("World Radio League : ") + qsos.Count + Tr.T(" QSO transmis (les doublons sont ignorés par le service).");
                if (st == "failed" || st == "rejected" || st == "expired" || st == "cancelled")
                {
                    Match me = Regex.Match(r, "\"errorCode\"\\s*:\\s*\"(\\w+)\""); throw new Exception(Tr.T("World Radio League : import ") + st + (me.Success ? " (" + me.Groups[1].Value + ")" : ""));
                }
            }
            throw new Exception(Tr.T("World Radio League : le traitement de l'import dépasse 3 minutes ; vérifiez sur le site."));
        }

        public static string UploadEqsl(List<Qso> qsos)
        {
            string u = App.Cfg.Get("eqsl.user", ""), p = App.Cfg.Get("eqsl.pass", ""); if (u.Length == 0) throw new Exception(Tr.T("eQSL : identifiant et mot de passe à renseigner."));
            int sent = 0; string last = "";
            foreach (Qso q in qsos)   // une fiche par requête, au fur et à mesure
            {
                string adif = OneAdif(new List<Qso> { q }); int eoh = adif.IndexOf("<EOH>");
                string body = "<EQSL_USER:" + u.Length + ">" + u + "<EQSL_PSWD:" + p.Length + ">" + p + "<PROGRAMID:12>HamRadioLink<EOH>" + adif.Substring(eoh + 5);
                string r = Http("https://www.eqsl.cc/qslcard/ImportADIF.cfm", Tr.T("ADIFData=") + Enc(body), 60000);
                if (r.IndexOf("Result: ", StringComparison.OrdinalIgnoreCase) < 0 && r.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0) throw new Exception(Tr.T("eQSL (") + q.S("COL_CALL") + ") : " + Regex.Replace(r, "<[^>]+>", " ").Trim());
                Match m = Regex.Match(r, Tr.T("Result:[^<]*"), RegexOptions.IgnoreCase); last = m.Success ? m.Value : ""; sent++;
            }
            return Tr.T("eQSL : ") + sent + " envoyé(s)" + (last.Length > 0 ? " (" + last + ")" : "") + ".";
        }

        // Lance TQSL en lisant sa sortie AU FIL DE L'EAU (sinon, sur un gros fichier, le tuyau se remplit et TQSL reste bloqué) ; code = -1 si délai dépassé
        public static string RunTqsl(string exe, string args, int timeoutMs, out int code)
        {
            ProcessStartInfo psi = new ProcessStartInfo(exe, args) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true };
            StringBuilder sb = new StringBuilder(); object lk = new object();
            using (Process p = Process.Start(psi))
            {
                p.OutputDataReceived += (s, e) => { if (e.Data != null) lock (lk) sb.AppendLine(e.Data); }; p.ErrorDataReceived += (s, e) => { if (e.Data != null) lock (lk) sb.AppendLine(e.Data); };
                p.BeginOutputReadLine(); p.BeginErrorReadLine();
                if (!p.WaitForExit(timeoutMs)) { try { p.Kill(); } catch { } code = -1; } else { p.WaitForExit(); code = p.ExitCode; }
            }
            lock (lk) return sb.ToString();
        }

        // LoTW : TQSL signe et envoie (tqsl.exe doit être installé et la « Station Location » créée)
        public static string UploadLotw(List<Qso> qsos)
        {
            string tqsl = App.Cfg.Get("lotw.tqsl", Tr.T("C:\\Program Files (x86)\\TrustedQSL\\tqsl.exe")), loc = App.Cfg.Get("lotw.location", "");
            if (!File.Exists(tqsl)) throw new Exception(Tr.T("TQSL introuvable : ") + tqsl + " (Tools > Online services).");
            if (loc.Length == 0) throw new Exception(Tr.T("LoTW : nom de la « Station Location » de TQSL à renseigner."));
            string f = Path.Combine(Path.GetTempPath(), "hrl-lotw-" + Guid.NewGuid().ToString("N") + ".adi");
            File.WriteAllText(f, ExportOnline(qsos), new UTF8Encoding(false));
            try
            {
                int code; string o = RunTqsl(tqsl, Tr.T("-d -q -a all -u -l \"") + loc + "\" \"" + f + "\"", 600000, out code);
                if (code == -1) throw new Exception(Tr.T("TQSL : délai dépassé (il attend peut-être le mot de passe de votre certificat)."));
                if (code != 0 && code != 9) throw new Exception(Tr.T("TQSL code ") + code + " : " + o.Trim());
            }
            finally { try { File.Delete(f); } catch { } }
            return Tr.T("LoTW : ") + qsos.Count + " QSO signés et envoyés par TQSL.";
        }
    }

    // Réglages des services en ligne
    public class OnlineForm : Form
    {
        readonly Dictionary<string, TextBox> t = new Dictionary<string, TextBox>();
        ComboBox cLook; CheckBox kNoCom;
        public OnlineForm()
        {
            AutoScaleMode = AutoScaleMode.None; Text = Tr.T("Online services"); Size = new Size(560, 560); StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; Font = Ui.Normal;
            int y = 12;
            Ui.Lbl(this, Tr.T("Recherche d'indicatif :"), 12, y); cLook = Ui.Combo(this, 160, y - 2, 120, new[] { "HamQTH", "QRZ.com", Tr.T("(aucune)") }); cLook.Text = App.Cfg.Get("lookup.service", "HamQTH"); y += 30;
            Row(Tr.T("Identifiant :"), "lookup.user", ref y, false); Row(Tr.T("Mot de passe :"), "lookup.pass", ref y, true); y += 6;
            Ui.Lbl(this, Tr.T("QRZ.com Logbook"), 12, y); y += 22; Row(Tr.T("Clé API :"), "qrzlog.key", ref y, true); y += 6;
            Ui.Lbl(this, Tr.T("Club Log"), 12, y); y += 22; Row(Tr.T("Indicatif :"), "clublog.call", ref y, false); Row(Tr.T("Jeton (cl_…) :"), "clublog.token", ref y, true);
            Ui.Btn(this, Tr.T("Demander un jeton"), 160, y - 2, 200, 26, (s, e) => { try { System.Diagnostics.Process.Start("https://clublog.org/edituser.php?tab=api"); } catch (Exception ex) { MessageBox.Show(ex.Message); } });
            Ui.Btn(this, Tr.T("Aide"), 370, y - 2, 80, 26, (s, e) => ShowHelp(Tr.T("Pour envoyer vos QSO vers Club Log, il faut un jeton personnel :\n\n1. Cliquez sur « Demander un jeton » : la page de Club Log s'ouvre dans votre navigateur (connectez-vous à votre compte si besoin).\n2. Dans « Label », tapez un nom pour reconnaître ce jeton, par exemple HamRadioLink (le nom est libre).\n3. Cliquez sur « Create token ».\n4. Le jeton (il commence par cl_) s'affiche UNE SEULE FOIS : copiez-le tout de suite avec le bouton de copie, sans espace en trop.\n5. Collez-le ici dans le champ « Jeton », vérifiez votre indicatif, puis cliquez sur OK.\n\nLe jeton se traite comme un mot de passe : ne le montrez à personne. Un jeton inutilisé pendant 6 mois est supprimé par Club Log ; il suffit alors d'en créer un nouveau."))); y += 32;
            Ui.Lbl(this, Tr.T("HRDLog.net (code d'envoi reçu par e-mail)"), 12, y); y += 22; Row(Tr.T("Indicatif :"), "hrdlog.call", ref y, false); Row(Tr.T("Code d'envoi :"), "hrdlog.code", ref y, true); y += 6;
            Ui.Lbl(this, "QRZCQ.com", 12, y); y += 22; Row(Tr.T("Indicatif :"), "qrzcq.call", ref y, false); Row(Tr.T("Clé API :"), "qrzcq.key", ref y, true);
            Ui.Btn(this, Tr.T("Aide"), 160, y - 2, 80, 26, (s, e) => ShowHelp(Tr.T("Pour envoyer vos QSO vers QRZCQ.com, il faut votre clé API :\n\n1. Connectez-vous à votre compte sur qrzcq.com dans votre navigateur.\n2. Cliquez sur l'onglet \"Manage\" en haut à droite de la page.\n3. Repérez la clé API (API Key) et copiez-la, sans espace en trop.\n4. Collez-la ici dans le champ \"Clé API\", vérifiez votre indicatif, puis cliquez sur OK.\n\nLa clé se traite comme un mot de passe : ne la montrez à personne. QRZCQ n'accepte pas plus d'un envoi par minute : le programme attend tout seul entre deux envois."))); y += 32;
            Ui.Lbl(this, "HamQTH", 12, y); y += 22; Row(Tr.T("Identifiant :"), "hamqth.user", ref y, false); Row(Tr.T("Mot de passe :"), "hamqth.pass", ref y, true); y += 6;
            Ui.Lbl(this, "World Radio League", 12, y); y += 22; Row(Tr.T("Clé API :"), "wrl.key", ref y, true);
            Row(Tr.T("Journal (facultatif) :"), "wrl.logbook", ref y, false);
            Ui.Btn(this, Tr.T("Mes journaux"), 160, y - 2, 160, 26, (s, e) => PickWrl());
            Ui.Btn(this, Tr.T("Demander une clé"), 326, y - 2, 164, 26, (s, e) => { try { System.Diagnostics.Process.Start("https://app.worldradioleague.com/integration/"); } catch (Exception ex) { MessageBox.Show(ex.Message); } });
            Ui.Btn(this, Tr.T("Aide"), 496, y - 2, 56, 26, (s, e) => ShowHelp(Tr.T("Pour envoyer vos QSO vers World Radio League, il faut une clé API :\n\n1. Cliquez sur « Demander une clé » : la page de WRL s'ouvre dans votre navigateur (connectez-vous à votre compte).\n2. Ouvrez « Integrations », rubrique « Developer API », puis cliquez sur « Generate API key ».\n3. La clé (elle commence par wrl_live_) n'est affichée QU'UNE SEULE FOIS : copiez-la tout de suite, sans espace en trop.\n4. Collez-la ici dans le champ « Clé API », puis cliquez sur OK.\n\nLa clé se traite comme un mot de passe : ne la montrez à personne. Les QSO sont envoyés sous forme de fichier ADIF ; les doublons sont ignorés par le service."))); y += 32;
            Ui.Lbl(this, "eQSL.cc", 12, y); y += 22; Row(Tr.T("Identifiant :"), "eqsl.user", ref y, false); Row(Tr.T("Mot de passe :"), "eqsl.pass", ref y, true); y += 6;
            Ui.Lbl(this, Tr.T("LoTW (TQSL)"), 12, y); y += 22; Row(Tr.T("tqsl.exe :"), "lotw.tqsl", ref y, false); Row(Tr.T("Station Location :"), "lotw.location", ref y, false);
            y += 6; kNoCom = Ui.Chk(this, Tr.T("Ne pas transmettre les commentaires"), 12, y, 520); kNoCom.Checked = App.Cfg.GetBool("online.nocomment", false); y += 30;   // valable pour tous les envois en ligne
            Button ok = Ui.Btn(this, "OK", 360, y + 8, 80, 28, (s, e) => Save()); Ui.Btn(this, Tr.T("Annuler"), 450, y + 8, 80, 28, (s, e) => { DialogResult = DialogResult.Cancel; Close(); });
            ClientSize = new Size(560, Math.Min(y + 50, Screen.PrimaryScreen.WorkingArea.Height - 80)); AutoScroll = true; AutoScrollMinSize = new Size(0, y + 50); AcceptButton = ok;
        }
        // Liste les journaux (logbooks) du compte World Radio League et remplit le champ « Journal » avec celui choisi.
        void PickWrl()
        {
            string key = t["wrl.key"].Text.Trim(); if (key.Length == 0) { MessageBox.Show(this, Tr.T("Saisissez d'abord la clé API."), "World Radio League"); return; }
            try
            {
                Cursor = Cursors.WaitCursor; string def; List<string[]> l = Online.WrlLogbooks(key, out def); Cursor = Cursors.Default;
                if (l.Count == 0) { MessageBox.Show(this, Tr.T("Aucun journal trouvé sur ce compte."), "World Radio League"); return; }
                Form f = new Form(); f.Text = Tr.T("Mes journaux World Radio League"); f.StartPosition = FormStartPosition.CenterParent; f.FormBorderStyle = FormBorderStyle.FixedDialog; f.MaximizeBox = false; f.MinimizeBox = false; f.Font = Ui.Normal; f.ClientSize = new Size(460, 250);
                ListBox lb = new ListBox { Left = 12, Top = 12, Width = 436, Height = 190 }; foreach (string[] x in l) lb.Items.Add(x[1] + (x[0] == def ? Tr.T("  (par défaut)") : "")); lb.SelectedIndex = 0; f.Controls.Add(lb);
                Button ok = new Button { Text = "OK", Left = 270, Top = 212, Width = 85 }; ok.DialogResult = DialogResult.OK; Button no = new Button { Text = Tr.T("Annuler"), Left = 363, Top = 212, Width = 85 }; no.DialogResult = DialogResult.Cancel; f.Controls.Add(ok); f.Controls.Add(no); f.AcceptButton = ok; f.CancelButton = no;
                if (f.ShowDialog(this) == DialogResult.OK && lb.SelectedIndex >= 0) t["wrl.logbook"].Text = l[lb.SelectedIndex][0];
            }
            catch (Exception ex) { Cursor = Cursors.Default; MessageBox.Show(this, ex.Message, "World Radio League"); }
        }

        Form helpFrm;
        // Aide non bloquante : fenêtre à part (Show, pas ShowDialog), les autres fenêtres restent utilisables.
        void ShowHelp(string txt)
        {
            if (helpFrm != null && !helpFrm.IsDisposed) { helpFrm.Activate(); return; }
            Form h = new Form(); helpFrm = h; h.Text = Tr.T("Club Log"); h.FormBorderStyle = FormBorderStyle.FixedToolWindow; h.StartPosition = FormStartPosition.CenterParent; h.Font = Ui.Normal; h.ShowInTaskbar = false;
            Label l = new Label(); l.Text = txt; l.AutoSize = false; l.SetBounds(12, 12, 480, 270); h.Controls.Add(l);
            Button b = new Button(); b.Text = "OK"; b.SetBounds(412, 290, 80, 28); b.Click += (s, e) => h.Close(); h.Controls.Add(b);
            h.ClientSize = new Size(504, 330); h.Show(this);
        }
        void Row(string label, string key, ref int y, bool pw)
        {
            Ui.Lbl(this, label, 24, y); TextBox b = Ui.Txt(this, 160, y - 2, 360); b.Text = App.Cfg.Get(key, key == "lotw.tqsl" ? Tr.T("C:\\Program Files (x86)\\TrustedQSL\\tqsl.exe") : ""); b.UseSystemPasswordChar = pw; t[key] = b; y += 28;
        }
        void Save() { App.Cfg.Set("online.nocomment", kNoCom.Checked ? "1" : "0"); App.Cfg.Set("lookup.service", cLook.Text); foreach (KeyValuePair<string, TextBox> kv in t) App.Cfg.Set(kv.Key, kv.Value.Text.Trim()); App.Cfg.Save(); DialogResult = DialogResult.OK; Close(); }
    }
}

