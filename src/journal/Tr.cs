using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace HamRadioLink.Journal
{
    // Langue du journal : les textes sont écrits en français dans le code ; les autres langues les traduisent (TrData.cs, texte français = clé).
    // Une traduction manquante laisse le texte français. Le choix se fait dans Configuration > Langue (réglage « jn.lang »).
    public static class Tr
    {
        public static readonly string[] Codes = { "fr", "en", "de", "es", "it", "nl", "pt", "pl", "cs", "sk", "sl", "hr", "hu", "ro", "bg", "el", "sv", "da", "nb", "fi", "et", "lv", "lt" };
        public static readonly string[] Names = { "Français", "English", "Deutsch", "Español", "Italiano", "Nederlands", "Português", "Polski", "Čeština", "Slovenčina", "Slovenščina", "Hrvatski", "Magyar", "Română", "Български", "Ελληνικά", "Svenska", "Dansk", "Norsk", "Suomi", "Eesti", "Latviešu", "Lietuvių" };
        static string code; static Dictionary<string, string> table;
        const char BS = (char)92;

        public static string Code
        {
            get { if (code == null) Load(ReadSetting()); return code; }
            set { Load(value); try { HamRadioLink.Cfg.Set("jn.lang", code); HamRadioLink.Cfg.Save(); } catch { } }
        }
        static string ReadSetting() { try { return HamRadioLink.Cfg.Get("jn.lang", "fr"); } catch { return "fr"; } }
        static void Load(string c)
        {
            code = Array.IndexOf(Codes, c) >= 0 ? c : "fr"; table = null; if (code == "fr") return;
            try { string gz; if (TrData.Gz.TryGetValue(code, out gz)) table = Parse(gz); } catch { table = null; }
        }
        static Dictionary<string, string> Parse(string b64)
        {
            Dictionary<string, string> d = new Dictionary<string, string>(StringComparer.Ordinal);
            using (GZipStream z = new GZipStream(new MemoryStream(Convert.FromBase64String(b64)), CompressionMode.Decompress))
            using (StreamReader r = new StreamReader(z, Encoding.UTF8))
            {
                string line; while ((line = r.ReadLine()) != null) { int t = line.IndexOf((char)9); if (t <= 0) continue; d[Unesc(line.Substring(0, t))] = Unesc(line.Substring(t + 1)); }
            }
            return d;
        }
        // fichier de traduction : une ligne « texte français TAB traduction », avec BS BS, BS n, BS r, BS t pour les caractères spéciaux
        public static string Esc(string s)
        {
            StringBuilder b = new StringBuilder();
            foreach (char c in s) { if (c == BS) { b.Append(BS); b.Append(BS); } else if (c == (char)10) { b.Append(BS); b.Append('n'); } else if (c == (char)13) { b.Append(BS); b.Append('r'); } else if (c == (char)9) { b.Append(BS); b.Append('t'); } else b.Append(c); }
            return b.ToString();
        }
        public static string Unesc(string s)
        {
            if (s.IndexOf(BS) < 0) return s; StringBuilder b = new StringBuilder();
            for (int i = 0; i < s.Length; i++) { char c = s[i]; if (c == BS && i + 1 < s.Length) { char n = s[++i]; b.Append(n == 'n' ? (char)10 : n == 'r' ? (char)13 : n == 't' ? (char)9 : n); } else b.Append(c); }
            return b.ToString();
        }

        // Traduit un texte français (les espaces de début et de fin sont conservés).
        public static string T(string fr)
        {
            if (string.IsNullOrEmpty(fr)) return fr; string c = Code; if (c == "fr" || table == null) return fr;
            int a = 0, z = fr.Length; while (a < z && char.IsWhiteSpace(fr[a])) a++; while (z > a && char.IsWhiteSpace(fr[z - 1])) z--; if (a == z) return fr;
            string core = fr.Substring(a, z - a), t; if (!table.TryGetValue(core, out t) || t.Length == 0) return fr;
            return fr.Substring(0, a) + t + fr.Substring(z);
        }
    }
}
