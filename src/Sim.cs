using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace HamRadioLink
{
    // Poste Yaesu simulé (FT-2000 ou FTdx10). Il applique STRICTEMENT les formats des manuels CAT (nombre de chiffres de chaque paramètre,
    // bornes) : toute commande mal formée ou hors bornes reçoit « ?; » et est enregistrée dans Bad.
    // Écrit à partir des tableaux « Control Command Tables » des manuels, indépendamment des gabarits de Radios.cs.
    public class YaesuSim
    {
        public readonly RadioDef Def; readonly bool F20, X5;
        public readonly List<string> Bad = new List<string>(); public int Count; public readonly List<string> Seen = new List<string>();
        public readonly Dictionary<string, string> S = new Dictionary<string, string>();
        readonly StringBuilder buf = new StringBuilder();
        class Reg { public Regex Set; public string ReadCmd, Fmt, Key; public long Lo = long.MinValue, Hi = long.MaxValue; public int Mod = 1; public Action<string> After; }
        readonly List<Reg> regs = new List<Reg>(); readonly List<KeyValuePair<Regex, Action<Match>>> acts = new List<KeyValuePair<Regex, Action<Match>>>();
        public bool Tx { get { return S["tx"] == "1"; } }
        readonly Random rnd = new Random(3);

        void R(string key, string setPat, string readCmd, string fmt, string init) { R(key, setPat, readCmd, fmt, init, long.MinValue, long.MaxValue); }
        void R(string key, string setPat, string readCmd, string fmt, string init, long lo, long hi)
        {
            Reg g = new Reg(); g.Key = key; g.Set = setPat == null ? null : new Regex("^" + setPat + "$"); g.ReadCmd = readCmd; g.Fmt = fmt; g.Lo = lo; g.Hi = hi; S[key] = init; regs.Add(g);
        }
        Reg Last { get { return regs[regs.Count - 1]; } }
        void A(string pat, Action<Match> a) { acts.Add(new KeyValuePair<Regex, Action<Match>>(new Regex("^" + pat + "$"), a)); }

        public YaesuSim(RadioDef def)
        {
            Def = def; X5 = def.Key == "ftdx5000"; F20 = def.Key == "ft2000" || X5;
            if (def.Civ >= 0) { cv = new CivSim(def, Bad, Seen, S); return; }
            if (def.Tci) { ts = new TciSim(false, Bad, Seen, S); return; }
            if (def.Bin || def.Leg != null) { y5 = new Y5Sim(def, Bad, Seen, S); return; }
            if (def.Catalog != null) { cs = new CatalogSim(def, Bad, Seen, S); return; }
            S["tx"] = "0"; S["ps"] = "1"; S["ai"] = "0";
            string modeSet = F20 ? "[1-9A-C]" : "[1-9A-F]";
            if (F20)
            {
                R("fa", @"FA(\d{8})", "FA", "FA{0}", "14074000", 30000, 60000000); R("fb", @"FB(\d{8})", "FB", "FB{0}", "07100000", 30000, 60000000);
                R("md0", "MD0(" + modeSet + ")", "MD0", "MD0{0}", "2"); R("md1", "MD1(" + modeSet + ")", "MD1", "MD1{0}", "1");
                R("ag0", @"AG0(\d{3})", "AG0", "AG0{0}", "064", 0, 255); R("ag1", @"AG1(\d{3})", "AG1", "AG1{0}", "064", 0, 255);
                R("rg0", @"RG0(\d{3})", "RG0", "RG0{0}", "255", 0, 255); R("rg1", @"RG1(\d{3})", "RG1", "RG1{0}", "255", 0, 255);
                R("sq0", @"SQ0(\d{3})", "SQ0", "SQ0{0}", "000", 0, 255); R("sq1", @"SQ1(\d{3})", "SQ1", "SQ1{0}", "000", 0, 255);
                R("mg", @"MG(\d{3})", "MG", "MG{0}", "050", 0, 255); R("pc", @"PC(\d{3})", "PC", "PC{0}", "100", 5, 100); R("pl", @"PL(\d{3})", "PL", "PL{0}", "050", 0, 255);
                R("pr", "PR([01])", "PR", "PR{0}", "0");
                R("ml0", @"ML0(\d{3})", "ML0", "ML0{0}", "000", 0, 1); R("ml1", @"ML1(\d{3})", "ML1", "ML1{0}", "050", 1, 255);
                R("vg", @"VG(\d{3})", "VG", "VG{0}", "050", 0, 255);
                R("nb0", "NB0([012])", "NB0", "NB0{0}", "0"); R("nb1", "NB1([012])", "NB1", "NB1{0}", "0");
                R("nl0", @"NL0(\d{3})", "NL0", "NL0{0}", "050", 0, 255); R("nr0", "NR0([01])", "NR0", "NR0{0}", "0"); R("rl0", @"RL0(\d{2})", "RL0", "RL0{0}", "05", 1, 15);
                R("bc", "BC0([01])", "BC0", "BC0{0}", "0");
                R("bpo", @"BP00(\d{3})", "BP00", "BP00{0}", "000", 0, 1); R("bpf", @"BP01(\d{3})", "BP01", "BP01{0}", "200", 1, 400);
                R("coo", @"CO00(\d{2})", "CO00", "CO00{0}", "00", 0, X5 ? 2 : 1); R("cof", @"CO01(\d{2})", "CO01", "CO01{0}", "10", 1, 40);
                R("is", @"IS0([+-]\d{4})", "IS0", "IS0{0}", "+0000", -1000, 1000); Last.Mod = 20;
                R("vd", @"VD(\d{4})", "VD", "VD{0}", "0300", X5 ? 20 : 0, 5000); if (X5) Last.Mod = 20; R("sd", @"SD(\d{4})", "SD", "SD{0}", "0300", X5 ? 20 : 0, 5000); if (X5) Last.Mod = 20; R("ms", "MS([0-5])", "MS", "MS{0}", "2");
                if (X5) { R("ex104", "EX104([0-5])", "EX104", "EX104{0}", "0"); R("ex114", "EX114([01])", "EX114", "EX114{0}", "0"); R("ex174", "EX174([01])", "EX174", "EX174{0}", "0"); }
                else { for (int mn = 1; mn <= 149; mn++) if (mn != 85 && mn != 94 && mn != 148 && F20) R("ex" + mn.ToString("000"), "EX" + mn.ToString("000") + "([+-]?\\d{1,7})", "EX" + mn.ToString("000"), "EX" + mn.ToString("000") + "{0}", mn <= 6 ? "0300" : mn == 86 ? "+0000" : "00"); R("ex085", "EX085([0-5])", "EX085", "EX085{0}", "0"); R("ex094", "EX094([01])", "EX094", "EX094{0}", "0"); R("ex148", "EX148([01])", "EX148", "EX148{0}", "0"); }
                R("sh", @"SH0(\d{2})", "SH0", "SH0{0}", "16", 0, X5 ? 25 : 31);
                R("na0", "NA0([01])", "NA0", "NA0{0}", "0"); R("na1", "NA1([01])", "NA1", "NA1{0}", "0");
                R("gt0", "GT0([0-4])", "GT0", "GT0{0}", "5"); R("gt1", "GT1([0-4])", "GT1", "GT1{0}", "1");
                R("pa", X5 ? "PA0([0-3])" : "PA0([0-2])", "PA0", "PA0{0}", "1"); R("ra", "RA0([0-3])", "RA0", "RA0{0}", "0");
                if (X5)
                {
                    // roofing : l'envoi (0-5) et la réponse (1-3, 4-6 AUTO, 7 600 Hz, 8 300 Hz, 9/A AUTO étroit) n'ont pas la même table
                    R("rf", "RF0([0-5])", "RF0", "RF0{0}", "5"); Last.After = delegate(string v) { string[] map = { "5", "1", "2", "3", "7", "8" }; S["rf"] = map[int.Parse(v)]; };
                    R("anx", "AN0([1-5])", "AN0_", "AN0_{0}", "1"); S["anprev"] = "1"; S["rxant"] = "0";
                    Last.After = delegate(string v) { if (v == "5") { S["rxant"] = "1"; S["anx"] = S["anprev"]; } else { S["anprev"] = v; S["rxant"] = "0"; } };
                    R("ft", "FT([23])", "FT", "FT{0}", "0"); Last.After = delegate(string v) { S["ft"] = v == "3" ? "1" : "0"; };
                    Latch("fs", "FS");
                    R("ca0", @"CA0(\d{3})", "CA0", "CA0{0}", "000", 0, 1); R("ca1", @"CA1(\d{3})", "CA1", "CA1{0}", "050", 1, 100);
                    R("vf0", @"VF0([01])\+0", "VF0", "VF0{0}1280", "0"); R("vf1", @"VF1([01])\+0", "VF1", "VF1{0}1280", "0");
                    R("os0", "OS0([0-2])", "OS0", "OS0{0}", "0"); R("ct0", "CT0([0-2])", "CT0", "CT0{0}", "0");
                    // récepteur sub (P1 = 1)
                    R("is1", @"IS1([+-]\d{4})", "IS1", "IS1{0}", "+0000", -1000, 1000); Last.Mod = 20; R("sh1", @"SH1(\d{2})", "SH1", "SH1{0}", "16", 0, 25);
                    R("bp10", @"BP10(\d{3})", "BP10", "BP10{0}", "000", 0, 1); R("bp11", @"BP11(\d{3})", "BP11", "BP11{0}", "200", 1, 400);
                    R("co10", @"CO10(\d{2})", "CO10", "CO10{0}", "00", 0, 2); R("co11", @"CO11(\d{2})", "CO11", "CO11{0}", "10", 1, 40);
                    R("bc1", "BC1([01])", "BC1", "BC1{0}", "0"); R("nr1", "NR1([01])", "NR1", "NR1{0}", "0"); R("rl1", @"RL1(\d{2})", "RL1", "RL1{0}", "05", 1, 15); R("nl1", @"NL1(\d{3})", "NL1", "NL1{0}", "050", 0, 255);
                    R("ra1", "RA1([0-3])", "RA1", "RA1{0}", "0"); R("pa1", "PA1([0-3])", "PA1", "PA1{0}", "1"); R("rf1", "RF1([0-3])", "RF1", "RF1{0}", "5");
                    R("anx1", "AN1([1-5])", "AN1_", "AN1_{0}", "1"); S["anprev1"] = "1"; S["rxantb"] = "0";
                    Last.After = delegate(string v) { if (v == "5") { S["rxantb"] = "1"; S["anx1"] = S["anprev1"]; } else { S["anprev1"] = v; S["rxantb"] = "0"; } };
                }
                else { R("rf", "RF0([0-3])", "RF0", "RF0{0}", "5"); R("an", "AN0([12])", "AN0", "AN0{0}0", "1"); R("ft", "FT([23])", "FT", "FT{0}", "0"); Last.After = delegate(string v) { S["ft"] = v == "3" ? "1" : "0"; }; }
                R("fr", "FR([0-3])", "FR", "FR{0}", "0");
                R("ks", @"KS(\d{3})", "KS", "KS{0}", "020", 4, 60); R("kp", @"KP(\d{2})", "KP", "KP{0}", "07", 0, X5 ? 75 : 15);
                R("ds", "DS([01])", "DS", "DS{0}", "0"); if (!X5) R("fs", "FS([01])", "FS", "FS{0}", "0");
                A(@"RU(\d{4})", delegate(Match m) { S["clar"] = "+" + m.Groups[1].Value; }); A(@"RD(\d{4})", delegate(Match m) { S["clar"] = "-" + m.Groups[1].Value; });
            }
            else
            {
                R("fa", @"FA(\d{9})", "FA", "FA{0}", "014074000", 30000, 75000000); R("fb", @"FB(\d{9})", "FB", "FB{0}", "007100000", 30000, 75000000);
                R("md0", "MD0(" + modeSet + ")", "MD0", "MD0{0}", "2"); R("md1", "MD1(" + modeSet + ")", "MD1", "MD1{0}", "1");
                R("ag0", @"AG0(\d{3})", "AG0", "AG0{0}", "064", 0, 255); R("rg0", @"RG0(\d{3})", "RG0", "RG0{0}", "255", 0, 255); R("sq0", @"SQ0(\d{3})", "SQ0", "SQ0{0}", "000", 0, 100);
                R("mg", @"MG(\d{3})", "MG", "MG{0}", "050", 0, 100); R("pc", @"PC(\d{3})", "PC", "PC{0}", "100", 5, 100); R("pl", @"PL(\d{3})", "PL", "PL{0}", "050", 0, 100);
                R("pr0", "PR0([012])", "PR0", "PR0{0}", "0");   // 0 = arrêt, 1 = marche (essai sur FTdx10 réel) ; le manuel imprime 1 = OFF, 2 = ON
                R("ml0", @"ML0(\d{3})", "ML0", "ML0{0}", "000", 0, 1); R("ml1", @"ML1(\d{3})", "ML1", "ML1{0}", "050", 0, 100);
                R("vg", @"VG(\d{3})", "VG", "VG{0}", "050", 0, 100);
                R("nb0", "NB0([01])", "NB0", "NB0{0}", "0"); R("nl0", @"NL0(\d{3})", "NL0", "NL0{0}", "005", 0, 10); R("nr0", "NR0([01])", "NR0", "NR0{0}", "0"); R("rl0", @"RL0(\d{2})", "RL0", "RL0{0}", "05", 1, 15);
                R("bc", "BC0([01])", "BC0", "BC0{0}", "0");
                R("bpo", @"BP00(\d{3})", "BP00", "BP00{0}", "000", 0, 1); R("bpf", @"BP01(\d{3})", "BP01", "BP01{0}", "100", 1, 320);
                R("coo", @"CO00(\d{4})", "CO00", "CO00{0}", "0000", 0, 1); R("cof", @"CO01(\d{4})", "CO01", "CO01{0}", "0500", 10, 3200); R("coa", @"CO02(\d{4})", "CO02", "CO02{0}", "0000", 0, 1); R("cof3", @"CO03(\d{4})", "CO03", "CO03{0}", "0025", 0, 50);
                R("is", @"IS00([+-]\d{4})", "IS0", "IS00{0}", "+0000", -1200, 1200); Last.Mod = 20; R("sh", @"SH00(\d{2})", "SH0", "SH00{0}", "16", 0, 23);
                R("vd", @"VD(\d{2})", "VD", "VD{0}", "05", 0, 33); R("sd", @"SD(\d{2})", "SD", "SD{0}", "10", 0, 33); R("ms", "MS([0-5])0", "MS", "MS{0}0", "0");
                R("na0", "NA0([01])", "NA0", "NA0{0}", "0");
                R("gt0", "GT0([0-4])", "GT0", "GT0{0}", "5");
                R("pa", "PA0([0-2])", "PA0", "PA0{0}", "1"); R("ra", "RA0([0-3])", "RA0", "RA0{0}", "0"); R("rf", "RF0([1-5])", "RF0", "RF0{0}", "2");
                R("st", "ST([012])", "ST", "ST{0}", "0"); R("fn", "FN([01])", "FN", "FN{0}", "0");
                R("ks", @"KS(\d{3})", "KS", "KS{0}", "020", 4, 60); R("kp", @"KP(\d{2})", "KP", "KP{0}", "40", 0, 75);
                R("cf", @"CF001([+-]\d{4})", "CF001", "CF001{0}", "+0000", -9990, 9990); Last.Mod = 10; Last.After = delegate(string v) { S["clar"] = v; };
                A("BA", delegate(Match m) { S["fa"] = S["fb"]; });
                A("ZI0", delegate(Match m) { });
            }
            // communs aux deux postes
            R("vs", "VS([01])", "VS", "VS{0}", "0"); R("ts", "TS([01])", "TS", "TS{0}", "0"); if (X5) Latch("lk", "LK"); else R("lk", "LK([01])", "LK", "LK{0}", "0");
            R("vx", "VX([01])", "VX", "VX{0}", "0"); R("mx", "MX([01])", "MX", "MX{0}", "0"); R("cs", "CS([01])", "CS", "CS{0}", "0");
            R("kr", "KR([01])", "KR", "KR{0}", "1"); R("bi", "BI([01])", "BI", "BI{0}", "0");
            R("ac", "AC00([012])", "AC", "AC00{0}", "0"); Last.After = delegate(string v) { if (v == "2") S["ac"] = "1"; };
            R("rt", "RT([01])", "RT", "RT{0}", "0"); R("xt", "XT([01])", "XT", "XT{0}", "0");
            R("ps", "PS([01])", "PS", "PS{0}", "1"); R("ai", "AI([01])", "AI", "AI{0}", "0");
            S["clar"] = "+0000"; S["vm"] = "0"; S["busy"] = "0";
            A("TX([01])", delegate(Match m) { S["tx"] = m.Groups[1].Value; });
            A("AB", delegate(Match m) { S["fb"] = S["fa"]; S["md1"] = S["md0"]; });
            A("SV", delegate(Match m) { string t = S["fa"]; S["fa"] = S["fb"]; S["fb"] = t; t = S["md0"]; S["md0"] = S["md1"]; S["md1"] = t; });
            A("VM", delegate(Match m) { S["vm"] = S["vm"] == "0" ? "1" : "0"; });
            A("QR", delegate(Match m) { }); A("QI", delegate(Match m) { }); A("QS", delegate(Match m) { });
            A("CH[01]", delegate(Match m) { });
            A("RC", delegate(Match m) { S["clar"] = "+0000"; if (S.ContainsKey("cf")) S["cf"] = "+0000"; });
            A("KY([1-9A])", delegate(Match m) { }); A("PB0([0-5])", delegate(Match m) { });
            A("B([UD])([01])", delegate(Match m) { long f = long.Parse(S["fa"]); int dir = m.Groups[1].Value == "U" ? 1 : -1; string k = m.Groups[2].Value == "0" ? "fa" : "fb"; int b = Band(long.Parse(S[k])); b = (b + dir + 12) % 12; SetBand(k, b); });
            A(@"BS(\d{2})", delegate(Match m) { int b = int.Parse(m.Groups[1].Value); if (b > 11 || (X5 && b == 2)) throw new FormatException(); SetBand("fa", b); });
        }
        // FTdx5000 : LK / FS acceptent 0-3 (VFO-A OFF/ON, VFO-B OFF/ON) et répondent 4-7 (4 + 1 si A actif + 2 si B actif)
        void Latch(string key, string cmd)
        {
            R(key, cmd + "([0-3])", cmd, cmd + "{0}", "4"); S[key + "a"] = "0"; S[key + "b"] = "0";
            Last.After = delegate(string v)
            {
                int n = int.Parse(v); if (n < 2) S[key + "a"] = v; else S[key + "b"] = (n - 2).ToString();
                S[key] = (4 + (S[key + "a"] == "1" ? 1 : 0) + (S[key + "b"] == "1" ? 2 : 0)).ToString();
            };
        }
        static readonly long[] BandF = { 1840000, 3573000, 5357000, 7074000, 10136000, 14074000, 18100000, 21074000, 24915000, 28074000, 50313000, 3000000 };
        int Band(long f) { for (int i = 0; i < 11; i++) if (f >= Radios.BandLo[i] && f <= Radios.BandHi[i]) return i; return 11; }
        void SetBand(string key, int b) { S[key] = BandF[b].ToString().PadLeft(F20 ? 8 : 9, '0'); }

        CatalogSim cs; CivSim cv; Y5Sim y5; TciSim ts;
        public string Input(string s)
        {
            if (cv != null) { string rr = cv.Input(s); Count = cv.Count; return rr; }
            if (y5 != null) { string rr = y5.Input(s); Count = y5.Count; return rr; }
            if (ts != null) { string rr = ts.Input(s); Count = ts.Count; return rr; }
            if (cs != null) { string rr = cs.Input(s); Count = cs.Count; return rr; }
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
        int Wob(int basev, int amp) { return Math.Max(0, Math.Min(255, basev + (int)(Math.Sin(Environment.TickCount / 700.0) * amp))); }

        string One(string cmd)
        {
            if (cmd == "ID") return Def.IdAnswer;
            if (X5 && cmd == "AN0") return "AN0" + S["anx"] + S["rxant"] + ";";
            if (X5 && cmd == "AN1") return "AN1" + S["anx1"] + S["rxantb"] + ";";
            // ---- lectures calculées
            if (cmd == "TX") return "TX" + (Tx ? "1" : "0") + ";";
            if (cmd == "BY") return "BY" + S["busy"] + (F20 ? S["busy"] : "0") + ";";
            if (cmd == "IF") return F20 ? "IF000" + S["fa"] + S["clar"] + S["rt"] + S["xt"] + S["md0"] + S["vm"] + "000" + "0;" : "IF000" + S["fa"] + S["clar"] + S["rt"] + S["xt"] + S["md0"] + S["vm"] + "000" + "0;";
            if (cmd == "SM0" || (F20 && cmd == "SM1")) return cmd + Wob(cmd == "SM0" ? 90 : 60, 40).ToString("000") + ";";
            Match mm = Regex.Match(cmd, @"^RM(\d)$");
            if (mm.Success)
            {
                int n = int.Parse(mm.Groups[1].Value); int v;
                switch (n)
                {
                    case 1: v = Wob(90, 40); break; case 2: if (!F20) return Fail(cmd); v = Wob(60, 40); break;
                    case 3: v = Tx ? 70 : 0; break; case 4: v = Tx ? 90 : 0; break; case 5: v = Tx ? Wob(180, 20) : 0; break; case 6: v = Tx ? 25 : 0; break; case 7: v = Tx ? 120 : 0; break; case 8: v = 200; break;
                    default: return Fail(cmd);
                }
                return "RM" + n + v.ToString("000") + (F20 ? "" : "000") + ";";
            }
            // ---- registres
            foreach (Reg g in regs)
            {
                if (g.ReadCmd == cmd) { string val = S[g.Key]; if (g.Key.StartsWith("gt") && val == "4") val = "5"; return Fmt(g, val) + ";"; }
                if (g.Set == null) continue; Match m = g.Set.Match(cmd);
                if (!m.Success) continue;
                string v = m.Groups[1].Value;
                if (g.Lo != long.MinValue)
                {
                    long n; if (!long.TryParse(v, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out n)) return Fail(cmd);
                    if (n < g.Lo || n > g.Hi) return Fail(cmd);
                    if (g.Mod > 1 && Math.Abs(n) % g.Mod != 0) return Fail(cmd);
                }
                S[g.Key] = v; if (g.Key == "mx") S["tx"] = v; if (g.After != null) g.After(v); return "";
            }
            foreach (KeyValuePair<Regex, Action<Match>> a in acts)
            {
                Match m = a.Key.Match(cmd); if (!m.Success) continue;
                try { a.Value(m); return ""; } catch (FormatException) { return Fail(cmd); }
            }
            return Fail(cmd);
        }
        string Fmt(Reg g, string val) { return g.Fmt.Replace("{0}", val); }
    }
}
