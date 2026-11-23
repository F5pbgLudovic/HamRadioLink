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
            sb.Append(Tr.T("HamRadioLink ADIF export\r\n")).Append(Field("ADIF_VER", "3.1.4")).Append(Field("PROGRAMID", "HamRadioLink")).Append("\r\n<EOH>\r\n\r\n");
            Skipped = 0;
            foreach (Qso q in qsos)
            {
                int mark = sb.Length;
                try { OneQso(sb, q); }
                catch { sb.Length = mark; Skipped++; }   // 1.13 : un contact défectueux est ignoré, l'export continue
            }
            return sb.ToString();
        }
        public static int Skipped;   // contacts ignorés lors du dernier export

        static void OneQso(StringBuilder sb, Qso q)
        {
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

        // Fréquence d'un ADIF en Hz. Norme : MHz (« 14.074 »), mais certains logiciels (OPSLOG...) écrivent des kHz, des Hz ou une virgule décimale :
        // on teste les trois échelles et on garde celle qui tombe dans la bande du contact (champ BAND) ; sans bande, une valeur de 1000 à 99999 est lue en kHz,
        // une valeur de 100000 et plus en Hz. Retourne -1 si la valeur est illisible, -2 si elle est incompatible avec la bande indiquée.
        static readonly string[] BandNames = { "2190m", "630m", "560m", "160m", "80m", "60m", "40m", "30m", "20m", "17m", "15m", "12m", "10m", "8m", "6m", "5m", "4m", "2m", "1.25m", "70cm", "33cm", "23cm", "13cm", "9cm", "6cm", "3cm", "1.25cm", "6mm", "4mm", "2.5mm", "2mm", "1mm" };
        static readonly double[][] BandMhz = { new[] { 0.13, 0.14 }, new[] { 0.47, 0.48 }, new[] { 0.5, 0.51 }, new[] { 1.8, 2.0 }, new[] { 3.5, 4.0 }, new[] { 5.06, 5.45 }, new[] { 7.0, 7.3 }, new[] { 10.1, 10.15 }, new[] { 14.0, 14.35 }, new[] { 18.068, 18.168 }, new[] { 21.0, 21.45 }, new[] { 24.89, 24.99 }, new[] { 28.0, 29.7 }, new[] { 40.0, 45.0 }, new[] { 50.0, 54.0 }, new[] { 54.000001, 69.9 }, new[] { 70.0, 71.0 }, new[] { 144.0, 148.0 }, new[] { 222.0, 225.0 }, new[] { 420.0, 450.0 }, new[] { 902.0, 928.0 }, new[] { 1240.0, 1300.0 }, new[] { 2300.0, 2450.0 }, new[] { 3300.0, 3500.0 }, new[] { 5650.0, 5925.0 }, new[] { 10000.0, 10500.0 }, new[] { 24000.0, 24250.0 }, new[] { 47000.0, 47200.0 }, new[] { 75500.0, 81000.0 }, new[] { 119980.0, 123000.0 }, new[] { 134000.0, 149000.0 }, new[] { 241000.0, 250000.0 } };
        public static long FreqHz(string value, string band)
        {
            double v; string s = (value ?? "").Trim().Replace(',', '.'); if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v) || v <= 0 || double.IsInfinity(v)) return -1;
            int bi = Array.IndexOf(BandNames, (band ?? "").Trim().ToLowerInvariant());
            double[] cand = { v, v / 1000.0, v / 1e6 };   // MHz, kHz, Hz
            if (bi >= 0) { foreach (double m in cand) if (m >= BandMhz[bi][0] * 0.95 && m <= BandMhz[bi][1] * 1.05) return (long)Math.Round(m * 1e6); return -2; }   // bande connue mais fréquence incompatible : saisie erronée, non enregistrée
            double mhz = v >= 100000 ? v / 1e6 : v >= 1000 && !(bi >= 0 && BandMhz[bi][0] >= 1000) ? v / 1000.0 : v;
            return (long)Math.Round(mhz * 1e6);
        }

        public static Qso ToQso(Dictionary<string, string> a, PeerLog log)
        {
            Qso q = new Qso();
            string d = Get(a, "QSO_DATE"), t = Get(a, "TIME_ON");
            if (d.Length == 8) q["COL_TIME_ON"] = FmtDt(d, t);
            string d2 = Get(a, "QSO_DATE_OFF"), t2 = Get(a, "TIME_OFF");
            if (d2.Length == 8) q["COL_TIME_OFF"] = FmtDt(d2, t2); else if (d.Length == 8) q["COL_TIME_OFF"] = FmtDt(d, t2.Length > 0 ? t2 : t);
            string big = null;
            foreach (KeyValuePair<string, string> kv in a)
            {
                string n = kv.Key, col = "COL_" + n;
                if (n == "QSO_DATE" || n == "TIME_ON" || n == "QSO_DATE_OFF" || n == "TIME_OFF") continue;
                if (n == "FREQ" || n == "FREQ_RX")
                {
                    long hz = FreqHz(kv.Value, Get(a, "BAND"));
                    if (hz == -2) { if (n == "FREQ") big = "[FREQ " + kv.Value.Trim() + " ?]"; }
                    else if (hz > int.MaxValue) { if (n == "FREQ") big = "[FREQ " + (hz / 1e6).ToString("0.######", CultureInfo.InvariantCulture) + " MHz]"; }   // la colonne COL_FREQ est un entier 32 bits (2,147 GHz au plus) : la valeur passe dans le commentaire
                    else if (hz > 0) q[col] = hz.ToString();
                    continue;
                }
                if (!log.Has(col)) continue;
                ColInfo c = log.Col(col);
                if (c.DateTime) { if (kv.Value.Length == 8) q[col] = FmtDt(kv.Value, "000000"); continue; }
                q[col] = kv.Value;
            }
            if (big != null) q["COL_COMMENT"] = (q.S("COL_COMMENT").Length > 0 ? q.S("COL_COMMENT") + " " : "") + big;
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
