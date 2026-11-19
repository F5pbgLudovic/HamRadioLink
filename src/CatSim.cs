using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace HamRadioLink
{
    // Poste virtuel « piloté par catalogue » : il lit le catalogue officiel du manuel CAT (commande | nom | format d'envoi | format de lecture)
    // et refuse (« ?; » + Bad) toute commande inconnue ou dont le nombre de caractères ne correspond pas au format du manuel.
    // Il mémorise la dernière valeur envoyée par commande et la renvoie à la lecture. Les valeurs min/max ne sont PAS contrôlées (le catalogue ne les contient pas).
    public class CatalogSim
    {
        class Tok { public string Name; public char Lit; public bool Sign; }   // Name != null : paramètre d'un caractère ; Sign : -/+
        class Ent
        {
            public string Cmd, Name, SetF, ReadF; public List<Tok> Set, Read; public Regex SetRx, ReadRx; public bool Variable;
        }
        readonly Dictionary<string, Ent> ents = new Dictionary<string, Ent>();
        readonly Dictionary<string, string> st = new Dictionary<string, string>();   // clé (ex. « AG0 ») -> dernière trame complète
        readonly List<string> Bad; readonly List<string> Seen; readonly Dictionary<string, string> S; readonly RadioDef Def; public int Count;
        readonly StringBuilder buf = new StringBuilder();
        public readonly string IdAnswer; readonly bool Kw;
        Ent Find(string cmd, out string c2) { for (int n = Math.Min(5, cmd.Length); n >= 2; n--) { Ent e; if (ents.TryGetValue(cmd.Substring(0, n), out e)) { c2 = cmd.Substring(0, n); return e; } } c2 = null; return null; }

        static List<Tok> Toks(string f, out bool variable)
        {
            variable = false; List<Tok> l = new List<Tok>(); if (f.EndsWith(";")) f = f.Substring(0, f.Length - 1); int i = 0;
            while (i < f.Length)
            {
                if (f[i] == '~') { variable = true; i++; continue; }
                if (f[i] == 'P' && i + 1 < f.Length && char.IsDigit(f[i + 1])) { int j = i + 1; while (j < f.Length && char.IsDigit(f[j])) j++; Tok t = new Tok(); t.Name = f.Substring(i, j - i); l.Add(t); i = j; continue; }
                if (f.Substring(i).StartsWith("-/+")) { Tok t = new Tok(); t.Name = "S"; t.Sign = true; l.Add(t); i += 3; continue; }
                Tok u = new Tok(); u.Lit = f[i]; l.Add(u); i++;
            }
            return l;
        }
        static Regex Rx(List<Tok> t, bool variable)
        {
            StringBuilder sb = new StringBuilder("^");
            foreach (Tok k in t) { if (k.Name == null) sb.Append(Regex.Escape(k.Lit.ToString())); else if (k.Sign) sb.Append("[+-]"); else sb.Append("[0-9A-Za-z]"); }
            if (variable) sb.Append("[0-9A-Za-z+-]*"); sb.Append("$"); return new Regex(sb.ToString());
        }

        public CatalogSim(RadioDef def, List<string> bad, List<string> seen, Dictionary<string, string> s)
        {
            Def = def; Bad = bad; Seen = seen; S = s; IdAnswer = def.IdAnswer; S["tx"] = "0"; S["ps"] = "1"; Kw = def.Catalog != null && def.Catalog.StartsWith("ts");
            foreach (string line in (Catalogs.D[def.Catalog] + "\n" + (def.CatExtra ?? "")).Split('\n'))
            {
                string[] p = line.TrimEnd('\r').Split('|'); if (p.Length < 4 || p[0].Length < 2 || p[0].Length > 5 || p[0][0] == '#') continue;
                Ent e = new Ent(); e.Cmd = p[0]; e.Name = p[1]; e.SetF = p[2]; e.ReadF = p[3]; bool v1 = false, v2 = false;
                if (e.SetF.Contains(" or ")) { e.SetF = e.SetF.Substring(0, e.SetF.IndexOf(" or ")); if (!e.SetF.EndsWith(";")) e.SetF += ";"; v1 = true; }
                if (e.SetF.Length > 0) { bool vv; e.Set = Toks(e.SetF, out vv); v1 = v1 || vv; e.SetRx = Rx(e.Set, v1); }
                if (e.ReadF.Length > 0) { e.Read = Toks(e.ReadF, out v2); e.ReadRx = Rx(e.Read, v2); }
                e.Variable = v1; ents[e.Cmd] = e;
            }
            Seed("FA", 14074000); Seed("FB", 7100000);
        }
        void Seed(string cmd, long f) { Ent e; if (!ents.TryGetValue(cmd, out e) || e.Set == null) return; st[cmd] = cmd + f.ToString().PadLeft(e.Set.Count - 2, '0') + ";"; S[cmd.ToLowerInvariant()] = st[cmd].Substring(2).TrimEnd(';'); }
        public bool Known(string cmd2) { return ents.ContainsKey(cmd2); }
        // la trame (avec ou sans « ; ») respecte-t-elle un format du catalogue ? (sans effet de bord)
        public bool Valid(string frame)
        {
            string c = frame.TrimEnd(';'); if (c == "ID") return true; if (c.Length < 2) return false; string c2; Ent e = Find(c, out c2); if (e == null) return false;
            if (c.Length == 2 && (c2 == "IF" || c2 == "BY" || c2 == "OI" || c2 == "RS" || c2 == "UL")) return true; if ((c2 == "SM" || c2 == "RM") && c.Length == 3) return true;
            if (e.ReadRx != null && c.Length == e.Read.Count && e.ReadRx.IsMatch(c)) return true;
            return e.SetRx != null && (e.Variable || c.Length == e.Set.Count) && e.SetRx.IsMatch(c);
        }
        public string SetFormat(string c) { Ent e; return ents.TryGetValue(c, out e) ? e.SetF : null; }
        public string ReadFormat(string c) { Ent e; return ents.TryGetValue(c, out e) ? e.ReadF : null; }

        public string Input(string s)
        {
            StringBuilder o = new StringBuilder();
            lock (buf)
            {
                buf.Append(s);
                while (true)
                {
                    string all = buf.ToString(); int i = all.IndexOf(';'); if (i < 0) { if (buf.Length > 200) buf.Length = 0; break; }
                    string cmd = all.Substring(0, i).Trim(); buf.Remove(0, i + 1); if (cmd.Length == 0) continue;
                    Count++; if (Seen.Count < 4000) Seen.Add(cmd + ";"); o.Append(One(cmd));
                }
            }
            return o.ToString();
        }
        string Fail(string cmd) { Bad.Add(cmd + ";"); return "?;"; }
        int Wob(int b, int amp) { return Math.Max(0, Math.Min(255, b + (int)(Math.Sin(Environment.TickCount / 700.0) * amp))); }
        bool Tx { get { return S["tx"] == "1"; } }

        // clé d'état = commande + paramètres du format de lecture (retrouvés par leur nom dans le format d'envoi)
        string KeyOf(Ent e, string frame)
        {
            StringBuilder k = new StringBuilder(e.Cmd);
            foreach (Tok r in e.Read)
            {
                if (r.Name == null) continue; int pos = -1;
                for (int i = 0; i < e.Set.Count; i++) { if (e.Set[i].Name == r.Name) { pos = i; break; } }
                if (pos >= 0 && pos < frame.Length) k.Append(frame[pos]);
            }
            return k.ToString();
        }
        string Default(Ent e, string readCmd)
        {
            StringBuilder sb = new StringBuilder();
            Dictionary<string, char> given = new Dictionary<string, char>();
            for (int i = 0; i < e.Read.Count && i < readCmd.Length; i++) if (e.Read[i].Name != null) given[e.Read[i].Name] = readCmd[i];
            foreach (Tok t in e.Set) { if (t.Name == null) sb.Append(t.Lit); else if (t.Sign) sb.Append('+'); else sb.Append(given.ContainsKey(t.Name) ? given[t.Name] : '0'); }
            string r = sb.ToString();
            if (e.Cmd == "MD" && r.Length > 3) r = r.Substring(0, r.Length - 1) + "2";
            if ((e.Cmd == "AG" || e.Cmd == "RG") && r.Length >= 5) r = r.Substring(0, r.Length - 3) + (e.Cmd == "AG" ? "064" : (Kw ? "100" : "255"));
            if (e.Cmd == "PC" && r.Length == 5) r = "PC100"; if (e.Cmd == "MG" && r.Length == 5) r = "MG050"; if (e.Cmd == "KS" && r.Length == 5) r = "KS020";
            return r + ";";
        }
        string Compose()
        {
            Ent f; ents.TryGetValue("FA", out f); int digits = f != null && f.Set != null ? f.Set.Count - 2 : 9;
            string fa = st.ContainsKey("FA") ? st["FA"].Substring(2).TrimEnd(';') : "014074000".Substring(9 - digits);
            string md = st.ContainsKey("MD0") ? st["MD0"] : "MD02"; md = md.TrimEnd(';'); string m = md.Substring(md.Length - 1);
            return "IF000" + fa + "+0000" + "00" + m + "0" + "0" + "00" + "0;";
        }
        // ---- particularités Kenwood : réponses dont la forme diffère de l'envoi (TS-480 : IF, SM0, RM, BY, RA, PA ; TS-890 : SM, BY, TX/RX si AI actif)
        string Kenwood(string cmd, string c2, Ent e)
        {
            bool t890 = Def.Catalog == "ts890"; if ((c2 == "BU" || c2 == "BD") && cmd.Length == 2) return "";
            if (cmd == "IF" && !t890)
            {
                string fa = st.ContainsKey("FA") ? st["FA"].Substring(2).TrimEnd(';') : "00014074000"; string md = st.ContainsKey("MD") ? st["MD"].Substring(2).TrimEnd(';') : "2";
                return "IF" + fa + "     " + "+0000" + "0" + "0" + "0" + "00" + (Tx ? "1" : "0") + md + "0" + "0" + "0" + "0" + "00" + " ;";
            }
            if (!t890 && cmd == "SM0") return "SM0" + (Wob(60, 40) * 30 / 255).ToString("0000") + ";";
            if (t890 && cmd == "SM") return "SM" + (Wob(90, 40) * 70 / 255).ToString("0000") + ";";
            if (!t890 && cmd.Length == 3 && c2 == "RM") return cmd + (Tx ? Wob(120, 30) * 10 / 255 : 0).ToString("0000") + ";";
            if (cmd == "BY") return t890 ? "BY0;" : "BY00;";
            if (c2 == "RX" && cmd == "RX") { S["tx"] = "0"; return t890 && st.ContainsKey("AI") && st["AI"] != "AI0;" ? "RX;" : ""; }
            if (c2 == "TX") { S["tx"] = "1"; return t890 && st.ContainsKey("AI") && st["AI"] != "AI0;" ? cmd + ";" : ""; }
            if (!t890 && (c2 == "RA" || c2 == "PA") && e.SetRx != null && e.SetRx.IsMatch(cmd) && cmd.Length == e.Set.Count) { st[c2] = cmd + (c2 == "RA" ? "00;" : "0;"); S[c2.ToLowerInvariant()] = cmd.Substring(2); return ""; }
            return null;
        }
        string One(string cmd)
        {
            if (cmd == "ID") return IdAnswer;
            if (cmd.Length < 2) return Fail(cmd);
            string c2; Ent e = Find(cmd, out c2); if (e == null) return Fail(cmd);
            if (Kw) { string k = Kenwood(cmd, c2, e); if (k != null) return k; }
            if (c2 == "IF" && cmd.Length == 2) return Compose();
            if (c2 == "SM" && cmd.Length == 3) return "SM" + cmd[2] + Wob(90, 40).ToString("000") + ";";
            if (c2 == "RM" && cmd.Length == 3) { int n = cmd[2] - '0'; int v = n == 1 ? Wob(90, 40) : (Tx ? Wob(120, 30) : 0); return "RM" + cmd[2] + v.ToString("000") + "000;"; }
            if (c2 == "BY" && cmd.Length == 2) return "BY00;";
            // ---- lecture
            if (e.ReadRx != null && cmd.Length == e.Read.Count && e.ReadRx.IsMatch(cmd))
            {
                if (e.Set == null) return (c2 == "OI" || c2 == "RS" || c2 == "UL") ? c2 + "0;" : Fail(cmd);
                string v; if (st.TryGetValue(cmd, out v)) return v;
                return Default(e, cmd);
            }
            // ---- envoi
            if (e.SetRx != null && (e.Variable || cmd.Length == e.Set.Count) && e.SetRx.IsMatch(cmd))
            {
                if (e.Read != null && !e.Variable) { string k = KeyOf(e, cmd); st[k] = cmd + ";"; S[k.ToLowerInvariant()] = cmd.Substring(k.Length); }
                if (c2 == "TX" || c2 == "MX") S["tx"] = cmd[2] == '0' ? "0" : "1";
                if (c2 == "PS") S["ps"] = cmd.Substring(2);
                return "";
            }
            return Fail(cmd);
        }
    }
}
