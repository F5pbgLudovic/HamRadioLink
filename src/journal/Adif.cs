using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace HamRadioLink.Journal
{
    // Import / export ADIF <-> colonnes l'autre logiciel (COL_xxx)
    public static class Adif
    {
        static readonly HashSet<string> NoExport = new HashSet<string> { "COL_PRIMARY_KEY", "COL_TIME_ON", "COL_TIME_OFF", "COL_FREQ", "COL_FREQ_RX", "COL_FORCE_INIT", "COL_HRDCOUNTRYNO", "COL_ISMULTIPLIER", "COL_ROVERLOCATION", "COL_PUBLIC_KEY", "COL_BIOGRAPHY",
            "COL_USER_DEFINED_0","COL_USER_DEFINED_1","COL_USER_DEFINED_2","COL_USER_DEFINED_3","COL_USER_DEFINED_4","COL_USER_DEFINED_5","COL_USER_DEFINED_6","COL_USER_DEFINED_7","COL_USER_DEFINED_8","COL_USER_DEFINED_9" };

        static string AdifName(string col)
        {
            string n = col.Substring(4);
            switch (n) { case "CHECK": return "CHECK"; case "AGE": return "AGE"; default: return n; }
        }

        static string Field(string name, string v) { return "<" + name + ":" + Encoding.UTF8.GetByteCount(v) + ">" + v; }

        public static string Export(IEnumerable<Qso> qsos)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("HamRadioLink ADIF export\r\n").Append(Field("ADIF_VER", "3.1.4")).Append(Field("PROGRAMID", "HamRadioLink")).Append("\r\n<EOH>\r\n\r\n");
            foreach (Qso q in qsos)
            {
                DateTime on, off;
                if (DateTime.TryParse(q.S("COL_TIME_ON"), CultureInfo.InvariantCulture, DateTimeStyles.None, out on))
                { sb.Append(Field("QSO_DATE", on.ToString("yyyyMMdd"))).Append(Field("TIME_ON", on.ToString("HHmmss"))); }
                if (DateTime.TryParse(q.S("COL_TIME_OFF"), CultureInfo.InvariantCulture, DateTimeStyles.None, out off))
                { sb.Append(Field("QSO_DATE_OFF", off.ToString("yyyyMMdd"))).Append(Field("TIME_OFF", off.ToString("HHmmss"))); }
                long hz; double mhz;
                if (long.TryParse(q.S("COL_FREQ"), out hz) && hz > 0) sb.Append(Field("FREQ", (hz / 1e6).ToString("0.000000", CultureInfo.InvariantCulture)));
                if (long.TryParse(q.S("COL_FREQ_RX"), out hz) && hz > 0) sb.Append(Field("FREQ_RX", (hz / 1e6).ToString("0.000000", CultureInfo.InvariantCulture)));
                foreach (KeyValuePair<string, string> kv in q)
                {
                    if (NoExport.Contains(kv.Key) || string.IsNullOrEmpty(kv.Value) || kv.Value == "0" || kv.Value == "0.0") continue;
                    string v = kv.Value;
                    if (kv.Key.EndsWith("DATE") && kv.Key != "COL_QSO_DATE")
                    { DateTime d; if (DateTime.TryParse(v, CultureInfo.InvariantCulture, DateTimeStyles.None, out d)) v = d.ToString("yyyyMMdd"); else continue; }
                    sb.Append(Field(AdifName(kv.Key), v));
                }
                sb.Append("<EOR>\r\n");
            }
            return sb.ToString();
        }

        // Lit un fichier ADIF : renvoie une liste de dictionnaires {NOM_ADIF -> valeur}
        public static List<Dictionary<string, string>> Parse(string text)
        {
            List<Dictionary<string, string>> list = new List<Dictionary<string, string>>();
            int i = text.IndexOf("<eoh>", StringComparison.OrdinalIgnoreCase);
            i = i < 0 ? 0 : i + 5;
            Dictionary<string, string> cur = new Dictionary<string, string>();
            while (i < text.Length)
            {
                int lt = text.IndexOf('<', i);
                if (lt < 0) break;
                int gt = text.IndexOf('>', lt);
                if (gt < 0) break;
                string tag = text.Substring(lt + 1, gt - lt - 1);
                string[] p = tag.Split(':');
                string name = p[0].ToUpperInvariant();
                if (name == "EOR") { if (cur.Count > 0) list.Add(cur); cur = new Dictionary<string, string>(); i = gt + 1; continue; }
                int len;
                if (p.Length >= 2 && int.TryParse(p[1], out len))
                {
                    // la longueur ADIF est en octets pour les fichiers UTF-8 ; on ajuste si besoin
                    int take = Math.Min(len, text.Length - gt - 1);
                    string v = text.Substring(gt + 1, take);
                    if (Encoding.UTF8.GetByteCount(v) > len) { while (take > 0 && Encoding.UTF8.GetByteCount(text.Substring(gt + 1, take)) > len) take--; v = text.Substring(gt + 1, take); }
                    cur[name] = v; i = gt + 1 + take;
                }
                else i = gt + 1;
            }
            return list;
        }

        public static Qso ToQso(Dictionary<string, string> a, PeerLog log)
        {
            Qso q = new Qso();
            string d = Get(a, "QSO_DATE"), t = Get(a, "TIME_ON");
            if (d.Length == 8) q["COL_TIME_ON"] = FmtDt(d, t);
            string d2 = Get(a, "QSO_DATE_OFF"), t2 = Get(a, "TIME_OFF");
            if (d2.Length == 8) q["COL_TIME_OFF"] = FmtDt(d2, t2); else if (d.Length == 8) q["COL_TIME_OFF"] = FmtDt(d, t2.Length > 0 ? t2 : t);
            foreach (KeyValuePair<string, string> kv in a)
            {
                string n = kv.Key, col = "COL_" + n;
                if (n == "QSO_DATE" || n == "TIME_ON" || n == "QSO_DATE_OFF" || n == "TIME_OFF") continue;
                if (n == "FREQ" || n == "FREQ_RX")
                {
                    double mhz; if (double.TryParse(kv.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out mhz)) q[col] = ((long)Math.Round(mhz * 1e6)).ToString();
                    continue;
                }
                if (!log.Has(col)) continue;
                ColInfo c = log.Col(col);
                if (c.DateTime) { if (kv.Value.Length == 8) q[col] = FmtDt(kv.Value, "000000"); continue; }
                q[col] = kv.Value;
            }
            return q;
        }

        static string Get(Dictionary<string, string> a, string k) { string v; return a.TryGetValue(k, out v) ? v : ""; }

        static string FmtDt(string d, string t)
        {
            t = (t ?? "").PadRight(6, '0');
            return d.Substring(0, 4) + "-" + d.Substring(4, 2) + "-" + d.Substring(6, 2) + " " + t.Substring(0, 2) + ":" + t.Substring(2, 2) + ":" + t.Substring(4, 2);
        }
    }
}
