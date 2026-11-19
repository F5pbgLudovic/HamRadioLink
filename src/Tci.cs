using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace HamRadioLink
{
    // TCI (Expert Electronics : ExpertSDR3 / SunSDR ; Thetis pour les ANAN et Apache) : commandes texte « nom:arguments; » dans une connexion WebSocket.
    // Le moteur parle le même texte ; TciLink n'est qu'un transport WebSocket, avec deux traductions :
    //   • S-mètre : le serveur donne des dBm (rx_smeter:0,0,-73;) → échelle du programme 0-241 (S9 = 120) ;
    //   • puissance : « drive:P; » devient « drive:0,P; » si le serveur annonce cette forme (ExpertSDR3), sinon reste « drive:P; ».
    public static class Tci
    {
        public static int Scale(int dbm) { int s = dbm <= -73 ? (int)Math.Round((dbm + 127) * 120.0 / 54) : (int)Math.Round(120 + (dbm + 73) * 121.0 / 60); return Math.Max(0, Math.Min(241, s)); }
        public static int Dbm(int scale) { return scale <= 120 ? (int)Math.Round(scale * 54.0 / 120 - 127) : (int)Math.Round((scale - 120) * 60.0 / 121 - 73); }
        public const string Modes = "AM=am,SAM=sam,DSB=dsb,LSB=lsb,USB=usb,CW=cw,NFM=nfm,WFM=wfm,DIGL=digl,DIGU=digu";
    }

    public class TciLink : ILink
    {
        readonly string host; readonly int port; ClientWebSocket ws; volatile bool run; volatile int driveArgs = 1; readonly object sendLock = new object();
        public event Action<string> Data;
        public string Name { get { return "TCI " + host + ":" + port; } }
        public int Baud { get { return 115200; } }
        public TciLink(string host, int port) { this.host = host; this.port = port; }
        public bool Open(out string err)
        {
            err = null;
            try
            {
                ws = new ClientWebSocket(); ws.Options.Proxy = null; Uri uri = new Uri("ws://" + host + ":" + port + "/"); ClientWebSocket w = ws;
                if (!System.Threading.Tasks.Task.Run(delegate { return w.ConnectAsync(uri, CancellationToken.None); }).Wait(4000) || ws.State != WebSocketState.Open)
                { err = "pas de serveur TCI sur " + host + ":" + port + " (ExpertSDR3 : activez le serveur TCI ; Thetis : TCI Server) "; try { ws.Dispose(); } catch { } ws = null; return false; }
                run = true; Thread t = new Thread(Receive); t.IsBackground = true; t.Start(); return true;
            }
            catch (Exception ex) { err = ex.InnerException != null ? ex.InnerException.Message : ex.Message; return false; }
        }
        public void Close() { run = false; try { ClientWebSocket w = ws; if (w != null) w.Abort(); } catch { } }
        public void Send(string s)
        {
            ClientWebSocket w = ws; if (w == null || w.State != WebSocketState.Open) return;
            foreach (string t in s.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string c = t.Trim(); if (c.Length == 0) continue;
                if (driveArgs == 2 && Regex.IsMatch(c, @"^drive:\d+$")) c = "drive:0," + c.Substring(6);
                byte[] b = Encoding.UTF8.GetBytes(c + ";");
                try { lock (sendLock) System.Threading.Tasks.Task.Run(delegate { return w.SendAsync(new ArraySegment<byte>(b), WebSocketMessageType.Text, true, CancellationToken.None); }).Wait(2000); } catch { }
            }
        }
        void Receive()
        {
            byte[] buf = new byte[65536]; StringBuilder msg = new StringBuilder(); ClientWebSocket w = ws;
            try
            {
                while (run && w.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult r = w.ReceiveAsync(new ArraySegment<byte>(buf), CancellationToken.None).Result;
                    if (r.MessageType == WebSocketMessageType.Close) break; if (r.MessageType != WebSocketMessageType.Text) continue;
                    msg.Append(Encoding.UTF8.GetString(buf, 0, r.Count));
                    if (r.EndOfMessage) { string o = Translate(msg.ToString()); msg.Length = 0; if (o.Length > 0 && Data != null) Data(o); }
                }
            }
            catch { }
            run = false;
        }
        string Translate(string s)
        {
            StringBuilder o = new StringBuilder();
            foreach (string t in s.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string c = t.Trim(); Match m;
                if ((m = Regex.Match(c, @"^rx_smeter:(\d+),(\d+),(-?\d+)(?:\.\d+)?$")).Success) c = "rx_smeter:" + m.Groups[1].Value + "," + m.Groups[2].Value + "," + Tci.Scale(int.Parse(m.Groups[3].Value));
                else if (Regex.IsMatch(c, @"^drive:\d+,\d+$")) driveArgs = 2; else if (Regex.IsMatch(c, @"^drive:\d+$")) driveArgs = 1;
                o.Append(c).Append(';');
            }
            return o.ToString();
        }
    }

    // Serveur TCI simulé strict : état d'un poste TCI, formes de commandes de l'ExpertSDR3 (wire = true : dBm, drive:0,P) ou du moteur (wire = false)
    public class TciSim
    {
        public readonly Dictionary<string, string> S = new Dictionary<string, string>(); public readonly List<string> Bad = new List<string>(), Seen; readonly bool wire; public int DriveArgs = 2; public bool Echo; public int Count;
        public TciSim(bool wire, List<string> bad, List<string> seen, Dictionary<string, string> s)
        {
            this.wire = wire; Bad = bad ?? Bad; Seen = seen ?? new List<string>(); if (s != null) S = s;
            S["fa"] = "14074000"; S["fb"] = "7100000"; S["md"] = "usb"; S["tx"] = "0"; S["tune"] = "false"; S["drive"] = "50"; S["vol"] = "-20"; S["mute"] = "false"; S["split"] = "false"; S["rit"] = "false"; S["ritoff"] = "0"; S["sm"] = "-73";
        }
        public string Init() { return "protocol:ExpertSDR3,1.5;device:SunSDR2DX;trx_count:2;" + (DriveArgs == 2 ? "drive:0,50;" : "drive:50;") + "volume:-20;ready;"; }
        static bool B(string s) { return s == "true" || s == "false"; }
        string Fail(string c) { Bad.Add(c + ";"); return ""; }
        string Note(string n) { return Echo ? n : ""; }
        public string Input(string text)
        {
            StringBuilder o = new StringBuilder();
            lock (S)
                foreach (string raw in text.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)) { string c = raw.Trim(); if (c.Length == 0) continue; Count++; if (Seen.Count < 4000) Seen.Add(c + ";"); o.Append(One(c)); }
            return o.ToString();
        }
        string One(string c)
        {
            Match m;
            if ((m = Regex.Match(c, @"^vfo:0,([01])$")).Success) return "vfo:0," + m.Groups[1].Value + "," + S[m.Groups[1].Value == "0" ? "fa" : "fb"] + ";";
            if ((m = Regex.Match(c, @"^vfo:0,([01]),(\d{1,9})$")).Success) { long f = long.Parse(m.Groups[2].Value); if (f < 1000 || f > 999999999) return Fail(c); S[m.Groups[1].Value == "0" ? "fa" : "fb"] = f.ToString(CultureInfo.InvariantCulture); return Note(c + ";"); }
            if (c == "modulation:0") return "modulation:0," + S["md"] + ";";
            if ((m = Regex.Match(c, @"^modulation:0,([a-z]+)$")).Success) { if (!Regex.IsMatch(m.Groups[1].Value, "^(am|sam|dsb|lsb|usb|cw|nfm|wfm|digl|digu)$")) return Fail(c); S["md"] = m.Groups[1].Value; return Note(c + ";"); }
            if (c == "trx:0") return "trx:0," + (S["tx"] == "1" ? "true" : "false") + ";";
            if ((m = Regex.Match(c, @"^trx:0,(true|false)$")).Success) { S["tx"] = m.Groups[1].Value == "true" ? "1" : "0"; return Note(c + ";"); }
            if (c == "tune:0") return "tune:0," + S["tune"] + ";";
            if ((m = Regex.Match(c, @"^tune:0,(true|false)$")).Success) { S["tune"] = m.Groups[1].Value; return Note(c + ";"); }
            if (c == "drive") return (DriveArgs == 2 && wire ? "drive:0," : "drive:") + S["drive"] + ";";
            if ((m = Regex.Match(c, wire && DriveArgs == 2 ? @"^drive:0,(\d{1,3})$" : @"^drive:(\d{1,3})$")).Success) { int v = int.Parse(m.Groups[1].Value); if (v > 100) return Fail(c); S["drive"] = v.ToString(); return Note(c + ";"); }
            if (c == "volume") return "volume:" + S["vol"] + ";";
            if ((m = Regex.Match(c, @"^volume:(-?\d{1,2})$")).Success) { int v = int.Parse(m.Groups[1].Value); if (v < -60 || v > 0) return Fail(c); S["vol"] = v.ToString(); return Note(c + ";"); }
            if (c == "mute") return "mute:" + S["mute"] + ";";
            if ((m = Regex.Match(c, @"^mute:(true|false)$")).Success) { S["mute"] = m.Groups[1].Value; return Note(c + ";"); }
            if (c == "split_enable:0") return "split_enable:0," + S["split"] + ";";
            if ((m = Regex.Match(c, @"^split_enable:0,(true|false)$")).Success) { S["split"] = m.Groups[1].Value; return Note(c + ";"); }
            if (c == "rit_enable:0") return "rit_enable:0," + S["rit"] + ";";
            if ((m = Regex.Match(c, @"^rit_enable:0,(true|false)$")).Success) { S["rit"] = m.Groups[1].Value; return Note(c + ";"); }
            if (c == "rit_offset:0") return "rit_offset:0," + S["ritoff"] + ";";
            if ((m = Regex.Match(c, @"^rit_offset:0,(-?\d{1,4})$")).Success) { int v = int.Parse(m.Groups[1].Value); if (v < -9999 || v > 9999) return Fail(c); S["ritoff"] = v.ToString(); return Note(c + ";"); }
            if (c == "rx_smeter:0,0") { int dbm = int.Parse(S["sm"]); return "rx_smeter:0,0," + (wire ? dbm : Tci.Scale(dbm)) + ";"; }
            return Fail(c);
        }
    }

    // Serveur TCI minimal (WebSocket écrit à la main) autour d'un TciSim : poste TCI virtuel du programme et essais
    public class TciServer : IDisposable
    {
        public readonly TciSim Sim; readonly TcpListener ls; volatile bool run = true; readonly List<NetworkStream> cl = new List<NetworkStream>(); public int Port, Clients;
        public TciServer(int driveArgs)
        {
            Sim = new TciSim(true, null, null, null); Sim.DriveArgs = driveArgs; Sim.Echo = true; ls = new TcpListener(IPAddress.Loopback, 0); ls.Start(); Port = ((IPEndPoint)ls.LocalEndpoint).Port;
            Thread t = new Thread(Accept); t.IsBackground = true; t.Start();
        }
        void Accept() { while (run) { TcpClient c; try { c = ls.AcceptTcpClient(); } catch { break; } Thread t = new Thread(delegate() { Serve(c); }); t.IsBackground = true; t.Start(); } }
        static byte[] Frame(string s, int op)
        {
            byte[] p = Encoding.UTF8.GetBytes(s); List<byte> f = new List<byte>(); f.Add((byte)(0x80 | op));
            if (p.Length < 126) f.Add((byte)p.Length); else { f.Add(126); f.Add((byte)(p.Length >> 8)); f.Add((byte)(p.Length & 255)); }
            f.AddRange(p); return f.ToArray();
        }
        static bool Exact(Stream s, byte[] b, int n) { int g = 0; while (g < n) { int r = s.Read(b, g, n - g); if (r <= 0) return false; g += r; } return true; }
        public void Push(string text) { byte[] f = Frame(text, 1); List<NetworkStream> l; lock (cl) l = new List<NetworkStream>(cl); foreach (NetworkStream n in l) { try { lock (n) n.Write(f, 0, f.Length); } catch { } } }
        void Serve(TcpClient c)
        {
            NetworkStream ns = c.GetStream();
            try
            {
                StringBuilder h = new StringBuilder(); byte[] one = new byte[1]; while (!h.ToString().EndsWith("\r\n\r\n")) { if (ns.Read(one, 0, 1) <= 0) return; h.Append((char)one[0]); if (h.Length > 8000) return; }
                Match m = Regex.Match(h.ToString(), @"Sec-WebSocket-Key:\s*(\S+)", RegexOptions.IgnoreCase); if (!m.Success) return;
                string acc; using (SHA1 sha = SHA1.Create()) acc = Convert.ToBase64String(sha.ComputeHash(Encoding.ASCII.GetBytes(m.Groups[1].Value + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));
                byte[] resp = Encoding.ASCII.GetBytes("HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Accept: " + acc + "\r\n\r\n"); ns.Write(resp, 0, resp.Length);
                lock (cl) cl.Add(ns); Clients++; Push2(ns, Sim.Init());
                byte[] hd = new byte[8];
                while (run)
                {
                    if (!Exact(ns, hd, 2)) break; int op = hd[0] & 15; bool mask = (hd[1] & 0x80) != 0; long len = hd[1] & 0x7F;
                    if (len == 126) { if (!Exact(ns, hd, 2)) break; len = (hd[0] << 8) | hd[1]; } else if (len == 127) { if (!Exact(ns, hd, 8)) break; len = 0; for (int i = 0; i < 8; i++) len = (len << 8) | hd[i]; }
                    byte[] mk = new byte[4]; if (mask && !Exact(ns, mk, 4)) break; byte[] pl = new byte[len]; if (len > 0 && !Exact(ns, pl, (int)len)) break; if (mask) for (int i = 0; i < pl.Length; i++) pl[i] ^= mk[i & 3];
                    if (op == 8) break; if (op == 9) { byte[] pong = Frame(Encoding.UTF8.GetString(pl), 10); lock (ns) ns.Write(pong, 0, pong.Length); continue; } if (op != 1) continue;
                    string r = Sim.Input(Encoding.UTF8.GetString(pl)); if (r.Length > 0) Push(r);
                }
            }
            catch { }
            finally { lock (cl) cl.Remove(ns); Clients--; try { c.Close(); } catch { } }
        }
        void Push2(NetworkStream n, string s) { byte[] f = Frame(s, 1); lock (n) n.Write(f, 0, f.Length); }
        public void Dispose() { run = false; try { ls.Stop(); } catch { } lock (cl) foreach (NetworkStream n in cl) try { n.Close(); } catch { } }
    }

    // Poste TCI virtuel : serveur TCI simulé du programme + liaison TCI réelle (WebSocket) sur la boucle locale
    public class TciVirtualLink : ILink
    {
        TciServer srv; TciLink lk; public event Action<string> Data;
        public string Name { get { return "Poste TCI virtuel (simulateur)"; } }
        public int Baud { get { return 115200; } }
        public bool Open(out string err)
        {
            srv = new TciServer(2); lk = new TciLink("127.0.0.1", srv.Port); lk.Data += delegate(string s) { if (Data != null) Data(s); };
            if (!lk.Open(out err)) { srv.Dispose(); return false; } return true;
        }
        public void Close() { if (lk != null) lk.Close(); if (srv != null) srv.Dispose(); }
        public void Send(string s) { if (lk != null) lk.Send(s); }
    }

    public static partial class Radios
    {
        static RadioDef TciDef()
        {
            RadioDef r = new RadioDef(); r.Key = "tci"; r.Name = "Poste commandé en TCI (ExpertSDR3, Thetis…)"; r.Tci = true; r.DefBaud = 115200; r.FreqDigits = 9; r.MaxFreq = 60000000; r.IdCmd = "vfo:0,0;"; r.IdAnswer = ""; r.Init = "";
            r.Notes.Add("commandé en TCI (WebSocket, port 40001 par défaut) : ExpertSDR3 / SunSDR, Thetis (ANAN, Apache) et autres logiciels SDR compatibles ; récepteur 0, VFO A et B ; essayé seulement contre un serveur TCI simulé (aucun vrai serveur) ; Flex : utiliser SmartCAT (Kenwood TS-2000) par un port COM virtuel ; tout se corrige par clic droit");
            Bld b = new Bld(r); Ctl c; const string tf = @"(?<v>true|false)";
            b.Frame("f_pwr", "ÉMISSION", 10, 124, 210, 330);
            c = b.Tog("ptt", "ÉMISSION (PTT)", 24, 152, 182, 60, "trx:0,{v};", "trx:0;", @"^trx:0," + tf + @"(?:,\w+)?;$", "true", "false"); c.Accent = "red"; c.Period = 250;
            b.Tog("tune", "TUNE", 24, 226, 182, 50, "tune:0,{v};", "tune:0;", @"^tune:0," + tf + ";$", "true", "false");
            b.Tog("mute", "MUTE", 24, 290, 182, 50, "mute:{v};", "mute;", @"^mute:" + tf + ";$", "true", "false");
            b.Tog("split", "SPLIT", 24, 354, 182, 50, "split_enable:0,{v};", "split_enable:0;", @"^split_enable:0," + tf + ";$", "true", "false");
            b.Frame("f_scr", "ÉCRAN", 230, 124, 1010, 330);
            b.Freq("fa", "VFO A", 244, 146, 986, 130, "vfo:0,0,{v};", "vfo:0,0;", @"^vfo:0,0,(?<v>\d+);$", "md0", "amber").Hint = "Fréquence du récepteur 0, VFO A (Hz).";
            b.Freq("fb", "VFO B", 244, 282, 986, 90, "vfo:0,1,{v};", "vfo:0,1;", @"^vfo:0,1,(?<v>\d+);$", "md0", "cyan").Hint = "Fréquence du récepteur 0, VFO B (Hz).";
            b.Meter("sm", "S", 244, 380, 986, 34, 241, "rx_smeter:0,0;", @"^rx_smeter:0,0,(?<v>\d+);$", "s", false).Hint = "rx_smeter en dBm converti par la liaison : 0 = S0, 120 = S9, 241 = S9+60 dB.";
            b.Frame("f_lvl", "NIVEAUX", 1250, 124, 600, 330); const int rx = 1264;
            c = b.Knob("drive", "DRIVE", rx, 150, 120, 130, 0, 100, "drive:{v};", "drive;", @"^drive:(?:\d+,)?(?<v>\d+);$", "pct"); c.Accent = "red"; c.Hint = "Puissance d'émission en % ; la liaison envoie drive:0,P; ou drive:P; selon l'annonce du serveur.";
            c = b.Knob("vol", "VOLUME", rx + 140, 150, 120, 130, -60, 0, "volume:{v};", "volume;", @"^volume:(?<v>-?\d+);$", "sgn"); c.Accent = "green"; c.Hint = "Volume principal en dB (-60 à 0).";
            b.Tog("rit", "RIT", rx + 290, 150, 120, 44, "rit_enable:0,{v};", "rit_enable:0;", @"^rit_enable:0," + tf + ";$", "true", "false");
            c = b.Knob("ritoff", "RIT Hz", rx + 440, 150, 120, 130, -9999, 9999, "rit_offset:0,{v};", "rit_offset:0;", @"^rit_offset:0,(?<v>-?\d+);$", "sgn"); c.Step = 10;
            b.Frame("f_mode", "BANDE / MODE", 10, 464, 830, 190);
            c = b.Row("band", "BANDE", 24, 490, 802, 52, 1, BandsFor(60000000, 0), "vfo:0,0,{v};", "", ""); c.Hint = "Chaque touche envoie la fréquence type de la bande au VFO A ; elle s'allume d'après la fréquence.";
            b.Row("md0", "MODE", 24, 550, 802, 90, 2, Tci.Modes, "modulation:0,{v};", "modulation:0;", @"^modulation:0,(?<v>[a-z]+);$").Period = 1500;
            b.Frame("f_dial", "ACCORD", 850, 464, 340, 406); b.Dial("dial", "VFO", 870, 492, 300, 300, "fa", "step");
            c = b.Cho("step", "PAS", 870, 800, 300, 56, "10 Hz=10,100 Hz=100,500 Hz=500,1 kHz=1000,5 kHz=5000,10 kHz=10000,100 kHz=100000", "", "", ""); c.Local = true;
            b.Fin();
            return r;
        }
    }
}
