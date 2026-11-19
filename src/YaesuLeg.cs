using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace HamRadioLink
{
    // Anciens Yaesu à protocole binaire de 5 octets [P4 P3 P2 P1 CMD] avec lecture par « bloc d'état » :
    // FT-100, FT-600, FT-747GX, FT-757GXII, FT-840, FT-890, FT-900, FT-920 ; FT-990, FT-1000, FT-1000D, FT-1000MP et Mark-V ;
    // FT-767GX (échange avec écho et accusé) ; FT-757GX et FT-736R (le poste ne renvoie rien).
    // Les dispositions des blocs viennent de Hamlib : AUCUN manuel CAT n'a été vérifié, rien n'est essayé sur un vrai poste.
    // Le moteur parle son « CAT texte » habituel (FAnnnnnnnnn; MDxx; TX0; TX1; et les lectures FA; MD; TX;) ; YLegLink le traduit.
    public class LM
    {
        public string Key, Name, Modes, Send, Recv; public int Baud = 4800; public long Max = 30000000;
        public int Kind;                      // 0 = bloc d'état · 1 = FT-757GX (écriture seule) · 2 = FT-736R (écriture seule) · 3 = FT-767GX
        public int P1 = 0, Len, FOff, MOff, ModeMask = 0xFF; public char Fmt;   // lecture du bloc : P1 de la commande 10h, longueur, positions, codage de la fréquence
        public int TxMask, FlagP1 = -1, FlagLen, FlagByte;                       // émission : bit dans l'octet 0 du bloc, ou drapeaux FAh
        public bool CatPtt = true; public int ByteDelay, PostDelay = 20;
        public bool TxReadable { get { return Kind == 0 && (TxMask != 0 || FlagP1 >= 0) || Kind == 3; } }
    }

    public static class LegCodec
    {
        public static Dictionary<int, int> Tab(string s)
        {
            Dictionary<int, int> d = new Dictionary<int, int>();
            foreach (string p in s.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) { string[] kv = p.Split('='); d[int.Parse(kv[0], NumberStyles.HexNumber)] = int.Parse(kv[1], NumberStyles.HexNumber); }
            return d;
        }
        static string Bcd4(long v) { string d = v.ToString(CultureInfo.InvariantCulture).PadLeft(8, '0'); if (d.Length > 8) d = d.Substring(d.Length - 8); StringBuilder sb = new StringBuilder(); for (int i = 0; i < 8; i += 2) sb.Append((char)(((d[i] - '0') << 4) | (d[i + 1] - '0'))); return sb.ToString(); }
        static string Rev(string s) { char[] c = s.ToCharArray(); Array.Reverse(c); return new string(c); }
        static long BcdVal(string b, int o, bool little) { long v = 0; for (int i = 0; i < 4; i++) { int x = b[little ? o + 3 - i : o + i]; v = v * 100 + ((x >> 4) & 15) * 10 + (x & 15); } return v; }
        static long U32(string b, int o) { return ((long)b[o] << 24) | ((long)b[o + 1] << 16) | ((long)b[o + 2] << 8) | b[o + 3]; }
        static string P32(long v) { return "" + (char)((v >> 24) & 255) + (char)((v >> 16) & 255) + (char)((v >> 8) & 255) + (char)(v & 255); }
        // fréquence lue dans un bloc d'état
        public static long GetFreq(LM m, string b)
        {
            int o = m.FOff;
            switch (m.Fmt)
            {
                case 'B': return BcdVal(b, o, false);
                case 'L': return BcdVal(b, o, true) * 10;
                case 'S': return BcdVal(b, o, false) * 10;
                case '3': return (((long)b[o] << 16) | ((long)b[o + 1] << 8) | b[o + 2]) * 10;
                case '4': return U32(b, o);
                case 'Q': return U32(b, o) * 5 / 4;
                default: return U32(b, o) * 10 / 16;
            }
        }
        // inverse : octets de la fréquence tels qu'ils sont dans le bloc
        public static string PutFreq(LM m, long hz)
        {
            switch (m.Fmt)
            {
                case 'B': return Bcd4(hz);
                case 'L': return Rev(Bcd4(hz / 10));
                case 'S': return Bcd4(hz / 10);
                case '3': { long v = hz / 10; return "" + (char)((v >> 16) & 255) + (char)((v >> 8) & 255) + (char)(v & 255); }
                case '4': return P32(hz);
                case 'Q': return P32(hz * 4 / 5);
                default: return P32(hz * 16 / 10);
            }
        }
        // commande « régler la fréquence » (5 octets)
        public static string FreqCmd(LM m, long hz)
        {
            if (m.Kind == 2) { string b = Bcd4((hz + 5) / 10); if (hz > 1200000000L) b = (char)((b[0] & 0x0F) | 0xC0) + b.Substring(1); return b + (char)0x01; }
            return Rev(Bcd4((hz + 5) / 10)) + (char)(m.Kind == 3 ? 0x08 : 0x0A);
        }
        public static long FromFreqCmd(LM m, string f)
        {
            if (m.Kind == 2) { string b = ((char)(f[0] & 0x0F)).ToString() + f.Substring(1, 3); long v = BcdVal(b, 0, false); long hz = v * 10; if ((f[0] & 0xC0) == 0xC0) hz += 1200000000L; return hz; }
            return BcdVal(f, 0, true) * 10;
        }
        public static string ModeCmd(LM m, int code)
        {
            if (m.Kind == 2) return "" + (char)code + (char)0 + (char)0 + (char)0 + (char)0x07;
            if (m.Kind == 3) return "" + (char)0 + (char)0 + (char)0 + (char)(0x10 + code) + (char)0x0A;
            return "" + (char)0 + (char)0 + (char)0 + (char)code + (char)0x0C;
        }
    }

    // Liaison : CAT texte du moteur ↔ ancien protocole binaire de 5 octets
    public class YLegLink : ILink
    {
        public static bool NoDelay;      // essais : pas de pauses entre octets
        readonly ILink inner; readonly LM m; readonly StringBuilder rx = new StringBuilder(); readonly object lk = new object(), io = new object();
        readonly Dictionary<int, int> send, recv; readonly Stopwatch clock = Stopwatch.StartNew();
        string block; long blockT = -10000; long lastHz; string lastMode = "", lastTx = "0";
        public event Action<string> Data;
        public string Name { get { return inner.Name + " · " + m.Name; } }
        public int Baud { get { return inner.Baud; } }
        public YLegLink(ILink inner, LM m) { this.inner = inner; this.m = m; send = LegCodec.Tab(m.Send); recv = LegCodec.Tab(m.Recv); }
        public bool Open(out string err)
        {
            inner.Data += OnRaw; if (!inner.Open(out err)) { inner.Data -= OnRaw; return false; }
            if (m.Kind == 2) lock (io) Write("\0\0\0\0\0");     // CAT ON du FT-736R
            return true;
        }
        public void Close() { if (m.Kind == 2) { try { lock (io) Write("\u0080\u0080\u0080\u0080\u0080"); } catch { } } inner.Data -= OnRaw; inner.Close(); }
        void OnRaw(string s) { lock (lk) { rx.Append(s); Monitor.PulseAll(lk); } }
        void Pause(int ms) { if (!NoDelay && ms > 0) Thread.Sleep(ms); }
        void Write(string cmd)
        {
            if (m.ByteDelay == 0 || NoDelay) { inner.Send(cmd); return; }
            for (int i = 0; i < cmd.Length; i++) { inner.Send(cmd.Substring(i, 1)); Thread.Sleep(m.ByteDelay); }
        }
        string Take(int len, int min, int ms)
        {
            lock (lk)
            {
                int t0 = Environment.TickCount;
                while (rx.Length < len) { int left = ms - (Environment.TickCount - t0); if (left <= 0) break; Monitor.Wait(lk, left); }
                int n = Math.Min(len, rx.Length); if (n < min) { rx.Length = 0; return null; }
                string r = rx.ToString(0, n); rx.Remove(0, n); return r;
            }
        }
        string Cmd(int p1, int cmd, int len, int min)
        {
            lock (lk) rx.Length = 0;
            Write("" + (char)0 + (char)0 + (char)0 + (char)p1 + (char)cmd);
            if (len == 0) return null;
            return Take(len, min, 400 + len * 3 + (NoDelay ? 0 : 400));
        }
        // FT-767GX : commande, écho, accusé 0B, réponse renvoyée à l'envers
        string Exchange(string cmd, int replyLen)
        {
            lock (lk) rx.Length = 0; Write(cmd);
            string echo = Take(5, 5, 600); if (echo == null || echo != cmd) return null;
            Write("\0\0\0\0\u000B"); string r = Take(replyLen, replyLen, 800); if (r == null) return null;
            char[] c = r.ToCharArray(); Array.Reverse(c); return new string(c);
        }
        string Status767() { if (Exchange("\0\0\0\0\0", 86) == null) return null; return Exchange("\0\0\0\u0001\0", 86); }
        string Blk(bool cached)
        {
            long now = clock.ElapsedMilliseconds; if (cached && block != null && now - blockT < 500) return block;
            if (m.Kind == 3) block = Status767(); else block = Cmd(m.P1, 0x10, m.Len, Math.Max(m.FOff + 4, m.MOff + 1));
            blockT = clock.ElapsedMilliseconds; return block;
        }

        public void Send(string s)
        {
            foreach (string t in s.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string c = t.Trim().ToUpperInvariant(); string o = null; Match mm;
                lock (io)
                {
                    if ((mm = Regex.Match(c, @"^FA(\d{9})$")).Success) SetFreq(long.Parse(mm.Groups[1].Value, CultureInfo.InvariantCulture));
                    else if ((mm = Regex.Match(c, @"^MD([0-9A-F]{2})$")).Success) SetMode(mm.Groups[1].Value);
                    else if (c == "TX1" || c == "TX0") SetPtt(c == "TX1");
                    else if (c == "FA") o = ReadFreq(); else if (c == "MD") o = ReadMode(); else if (c == "TX") o = ReadTx();
                }
                if (o != null && o.Length > 0 && Data != null) Data(o);
            }
        }
        void SetFreq(long hz)
        {
            lastHz = hz / 10 * 10;
            if (m.Kind == 3) { Status767Wrap(LegCodec.FreqCmd(m, hz), 5); return; }
            Write(LegCodec.FreqCmd(m, hz)); block = null; Pause(m.PostDelay);
        }
        void SetMode(string code)
        {
            int e = int.Parse(code, NumberStyles.HexNumber), v; if (!send.TryGetValue(e, out v)) return; lastMode = code;
            if (m.Kind == 3) { Status767Wrap(LegCodec.ModeCmd(m, v), 8); return; }
            Write(LegCodec.ModeCmd(m, v)); block = null; Pause(m.PostDelay);
        }
        void Status767Wrap(string cmd, int reply)
        {
            if (Exchange("\0\0\0\0\0", 86) == null) return; Exchange(cmd, reply); Exchange("\0\0\0\u0001\0", 86); block = null;
        }
        void SetPtt(bool tx)
        {
            lastTx = tx ? "1" : "0"; if (!m.CatPtt) return;
            if (m.Kind == 2) Write("\0\0\0\0" + (char)(tx ? 0x08 : 0x88)); else Write("\0\0\0" + (char)(tx ? 1 : 0) + (char)0x0F);
            Pause(m.PostDelay);
        }
        string ReadFreq()
        {
            long hz;
            if (m.Kind == 1 || m.Kind == 2) hz = lastHz;      // le poste ne répond pas : dernière fréquence envoyée (0 au début)
            else { string b = Blk(true); if (b == null) return null; hz = LegCodec.GetFreq(m, b); if (hz <= 0) return null; }
            return "FA" + hz.ToString(CultureInfo.InvariantCulture).PadLeft(9, '0') + ";";
        }
        string ReadMode()
        {
            if (m.Kind == 1 || m.Kind == 2) return lastMode.Length > 0 ? "MD" + lastMode + ";" : null;
            string b = Blk(true); if (b == null) return null; int v; if (!recv.TryGetValue(b[m.MOff] & m.ModeMask, out v)) return null; return "MD" + v.ToString("X2") + ";";
        }
        string ReadTx()
        {
            if (m.Kind == 1 || m.Kind == 2 || !m.TxReadable) return "TX" + lastTx + ";";
            if (m.TxMask != 0 || m.Kind == 3) { string b = Blk(false); if (b == null) return null; return "TX" + ((b[0] & (m.Kind == 3 ? 1 : m.TxMask)) != 0 ? "1" : "0") + ";"; }
            string f = Cmd(m.FlagP1, 0xFA, m.FlagLen, m.FlagLen); if (f == null) return null; return "TX" + ((f[m.FlagByte] & 0x80) != 0 ? "1" : "0") + ";";
        }
    }

    // Poste simulé sur le fil, d'après le profil : mêmes tables que la liaison, donc cohérent avec elle (mais pas vérifié sur un vrai poste)
    public class LegWireLink : ILink
    {
        readonly LM m; readonly StringBuilder rx = new StringBuilder(); readonly Dictionary<int, int> send, recv; public event Action<string> Data;
        public long Hz = 14074000; public string Mode = "01"; public bool Tx, CatOn; public readonly List<string> Bad = new List<string>(); public int Cmds;
        string pendingCmd; int pendingReply;      // FT-767GX : une commande attend l'accusé
        public string Name { get { return "Ancien Yaesu simulé (fil)"; } }
        public int Baud { get { return 4800; } }
        public LegWireLink(LM m) { this.m = m; send = LegCodec.Tab(m.Send); recv = LegCodec.Tab(m.Recv); }
        public bool Open(out string err) { err = null; return true; }
        public void Close() { }
        string Block()
        {
            char[] b = new char[m.Len]; string f = LegCodec.PutFreq(m, Hz); for (int i = 0; i < f.Length; i++) b[m.FOff + i] = f[i];
            foreach (KeyValuePair<int, int> kv in recv) if (kv.Value == int.Parse(Mode, NumberStyles.HexNumber)) { b[m.MOff] = (char)kv.Key; break; }
            if (m.TxMask != 0 && Tx) b[0] = (char)(b[0] | m.TxMask); if (m.Kind == 3 && Tx) b[0] = (char)(b[0] | 1);
            return new string(b);
        }
        static string Reverse(string s) { char[] c = s.ToCharArray(); Array.Reverse(c); return new string(c); }
        public void Send(string s)
        {
            StringBuilder o = new StringBuilder(); lock (rx)
            {
                rx.Append(s);
                while (rx.Length >= 5)
                {
                    string f = rx.ToString(0, 5); rx.Remove(0, 5); Cmds++; int cmd = f[4];
                    if (m.Kind == 3) { Handshake(f, o); continue; }
                    if (m.Kind == 2)
                    {
                        if (cmd == 0x01) Hz = LegCodec.FromFreqCmd(m, f); else if (cmd == 0x07) { Mode = ModeOf(f[0]); } else if (cmd == 0x08 || cmd == 0x88) Tx = cmd == 0x08; else if (cmd == 0x00 && f == "\0\0\0\0\0") CatOn = true; else if (f == "\u0080\u0080\u0080\u0080\u0080") CatOn = false; else Bad.Add("commande FT-736R inconnue " + cmd.ToString("X2"));
                        continue;
                    }
                    switch (cmd)
                    {
                        case 0x0A: Hz = LegCodec.FromFreqCmd(m, f); break;
                        case 0x0C: Mode = ModeOf(f[3]); break;
                        case 0x0F: if (m.CatPtt && m.Kind != 1) Tx = f[3] == 1; else Bad.Add("PTT non prévu"); break;
                        case 0x10: if (m.Kind == 0 && f[3] == m.P1) o.Append(Block()); else Bad.Add("lecture de bloc non prévue " + (int)f[3]); break;
                        case 0xFA: if (m.Kind == 0 && m.FlagP1 == f[3]) { char[] b = new char[m.FlagLen]; if (Tx) b[m.FlagByte] = '\u0080'; o.Append(new string(b)); } else Bad.Add("drapeaux non prévus"); break;
                        default: Bad.Add("octet de commande inconnu " + cmd.ToString("X2")); break;
                    }
                }
            }
            if (o.Length > 0 && Data != null) Data(o.ToString());
        }
        string ModeOf(int sent) { foreach (KeyValuePair<int, int> kv in send) if (kv.Value == sent) return kv.Key.ToString("X2"); Bad.Add("code de mode inconnu " + sent); return Mode; }
        void Handshake(string f, StringBuilder o)
        {
            if (pendingCmd != null)
            {
                if (f != "\0\0\0\0\u000B") { Bad.Add("accusé attendu"); pendingCmd = null; return; }
                string c = pendingCmd; pendingCmd = null; int cmd = c[4], n = pendingReply;
                string reply = new string('\0', n);
                if (cmd == 0x00) { string blk = Block(); reply = Reverse(blk); }
                else if (cmd == 0x08) Hz = LegCodec.FromFreqCmd(m, c);
                else if (cmd == 0x0A) { int code = c[3] - 0x10; Mode = ModeOf(code); }
                o.Append(cmd == 0x00 ? reply : reply); return;
            }
            o.Append(f); pendingCmd = f; int k = f[4]; pendingReply = k == 0x00 ? 86 : (k == 0x08 ? 5 : 8);
        }
    }

    public static partial class Radios
    {
        static LM L(string key, string name, string modes, string send, string recv) { LM m = new LM(); m.Key = key; m.Name = name; m.Modes = modes; m.Send = send; m.Recv = recv; return m; }
        const string LM5 = "LSB=00,USB=01,CW=02,AM=04,FM=08", LM3 = "LSB=00,USB=01,CW=02,CW-R=03,AM=04,FM=08,DIG=0A,PKT=0C";
        static List<LM> LegModels()
        {
            List<LM> l = new List<LM>(); LM m;
            m = L("ft747gx", "FT-747GX", LM5, "00=00,01=01,02=02,03=02,04=04,08=06,0C=00,0A=01", "01=08,02=04,04=02,08=01,10=00"); m.P1 = 0; m.Len = 344; m.FOff = 1; m.Fmt = 'B'; m.MOff = 0x18; m.ModeMask = 0x1F; m.TxMask = 0x20; m.ByteDelay = 5; l.Add(m);
            m = L("ft757gx2", "FT-757GXII", LM5, "00=00,01=01,02=02,03=02,04=04,08=05,0C=00,0A=01", "00=00,01=01,02=02,03=02,04=04,05=08"); m.P1 = 0; m.Len = 75; m.FOff = 5; m.Fmt = 'L'; m.MOff = 9; m.TxMask = 0x20; m.ByteDelay = 50; m.CatPtt = false; l.Add(m);
            foreach (string n in new string[] { "FT-840", "FT-890", "FT-900" })
            {
                m = L(n.Replace("-", "").ToLowerInvariant(), n, LM5, "00=00,01=01,02=02,03=02,04=04,08=06,0C=00,0A=01", "00=00,01=01,02=02,03=04,04=08"); m.P1 = 2; m.Len = 19; m.FOff = 2; m.Fmt = '3'; m.MOff = 7; m.ModeMask = 7; m.FlagP1 = 0; m.FlagLen = 5; m.FlagByte = 2; m.ByteDelay = n == "FT-900" ? 5 : 50; l.Add(m);
            }
            m = L("ft920", "FT-920", LM3, "00=00,01=01,02=02,03=03,04=04,08=06,0C=08,0A=0A", "00=00,01=03,02=04,03=08,06=08,04=0C,05=0A,40=01,41=02"); m.P1 = 2; m.Len = 28; m.FOff = 1; m.Fmt = '4'; m.MOff = 7; m.ModeMask = 0x47; m.FlagP1 = 0; m.FlagLen = 8; m.FlagByte = 0; m.PostDelay = 80; m.Max = 56000000; l.Add(m);
            m = L("ft100", "FT-100 / FT-100D", LM3, "00=00,01=01,02=02,03=03,04=04,08=06,0C=00,0A=05", "00=00,01=01,02=02,03=03,04=04,05=0A,06=08,07=08"); m.P1 = 0; m.Len = 32; m.FOff = 1; m.Fmt = 'Q'; m.MOff = 5; m.ModeMask = 0x0F; m.FlagP1 = 1; m.FlagLen = 8; m.FlagByte = 0; m.PostDelay = 25; m.Max = 470000000; l.Add(m);
            m = L("ft600", "FT-600", "LSB=00,USB=01,CW=02,AM=04,DIG=0A", "00=00,01=01,02=03,03=03,04=04,0C=00,0A=09", "00=00,01=01,02=02,04=04,05=0A"); m.P1 = 2; m.Len = 19; m.FOff = 1; m.Fmt = '3'; m.MOff = 6; m.ByteDelay = 5; m.PostDelay = 200; l.Add(m);
            string omodes = "LSB=00,USB=01,CW=02,AM=04,FM=08,DIG=0A,PKT=0C", osend = "00=00,01=01,02=02,03=02,04=04,08=06,0C=0A,0A=01", orecv = "00=00,01=01,02=02,03=04,04=08,05=0A,06=0C";
            string[][] old = { new string[] { "ft990", "FT-990" }, new string[] { "ft1000", "FT-1000" }, new string[] { "ft1000d", "FT-1000D" }, new string[] { "ft1000mp", "FT-1000MP" }, new string[] { "ft1000mpv", "FT-1000MP Mark-V" }, new string[] { "ft1000mpvf", "FT-1000MP Mark-V Field" } };
            foreach (string[] o in old)
            {
                bool mp = o[1].Contains("MP"); m = L(o[0], o[1], mp ? omodes.Replace("CW=02,", "CW=02,CW-R=03,") : omodes, mp ? osend.Replace("03=02", "03=03") : osend, orecv);
                m.P1 = 2; m.Len = o[0] == "ft990" ? 32 : 16; m.FOff = 1; m.Fmt = mp ? 'M' : '3'; m.MOff = 7; m.ModeMask = 7; m.ByteDelay = o[0] == "ft990" ? 50 : (mp ? 5 : 0);
                if (!mp) { m.FlagP1 = 0; m.FlagLen = 5; m.FlagByte = 0; }
                if (mp) m.Max = 30000000; l.Add(m);
            }
            m = L("ft767gx", "FT-767GX", LM5, "00=00,01=01,02=02,03=02,04=03,08=04,0C=00,0A=01", "00=00,01=01,02=02,03=04,04=08,05=0A"); m.Kind = 3; m.Len = 86; m.FOff = 1; m.Fmt = 'S'; m.MOff = 6; m.ModeMask = 7; m.TxMask = 1; m.CatPtt = false; m.Max = 470000000; l.Add(m);
            m = L("ft757gx", "FT-757GX", LM5, "00=00,01=01,02=02,03=02,04=04,08=05,0C=00,0A=01", "00=00"); m.Kind = 1; m.ByteDelay = 50; m.CatPtt = false; l.Add(m);
            m = L("ft736r", "FT-736R", "LSB=00,USB=01,CW=02,FM=08", "00=00,01=01,02=02,08=08", "00=00"); m.Kind = 2; m.ByteDelay = 30; m.Max = 470000000; l.Add(m);
            return l;
        }
        public static LM LegProfile(string key) { foreach (LM m in LegModels()) if (m.Key == key) return m; return null; }
        public static RadioDef LegByKey(string key) { LM m = LegProfile(key); return m == null ? null : LegDef(m); }
        static List<RadioDef> LegAll() { List<RadioDef> l = new List<RadioDef>(); foreach (LM m in LegModels()) l.Add(LegDef(m)); return l; }

        static RadioDef LegDef(LM m)
        {
            RadioDef r = new RadioDef(); r.Key = m.Key; r.Name = "Yaesu " + m.Name; r.Leg = m; r.Stop2 = true; r.DefBaud = m.Baud; r.FreqDigits = 9; r.MaxFreq = m.Max; r.IdCmd = "FA;"; r.IdAnswer = ""; r.Init = "";
            r.Notes.Add("ancien protocole binaire (" + (m.Kind == 1 || m.Kind == 2 ? "le poste ne renvoie rien : HamRadioLink affiche la dernière valeur envoyée" : "lecture par bloc d'état") + ") traduit par la liaison ; d'après Hamlib, AUCUN manuel CAT vérifié : fréquence et mode" + (m.CatPtt ? ", PTT" : "") + " seulement ; à contrôler sur le poste, tout se corrige par clic droit");
            Bld b = new Bld(r); Ctl c;
            b.Frame("f_pwr", "ÉMISSION", 10, 124, 210, 330);
            if (m.CatPtt) { c = b.Tog("ptt", "ÉMISSION (PTT)", 24, 152, 182, 60, "TX{v};", m.TxReadable ? "TX;" : "", m.TxReadable ? @"^TX(?<v>[01]);$" : "", "1", "0"); c.Accent = "red"; if (m.TxReadable) c.Period = 400; }
            b.Frame("f_scr", "ÉCRAN", 230, 124, 1010, 330);
            b.Freq("fa", "VFO", 244, 150, 986, 150, "FA{v:9};", "FA;", @"^FA(?<v>\d{9});$", "md0", "amber").Hint = "9 chiffres en Hz ; la liaison arrondit à la résolution du poste.";
            if (m.CatPtt) b.Led("l_tx", "TX", 244, 400, 110, 30, "ptt", "1", "red");
            b.Frame("f_mode", "BANDE / MODE", 10, 464, 830, 190);
            c = b.Row("band", "BANDE", 24, 490, 802, 52, 1, m.Kind == 2 ? "144=144300000,430=433000000" : BandsFor(m.Max, 0), "FA{v};", "", ""); c.Hint = "Chaque touche envoie la fréquence type de la bande ; elle s'allume d'après la fréquence.";
            b.Row("md0", "MODE", 24, 550, 802, 90, 2, m.Modes, "MD{v};", "MD;", @"^MD(?<v>[0-9A-F]{2});$").Period = 1500;
            b.Frame("f_dial", "ACCORD", 850, 464, 340, 406); b.Dial("dial", "VFO", 870, 492, 300, 300, "fa", "step");
            c = b.Cho("step", "PAS", 870, 800, 300, 56, "10 Hz=10,100 Hz=100,500 Hz=500,1 kHz=1000,5 kHz=5000,10 kHz=10000,100 kHz=100000", "", "", ""); c.Local = true;
            b.Fin();
            return r;
        }
    }
}
