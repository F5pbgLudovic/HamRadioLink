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
            string u = App.Cfg.Get("lookup.user", ""), p = App.Cfg.Get("lookup.pass", ""); if (u.Length == 0) throw new Exception("Renseignez votre compte HamQTH (Tools > Online services).");
            for (int attempt = 0; attempt < 2; attempt++)
            {
                if (hamQthSession == null || (DateTime.Now - hamQthTime).TotalMinutes > 50)
                {
                    string x = Http("https://www.hamqth.com/xml.php?u=" + Enc(u) + "&p=" + Enc(p));
                    hamQthSession = Tag(x, "session_id"); hamQthTime = DateTime.Now;
                    if (hamQthSession.Length == 0) throw new Exception("HamQTH : " + Tag(x, "error"));
                }
                string r = Http("https://www.hamqth.com/xml.php?id=" + hamQthSession + "&callsign=" + Enc(call) + "&prg=HamRadioLink");
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
            string u = App.Cfg.Get("lookup.user", ""), p = App.Cfg.Get("lookup.pass", ""); if (u.Length == 0) throw new Exception("Renseignez votre compte QRZ.com (Tools > Online services).");
            for (int attempt = 0; attempt < 2; attempt++)
            {
                if (qrzKey == null || (DateTime.Now - qrzTime).TotalMinutes > 50)
                {
                    string x = Http("https://xmldata.qrz.com/xml/current/?username=" + Enc(u) + ";password=" + Enc(p) + ";agent=HamRadioLink" + App.Version);
                    qrzKey = Tag(x, "Key"); qrzTime = DateTime.Now; if (qrzKey.Length == 0) throw new Exception("QRZ.com : " + Tag(x, "Error"));
                }
                string r = Http("https://xmldata.qrz.com/xml/current/?s=" + qrzKey + ";callsign=" + Enc(call));
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
        static string OneAdif(List<Qso> q) { return Adif.Export(q); }

        public static string UploadQrz(List<Qso> qsos)
        {
            string key = App.Cfg.Get("qrzlog.key", ""); if (key.Length == 0) throw new Exception("QRZ Logbook : clé API à renseigner (Tools > Online services).");
            int ok = 0, dup = 0; string msg = "";
            foreach (Qso q in qsos)
            {
                string adif = OneAdif(new List<Qso> { q }); adif = adif.Substring(adif.IndexOf("<EOH>") + 5).Trim();
                string r = Http("https://logbook.qrz.com/api", "KEY=" + Enc(key) + "&ACTION=INSERT&ADIF=" + Enc(adif));
                if (r.Contains("RESULT=OK")) ok++; else if (r.Contains("duplicate")) dup++; else msg = r;
            }
            if (ok + dup == 0 && msg.Length > 0) throw new Exception("QRZ Logbook : " + msg);
            return "QRZ Logbook : " + ok + " envoyés, " + dup + " déjà présents.";
        }

        public static string UploadClubLog(List<Qso> qsos)
        {
            string mail = App.Cfg.Get("clublog.email", ""), pw = App.Cfg.Get("clublog.pass", ""), call = App.Cfg.Get("clublog.call", App.Cfg.Get("me.call", "")), api = App.Cfg.Get("clublog.api", "");
            if (mail.Length == 0) throw new Exception("Club Log : e-mail et mot de passe à renseigner (Tools > Online services).");
            // Club Log répond « 403 Forbidden » à toute requête sans clé API de logiciel (et peut bloquer l'adresse IP si on insiste) : on n'envoie donc rien sans clé.
            if (api.Length == 0) throw new Exception("Club Log refuse les envois sans clé API de logiciel (erreur 403). Demandez une clé au support de Club Log (helpdesk) pour HamRadioLink et saisissez-la dans Outils > Online services ; en attendant, exportez un fichier ADIF (clic droit > Exporter la sélection) et déposez-le sur le site de Club Log.");
            int ok = 0;
            foreach (Qso q in qsos)
            {
                string adif = OneAdif(new List<Qso> { q }); adif = adif.Substring(adif.IndexOf("<EOH>") + 5).Trim();
                string r = Http("https://clublog.org/realtime.php", "email=" + Enc(mail) + "&password=" + Enc(pw) + "&callsign=" + Enc(call) + (api.Length > 0 ? "&api=" + Enc(api) : "") + "&adif=" + Enc(adif));
                if (r.Trim().StartsWith("OK") || r.IndexOf("Dupe", StringComparison.OrdinalIgnoreCase) >= 0) ok++; else throw new Exception("Club Log : " + (r.Contains("403") ? "403 Forbidden : clé API refusée ou adresse bloquée (arrêt immédiat pour ne pas aggraver le blocage)." : r.Trim()));
            }
            return "Club Log : " + ok + " / " + qsos.Count + " QSO envoyés.";
        }

        public static string UploadEqsl(List<Qso> qsos)
        {
            string u = App.Cfg.Get("eqsl.user", ""), p = App.Cfg.Get("eqsl.pass", ""); if (u.Length == 0) throw new Exception("eQSL : identifiant et mot de passe à renseigner.");
            int sent = 0; string last = "";
            foreach (Qso q in qsos)   // une fiche par requête, au fur et à mesure
            {
                string adif = OneAdif(new List<Qso> { q }); int eoh = adif.IndexOf("<EOH>");
                string body = "<EQSL_USER:" + u.Length + ">" + u + "<EQSL_PSWD:" + p.Length + ">" + p + "<PROGRAMID:12>HamRadioLink<EOH>" + adif.Substring(eoh + 5);
                string r = Http("https://www.eqsl.cc/qslcard/ImportADIF.cfm", "ADIFData=" + Enc(body), 60000);
                if (r.IndexOf("Result: ", StringComparison.OrdinalIgnoreCase) < 0 && r.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0) throw new Exception("eQSL (" + q.S("COL_CALL") + ") : " + Regex.Replace(r, "<[^>]+>", " ").Trim());
                Match m = Regex.Match(r, "Result:[^<]*", RegexOptions.IgnoreCase); last = m.Success ? m.Value : ""; sent++;
            }
            return "eQSL : " + sent + " envoyé(s)" + (last.Length > 0 ? " (" + last + ")" : "") + ".";
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
            string tqsl = App.Cfg.Get("lotw.tqsl", "C:\\Program Files (x86)\\TrustedQSL\\tqsl.exe"), loc = App.Cfg.Get("lotw.location", "");
            if (!File.Exists(tqsl)) throw new Exception("TQSL introuvable : " + tqsl + " (Tools > Online services).");
            if (loc.Length == 0) throw new Exception("LoTW : nom de la « Station Location » de TQSL à renseigner.");
            string f = Path.Combine(Path.GetTempPath(), "hrl-lotw-" + Guid.NewGuid().ToString("N") + ".adi");
            File.WriteAllText(f, Adif.Export(qsos), new UTF8Encoding(false));
            try
            {
                int code; string o = RunTqsl(tqsl, "-d -q -a all -u -l \"" + loc + "\" \"" + f + "\"", 600000, out code);
                if (code == -1) throw new Exception("TQSL : délai dépassé (il attend peut-être le mot de passe de votre certificat).");
                if (code != 0 && code != 9) throw new Exception("TQSL code " + code + " : " + o.Trim());
            }
            finally { try { File.Delete(f); } catch { } }
            return "LoTW : " + qsos.Count + " QSO signés et envoyés par TQSL.";
        }
    }

    // Réglages des services en ligne
    public class OnlineForm : Form
    {
        readonly Dictionary<string, TextBox> t = new Dictionary<string, TextBox>();
        ComboBox cLook;
        public OnlineForm()
        {
            Text = "Online services"; Size = new Size(560, 560); StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; Font = Ui.Normal;
            int y = 12;
            Ui.Lbl(this, "Recherche d'indicatif :", 12, y); cLook = Ui.Combo(this, 160, y - 2, 120, new[] { "HamQTH", "QRZ.com", "(aucune)" }); cLook.Text = App.Cfg.Get("lookup.service", "HamQTH"); y += 30;
            Row("Identifiant :", "lookup.user", ref y, false); Row("Mot de passe :", "lookup.pass", ref y, true); y += 6;
            Ui.Lbl(this, "QRZ.com Logbook", 12, y); y += 22; Row("Clé API :", "qrzlog.key", ref y, true); y += 6;
            Ui.Lbl(this, "Club Log", 12, y); y += 22; Row("E-mail :", "clublog.email", ref y, false); Row("Mot de passe :", "clublog.pass", ref y, true); Row("Clé API logiciel :", "clublog.api", ref y, true); y += 6;
            Ui.Lbl(this, "eQSL.cc", 12, y); y += 22; Row("Identifiant :", "eqsl.user", ref y, false); Row("Mot de passe :", "eqsl.pass", ref y, true); y += 6;
            Ui.Lbl(this, "LoTW (TQSL)", 12, y); y += 22; Row("tqsl.exe :", "lotw.tqsl", ref y, false); Row("Station Location :", "lotw.location", ref y, false);
            Button ok = Ui.Btn(this, "OK", 360, y + 8, 80, 28, (s, e) => Save()); Ui.Btn(this, "Annuler", 450, y + 8, 80, 28, (s, e) => { DialogResult = DialogResult.Cancel; Close(); });
            ClientSize = new Size(560, y + 50); AcceptButton = ok;
        }
        void Row(string label, string key, ref int y, bool pw)
        {
            Ui.Lbl(this, label, 24, y); TextBox b = Ui.Txt(this, 160, y - 2, 360); b.Text = App.Cfg.Get(key, key == "lotw.tqsl" ? "C:\\Program Files (x86)\\TrustedQSL\\tqsl.exe" : ""); b.UseSystemPasswordChar = pw; t[key] = b; y += 28;
        }
        void Save() { App.Cfg.Set("lookup.service", cLook.Text); foreach (KeyValuePair<string, TextBox> kv in t) App.Cfg.Set(kv.Key, kv.Value.Text.Trim()); App.Cfg.Save(); DialogResult = DialogResult.OK; Close(); }
    }
}

