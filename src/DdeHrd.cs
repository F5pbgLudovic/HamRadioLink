using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace HamRadioLink
{
    // Serveur DDE « HRD » : HDSDR (RF Front-End « controlled by HRD (DDE) ») se connecte au service HRD_RADIO_000, sujet HRD_CAT,
    // et suit le poste par les éléments HRD_HERTZ, HRD_MODE, HRD_TX (lecture + avis de changement). À créer sur le fil de l'interface.
    // Les ordres reçus d'HDSDR (changement de fréquence ou de mode) sont appliqués au poste ; l'émission n'est JAMAIS commandée par DDE.
    public class DdeHrdServer : IDisposable
    {
        delegate IntPtr DdeCb(uint type, uint fmt, IntPtr hconv, IntPtr hsz1, IntPtr hsz2, IntPtr hdata, IntPtr d1, IntPtr d2);
        [DllImport("user32.dll")] static extern uint DdeInitializeW(ref uint idInst, DdeCb cb, uint flags, uint res);
        [DllImport("user32.dll")] static extern IntPtr DdeCreateStringHandleW(uint idInst, string s, int cp);
        [DllImport("user32.dll")] static extern uint DdeQueryStringW(uint idInst, IntPtr hsz, StringBuilder sb, uint max, int cp);
        [DllImport("user32.dll")] static extern IntPtr DdeNameService(uint idInst, IntPtr hsz1, IntPtr hsz2, uint cmd);
        [DllImport("user32.dll")] static extern IntPtr DdeCreateDataHandle(uint idInst, byte[] data, uint cb, uint off, IntPtr hszItem, uint fmt, uint cmd);
        [DllImport("user32.dll")] static extern IntPtr DdeAccessData(IntPtr hdata, out uint size);
        [DllImport("user32.dll")] static extern bool DdeUnaccessData(IntPtr hdata);
        [DllImport("user32.dll")] static extern bool DdePostAdvise(uint idInst, IntPtr hszTopic, IntPtr hszItem);
        [DllImport("user32.dll")] static extern bool DdeUninitialize(uint idInst);

        readonly Func<Rig> getRig; CmdServer h; uint inst; DdeCb cb; IntPtr hTopic; readonly Timer tm = new Timer(); readonly Dictionary<string, string> last = new Dictionary<string, string>();
        static readonly string[] Items = { "HRD_HERTZ", "HRD_MODE", "HRD_TX" };
        public int Requests; public string Last = "";

        public DdeHrdServer(Func<Rig> getRig)
        {
            this.getRig = getRig; h = new CmdServer(getRig); cb = Callback;
            if (DdeInitializeW(ref inst, cb, 0, 0) != 0) throw new Exception("DdeInitialize a échoué");
            hTopic = DdeCreateStringHandleW(inst, "HRD_CAT", 1004); DdeNameService(inst, DdeCreateStringHandleW(inst, "HRD_RADIO_000", 1004), IntPtr.Zero, 1);
            tm.Interval = 150; tm.Tick += delegate { Poll(); }; tm.Start();
        }
        public void Dispose() { tm.Stop(); try { DdeUninitialize(inst); } catch { } }

        string Value(string item)
        {
            Rig r = getRig(); if (r == null || r.Def == null || !r.Connected) return "";
            try
            {
                if (item == "HRD_HERTZ") return h.Exec("get frequency");
                if (item == "HRD_MODE") { string m = h.Exec("get dropdown-text Mode"); return m.StartsWith("Mode:") ? m.Substring(5) : m; }
                if (item == "HRD_TX") return h.Exec("get button-select TX") == "1" ? "1" : "0";
            }
            catch { }
            return "";
        }
        void Poll()
        {
            foreach (string it in Items) { string v = Value(it); string o; if (!last.TryGetValue(it, out o) || o != v) { last[it] = v; try { DdePostAdvise(inst, hTopic, DdeCreateStringHandleW(inst, it, 1004)); } catch { } } }
        }
        static void Log(string s) { try { System.IO.File.AppendAllText(System.IO.Path.Combine(Cfg.Dir ?? ".", "dde-hrd.log"), DateTime.Now.ToString("HH:mm:ss.fff") + "  " + s + "\r\n"); } catch { } }
        string Q(IntPtr x) { if (x == IntPtr.Zero) return ""; StringBuilder sb = new StringBuilder(260); DdeQueryStringW(inst, x, sb, 260, 1004); return sb.ToString(); }
        string Data(IntPtr x)
        {
            if (x == IntPtr.Zero) return ""; uint n; IntPtr p = DdeAccessData(x, out n); if (p == IntPtr.Zero) return ""; byte[] b = new byte[Math.Min(n, 256)]; Marshal.Copy(p, b, 0, b.Length); DdeUnaccessData(x);
            bool u16 = b.Length > 2 && b[1] == 0 && b[0] != 0; return (u16 ? Encoding.Unicode.GetString(b) : Encoding.Default.GetString(b)).TrimEnd('\0');
        }
        IntPtr Reply(string item, IntPtr hszItem)
        {
            byte[] b = Encoding.Default.GetBytes(Value(item) + "\0"); return DdeCreateDataHandle(inst, b, (uint)b.Length, 0, hszItem, 1, 0);
        }
        IntPtr Callback(uint type, uint fmt, IntPtr hconv, IntPtr hsz1, IntPtr hsz2, IntPtr hdata, IntPtr d1, IntPtr d2)
        {
            try
            {
                switch (type)
                {
                    case 0x1062: return (IntPtr)1;                                                                  // CONNECT
                    case 0x1030: return (IntPtr)1;                                                                  // ADVSTART
                    case 0x20B0: case 0x2022: { Requests++; string it = Q(hsz2); return Array.IndexOf(Items, it) >= 0 ? Reply(it, hsz2) : IntPtr.Zero; }   // REQUEST / ADVREQ
                    case 0x4090:                                                                                    // POKE : HDSDR change la fréquence ou le mode
                        {
                            string it = Q(hsz2), v = Data(hdata); Last = it + "=" + v; Log("POKE " + Last); Rig r = getRig(); if (r == null || !r.Connected) return (IntPtr)0x8000;
                            if (it == "HRD_HERTZ") { long f; if (long.TryParse(v.Trim(), out f)) h.Exec("set frequency-hz " + f); }
                            else if (it == "HRD_MODE" && v.Trim().Length > 0) h.Exec("set dropdown Mode " + v.Trim());
                            return (IntPtr)0x8000;
                        }
                    case 0x4050: { string c = Data(hdata).Trim(); Last = "EXEC " + c; Log(Last); Rig r = getRig(); Match mm; if (r != null && r.Connected) { if ((mm = Regex.Match(c, @"^freq\s+(\d+)$", RegexOptions.IgnoreCase)).Success) h.Exec("set frequency-hz " + mm.Groups[1].Value); else if ((mm = Regex.Match(c, @"^mode\s+(\S+)$", RegexOptions.IgnoreCase)).Success) h.Exec("set dropdown Mode " + mm.Groups[1].Value); } return (IntPtr)0x8000; }                           // EXECUTE : noté, rien d'appliqué
                    default: if (type != 0x80C2 && type != 0x8072 && type != 0x80A2 && type != 0x8040) Log("type 0x" + type.ToString("X") + " " + Q(hsz1) + " / " + Q(hsz2)); break;
                }
            }
            catch { }
            return IntPtr.Zero;
        }
    }
}


