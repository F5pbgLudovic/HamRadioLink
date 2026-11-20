using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace HamRadioLink
{
    // Réglages mémorisés à côté de l'exe (logiciel portable, dossier data) ; sinon dans %APPDATA%
    public static class Cfg
    {
        static readonly Dictionary<string, string> d = new Dictionary<string, string>();
        static string path;
        public static string Dir;
        public static void Load()
        {
            Dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            try { Directory.CreateDirectory(Dir); string t = Path.Combine(Dir, "w.tmp"); File.WriteAllText(t, "x"); File.Delete(t); }
            catch
            {
                Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HamRadioLink");
                try { Directory.CreateDirectory(Dir); } catch { }
            }
            path = Path.Combine(Dir, "settings.ini");
            try
            {
                if (File.Exists(path))
                    foreach (string line in File.ReadAllLines(path, Encoding.UTF8)) { int i = line.IndexOf('='); if (i > 0) d[line.Substring(0, i).Trim()] = line.Substring(i + 1); }
            }
            catch { }
        }
        public static int CountPrefix(string pre) { int n = 0; lock (d) foreach (KeyValuePair<string, string> kv in d) if (kv.Key.StartsWith(pre, StringComparison.Ordinal) && kv.Value == "1") n++; return n; }
        public static int RemovePrefix(string pre) { List<string> k = new List<string>(); lock (d) { foreach (string x in d.Keys) if (x.StartsWith(pre, StringComparison.Ordinal)) k.Add(x); foreach (string x in k) d.Remove(x); } return k.Count; }
        public static void Save()
        {
            try
            {
                StringBuilder sb = new StringBuilder(); lock (d) { List<string> k = new List<string>(d.Keys); k.Sort(); foreach (string s in k) sb.AppendLine(s + "=" + d[s]); }
                lock (path) File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }
        public static string Dump() { StringBuilder sb = new StringBuilder(); lock (d) { List<string> k = new List<string>(d.Keys); k.Sort(); foreach (string s in k) if (!s.StartsWith("pending.")) sb.AppendLine(s + "=" + d[s]); } return sb.ToString(); }
        public static int Import(string text)
        {
            Dictionary<string, string> n = new Dictionary<string, string>();
            foreach (string line in text.Replace("\r\n", "\n").Split('\n')) { int i = line.IndexOf('='); if (i > 0 && line[0] != '#') n[line.Substring(0, i).Trim()] = line.Substring(i + 1); }
            if (n.Count == 0) return 0; lock (d) { d.Clear(); foreach (KeyValuePair<string, string> kv in n) d[kv.Key] = kv.Value; } Save(); return n.Count;
        }
        public static string Get(string k, string def) { string v; lock (d) return d.TryGetValue(k, out v) ? v : def; }
        public static void Set(string k, string v) { lock (d) d[k] = v; }
        public static int GetInt(string k, int def) { int v; return int.TryParse(Get(k, ""), out v) ? v : def; }
        public static void SetInt(string k, int v) { lock (d) d[k] = v.ToString(); }
        public static bool GetBool(string k, bool def) { return GetInt(k, def ? 1 : 0) != 0; }
        public static void SetBool(string k, bool v) { SetInt(k, v ? 1 : 0); }
    }

    public enum Kind { Toggle, Knob, Choice, Action, Freq, Meter, Led, Dial, Frame, Text, Gauge, Chain, Dsp }

    // Un élément d'un choix : libellé affiché, code envoyé dans {v}, et codes équivalents reconnus à la lecture (alias)
    public class Item
    {
        public string Label, Code; public string[] Alias = new string[0];
        public bool Match(string v) { if (v == Code) return true; foreach (string a in Alias) if (a == v) return true; return false; }
        // "LSB=1,USB=2,AUTO=4|5|6"
        public static Item[] ParseList(string s)
        {
            List<Item> l = new List<Item>();
            foreach (string p in (s ?? "").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int eq = p.LastIndexOf('='); if (eq <= 0) continue;
                Item it = new Item(); it.Label = p.Substring(0, eq).Trim(); string[] codes = p.Substring(eq + 1).Trim().Split('|');
                it.Code = codes[0]; if (codes.Length > 1) { it.Alias = new string[codes.Length - 1]; Array.Copy(codes, 1, it.Alias, 0, it.Alias.Length); }
                l.Add(it);
            }
            return l.ToArray();
        }
    }

    // Un élément de la face avant : sa place, son aspect et les commandes CAT qui le pilotent.
    public class Ctl
    {
        public string Id, Label, DLabel, Group, Tip; public Kind K;
        public int X, Y, W, H;                         // position et taille actuelles (unités de conception)
        public int DX, DY, DW, DH;                     // position par défaut
        // --- commandes (valeurs actuelles et valeurs par défaut)
        public string Set = "", Read = "", Parse = ""; public string DSet = "", DRead = "", DParse = "";
        public string Items = "", DItems = "";         // choix : "LSB=1,USB=2,AUTO=4|5|6"
        public string OnVal = "1", OffVal = "0", DOnVal = "1", DOffVal = "0";   // bascule : OnVal peut contenir des alias "1|2"
        public int Min, Max = 255, DMin, DMax = 255, Step = 1;   // curseur (Step : pas des valeurs envoyées)
        public string DFmt; public string Fmt = "raw";                     // affichage : raw, pct, sgn, hz, wpm, pitch10, pitch50, w
        public int Rows = 1;                           // choix en ligne : nombre de rangées
        public bool Row, Local, TxOnly, NoPoll, Hidden, Custom; public int Period = 2200;   // Local : réglage interne, aucune commande CAT
        public string Ref = "";                        // freq : id du mode ; dial : id de la fréquence ; meter : unité
        public string Hint = "";                       // texte d'aide affiché dans l'éditeur
        public string Accent = "";                     // "amber", "cyan", "red", "green", "blue"
        public bool Dirty { get { return Set != DSet || Read != DRead || Parse != DParse || Items != DItems || OnVal != DOnVal || OffVal != DOffVal || Min != DMin || Max != DMax; } }
        public bool HasCat { get { return !Local && K != Kind.Frame && K != Kind.Text; } }
        Regex re; string reSrc;
        public Regex Rx
        {
            get
            {
                if (string.IsNullOrEmpty(Parse)) return null;
                if (re == null || reSrc != Parse) { try { re = new Regex(Parse, RegexOptions.CultureInvariant); reSrc = Parse; } catch { re = null; reSrc = null; return null; } }
                return re;
            }
        }
        Item[] items; string itemsSrc;
        public Item[] ItemList { get { if (items == null || itemsSrc != Items) { items = Item.ParseList(Items); itemsSrc = Items; } return items; } }
        public void ResetCat() { Set = DSet; Read = DRead; Parse = DParse; Items = DItems; OnVal = DOnVal; OffVal = DOffVal; Min = DMin; Max = DMax; Label = DLabel; if (DFmt != null) Fmt = DFmt; }
        public void ResetPos() { X = DX; Y = DY; W = DW; H = DH; }
        public bool IsOn(string v) { if (v == null) return false; foreach (string s in OnVal.Split('|')) if (s == v) return true; return false; }
        public string OnSend { get { return OnVal.Split('|')[0]; } }
        public string OffSend { get { return OffVal.Split('|')[0]; } }
    }

    // Gabarits de commande : {v} valeur brute, {v:N} entier sur N chiffres, {v:+N} avec signe,
    // {p:XX:N:S} / {n:XX:N:S} : XX + N chiffres + S (suffixe facultatif) si la valeur est >= 0 / < 0, {wait:ms} pause.
    public static class Tpl
    {
        static readonly Regex tok = new Regex(@"\{(v|wait|p|n)(?::([^}]*))?\}", RegexOptions.CultureInvariant);
        public static string Fill(string tpl, string v)
        {
            long n; bool isNum = long.TryParse(v ?? "", NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out n);
            return tok.Replace(tpl ?? "", delegate(Match m)
            {
                string t = m.Groups[1].Value, a = m.Groups[2].Value;
                if (t == "wait") return m.Value;
                if (t == "v")
                {
                    if (a.Length == 0) return v ?? "";
                    bool sign = a[0] == '+'; int w; if (!int.TryParse(sign ? a.Substring(1) : a, out w)) return m.Value;
                    if (!isNum) return v ?? "";
                    string d = Math.Abs(n).ToString().PadLeft(w, '0');
                    return sign ? (n < 0 ? "-" : "+") + d : (n < 0 ? "-" : "") + d;
                }
                string[] q = a.Split(':'); if (q.Length < 2 || !isNum) return "";
                int w2; if (!int.TryParse(q[1], out w2)) return ""; string suf = q.Length > 2 ? q[2] : "";
                if (t == "p" && n >= 0) return q[0] + Math.Abs(n).ToString().PadLeft(w2, '0') + suf;
                if (t == "n" && n < 0) return q[0] + Math.Abs(n).ToString().PadLeft(w2, '0') + suf;
                return "";
            });
        }
        // Découpe en étapes : texte à envoyer, ou pause (étape commençant par \0 suivi des millisecondes)
        public static List<string> Steps(string filled)
        {
            List<string> l = new List<string>(); int i = 0;
            foreach (Match m in Regex.Matches(filled, @"\{wait:(\d+)\}"))
            {
                if (m.Index > i) l.Add(filled.Substring(i, m.Index - i));
                l.Add("\0" + m.Groups[1].Value); i = m.Index + m.Length;
            }
            if (i < filled.Length) l.Add(filled.Substring(i));
            return l;
        }
        // Teste un gabarit : renvoie un message d'erreur ou null
        public static string Check(string tpl)
        {
            if (string.IsNullOrEmpty(tpl)) return null; int o = 0;
            foreach (char ch in tpl) { if (ch == '{') o++; else if (ch == '}') o--; if (o < 0 || o > 1) return "Accolades { } mal placées."; }
            if (o != 0) return "Accolades { } mal placées.";
            string s = tok.Replace(tpl, "");
            if (s.IndexOf('{') >= 0 || s.IndexOf('}') >= 0) return "Jeton inconnu : utilisez {v}, {v:3}, {v:+4}, {p:XX:4:;}, {n:XX:4:;} ou {wait:ms}.";
            return null;
        }
    }

    // Une radio : liste de ses éléments et paramètres de liaison
    public class RadioDef
    {
        public string Key, Name, Catalog, CivCat, IdCmd = "ID;", IdAnswer = "", CatExtra = ""; public int Civ = -1; public bool Bin, Stop2, Tci; public LM Leg; public int DefBaud = 38400, FreqDigits = 9; public long MaxFreq = 75000000;
        public string Init = "AI0;";                   // envoyé à la connexion
        public List<Ctl> C = new List<Ctl>(); public List<string> Notes = new List<string>();
        public string IdValue { get { string a = IdAnswer ?? ""; if (a.StartsWith("ID")) a = a.Substring(2); else if (a.StartsWith("1900")) a = a.Substring(4); return a.TrimEnd(';'); } }
        public Ctl Find(string id) { foreach (Ctl c in C) if (c.Id == id) return c; return null; }
        string F(string k) { return Path.Combine(Cfg.Dir ?? ".", Key + "." + k + ".ini"); }
        // ---- éléments ajoutés par l'utilisateur (bouton « Ajouter » de la vue config) : data\<poste>.custom.ini
        // La liste est remplacée (jamais modifiée en place) : le moteur de lecture peut la parcourir en même temps.
        public void AddCtl(Ctl c) { List<Ctl> n = new List<Ctl>(C); n.Add(c); C = n; }
        public void RemoveCtl(Ctl c) { List<Ctl> n = new List<Ctl>(C); n.Remove(c); C = n; }
        public string NewId() { int i = 1; while (Find("u" + i) != null) i++; return "u" + i; }
        public string[] CustomLines()
        {
            List<string> l = new List<string>();
            foreach (Ctl c in C) if (c.Custom)
                l.Add(string.Join("\t", new string[] { c.Id, c.K.ToString(), Esc(c.Label), c.X.ToString(), c.Y.ToString(), c.W.ToString(), c.H.ToString(), Esc(c.Set), Esc(c.Read), Esc(c.Parse), Esc(c.Items), Esc(c.OnVal), Esc(c.OffVal), c.Min.ToString(), c.Max.ToString(), Esc(c.Fmt), Esc(c.Accent), c.Step.ToString(), c.Period.ToString(), c.Row ? "1" : "0", c.Rows.ToString(), c.TxOnly ? "1" : "0", Esc(c.Hint) }));
            return l.ToArray();
        }
        public void SetCustomLines(string[] lines)
        {
            List<Ctl> n = new List<Ctl>(); foreach (Ctl c in C) if (!c.Custom) n.Add(c);
            foreach (string line in lines)
            {
                string[] p = line.Split('\t'); if (p.Length < 22) continue; Ctl c = new Ctl(); c.Custom = true;
                try
                {
                    c.Id = p[0]; c.K = (Kind)Enum.Parse(typeof(Kind), p[1]); c.Label = Unesc(p[2]); c.X = int.Parse(p[3]); c.Y = int.Parse(p[4]); c.W = int.Parse(p[5]); c.H = int.Parse(p[6]);
                    c.Set = Unesc(p[7]); c.Read = Unesc(p[8]); c.Parse = Unesc(p[9]); c.Items = Unesc(p[10]); c.OnVal = Unesc(p[11]); c.OffVal = Unesc(p[12]); c.Min = int.Parse(p[13]); c.Max = int.Parse(p[14]);
                    c.Fmt = Unesc(p[15]); c.Accent = Unesc(p[16]); c.Step = int.Parse(p[17]); c.Period = int.Parse(p[18]); c.Row = p[19] == "1"; c.Rows = int.Parse(p[20]); c.TxOnly = p[21] == "1"; if (p.Length > 22) c.Hint = Unesc(p[22]);
                }
                catch { continue; }
                c.DSet = c.Set; c.DRead = c.Read; c.DParse = c.Parse; c.DItems = c.Items; c.DOnVal = c.OnVal; c.DOffVal = c.OffVal; c.DMin = c.Min; c.DMax = c.Max; c.DLabel = c.Label; c.DX = c.X; c.DY = c.Y; c.DW = c.W; c.DH = c.H;
                n.Add(c);
            }
            C = n;
        }
        public void RemoveCustoms() { SetCustomLines(new string[0]); }
        public void SaveCustom()
        {
            try
            {
                string[] l = CustomLines();
                if (l.Length == 0) { if (File.Exists(F("custom"))) File.Delete(F("custom")); } else File.WriteAllLines(F("custom"), l, Encoding.UTF8);
            }
            catch { }
        }
        // ---- personnalisation (seules les différences avec les valeurs par défaut sont enregistrées)
        public void LoadUser()
        {
            try { SetCustomLines(File.Exists(F("custom")) ? File.ReadAllLines(F("custom"), Encoding.UTF8) : new string[0]); } catch { }
            foreach (Ctl c in C) { c.ResetCat(); c.ResetPos(); }
            try
            {
                if (File.Exists(F("cat")))
                    foreach (string line in File.ReadAllLines(F("cat"), Encoding.UTF8))
                    {
                        string[] p = line.Split('\t'); if (p.Length < 3) continue; Ctl c = Find(p[0]); if (c == null) continue; string v = Unesc(p[2]);
                        switch (p[1])
                        {
                            case "Set": c.Set = v; break; case "Read": c.Read = v; break; case "Parse": c.Parse = v; break; case "Items": c.Items = v; break;
                            case "Fmt": if (c.DFmt == null) c.DFmt = c.Fmt; c.Fmt = v; break; case "OnVal": c.OnVal = v; break; case "OffVal": c.OffVal = v; break; case "Label": c.Label = v; break;
                            case "Min": int.TryParse(v, out c.Min); break; case "Max": int.TryParse(v, out c.Max); break;
                        }
                    }
                if (File.Exists(F("layout")))
                    foreach (string line in File.ReadAllLines(F("layout"), Encoding.UTF8))
                    {
                        string[] p = line.Split('\t'); if (p.Length < 5) continue; Ctl c = Find(p[0]); if (c == null) continue;
                        int x, y, w, h; if (int.TryParse(p[1], out x) && int.TryParse(p[2], out y) && int.TryParse(p[3], out w) && int.TryParse(p[4], out h)) { c.X = x; c.Y = y; c.W = Math.Max(20, w); c.H = Math.Max(14, h); }
                    }
            }
            catch { }
        }
        public void SaveCat()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (Ctl c in C)
                {
                    if (c.Set != c.DSet) sb.AppendLine(c.Id + "\tSet\t" + Esc(c.Set)); if (c.Read != c.DRead) sb.AppendLine(c.Id + "\tRead\t" + Esc(c.Read));
                    if (c.Parse != c.DParse) sb.AppendLine(c.Id + "\tParse\t" + Esc(c.Parse)); if (c.Items != c.DItems) sb.AppendLine(c.Id + "\tItems\t" + Esc(c.Items));
                    if (c.OnVal != c.DOnVal) sb.AppendLine(c.Id + "\tOnVal\t" + Esc(c.OnVal)); if (c.OffVal != c.DOffVal) sb.AppendLine(c.Id + "\tOffVal\t" + Esc(c.OffVal));
                    if (c.Min != c.DMin) sb.AppendLine(c.Id + "\tMin\t" + c.Min); if (c.Max != c.DMax) sb.AppendLine(c.Id + "\tMax\t" + c.Max);
                    if (c.Label != c.DLabel) sb.AppendLine(c.Id + "\tLabel\t" + Esc(c.Label)); if (c.DFmt != null && c.Fmt != c.DFmt) sb.AppendLine(c.Id + "\tFmt\t" + Esc(c.Fmt));
                }
                if (sb.Length == 0) { if (File.Exists(F("cat"))) File.Delete(F("cat")); } else File.WriteAllText(F("cat"), sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }
        public void SaveLayout()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (Ctl c in C) if (c.X != c.DX || c.Y != c.DY || c.W != c.DW || c.H != c.DH) sb.AppendLine(c.Id + "\t" + c.X + "\t" + c.Y + "\t" + c.W + "\t" + c.H);
                if (sb.Length == 0) { if (File.Exists(F("layout"))) File.Delete(F("layout")); } else File.WriteAllText(F("layout"), sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }
        static string Esc(string s) { return (s ?? "").Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\r", "").Replace("\n", "\\n"); }
        static string Unesc(string s) { return s.Replace("\\n", "\n").Replace("\\t", "\t").Replace("\\\\", "\\"); }
    }
}
