using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace HamRadioLink
{
    // Serveur de commande IP (port 7809 par défaut) : les logiciels qui savent piloter un poste par un serveur IP (WSJT-X, JTDX, JS8Call,
    // SDRLink…) se branchent sur HamRadioLink. Deux formes de messages sont acceptées, reconnues à chaque message :
    //   • v5 / v6 : en-tête de 16 octets (taille, 0x1234ABCD, 0xABCD1234, somme) + texte UTF-16 terminé par un zéro ;
    //   • v4 : texte simple terminé par un retour chariot.
    // Les commandes (« get frequency », « set dropdown Mode USB 3 »…) sont traduites vers le poste actuellement choisi dans HamRadioLink,
    // quel que soit son modèle : fréquence A / B, mode, émission, split, volume. Un organe verrouillé (clic droit) refuse les ordres à distance.
    public class CmdServer : IDisposable
    {
        const uint Magic1 = 0x1234ABCD, Magic2 = 0xABCD1234;
        readonly Func<Rig> getRig; Rig rig { get { return getRig(); } } readonly TcpListener ls; volatile bool run = true; readonly List<TcpClient> cl = new List<TcpClient>();
        public readonly int Port; public int Clients, Commands; public readonly List<string> Bad = new List<string>();

        public CmdServer(Rig rig, int port, bool net) : this(delegate { return rig; }, port, net) { }
        public CmdServer(Func<Rig> getRig) { this.getRig = getRig; }      // sans écoute IP : sert de traducteur de commandes (serveur DDE)
        public string Exec(string cmd) { return Handle0(cmd); }
        public CmdServer(Func<Rig> getRig, int port, bool net)
        {
            this.getRig = getRig; ls = new TcpListener(net ? IPAddress.Any : IPAddress.Loopback, Math.Max(0, port)); ls.Start(); Port = ((IPEndPoint)ls.LocalEndpoint).Port;
            Thread t = new Thread(Accept); t.IsBackground = true; t.Priority = ThreadPriority.AboveNormal; t.Start();
        }
        public void Dispose() { run = false; try { ls.Stop(); } catch { } lock (cl) { foreach (TcpClient c in cl) try { c.Close(); } catch { } cl.Clear(); } }

        void Accept()
        {
            while (run)
            {
                TcpClient c; try { c = ls.AcceptTcpClient(); } catch { break; }
                c.NoDelay = true; lock (cl) cl.Add(c); Thread t = new Thread(delegate() { Serve(c); }); t.IsBackground = true; t.Priority = ThreadPriority.AboveNormal; t.Start();
            }
        }

        void Serve(TcpClient c)
        {
            Interlocked.Increment(ref Clients); int nCmd = 0; string who = "?"; try { who = c.Client.RemoteEndPoint.ToString(); } catch { } CatLog.Event("serveur " + Port, "client connecté " + who);
            try
            {
                NetworkStream ns = c.GetStream(); byte[] rb = new byte[4096]; List<byte> buf = new List<byte>();
                while (run)
                {
                    int n = ns.Read(rb, 0, rb.Length); if (n <= 0) break; for (int i = 0; i < n; i++) buf.Add(rb[i]);
                    while (true)
                    {
                        string msg = null; bool v5 = false;
                        if (buf.Count >= 16 && BitConverter.ToUInt32(buf.ToArray(), 4) == Magic1)
                        {
                            uint size = BitConverter.ToUInt32(buf.ToArray(), 0); if (size < 16 || size > 1 << 20) { buf.Clear(); break; }
                            if (buf.Count < size) break;
                            msg = Encoding.Unicode.GetString(buf.GetRange(16, (int)size - 16).ToArray()).TrimEnd('\0'); buf.RemoveRange(0, (int)size); v5 = true;
                        }
                        else
                        {
                            int e = buf.FindIndex(delegate(byte b) { return b == 13 || b == 10; }); if (e < 0) { if (buf.Count > 4000) buf.Clear(); break; }
                            msg = Encoding.UTF8.GetString(buf.GetRange(0, e).ToArray()); buf.RemoveRange(0, e + 1); if (msg.Trim().Length == 0) continue;
                        }
                        nCmd++; string reply; try { reply = Handle(msg.Trim()); } catch (Exception ex) { reply = "ERROR"; Bad.Add(msg + " : " + ex.Message); }
                        byte[] outb;
                        if (v5)
                        {
                            byte[] pl = Encoding.Unicode.GetBytes(reply + "\0"); outb = new byte[16 + pl.Length];
                            BitConverter.GetBytes((uint)outb.Length).CopyTo(outb, 0); BitConverter.GetBytes(Magic1).CopyTo(outb, 4); BitConverter.GetBytes(Magic2).CopyTo(outb, 8); Array.Copy(pl, 0, outb, 16, pl.Length);
                        }
                        else outb = Encoding.UTF8.GetBytes(reply + "\r");
                        ns.Write(outb, 0, outb.Length);
                    }
                }
            }
            catch { }
            finally { CatLog.Event("serveur " + Port, "client déconnecté " + who + " (" + nCmd + " commande(s))"); Interlocked.Decrement(ref Clients); lock (cl) cl.Remove(c); try { c.Close(); } catch { } }
        }

        // ------------------------------------------------------------ traduction des commandes vers le poste choisi
        static readonly string[,] Alias = { { "CW-U", "CW-USB" }, { "CW-L", "CW-LSB" }, { "RTTY-L", "RTTY-LSB" }, { "RTTY-U", "RTTY-USB" } };
        static string Show(string label) { for (int i = 0; i < Alias.GetLength(0); i++) if (label == Alias[i, 0]) return Alias[i, 1]; return label.Replace(',', ' ').Replace(':', ' ').Trim(); }
        public static string ModelName(RadioDef d)
        {
            string n = d.Name; foreach (string b in new string[] { "Yaesu ", "Icom ", "Kenwood ", "Elecraft ", "Xiegu " }) if (n.StartsWith(b)) { n = n.Substring(b.Length); break; }
            return n.Replace(',', ' ').Replace(':', ' ').Trim();
        }
        RadioDef D { get { return rig.Def; } }
        Ctl Mode { get { Ctl c = D.Find("md0"); return c != null && c.K == Kind.Choice && c.ItemList.Length > 0 ? c : null; } }
        Ctl Mode2 { get { Ctl c = D.Find("md1"); return c != null && c.K == Kind.Choice && c.ItemList.Length > 0 ? c : null; } }     // mode du VFO B / sous-récepteur (postes qui l'ont)
        Ctl Af { get { Ctl c = D.Find("af"); return c != null && c.K == Kind.Knob ? c : null; } }
        Ctl Ptt { get { return D.Find("ptt"); } }
        Ctl Split { get { Ctl c = D.Find("split"); return c != null && c.K == Kind.Toggle ? c : null; } }
        bool Locked(Ctl c) { return c != null && Cfg.GetBool("lock." + D.Key + "." + c.Id, false); }
        static bool IsModeName(string n) { return n.Equals("Mode", StringComparison.OrdinalIgnoreCase) || n.Equals("Main Mode", StringComparison.OrdinalIgnoreCase) || n.Equals("Mode A", StringComparison.OrdinalIgnoreCase); }      // noms de la liste des modes selon le poste / le logiciel client
        static bool IsMode2Name(string n) { return n.Equals("Mode2", StringComparison.OrdinalIgnoreCase) || n.Equals("Mode B", StringComparison.OrdinalIgnoreCase) || n.Equals("Sub Mode", StringComparison.OrdinalIgnoreCase); }
        static string Clean(string s) { return s.Trim().Trim('{', '}').Replace('~', ' ').Trim(); }

        string Handle(string cmd)
        {
            string r0; try { r0 = Handle0(cmd); } catch (Exception ex) { CatLog.Event("serveur " + Port, cmd + " -> EXCEPTION " + ex.Message); throw; }
            CatLog.Server("serveur " + Port, cmd, r0); return r0;
        }
        string Handle0(string cmd)
        {
            Commands++; if (rig == null || D == null) return "ERROR"; Match m = Regex.Match(cmd, @"^\[\d+\]\s*(.*)$"); if (m.Success) cmd = m.Groups[1].Value.Trim();
            string lc = cmd.ToLowerInvariant();
            // poste non joignable : toute commande de changement (fréquence, mode, PTT…) est refusée, l'affichage garde la dernière valeur réelle
            if (lc.StartsWith("set ") && !rig.Connected) { CatLog.Event("serveur " + Port, "refusé, poste non connecté : " + cmd); Bad.Add(cmd); return "ERROR"; }
            switch (lc)
            {
                case "get context": return "1";
                case "get id": return "HamRadioLink";
                case "get version": return MainForm.Version;
                case "get radios": return "1:" + ModelName(D);
                case "get radio": return ModelName(D);
                case "get vfo-count": return "1";
                case "get buttons": { List<string> b = new List<string>(); if (Ptt != null) { b.Add("TX"); b.Add("MOX"); } if (Split != null) b.Add("Split"); return string.Join(",", b.ToArray()); }
                case "get dropdowns": return Mode != null ? "Mode" : "";      // Mode2 (VFO B) reste accessible par « get dropdown-text / dropdown-list / set dropdown Mode2 » sans changer cette liste
                case "get sliders": return Af != null ? "AF gain" : "";
                case "get frequency": return Freq("fa");
                case "get frequencies": { Ctl fb = D.Find("fb"); return Freq("fa") + "-" + (fb != null ? Freq("fb") : Freq("fa")); }
            }
            if ((m = Regex.Match(cmd, @"^get dropdown-list\s+(.+)$", RegexOptions.IgnoreCase)).Success && IsMode2Name(Clean(m.Groups[1].Value)))
            {
                if (Mode2 == null) return "ERROR"; List<string> l2 = new List<string>(); foreach (Item it in Mode2.ItemList) l2.Add(Show(it.Label)); return string.Join(",", l2.ToArray());
            }
            if ((m = Regex.Match(cmd, @"^get dropdown-text\s+(.+)$", RegexOptions.IgnoreCase)).Success && IsMode2Name(Clean(m.Groups[1].Value)))
            {
                if (Mode2 == null) return "ERROR"; string v2 = rig.Get("md1") ?? "\u0001"; foreach (Item it in Mode2.ItemList) if (it.Match(v2)) return "Mode2:" + Show(it.Label); return "Mode2:";
            }
            if ((m = Regex.Match(cmd, @"^set dropdown\s+(\S+)\s+(\S+)(?:\s+(\d+))?$", RegexOptions.IgnoreCase)).Success && IsMode2Name(Clean(m.Groups[1].Value)))
            {
                if (Mode2 == null) return "ERROR"; string want2 = Clean(m.Groups[2].Value);
                foreach (Item it in Mode2.ItemList) if (it.Label == want2 || Show(it.Label) == want2) { rig.Set(Mode2, it.Code); VerifyCw(Mode2, it); return "OK"; }
                return "ERROR";
            }
            if ((m = Regex.Match(cmd, @"^get dropdown-list\s+(.+)$", RegexOptions.IgnoreCase)).Success)
            {
                if (!IsModeName(Clean(m.Groups[1].Value)) || Mode == null) return "ERROR"; List<string> l = new List<string>(); foreach (Item it in Mode.ItemList) l.Add(Show(it.Label)); return string.Join(",", l.ToArray());
            }
            if ((m = Regex.Match(cmd, @"^get dropdown-text\s+(.+)$", RegexOptions.IgnoreCase)).Success)
            {
                if (!IsModeName(Clean(m.Groups[1].Value)) || Mode == null) return "ERROR"; string v = rig.Get("md0") ?? "\u0001"; foreach (Item it in Mode.ItemList) if (it.Match(v)) return "Mode:" + Show(it.Label); return "Mode:";
            }
            if ((m = Regex.Match(cmd, @"^get slider-range\s+(.+?)\s+(\S+)$", RegexOptions.IgnoreCase)).Success) { Ctl a = Af; return a == null ? "ERROR" : a.Min + "," + a.Max; }
            if ((m = Regex.Match(cmd, @"^get slider-pos\s+(.+?)\s+(\S+)$", RegexOptions.IgnoreCase)).Success) { Ctl a = Af; if (a == null) return "ERROR"; long v = rig.GetL("af"); return v.ToString(CultureInfo.InvariantCulture); }
            if ((m = Regex.Match(cmd, @"^get button-select\s+(\S+)$", RegexOptions.IgnoreCase)).Success)
            {
                string b = Clean(m.Groups[1].Value).ToUpperInvariant();
                if ((b == "TX" || b == "MOX" || b == "PTT") && Ptt != null) return rig.Tx ? "1" : "0";
                if (b == "SPLIT" && Split != null) return rig.IsOn(Split) ? "1" : "0";
                return "ERROR";
            }
            if ((m = Regex.Match(cmd, @"^set frequency-hz\s+(\d+)$", RegexOptions.IgnoreCase)).Success) return SetFreq("fa", m.Groups[1].Value);
            if ((m = Regex.Match(cmd, @"^set frequencies-hz\s+(\d+)\s+(\d+)$", RegexOptions.IgnoreCase)).Success)
            {
                string r = SetFreq("fa", m.Groups[1].Value); if (r == "OK" && D.Find("fb") != null) r = SetFreq("fb", m.Groups[2].Value); return r;
            }
            if ((m = Regex.Match(cmd, @"^set dropdown\s+(\S+)\s+(\S+)(?:\s+(\d+))?$", RegexOptions.IgnoreCase)).Success)
            {
                if (!IsModeName(Clean(m.Groups[1].Value)) || Mode == null) return "ERROR"; string want = Clean(m.Groups[2].Value);
                foreach (Item it in Mode.ItemList) if (it.Label == want || Show(it.Label) == want) { rig.Set(Mode, it.Code); VerifyCw(Mode, it); return "OK"; }
                return "ERROR";
            }
            if ((m = Regex.Match(cmd, @"^set button-select\s+(\S+)\s+([01])$", RegexOptions.IgnoreCase)).Success)
            {
                string b = Clean(m.Groups[1].Value).ToUpperInvariant(); bool on = m.Groups[2].Value == "1";
                if ((b == "TX" || b == "MOX" || b == "PTT") && Ptt != null) { rig.Set(Ptt, on ? Ptt.OnSend : Ptt.OffSend); return "OK"; }
                if (b == "SPLIT" && Split != null) { rig.Set(Split, on ? Split.OnSend : Split.OffSend); return "OK"; }
                return "ERROR";
            }
            if ((m = Regex.Match(cmd, @"^set slider-pos\s+(.+?)\s+(\S+)\s+(-?\d+)$", RegexOptions.IgnoreCase)).Success)
            {
                Ctl a = Af; if (a == null) return "ERROR"; int v = int.Parse(m.Groups[3].Value); rig.Set(a, Math.Max(a.Min, Math.Min(a.Max, v)).ToString(CultureInfo.InvariantCulture)); return "OK";
            }
            Bad.Add(cmd); return "ERROR";
        }
        // FT-2000 : « MD03; » / « MD07; » se comportent comme la touche CW du poste (déjà en CW ou CW-R, il bascule de l'un à l'autre quel que soit l'ordre ;
        // depuis un autre mode il prend la variante mémorisée). On relit donc le mode réel et on renvoie l'ordre une fois si le poste est de l'autre côté.
        void VerifyCw(Ctl c, Item want)
        {
            if (c == null || want == null || string.IsNullOrEmpty(c.Read) || want.Label.IndexOf("CW", StringComparison.OrdinalIgnoreCase) < 0) return;
            ThreadPool.QueueUserWorkItem(delegate(object o)
            {
                try
                {
                    Thread.Sleep(450); string pre = c.Read.TrimEnd(';'); string r = rig.AskPrefix(c.Read, pre, 900);
                    if (r == null || r.Length <= pre.Length) return;
                    string v = r.Substring(pre.Length).TrimEnd(';'); if (want.Match(v)) return;
                    foreach (Item it in c.ItemList) if (it.Match(v))
                    {
                        if (it.Label.IndexOf("CW", StringComparison.OrdinalIgnoreCase) >= 0) { rig.Set(c, want.Code); CatLog.Event("serveur " + Port, "mode CW : le poste a basculé de l'autre côté, ordre renvoyé (" + want.Label + ")"); }
                        break;
                    }
                }
                catch { }
            });
        }
        string Freq(string id) { long f = rig.GetL(id); return f.ToString(CultureInfo.InvariantCulture); }
        string SetFreq(string id, string hz)
        {
            Ctl c = D.Find(id); long f; if (c == null || !long.TryParse(hz, NumberStyles.Integer, CultureInfo.InvariantCulture, out f) || f < 1000 || f > D.MaxFreq) { CatLog.Event("serveur " + Port, "fréquence refusée (" + hz + " Hz : hors limites ou poste sans " + id + ")"); return "ERROR"; }
            rig.Set(c, f.ToString(CultureInfo.InvariantCulture)); return "OK";
        }
    }
}
