using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace HamRadioLink
{
    // CI-V (Icom, Xiegu) : le moteur du programme manipule du texte « HEX; ». CivLink fait le pont avec le fil binaire
    // FE FE <poste> E0 <commande> <données> FD. Dans le texte, une fréquence est écrite dans l'ordre naturel des chiffres
    // (14074000 Hz = 0014074000) alors que sur le fil les octets BCD partent par le poids faible : Swap() inverse les octets.
    public static class Civ
    {
        public static readonly Encoding L1 = Encoding.GetEncoding(28591);
        public const int Ctrl = 0xE0;
        static string union;
        // texte du catalogue : celui du modèle, ou pour « * » l'ensemble des 8 manuels CI-V disponibles (une ligne par commande + sous-commande)
        public static string CatText(string key)
        {
            string t; if (key != null && key != "*" && CivCatalogs.D.TryGetValue(key, out t)) return t;
            if (union == null)
            {
                StringBuilder sb = new StringBuilder(); HashSet<string> seen = new HashSet<string>();
                foreach (string k in new string[] { "ic7300", "ic7610", "ic9700", "ic7760", "ic7300mk2", "ic7100", "ic7410", "ic7000" })
                    foreach (string line in CivCatalogs.D[k].Split('\n')) { string[] p = line.TrimEnd('\r').Split('|'); if (p.Length < 4 || p[0].Length != 2) continue; if (seen.Add(p[0] + p[1].Replace(" ", ""))) sb.Append(line.TrimEnd('\r')).Append('\n'); }
                union = sb.ToString();
            }
            return union;
        }
        static string Rev(string h, int from)
        {
            if (h.Length <= from) return h; StringBuilder sb = new StringBuilder(h.Substring(0, from)); string t = h.Substring(from);
            for (int i = t.Length - (t.Length % 2 == 0 ? 2 : 1); i >= 0; i -= 2) sb.Append(t, i, Math.Min(2, t.Length - i)); return sb.ToString();
        }
        // texte <-> fil : 00/03/05 (fréquence), 25 (après l'octet VFO), 1C 03 (fréquence d'émission)
        public static string Swap(string h)
        {
            if (h.Length < 2) return h; string c = h.Substring(0, 2);
            if (c == "00" || c == "03" || c == "05") return Rev(h, 2);
            if (c == "25" && h.Length >= 4) return Rev(h, 4);
            if (c == "1C" && h.Length >= 4 && h.Substring(2, 2) == "03") return Rev(h, 4);
            return h;
        }
        public static bool IsHex(string s) { if (s.Length == 0 || s.Length % 2 != 0) return false; foreach (char c in s) if (!Uri.IsHexDigit(c)) return false; return true; }
        public static string Frame(int to, int from, string hex)
        {
            StringBuilder sb = new StringBuilder(); sb.Append((char)0xFE).Append((char)0xFE).Append((char)to).Append((char)from);
            for (int i = 0; i < hex.Length; i += 2) sb.Append((char)Convert.ToInt32(hex.Substring(i, 2), 16)); sb.Append((char)0xFD); return sb.ToString();
        }
        // Extrait de s toutes les trames complètes (FE FE à de ... FD) : (à, de, octets en hexadécimal). Le reste incomplet est laissé dans rest.
        public static List<string[]> Parse(StringBuilder buf, string s)
        {
            List<string[]> l = new List<string[]>(); buf.Append(s);
            while (true)
            {
                string t = buf.ToString(); int i = t.IndexOf("þþ", StringComparison.Ordinal);
                if (i < 0) { buf.Length = 0; if (t.EndsWith("þ")) buf.Append('þ'); break; }
                while (i + 2 < t.Length && t[i + 2] == 'þ') i++;
                int e = t.IndexOf('ý', i + 2); if (e < 0) { buf.Length = 0; if (t.Length - i < 600) buf.Append(t.Substring(i)); break; }
                string body = t.Substring(i + 2, e - i - 2); buf.Length = 0; buf.Append(t.Substring(e + 1));
                if (body.Length < 3) continue; StringBuilder h = new StringBuilder(); for (int k = 2; k < body.Length; k++) h.Append(((int)body[k]).ToString("X2"));
                l.Add(new string[] { ((int)body[0]).ToString("X2"), ((int)body[1]).ToString("X2"), h.ToString() });
            }
            return l;
        }
    }

    // Liaison CI-V au-dessus d'un port COM / TCP : texte HEX; <-> trames binaires
    public class CivLink : ILink
    {
        readonly ILink inner; readonly int addr; readonly StringBuilder rx = new StringBuilder(); readonly object lk = new object();
        public event Action<string> Data;
        public string Name { get { return inner.Name + " · CI-V " + addr.ToString("X2") + "h"; } }
        public int Baud { get { return inner.Baud; } }
        public CivLink(ILink inner, int addr) { this.inner = inner; this.addr = addr; }
        public bool Open(out string err) { inner.Data += OnRaw; if (inner.Open(out err)) return true; inner.Data -= OnRaw; return false; }
        public void Close() { inner.Data -= OnRaw; inner.Close(); }
        public void Send(string s)
        {
            foreach (string tok in s.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string h = tok.Trim().ToUpperInvariant(); if (!Civ.IsHex(h)) continue;
                if (Regex.IsMatch(h, "^(FE)+$")) { inner.Send(new string((char)0xFE, h.Length / 2)); continue; }   // réveil : octets FE bruts
                inner.Send(Civ.Frame(addr, Civ.Ctrl, Civ.Swap(h)));
            }
        }
        void OnRaw(string s)
        {
            List<string[]> fr; lock (lk) fr = Civ.Parse(rx, s); StringBuilder o = new StringBuilder();
            foreach (string[] f in fr)
            {
                if (f[1] != addr.ToString("X2") || (f[0] != "E0" && f[0] != "00")) continue;   // écho de nos propres trames, autres postes
                if (f[2] == "FB") o.Append("FB;"); else if (f[2] == "FA") o.Append("?;"); else o.Append(Civ.Swap(f[2])).Append(';');
            }
            if (o.Length > 0 && Data != null) Data(o.ToString());
        }
    }

    // Poste CI-V simulé sur le fil binaire (essais de CivLink : trames, inversion des octets, écho, trames « transceive »)
    public class CivWireLink : ILink
    {
        public readonly CivSim Sim; readonly int addr; readonly bool echo; readonly StringBuilder rx = new StringBuilder(); public event Action<string> Data;
        public string Name { get { return "Poste CI-V simulé (fil)"; } }
        public int Baud { get { return 115200; } }
        public CivWireLink(CivSim sim, int addr, bool echo) { Sim = sim; this.addr = addr; this.echo = echo; }
        public bool Open(out string err) { err = null; return true; }
        public void Close() { }
        public void Send(string s)
        {
            if (echo && Data != null) Data(s); StringBuilder o = new StringBuilder();
            foreach (string[] f in Civ.Parse(rx, s))
            {
                if (f[0] != addr.ToString("X2") || f[1] != "E0") continue;
                foreach (string tok in Sim.Input(Civ.Swap(f[2])).Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (tok == "FB") o.Append(Civ.Frame(Civ.Ctrl, addr, "FB")); else if (tok == "?") o.Append(Civ.Frame(Civ.Ctrl, addr, "FA")); else o.Append(Civ.Frame(Civ.Ctrl, addr, Civ.Swap(tok)));
                }
            }
            if (o.Length > 0 && Data != null) Data(o.ToString());
        }
    }

    // Poste virtuel CI-V : lit le catalogue du manuel (commande | sous-commande | données), refuse toute commande inconnue,
    // toute longueur de données fausse et tout niveau hors 0000-0255 ; mémorise ce qu'on lui envoie et le renvoie à la lecture.
    public class CivSim
    {
        class E { public string Key, Data, Name; public int Len = -1, Max = -1; public bool Bcd; }
        readonly Dictionary<string, E> ents = new Dictionary<string, E>(); readonly HashSet<string> withSub = new HashSet<string>();
        readonly List<string> Bad, Seen; readonly Dictionary<string, string> S; readonly RadioDef Def; public int Count;
        readonly Dictionary<string, string> st = new Dictionary<string, string>(); readonly Random rnd = new Random(5);
        string fa, fb; int sel; string md = "01", filt = "01"; readonly int digits;
        public int Entries { get { return ents.Count; } }
        public bool Has(string key) { return ents.ContainsKey(key); }
        // la commande (octets hexadécimaux du début d'un gabarit) existe-t-elle dans le catalogue ? (commande seule, ou commande + sous-commande)
        public bool Known(string h)
        {
            for (int n = 2; n <= Math.Min(8, h.Length); n += 2) if (ents.ContainsKey(h.Substring(0, n))) return true;
            foreach (string k in ents.Keys) if (k.StartsWith(h, StringComparison.Ordinal)) return true; return false;
        }

        public CivSim(RadioDef def, List<string> bad, List<string> seen, Dictionary<string, string> s)
        {
            Def = def; Bad = bad; Seen = seen; S = s; digits = def.FreqDigits;
            string txt = Civ.CatText(def.CivCat);
            foreach (string line in txt.Split('\n'))
            {
                string[] p = line.TrimEnd('\r').Split('|'); if (p.Length < 4 || p[0].Length != 2) continue;
                E e = new E(); string sub = p[1].Replace(" ", "").ToUpperInvariant(); e.Key = p[0].ToUpperInvariant() + sub; e.Data = p[2]; e.Name = p[3];
                if (sub.Length > 0) withSub.Add(p[0].ToUpperInvariant());
                Match m = Regex.Match(p[2], @"BCD (\d+) bytes?", RegexOptions.IgnoreCase); if (m.Success) { e.Len = int.Parse(m.Groups[1].Value); e.Bcd = true; }
                else { m = Regex.Match(p[2], @"^(\d+) byte", RegexOptions.IgnoreCase); if (m.Success) e.Len = int.Parse(m.Groups[1].Value); else if (p[2].Trim() == "none") e.Len = 0; }
                if (Regex.IsMatch(p[2], @"0000\s*(~|to)\s*0255")) e.Max = 255;
                if (!ents.ContainsKey(e.Key)) ents[e.Key] = e;
            }
            fa = Z(14074000); fb = Z(7074000); Mirror();
        }
        string Z(long hz) { return hz.ToString().PadLeft(digits, '0'); }
        void Mirror() { S["fa"] = (sel == 0 ? fa : fb).TrimStart('0'); S["fb"] = (sel == 0 ? fb : fa).TrimStart('0'); S["md"] = md; if (!S.ContainsKey("tx")) S["tx"] = "0"; }
        public bool Tx { get { return S["tx"] == "1"; } }

        public string Input(string text)
        {
            StringBuilder o = new StringBuilder();
            foreach (string tok in text.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string h = tok.Trim().ToUpperInvariant(); if (Regex.IsMatch(h, "^(FE)+$")) continue; Count++; Seen.Add(h + ";"); string r = Do(h);
                if (r == "?") Bad.Add(h + ";"); o.Append(r).Append(';');
            }
            return o.ToString();
        }
        const string NG = "?", OK = "FB";
        string Cur() { return sel == 0 ? fa : fb; }
        string Do(string h)
        {
            if (!Civ.IsHex(h)) return NG; string cmd = h.Substring(0, 2), rest = h.Substring(2);
            switch (cmd)
            {
                case "03": return rest.Length == 0 ? "03" + Cur() : NG;
                case "04": return rest.Length == 0 ? "04" + md + filt : NG;
                case "05": if (rest.Length != digits || !AllDigits(rest)) return NG; if (sel == 0) fa = rest; else fb = rest; Mirror(); return OK;
                case "06":
                    if (rest.Length != 2 && rest.Length != 4) return NG; if (!(IsMode(rest.Substring(0, 2)))) return NG; md = rest.Substring(0, 2); if (rest.Length == 4) filt = rest.Substring(2); Mirror(); return OK;
                case "07":
                    if (rest.Length == 0 || rest == "00") { sel = 0; Mirror(); return OK; }
                    if (rest == "01") { sel = 1; Mirror(); return OK; } if (rest == "A0") { fb = fa; Mirror(); return OK; } if (rest == "B0") { string t = fa; fa = fb; fb = t; Mirror(); return OK; }
                    if (rest == "D0" || rest == "D1") return OK; return NG;
                case "0F":
                    if (rest.Length == 0) return "0F" + Get("0F", "00"); if (rest == "00" || rest == "01") { st["0F"] = rest; return OK; } if (rest == "10" || rest == "11" || rest == "12" || rest == "13") return OK; return NG;
                case "10":
                    if (rest.Length == 0) return "10" + Get("10", "00"); if (rest.Length == 2 && AllDigits(rest)) { st["10"] = rest; return OK; } return NG;
                case "11":
                    if (rest.Length == 0) return "11" + Get("11", "00"); if (rest.Length == 2 && AllDigits(rest)) { st["11"] = rest; return OK; } return NG;
                case "18": return (rest == "00" || rest == "01") ? OK : NG;
                case "19": return rest == "00" ? "1900" + Def.Civ.ToString("X2") : NG;
                case "25":
                    if (!ents.ContainsKey("25")) return NG;
                    if (rest == "00" || rest == "01") return "25" + rest + ((rest == "00") == (sel == 0) ? fa : fb);
                    if ((rest.StartsWith("00") || rest.StartsWith("01")) && rest.Length == 2 + digits && AllDigits(rest.Substring(2))) { bool selected = rest.StartsWith("00"); if ((selected && sel == 0) || (!selected && sel == 1)) fa = rest.Substring(2); else fb = rest.Substring(2); Mirror(); return OK; }
                    return NG;
                case "26":
                    if (!ents.ContainsKey("26")) return NG; if (rest == "00" || rest == "01") return "26" + rest + md + "00" + filt; if (rest.Length >= 4 && IsMode(rest.Substring(2, 2))) { if (rest.StartsWith("00")) { md = rest.Substring(2, 2); Mirror(); } return OK; } return NG;
            }
            return Generic(cmd, rest);
        }
        string Get(string k, string d) { string v; return st.TryGetValue(k, out v) ? v : d; }
        static bool AllDigits(string s) { foreach (char c in s) if (c < '0' || c > '9') return false; return true; }
        static bool IsMode(string m) { int v; return int.TryParse(m, System.Globalization.NumberStyles.HexNumber, null, out v) && (v <= 0x0A || v == 0x17 || v == 0x22); }

        string Generic(string cmd, string rest)
        {
            string key = cmd, data = rest; E e = null;
            if (withSub.Contains(cmd))
            {
                for (int n = Math.Min(rest.Length, 8); n >= 2; n -= 2) { E t; if (ents.TryGetValue(cmd + rest.Substring(0, n), out t)) { e = t; key = cmd + rest.Substring(0, n); data = rest.Substring(n); break; } }
                if (e == null) return NG;
            }
            else if (!ents.TryGetValue(cmd, out e)) return NG;
            if (data.Length == 0)
            {
                if (e.Len == 0) return OK;
                string v = Get(key, null);
                if (v == null) { if (key == "1502") v = "0120"; else if (key.StartsWith("15") && key.Length == 4 && key != "1501" && key != "1505") v = Tx ? "0143" : "0000"; else v = new string('0', Math.Max(2, e.Len * 2)); }
                if (key == "1502") v = rnd.Next(0, 3) == 0 ? "0121" : "0120";
                return key + v;
            }
            if (e.Len >= 0 && data.Length != e.Len * 2) return NG;
            if (e.Bcd && !AllDigits(data)) return NG;
            if (e.Max >= 0 && AllDigits(data) && long.Parse(data) > e.Max) return NG;
            st[key] = data; if (key == "1C00") S["tx"] = data == "01" ? "1" : "0"; S["c" + key] = data; return OK;
        }
    }
}
