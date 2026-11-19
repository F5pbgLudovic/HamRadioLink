using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace HamRadioLink
{
    // Une liaison transporte du texte CAT « XX...; » vers le poste et ramène ses réponses.
    public interface ILink
    {
        string Name { get; }
        int Baud { get; }
        bool Open(out string err);
        void Close();
        void Send(string s);
        event Action<string> Data;
    }

    // ------------------------------------------------------------------ port COM (CAT direct)
    public class SerialLink : ILink
    {
        readonly string port; readonly int baud, stop; readonly bool rts, dtr; SerialPort sp;
        public event Action<string> Data;
        public string Name { get { return port + " " + baud + " bauds"; } }
        public int Baud { get { return baud; } }
        public SerialLink(string port, int baud, int stopBits, bool rts, bool dtr) { this.port = port; this.baud = baud; stop = stopBits; this.rts = rts; this.dtr = dtr; }
        public bool Open(out string err)
        {
            err = null;
            try
            {
                sp = new SerialPort(port, baud, Parity.None, 8, stop == 2 ? StopBits.Two : StopBits.One);
                sp.Handshake = Handshake.None; sp.ReadTimeout = 300; sp.WriteTimeout = 1000; sp.Encoding = Civ.L1;
                sp.DataReceived += delegate { try { string s = sp.ReadExisting(); if (s.Length > 0 && Data != null) Data(s); } catch { } };
                sp.Open(); sp.RtsEnable = rts; sp.DtrEnable = dtr;     // certains postes (FTdx10, FT-2000) n'écoutent le CAT que si RTS est actif
                Thread.Sleep(80); sp.DiscardInBuffer(); return true;
            }
            catch (Exception ex) { err = ex.Message; try { if (sp != null) sp.Dispose(); } catch { } sp = null; return false; }
        }
        public void Close() { try { if (sp != null) { sp.Close(); sp.Dispose(); } } catch { } sp = null; }
        public void Send(string s) { SerialPort p = sp; if (p == null) return; try { p.Write(s); } catch { } }
    }

    // ------------------------------------------------------------------ CAT brut sur TCP (serveur série-réseau, port COM déporté...)
    public class TcpLink : ILink
    {
        readonly string host; readonly int port; TcpClient cl; NetworkStream ns; volatile bool run;
        public event Action<string> Data;
        public string Name { get { return "TCP " + host + ":" + port; } }
        public int Baud { get { return 115200; } }
        public TcpLink(string host, int port) { this.host = host; this.port = port; }
        public bool Open(out string err)
        {
            err = null;
            try
            {
                cl = new TcpClient(); cl.NoDelay = true; IAsyncResult ar = cl.BeginConnect(host, port, null, null);
                if (!ar.AsyncWaitHandle.WaitOne(3000)) { cl.Close(); err = "pas de réponse de " + host + ":" + port; return false; }
                cl.EndConnect(ar); ns = cl.GetStream(); run = true;
                Thread t = new Thread(delegate()
                {
                    byte[] buf = new byte[512];
                    while (run) { try { int n = ns.Read(buf, 0, buf.Length); if (n <= 0) break; if (Data != null) Data(Civ.L1.GetString(buf, 0, n)); } catch { break; } }
                });
                t.IsBackground = true; t.Start(); return true;
            }
            catch (Exception ex) { err = ex.Message; return false; }
        }
        public void Close() { run = false; try { if (cl != null) cl.Close(); } catch { } }
        public void Send(string s) { try { byte[] b = Civ.L1.GetBytes(s); ns.Write(b, 0, b.Length); } catch { } }
    }

    // ------------------------------------------------------------------ poste simulé (essais sans matériel)
    public class VirtualLink : ILink
    {
        public readonly YaesuSim Sim; public event Action<string> Data;
        public string Name { get { return "Poste virtuel (" + Sim.Def.Name + ")"; } }
        public int Baud { get { return 115200; } }
        public VirtualLink(RadioDef d) { Sim = new YaesuSim(d); }
        public bool Open(out string err) { err = null; return true; }
        public void Close() { }
        public void Send(string s) { string r = Sim.Input(s); if (r.Length > 0 && Data != null) Data(r); }
    }
}
