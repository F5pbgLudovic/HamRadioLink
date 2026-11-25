using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;

namespace HamRadioLink
{
    // Partage du poste : HamRadioLink garde la liaison complète avec le poste (port COM, TCP) et ouvre à côté
    //   • un port COM virtuel (une extrémité d'une paire com0com / VSPE ; l'autre extrémité va à OmniRig, un logiciel de log, WSJT-X…)
    //   • et / ou un serveur TCP.
    // Les octets sont transmis tels quels (aucune perte, aucune limite de commandes) ; la liaison ne fait que répartir les réponses.
    public enum Framing { Text, Civ, Fixed5 }

    public class ShareOpts
    {
        public string Com = "", Owner = ""; public int Baud = 9600, Stop = 1; public int TcpPort; public bool Net; public Framing Fr;
        public bool Any { get { return Com.Length > 0 || TcpPort != 0; } }
        // longueurs des commandes de lecture connues, par préfixe de 2 lettres : sert à distinguer « FA; » (question) de « FA007074000; » (réglage sans réponse)
        public Dictionary<string, HashSet<int>> Reads = new Dictionary<string, HashSet<int>>();
        public void LearnReads(RadioDef d)
        {
            foreach (Ctl c in d.C)
                foreach (string r in new string[] { c.Read, c.DRead })
                    foreach (string tok in (r ?? "").Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string x = tok.Trim().ToUpperInvariant(); if (x.Length < 2 || x.IndexOf('{') >= 0) continue; HashSet<int> h; if (!Reads.TryGetValue(x.Substring(0, 2), out h)) { h = new HashSet<int>(); Reads[x.Substring(0, 2)] = h; } h.Add(x.Length + 1);
                    }
        }
        public bool IsRead(string u)
        {
            if (u.Length < 3) return false; HashSet<int> h; if (Reads.TryGetValue(u.Substring(0, 2).ToUpperInvariant(), out h)) return h.Contains(u.Length); return u.Length <= 4;
        }
        // le partage se règle POSTE PAR POSTE : clés share.<poste>.on / comOn / com / baud / stop / tcpOn / tcpPort / net
        public static string K(string radio, string name) { return "share." + radio + "." + name; }
        public static bool IsOn(string radio) { return Cfg.GetBool(K(radio, "on"), false); }
        public static ShareOpts Load(Framing fr, string radio)
        {
            ShareOpts o = new ShareOpts(); o.Fr = fr; if (!IsOn(radio)) return o;
            if (Cfg.GetBool(K(radio, "comOn"), false)) o.Com = Cfg.Get(K(radio, "com"), "");
            o.Baud = Cfg.GetInt(K(radio, "baud"), 9600); o.Stop = Cfg.GetInt(K(radio, "stop"), 1);
            if (Cfg.GetBool(K(radio, "tcpOn"), false)) o.TcpPort = Cfg.GetInt(K(radio, "tcpPort"), 4540);
            o.Net = Cfg.GetBool(K(radio, "net"), false); return o;
        }
        // une seule fois : le partage d'avant (valable pour tous) est attribué au poste qui était choisi
        public static void MigrateGlobal(string radio)
        {
            if (Cfg.GetBool("share.mig", false)) return; Cfg.SetBool("share.mig", true); if (string.IsNullOrEmpty(radio)) return;
            foreach (string n in new string[] { "on", "comOn", "com", "baud", "stop", "tcpOn", "tcpPort", "net" }) { string g = Cfg.Get("share." + n, null); if (g != null) Cfg.Set(K(radio, n), g); }
            Cfg.Save();
        }
    }

    public class ShareLink : ILink
    {
        class Conn { public NetworkStream Ns; public StringBuilder B = new StringBuilder(); }
        class Req { public int Who; public long T; public int Len; }
        const int Window = 900;        // ms : une question restée sans réponse plus longtemps est oubliée
        readonly ILink inner; readonly ShareOpts o; readonly object lk = new object();
        readonly StringBuilder rxIn = new StringBuilder(), extSerial = new StringBuilder();
        readonly Dictionary<string, Queue<Req>> pend = new Dictionary<string, Queue<Req>>();     // questions posées au poste, dans l'ordre, par commande
        readonly Queue<Req> q = new Queue<Req>(); readonly Stopwatch clock = Stopwatch.StartNew();
        readonly List<Conn> conns = new List<Conn>(); SerialPort sp; TcpListener ls; volatile bool run; int lastWho = 1; bool listenAll;
        // ports COM virtuels que HamRadioLink lui-même tient ouverts pour un partage : port -> liaison du poste
        public static readonly Dictionary<string, string> Held = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public static readonly HashSet<int> HeldTcp = new HashSet<int>();     // ports TCP de partage ouverts par HamRadioLink
        // écriture vers le port COM virtuel dans un fil à part : si l'autre logiciel ne lit plus, la lecture du poste n'est jamais bloquée
        readonly Queue<string> outQ = new Queue<string>(); readonly AutoResetEvent outEv = new AutoResetEvent(false); int dropped;
        void Writer()
        {
            while (run)
            {
                outEv.WaitOne(500); if (!run) break;
                while (run)
                {
                    string s; lock (outQ) { if (outQ.Count == 0) break; s = outQ.Dequeue(); }
                    try { SerialPort p = sp; if (p != null && p.IsOpen) p.Write(s); }
                    catch (TimeoutException) { try { sp.DiscardOutBuffer(); } catch { } lock (outQ) { dropped += outQ.Count + 1; outQ.Clear(); } CatLog.Event("partage", "le logiciel sur " + o.Com + " ne lit plus : données abandonnées (" + dropped + ")"); }
                    catch { }
                }
            }
        }
        public int ExtCommands, LocCommands;
        public int Clients { get { lock (conns) return conns.Count; } }
        public int TcpActual { get { return ls != null ? ((IPEndPoint)ls.LocalEndpoint).Port : 0; } }
        public event Action<string> Data;
        public string Name { get { return inner.Name + " · partagé" + (o.Com.Length > 0 ? " " + o.Com : "") + (o.TcpPort > 0 ? " TCP " + TcpActual : ""); } }
        public int Baud { get { return inner.Baud; } }
        public ShareLink(ILink inner, ShareOpts o) { this.inner = inner; this.o = o; }

        public bool Open(out string err)
        {
            inner.Data += OnRaw; if (!inner.Open(out err)) { inner.Data -= OnRaw; return false; }
            try
            {
                run = true;
                if (o.Com.Length > 0)
                {
                    sp = new SerialPort(o.Com, o.Baud, Parity.None, 8, o.Stop == 2 ? StopBits.Two : StopBits.One); sp.Handshake = Handshake.None; sp.Encoding = Civ.L1; sp.WriteTimeout = 500;
                    sp.DataReceived += delegate { try { string t = sp.ReadExisting(); if (t.Length > 0) FromExt(extSerial, t); } catch { } };
                    sp.Open(); Thread wt = new Thread(Writer); wt.IsBackground = true; wt.Start(); sp.RtsEnable = true; sp.DtrEnable = true; lock (Held) Held[o.Com] = !string.IsNullOrEmpty(o.Owner) ? o.Owner : inner.Name; CatLog.Event("partage", "port COM virtuel " + o.Com + " ouvert pour " + (o.Owner.Length > 0 ? o.Owner : inner.Name));
                }
                if (o.TcpPort > 0 || (o.TcpPort < 0))
                {
                    ls = new TcpListener(o.Net ? IPAddress.Any : IPAddress.Loopback, Math.Max(0, o.TcpPort)); ls.Start(); lock (HeldTcp) HeldTcp.Add(TcpActual);
                    Thread t = new Thread(Accept); t.IsBackground = true; t.Start();
                }
                return true;
            }
            catch (Exception ex) { err = "partage : " + ex.Message; Close(); return false; }
        }
        public void Close()
        {
            run = false; outEv.Set(); inner.Data -= OnRaw; inner.Close();
            try { if (sp != null) { sp.Close(); sp.Dispose(); } } catch { } sp = null; if (o.Com != null && o.Com.Length > 0) lock (Held) Held.Remove(o.Com);
            try { if (ls != null) { lock (HeldTcp) HeldTcp.Remove(TcpActual); ls.Stop(); } } catch { } ls = null;
            lock (conns) { foreach (Conn c in conns) try { c.Ns.Close(); } catch { } conns.Clear(); }
        }

        void Accept()
        {
            TcpListener l = ls;
            while (run && l != null)
            {
                TcpClient c; try { c = l.AcceptTcpClient(); } catch { break; }
                c.NoDelay = true; Conn cn = new Conn(); cn.Ns = c.GetStream(); lock (conns) conns.Add(cn);
                Thread t = new Thread(delegate()
                {
                    byte[] buf = new byte[512];
                    while (run) { try { int n = cn.Ns.Read(buf, 0, buf.Length); if (n <= 0) break; FromExt(cn.B, Civ.L1.GetString(buf, 0, n)); } catch { break; } }
                    lock (conns) conns.Remove(cn); try { cn.Ns.Close(); } catch { }
                });
                t.IsBackground = true; t.Start();
            }
        }

        // ------------------------------------------------------------ découpage en commandes complètes
        static List<string> Extract(Framing f, StringBuilder b, string add)
        {
            List<string> l = new List<string>(); b.Append(add);
            while (true)
            {
                string t = b.ToString();
                if (f == Framing.Text)
                {
                    int i = t.IndexOf(';'); if (i < 0) { if (t.Length > 300) b.Length = 0; break; }
                    string u = t.Substring(0, i + 1).TrimStart('\r', '\n', ' '); b.Remove(0, i + 1); if (u.Length > 1) l.Add(u);
                }
                else if (f == Framing.Civ)
                {
                    int i = t.IndexOf("þþ", StringComparison.Ordinal); if (i < 0) { b.Length = 0; if (t.EndsWith("þ")) b.Append('þ'); break; }
                    while (i + 2 < t.Length && t[i + 2] == 'þ') i++;
                    int e = t.IndexOf('ý', i + 2); if (e < 0) { b.Length = 0; if (t.Length - i < 600) b.Append(t.Substring(i)); break; }
                    b.Length = 0; b.Append(t.Substring(e + 1)); if (e - i >= 5) l.Add(t.Substring(i, e - i + 1));
                }
                else { if (t.Length < 5) break; l.Add(t.Substring(0, 5)); b.Remove(0, 5); }
            }
            return l;
        }
        static int ReplyLen(char cmd) { switch ((int)cmd) { case 0x03: return 5; case 0xE7: case 0xF7: return 1; case 0xBB: return 2; } return 0; }
        string Key(string u) { return o.Fr == Framing.Text ? (u.Length >= 2 ? u.Substring(0, 2).ToUpperInvariant() : u) : "*"; }
        // enregistre qui pose la question (dans l'ordre exact où elle part vers le poste)
        void Note(int who, string u)
        {
            lock (lk)
            {
                long now = clock.ElapsedMilliseconds; lastWho = who; if (who == 2) ExtCommands++; else LocCommands++;
                if (o.Fr == Framing.Fixed5) { while (q.Count > 0 && now - q.Peek().T > 1800) { q.Dequeue(); rxIn.Length = 0; } int n = ReplyLen(u[4]); if (n > 0) { Req r = new Req(); r.Who = who; r.Len = n; r.T = now; q.Enqueue(r); } return; }
                if (o.Fr == Framing.Text) { if (who != 2) return; if (u.StartsWith("AI", StringComparison.OrdinalIgnoreCase) && u.Length > 3) listenAll = u[2] != '0'; if (!o.IsRead(u)) return; }   // seules les questions du client sont suivies ; il reçoit tout si le poste lui est annoncé en mode AI
                Queue<Req> qq; string k = Key(u); if (!pend.TryGetValue(k, out qq)) { qq = new Queue<Req>(); pend[k] = qq; }
                Req x = new Req(); x.Who = who; x.T = now; qq.Enqueue(x);
            }
        }

        // ------------------------------------------------------------ vers le poste
        public void Send(string s)
        {
            lock (inner) { foreach (string u in Extract(o.Fr, new StringBuilder(), s)) Note(1, u); inner.Send(s); }
        }
        void FromExt(StringBuilder buf, string s)
        {
            List<string> l; lock (buf) l = Extract(o.Fr, buf, s);
            foreach (string u in l) lock (inner) { Note(2, u); inner.Send(u); }
        }

        // ------------------------------------------------------------ du poste : à qui revient la réponse ?
        void OnRaw(string s)
        {
            List<string[]> outl = new List<string[]>();    // { cible : L / E / LE, texte }
            lock (lk)
            {
                rxIn.Append(s);
                if (o.Fr == Framing.Fixed5)
                {
                    while (q.Count > 0 && rxIn.Length >= q.Peek().Len) { Req r = q.Dequeue(); string b = rxIn.ToString(0, r.Len); rxIn.Remove(0, r.Len); outl.Add(new string[] { r.Who == 2 ? "E" : "L", b }); }
                    if (q.Count == 0) rxIn.Length = 0;
                }
                else
                {
                    StringBuilder tmp = new StringBuilder(); string all = rxIn.ToString(); rxIn.Length = 0;
                    foreach (string u in Extract(o.Fr, tmp, all)) outl.Add(new string[] { Target(u), u });
                    rxIn.Append(tmp.ToString());
                }
            }
            foreach (string[] r in outl)
            {
                if (r[0].IndexOf('L') >= 0 && Data != null) Data(r[1]);
                if (r[0].IndexOf('E') >= 0) ToExt(r[1]);
            }
        }
        string Target(string u)
        {
            long now = clock.ElapsedMilliseconds; string k;
            if (o.Fr == Framing.Civ)
            {
                char to = u[2]; if (to == '\u0000') return "LE";             // diffusion du poste (transceive) : pour tout le monde
                if (to != (char)Civ.Ctrl) return "E";                         // adressée à un autre contrôleur que HamRadioLink
                if (u[3] == (char)Civ.Ctrl) return "";                        // écho d'une commande envoyée : personne n'en a besoin
                k = "*";
            }
            else if (u == "?;" || u == "E;" || u == "O;") return lastWho == 2 ? "E" : "L";
            else k = Key(u);
            Queue<Req> qq; if (!pend.TryGetValue(k, out qq)) return o.Fr == Framing.Civ || listenAll ? "LE" : "L";
            while (qq.Count > 0 && now - qq.Peek().T > (o.Fr == Framing.Civ ? 2500 : Window)) qq.Dequeue();
            if (qq.Count == 0) return o.Fr == Framing.Civ || listenAll ? "LE" : "L";   // réponse spontanée (AI / transceive)
            return qq.Dequeue().Who == 2 ? (o.Fr == Framing.Civ ? "E" : "LE") : "L";
        }
        void ToExt(string s)
        {
            if (sp != null) { lock (outQ) { if (outQ.Count < 2000) outQ.Enqueue(s); else dropped++; } outEv.Set(); }
            List<Conn> l; lock (conns) l = new List<Conn>(conns);
            byte[] b = Civ.L1.GetBytes(s);
            foreach (Conn c in l) { try { c.Ns.Write(b, 0, b.Length); } catch { try { c.Ns.Close(); } catch { } lock (conns) conns.Remove(c); } }
        }
    }
}
