using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace HamRadioLink
{
    // Yaesu à protocole binaire de 5 octets (FT-817, FT-818, FT-857, FT-897) : le moteur parle un « CAT texte » simple, Y5Link le traduit en trames
    //   P1 P2 P3 P4 CMD  (réponses sans en-tête, de longueur fixe selon la commande).
    // Texte (chaque commande finit par « ; ») :
    //   écriture : FAnnnnnnnnn (Hz, multiple de 10) · MDxx (mode) · TX0/TX1 · LK0/LK1 · SP0/SP1 · CL0/CL1 · VS (bascule VFO A/B)
    //   lecture  : FA; MD; (commande 03) · SM; BY; (E7 : S-mètre, squelch) · TX; PO; SW; (F7 : émission, puissance, SWR élevé)
    public static class Y5
    {
        public const string Modes = "LSB=00,USB=01,CW=02,CW-R=03,AM=04,FM=08,WFM=06,DIG=0A,PKT=0C";
        public static string BcdFreq(long hz) { string d = (hz / 10).ToString(CultureInfo.InvariantCulture).PadLeft(8, '0'); return Hex(d); }
        // « 01425000 » → 4 caractères (un par octet)
        public static string Hex(string h) { StringBuilder sb = new StringBuilder(); for (int i = 0; i + 1 < h.Length; i += 2) sb.Append((char)Convert.ToInt32(h.Substring(i, 2), 16)); return sb.ToString(); }
        public static string Frame(string p4hex, int cmd) { return Hex(p4hex) + (char)cmd; }
        public static string FromBcd(string s, int n) { StringBuilder sb = new StringBuilder(); for (int i = 0; i < n; i++) sb.Append(((int)s[i]).ToString("X2")); return sb.ToString(); }

        // texte → trame ; read = nature de la réponse attendue (' ' = aucune), len = longueur de cette réponse. null = commande inconnue.
        public static string Encode(string tok, out char read, out int len)
        {
            read = ' '; len = 0; Match m;
            if ((m = Regex.Match(tok, @"^FA(\d{9})$")).Success) return BcdFreq(long.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture)) + (char)0x01;
            if ((m = Regex.Match(tok, @"^MD([0-9A-F]{2})$")).Success) return Frame(m.Groups[1].Value + "000000", 0x07);
            if (tok == "FA") { read = 'F'; len = 5; return Frame("00000000", 0x03); }
            if (tok == "MD") { read = 'M'; len = 5; return Frame("00000000", 0x03); }
            if (tok == "SM") { read = 'S'; len = 1; return Frame("00000000", 0xE7); }
            if (tok == "BY") { read = 'B'; len = 1; return Frame("00000000", 0xE7); }
            if (tok == "TX") { read = 'T'; len = 1; return Frame("00000000", 0xF7); }
            if (tok == "PO") { read = 'P'; len = 1; return Frame("00000000", 0xF7); }
            if (tok == "SW") { read = 'W'; len = 1; return Frame("00000000", 0xF7); }
            switch (tok)
            {
                case "TX1": return Frame("00000000", 0x08); case "TX0": return Frame("00000000", 0x88);
                case "LK1": return Frame("00000000", 0x00); case "LK0": return Frame("00000000", 0x80);
                case "SP1": return Frame("00000000", 0x02); case "SP0": return Frame("00000000", 0x82);
                case "CL1": return Frame("00000000", 0x05); case "CL0": return Frame("00000000", 0x85);
                case "VS": return Frame("00000000", 0x81);
            }
            return null;
        }
        public static int Smeter(int n) { n &= 0x0F; return n <= 9 ? n * 120 / 9 : 120 + (n - 9) * 121 / 6; }   // 0-15 du poste → 0-241 (S9 = 120)
        // réponse binaire → texte
        public static string Decode(char kind, string b)
        {
            switch (kind)
            {
                case 'F': { long hz = long.Parse(FromBcd(b, 4), CultureInfo.InvariantCulture) * 10; return "FA" + hz.ToString(CultureInfo.InvariantCulture).PadLeft(9, '0') + ";"; }
                case 'M': return "MD" + ((int)b[4]).ToString("X2") + ";";
                case 'S': return "SM" + Smeter(b[0]) + ";";
                case 'B': return "BY" + ((b[0] & 0x80) == 0 ? "1" : "0") + ";";        // bit 7 : 1 = squelch fermé
                case 'T': return "TX" + ((b[0] & 0x80) == 0 ? "1" : "0") + ";";        // bit 7 : 0 = en émission
                case 'P': return "PO" + (b[0] & 0x0F) + ";";
                case 'W': return "SW" + ((b[0] & 0x40) != 0 ? "1" : "0") + ";";        // bit 6 : SWR élevé
            }
            return "";
        }
    }

    // Liaison : texte du moteur ↔ trames de 5 octets
    public class Y5Link : ILink
    {
        class Pend { public char K; public int Len; public long T; }
        readonly ILink inner; readonly StringBuilder rx = new StringBuilder(); readonly Queue<Pend> q = new Queue<Pend>(); readonly object lk = new object();
        readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
        public event Action<string> Data;
        public string Name { get { return inner.Name + " · Yaesu 5 octets"; } }
        public int Baud { get { return inner.Baud; } }
        public Y5Link(ILink inner) { this.inner = inner; }
        public bool Open(out string err) { inner.Data += OnRaw; if (inner.Open(out err)) return true; inner.Data -= OnRaw; return false; }
        public void Close() { inner.Data -= OnRaw; inner.Close(); }
        void Stale() { lock (lk) { while (q.Count > 0 && clock.ElapsedMilliseconds - q.Peek().T > 1800) { q.Dequeue(); rx.Length = 0; } } }
        public void Send(string s)
        {
            Stale();
            foreach (string t in s.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                char rd; int len; string fr = Y5.Encode(t.Trim().ToUpperInvariant(), out rd, out len); if (fr == null) continue;
                if (rd != ' ') lock (lk) { Pend p = new Pend(); p.K = rd; p.Len = len; p.T = clock.ElapsedMilliseconds; q.Enqueue(p); }
                inner.Send(fr);
            }
        }
        void OnRaw(string s)
        {
            StringBuilder o = new StringBuilder();
            lock (lk)
            {
                Stale(); rx.Append(s);
                while (q.Count > 0 && rx.Length >= q.Peek().Len) { Pend p = q.Dequeue(); string b = rx.ToString(0, p.Len); rx.Remove(0, p.Len); o.Append(Y5.Decode(p.K, b)); }
                if (q.Count == 0) rx.Length = 0;      // rien n'est attendu : octets parasites
            }
            if (o.Length > 0 && Data != null) Data(o.ToString());
        }
    }

    // Poste simulé sur le fil (essais de Y5Link) : décode les trames de 5 octets et les passe au simulateur texte
    public class Y5WireLink : ILink
    {
        public readonly YaesuSim Sim; readonly StringBuilder rx = new StringBuilder(); public event Action<string> Data;
        public string Name { get { return "Poste Yaesu 5 octets simulé (fil)"; } }
        public int Baud { get { return 9600; } }
        public Y5WireLink(YaesuSim sim) { Sim = sim; }
        public bool Open(out string err) { err = null; return true; }
        public void Close() { }
        string Ask(string t) { return Sim.Input(t + ";"); }
        static string Val(string r) { Match m = Regex.Match(r, @"^[A-Z]{2}(\d+);$"); return m.Success ? m.Groups[1].Value : "0"; }
        public void Send(string s)
        {
            StringBuilder o = new StringBuilder(); rx.Append(s);
            while (rx.Length >= 5)
            {
                string f = rx.ToString(0, 5); rx.Remove(0, 5); int cmd = f[4]; string p = Y5.FromBcd(f, 4);
                switch (cmd)
                {
                    case 0x01: Ask("FA" + (long.Parse(p, CultureInfo.InvariantCulture) * 10).ToString(CultureInfo.InvariantCulture).PadLeft(9, '0')); break;
                    case 0x03: { string fa = Ask("FA"), md = Ask("MD"); if (fa.Length >= 12 && md.Length >= 5) o.Append(Y5.Hex((long.Parse(Val(fa)) / 10).ToString().PadLeft(8, '0'))).Append((char)Convert.ToInt32(md.Substring(2, 2), 16)); } break;
                    case 0x07: Ask("MD" + p.Substring(0, 2)); break;
                    case 0x08: Ask("TX1"); break; case 0x88: Ask("TX0"); break;
                    case 0x00: Ask("LK1"); break; case 0x80: Ask("LK0"); break;
                    case 0x02: Ask("SP1"); break; case 0x82: Ask("SP0"); break;
                    case 0x05: Ask("CL1"); break; case 0x85: Ask("CL0"); break;
                    case 0x81: Ask("VS"); break;
                    case 0xE7: { int n = Y5Inv(int.Parse(Val(Ask("SM")))); bool busy = Val(Ask("BY")) == "1"; o.Append((char)(n | (busy ? 0x80 : 0))); } break;
                    case 0xF7: { bool tx = Val(Ask("TX")) == "1"; int po = int.Parse(Val(Ask("PO"))); bool sw = Val(Ask("SW")) == "1"; o.Append((char)((tx ? 0 : 0x80) | (sw ? 0x40 : 0) | (po & 0x0F))); } break;
                    default: Sim.Bad.Add("octet de commande inconnu " + cmd.ToString("X2")); break;
                }
            }
            if (o.Length > 0 && Data != null) Data(o.ToString());
        }
        static int Y5Inv(int scaled) { for (int n = 0; n < 16; n++) if (Y5.Smeter(n) >= scaled) return n; return 15; }
    }

    // Poste simulé (texte) : formats stricts
    public class Y5Sim
    {
        readonly RadioDef Def; readonly List<string> Bad, Seen; readonly Dictionary<string, string> S; public int Count;
        public Y5Sim(RadioDef def, List<string> bad, List<string> seen, Dictionary<string, string> s)
        {
            Def = def; Bad = bad; Seen = seen; S = s;
            S["fa"] = "14074000"; S["md"] = "01"; S["tx"] = "0"; S["lk"] = "0"; S["sp"] = "0"; S["cl"] = "0"; S["vs"] = "0"; S["sm"] = "9"; S["po"] = "0"; S["sw"] = "0"; S["by"] = "1";
        }
        readonly StringBuilder buf = new StringBuilder();
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
        string Fail(string c) { Bad.Add(c + ";"); return "?;"; }
        string One(string c)
        {
            Match m;
            if ((m = Regex.Match(c, @"^FA(\d{9})$")).Success)
            {
                long hz = long.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture); if (hz < 100000 || hz > Def.MaxFreq || hz % 10 != 0) return Fail(c);
                S["fa"] = hz.ToString(CultureInfo.InvariantCulture); return "";
            }
            if ((m = Regex.Match(c, @"^MD([0-9A-F]{2})$")).Success)
            {
                if (!Regex.IsMatch(m.Groups[1].Value, "^(00|01|02|03|04|06|08|0A|0C)$")) return Fail(c);
                S["md"] = m.Groups[1].Value; return "";
            }
            if ((m = Regex.Match(c, @"^(TX|LK|SP|CL)([01])$")).Success) { S[m.Groups[1].Value.ToLowerInvariant()] = m.Groups[2].Value; return ""; }
            switch (c)
            {
                case "VS": S["vs"] = S["vs"] == "0" ? "1" : "0"; return "";
                case "FA": return "FA" + S["fa"].PadLeft(9, '0') + ";";
                case "MD": return "MD" + S["md"] + ";";
                case "SM": return "SM" + Y5.Smeter(int.Parse(S["sm"])) + ";";
                case "BY": return "BY" + (S["tx"] == "1" ? "0" : S["by"]) + ";";
                case "TX": return "TX" + S["tx"] + ";";
                case "PO": return "PO" + (S["tx"] == "1" ? "8" : "0") + ";";
                case "SW": return "SW" + S["sw"] + ";";
            }
            return Fail(c);
        }
    }

    public static partial class Radios
    {
        class YM { public string Key, Name; public long Max; public int Baud = 9600; public YM(string k, string n, long max) { Key = k; Name = n; Max = max; } public YM(string k, string n, long max, int baud) { Key = k; Name = n; Max = max; Baud = baud; } }
        static List<YM> Y5Models()
        {
            List<YM> l = new List<YM>();
            l.Add(new YM("ft817", "FT-817 / FT-817ND", 470000000)); l.Add(new YM("ft818", "FT-818 / FT-818ND", 470000000));
            l.Add(new YM("ft857", "FT-857 / FT-857D", 470000000)); l.Add(new YM("ft897", "FT-897 / FT-897D", 470000000));
            l.Add(new YM("ft847", "FT-847", 470000000, 57600)); l.Add(new YM("ft650", "FT-650", 56000000, 4800));
            return l;
        }
        public static RadioDef Y5ByKey(string key) { foreach (YM m in Y5Models()) if (m.Key == key) return Yaesu5(m); return null; }
        static List<RadioDef> Y5All() { List<RadioDef> l = new List<RadioDef>(); foreach (YM m in Y5Models()) l.Add(Yaesu5(m)); return l; }

        const string Y5Bands = "1.8=001840000,3.5=003573000,7=007074000,10=010136000,14=014074000,18=018100000,21=021074000,24.9=024915000,28=028074000,50=050313000,144=144300000,430=433000000";

        // touches de bande utilisables : seules celles dont la fréquence type est dans la couverture du poste
        static string BandsFor(long max, long min)
        {
            List<string> l = new List<string>(); foreach (string p in Y5Bands.Split(',')) { long f = long.Parse(p.Substring(p.IndexOf('=') + 1)); if (f <= max && f >= min) l.Add(p); } return string.Join(",", l.ToArray());
        }
        static RadioDef Yaesu5(YM m)
        {
            RadioDef r = new RadioDef(); r.Key = m.Key; r.Name = "Yaesu " + m.Name; r.Bin = true; r.Stop2 = true; r.DefBaud = m.Baud; r.FreqDigits = 9; r.MaxFreq = m.Max; r.IdCmd = "FA;"; r.IdAnswer = ""; r.Init = "";
            r.Notes.Add("protocole binaire 5 octets traduit par la liaison ; jeu de commandes de base (fréquence, mode, PTT, verrouillage, split, CLAR, VFO A/B, S-mètre, puissance) ; AUCUN manuel CAT disponible pour ce poste : à vérifier sur le poste, tout se corrige par clic droit");
            Bld b = new Bld(r); Ctl c;
            b.Frame("f_pwr", "ÉMISSION", 10, 124, 210, 330);
            c = b.Tog("ptt", "ÉMISSION (PTT)", 24, 152, 182, 60, "TX{v};", "TX;", @"^TX(?<v>[01]);$", "1", "0"); c.Accent = "red"; c.Period = 250;
            b.Tog("lock", "LOCK", 24, 226, 182, 50, "LK{v};", "", "").Hint = "Verrouillage du poste (00 / 80) ; l'état n'est pas relu : le poste ne le renvoie pas.";
            b.Frame("f_scr", "ÉCRAN", 230, 124, 1010, 330);
            b.Freq("fa", "VFO SÉLECTIONNÉ", 244, 150, 986, 150, "FA{v:9};", "FA;", @"^FA(?<v>\d{9});$", "md0", "amber").Hint = "9 chiffres en Hz ; le poste travaille par pas de 10 Hz (le dernier chiffre est arrondi par la liaison).";
            b.Meter("sm", "S", 244, 310, 986, 34, 241, "SM;", @"^SM(?<v>\d+);$", "s", false).Hint = "Octet E7 (bits 0-3 : 0 à 15) converti : 0 = S0, 120 = S9, 241 = S9+60 dB.";
            b.Meter("po", "PO", 244, 350, 986, 34, 15, "PO;", @"^PO(?<v>\d+);$", "pct", true).Hint = "Octet F7 (bits 0-3 : 0 à 15), relu seulement en émission.";
            b.LedRd("l_busy", "SQUELCH", 244, 400, 140, 30, "BY;", @"^BY(?<v>[01]);$", "green");
            b.Led("l_tx", "TX", 396, 400, 110, 30, "ptt", "1", "red");
            b.LedRd("l_swr", "SWR ÉLEVÉ", 518, 400, 160, 30, "SW;", @"^SW(?<v>[01]);$", "red");
            b.Frame("f_vfo", "VFO", 1250, 124, 600, 330); const int rx = 1264; const int bw = 108;
            b.Tog("split", "SPLIT", rx, 152, bw, 44, "SP{v};", "", "").Hint = "Commande 02 / 82 ; l'état n'est pas relu.";
            b.Act("swap", "A ⇄ B", rx + 116, 152, bw, 44, "VS;").Hint = "Commande 81 (bascule A/B) ; le poste ne dit pas quel VFO est actif.";
            b.Tog("clar", "CLAR", rx + 232, 152, bw, 44, "CL{v};", "", "").Hint = "Commande 05 / 85 ; la valeur du décalage n'est pas gérée.";
            b.Frame("f_mode", "BANDE / MODE", 10, 464, 830, 190);
            c = b.Row("band", "BANDE", 24, 490, 802, 52, 1, BandsFor(m.Max, 0), "FA{v};", "", ""); c.Hint = "Chaque touche envoie la fréquence type de la bande au VFO sélectionné ; elle s'allume d'après la fréquence.";
            b.Row("md0", "MODE", 24, 550, 802, 90, 2, Y5.Modes, "MD{v};", "MD;", @"^MD(?<v>[0-9A-F]{2});$").Period = 1500;
            b.Frame("f_dial", "ACCORD", 850, 464, 340, 406); b.Dial("dial", "VFO", 870, 492, 300, 300, "fa", "step");
            c = b.Cho("step", "PAS", 870, 800, 300, 56, "10 Hz=10,100 Hz=100,500 Hz=500,1 kHz=1000,5 kHz=5000,10 kHz=10000,100 kHz=100000", "", "", ""); c.Local = true;
            b.Fin();
            return r;
        }
    }
}
