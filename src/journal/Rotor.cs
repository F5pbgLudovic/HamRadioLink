using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace HamRadioLink.Journal
{
    // Protocoles série / réseau des rotors d'antenne (azimut seulement)
    public enum RotorProto { Gs232, Dcu, Spid, Easycomm, Net }

    public class RotorModel
    {
        public string Brand, Name, Note; public RotorProto Proto; public int Baud;
        public override string ToString() { return Brand + " " + Name; }
    }

    // Commande d'un rotor : une seule liaison, ouverte à la demande, commandes envoyées hors du fil de l'interface.
    public static class Rotor
    {
        public static readonly List<RotorModel> Models = Build();
        static readonly object Gate = new object(); static Link link; static string linkKey = "";
        public static string Last = "";   // dernier message d'erreur

        static RotorModel M(string brand, string name, RotorProto p, int baud, string note)
        { return new RotorModel { Brand = brand, Name = name, Proto = p, Baud = baud, Note = note }; }
        static List<RotorModel> Build()
        {
            List<RotorModel> l = new List<RotorModel>();
            string yi = "Via le boîtier d'interface GS-232 (prise « Ext. Control » du contrôleur) : 9600 bauds.";
            foreach (string n in new[] { "G-450A", "G-450C", "G-550", "G-650A", "G-650C", "G-800SA", "G-800DXA", "G-1000SA", "G-1000DXA", "G-2800SDX", "G-2800DXA", "G-5500", "G-5500DC", "G-5400B", "G-5600B" }) l.Add(M("Yaesu", n, RotorProto.Gs232, 9600, yi));
            l.Add(M("Yaesu", "GS-232A (interface)", RotorProto.Gs232, 9600, "Interface GS-232A."));
            l.Add(M("Yaesu", "GS-232B (interface)", RotorProto.Gs232, 9600, "Interface GS-232B."));
            l.Add(M("WiMo", "YRC-1X (contrôleur numérique)", RotorProto.Gs232, 9600, "Commandes Yaesu GS-232."));
            l.Add(M("WiMo", "RTC-200 (interface USB)", RotorProto.Gs232, 9600, "Interface USB pour rotors Yaesu : commandes GS-232."));
            l.Add(M("WiMo", "ERC-DUO (interface USB)", RotorProto.Gs232, 9600, "Interface USB pour rotors Yaesu : commandes GS-232."));
            l.Add(M("Green Heron", "RT-21", RotorProto.Gs232, 9600, "Option numérique, commandes GS-232."));
            l.Add(M("Green Heron", "RT-20", RotorProto.Gs232, 9600, "Option numérique, commandes GS-232."));
            foreach (string n in new[] { "RC5-1", "RC5-3", "RC5A-2", "RC5A-3", "RC5B-3" }) l.Add(M("Create", n, RotorProto.Gs232, 9600, "Par un boîtier d'interface compatible GS-232."));
            foreach (string n in new[] { "AR-40", "CD-45II", "Ham-IV", "Ham-V", "T2X Tailtwister", "HDR-300A" }) l.Add(M("Hy-Gain", n, RotorProto.Dcu, 4800, "Par boîtier PC type DCU-1 (commandes AP1 / AM1) : 4800 bauds. Non essayé sur matériel."));
            l.Add(M("Hy-Gain", "DCU-1 / DCU-3 (boîtier PC)", RotorProto.Dcu, 4800, "Commandes AP1 / AM1. Non essayé sur matériel."));
            foreach (string n in new[] { "RAS", "BIG-RAS", "RAK", "BIG-RAK", "MD-01", "MD-02" }) l.Add(M("SPID", n, RotorProto.Spid, 600, "Contrôleur SPID en mode PC (trame de 13 octets) : 600 bauds. Non essayé sur matériel."));
            l.Add(M("Autre", "Contrôleur compatible GS-232A / GS-232B", RotorProto.Gs232, 9600, "Commandes M / C / S."));
            l.Add(M("Autre", "Easycomm I / II (contrôleurs Arduino...)", RotorProto.Easycomm, 9600, "Commandes AZ / EL. Non essayé sur matériel."));
            l.Add(M("Réseau", "Serveur de rotor (TCP)", RotorProto.Net, 0, "Commandes P / p / S d'un serveur de rotor sur le réseau (adresse et port TCP, 4533 en général)."));
            return l;
        }

        public static RotorModel Current
        {
            get { string n = App.Cfg.Get("rotor.model", ""); foreach (RotorModel m in Models) if (m.ToString() == n) return m; return null; }
        }

        // Rotor « présent » : un modèle est choisi et son port COM existe sur ce PC (ou une adresse réseau est renseignée)
        public static bool Present
        {
            get
            {
                RotorModel m = Current; if (m == null) return false;
                if (m.Proto == RotorProto.Net) return App.Cfg.Get("rotor.host", "").Trim().Length > 0;
                string p = App.Cfg.Get("rotor.port", "").Trim(); if (p.Length == 0) return false;
                try { foreach (string n in SerialPort.GetPortNames()) if (string.Equals(n, p, StringComparison.OrdinalIgnoreCase)) return true; } catch { }
                return false;
            }
        }

        // Étalonnage : décalage (degrés) entre le cap indiqué par le rotor et le cap réel de l'antenne ; cap réel = indication + décalage
        public static double Offset
        {
            get { double v; return double.TryParse(App.Cfg.Get("rotor.offset", "0"), NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : 0; }
        }
        public static void SetOffset(double v)
        {
            v = Norm(v); if (v > 180) v -= 360; App.Cfg.Set("rotor.offset", Math.Round(v, 1).ToString(CultureInfo.InvariantCulture)); try { App.Cfg.Save(); } catch { }
        }

        // ---------- liaison ----------
        class Link : IDisposable
        {
            SerialPort sp; TcpClient tc; NetworkStream ns;
            public static Link Open(RotorModel m)
            {
                Link l = new Link();
                try
                {
                    if (m.Proto == RotorProto.Net)
                    {
                        int port; if (!int.TryParse(App.Cfg.Get("rotor.tport", "4533"), out port)) port = 4533;
                        l.tc = new TcpClient(); IAsyncResult ar = l.tc.BeginConnect(App.Cfg.Get("rotor.host", "127.0.0.1"), port, null, null);
                        if (!ar.AsyncWaitHandle.WaitOne(1500)) throw new IOException(Tr.T("Connexion au serveur de rotor impossible."));
                        l.tc.EndConnect(ar); l.ns = l.tc.GetStream();
                    }
                    else
                    {
                        int baud; if (!int.TryParse(App.Cfg.Get("rotor.baud", ""), out baud) || baud <= 0) baud = m.Baud;
                        string pn = App.Cfg.Get("rotor.port", ""); if (pn.Length == 0) throw new IOException(Tr.T("Aucun port COM choisi pour le rotor."));
                        l.sp = new SerialPort(pn, baud, Parity.None, 8, StopBits.One) { ReadTimeout = 300, WriteTimeout = 500 }; l.sp.Open();
                    }
                }
                catch { l.Dispose(); throw; }
                return l;
            }
            public void Write(byte[] b) { if (sp != null) sp.Write(b, 0, b.Length); else ns.Write(b, 0, b.Length); }
            public int Avail { get { return sp != null ? sp.BytesToRead : (ns.DataAvailable ? 1 : 0); } }
            public int Read(byte[] b, int n) { return sp != null ? sp.Read(b, 0, n) : ns.Read(b, 0, n); }
            public void Clear() { byte[] t = new byte[256]; while (Avail > 0) Read(t, 256); }
            public void Dispose() { try { if (sp != null) sp.Dispose(); } catch { } try { if (tc != null) tc.Close(); } catch { } sp = null; tc = null; ns = null; }
        }

        static string Key(RotorModel m) { return m.ToString() + "|" + App.Cfg.Get("rotor.port", "") + "|" + App.Cfg.Get("rotor.baud", "") + "|" + App.Cfg.Get("rotor.host", "") + "|" + App.Cfg.Get("rotor.tport", ""); }
        static Link Get(RotorModel m)
        {
            string k = Key(m); if (link != null && linkKey != k) Disconnect_();
            if (link == null) { link = Link.Open(m); linkKey = k; }
            return link;
        }
        static void Disconnect_() { if (link != null) { link.Dispose(); link = null; } linkKey = ""; }
        public static void Disconnect() { lock (Gate) Disconnect_(); }

        static byte[] Reply(Link l, int ms, Func<List<byte>, bool> done)
        {
            List<byte> r = new List<byte>(); DateTime end = DateTime.UtcNow.AddMilliseconds(ms); byte[] t = new byte[256];
            while (DateTime.UtcNow < end)
            {
                while (l.Avail > 0) { int n = l.Read(t, 256); for (int i = 0; i < n; i++) r.Add(t[i]); }
                if (done(r)) break; Thread.Sleep(15);
            }
            return r.ToArray();
        }
        static byte[] Asc(string s) { return Encoding.ASCII.GetBytes(s); }
        static string D3(double az) { return ((int)Math.Round(Norm(az)) % 360).ToString("000"); }
        public static double Norm(double a) { a = a % 360; if (a < 0) a += 360; return a; }

        // ---------- décodage / trames (publics pour les essais) ----------
        // Réponse GS-232 : « +0123 » ou « AZ=123 » -> 123
        public static bool ParseDigits(string s, out double az)
        {
            az = 0; Match m = Regex.Match(s ?? "", @"(\d{3,4})"); if (!m.Success) return false;
            int v = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture); if (v > 450) return false; az = v; return true;
        }
        // Trame de positionnement du contrôleur SPID (13 octets), azimut entier
        public static byte[] SpidFrame(double az)
        {
            int h = (int)Math.Round(360 + Norm(az)); byte[] f = new byte[13]; f[0] = 0x57;
            f[1] = (byte)('0' + h / 1000 % 10); f[2] = (byte)('0' + h / 100 % 10); f[3] = (byte)('0' + h / 10 % 10); f[4] = (byte)('0' + h % 10); f[5] = 1;
            f[6] = (byte)'0'; f[7] = (byte)'0'; f[8] = (byte)'0'; f[9] = (byte)'0'; f[10] = 1; f[11] = 0x2F; f[12] = 0x20; return f;
        }
        public static byte[] SpidAsk(byte cmd) { byte[] f = new byte[13]; f[0] = 0x57; f[11] = cmd; f[12] = 0x20; return f; }   // 0x1F = lire, 0x0F = arrêt
        public static bool SpidDecode(byte[] r, out double az)
        {
            az = 0; if (r == null || r.Length < 6 || (r[0] != 0x58 && r[0] != 0x57)) return false;
            int[] d = new int[4]; for (int i = 0; i < 4; i++) { int b = r[1 + i]; d[i] = b >= 0x30 ? b - 0x30 : b; }
            int ph = r[5] >= 0x30 ? r[5] - 0x30 : r[5]; if (ph <= 0) ph = 1;
            az = (d[0] * 1000 + d[1] * 100 + d[2] * 10 + d[3]) / (double)ph - 360; return az > -1 && az < 451;
        }

        // ---------- commandes (synchrones : à lancer hors du fil de l'interface) ----------
        public static void GotoSync(double az, bool raw = false)
        {
            RotorModel m = Current; if (m == null) throw new InvalidOperationException(Tr.T("Choisissez d'abord un rotor (menu Rotor)."));
            if (!raw) az = Norm(az - Offset);   // étalonnage : cap réel -> cap indiqué par le rotor
            lock (Gate)
            {
                try
                {
                    Link l = Get(m); l.Clear();
                    switch (m.Proto)
                    {
                        case RotorProto.Gs232: l.Write(Asc("M" + D3(az) + "\r")); break;
                        case RotorProto.Dcu: l.Write(Asc("AP1" + D3(az) + ";AM1;")); break;
                        case RotorProto.Spid: l.Write(SpidFrame(az)); break;
                        case RotorProto.Easycomm: l.Write(Asc("AZ" + Norm(az).ToString("0.0", CultureInfo.InvariantCulture) + " EL0.0\r\n")); break;
                        case RotorProto.Net: l.Write(Asc("P " + Norm(az).ToString("0.0", CultureInfo.InvariantCulture) + " 0.0\n")); Reply(l, 500, r => r.Contains((byte)'\n')); break;
                    }
                }
                catch { Disconnect_(); throw; }
            }
        }
        public static void StopSync()
        {
            RotorModel m = Current; if (m == null) throw new InvalidOperationException(Tr.T("Choisissez d'abord un rotor (menu Rotor)."));
            lock (Gate)
            {
                try
                {
                    Link l = Get(m); l.Clear();
                    switch (m.Proto)
                    {
                        case RotorProto.Gs232: l.Write(Asc("S\r")); break;
                        case RotorProto.Spid: l.Write(SpidAsk(0x0F)); break;
                        case RotorProto.Easycomm: l.Write(Asc("SA SE\r\n")); break;
                        case RotorProto.Net: l.Write(Asc("S\n")); Reply(l, 500, r => r.Contains((byte)'\n')); break;
                        case RotorProto.Dcu: { double a; if (ReadLocked(l, m, out a)) l.Write(Asc("AP1" + D3(a) + ";AM1;")); break; }   // pas de commande d'arrêt : on rejoint la position actuelle
                    }
                }
                catch { Disconnect_(); throw; }
            }
        }
        public static bool ReadSync(out double az, bool raw = false)
        {
            az = 0; RotorModel m = Current; if (m == null) return false;
            lock (Gate)
            {
                try { Link l = Get(m); l.Clear(); bool ok = ReadLocked(l, m, out az); if (ok && !raw) az = Norm(az + Offset); return ok; }
                catch { Disconnect_(); throw; }
            }
        }
        static bool ReadLocked(Link l, RotorModel m, out double az)
        {
            az = 0; byte[] r;
            switch (m.Proto)
            {
                case RotorProto.Gs232:
                    l.Write(Asc("C\r")); r = Reply(l, 700, x => x.Count >= 5 && x.Contains((byte)'\r')); return ParseDigits(Encoding.ASCII.GetString(r), out az);
                case RotorProto.Dcu:
                    l.Write(Asc("AI1;")); r = Reply(l, 700, x => x.Contains((byte)';') || x.Contains((byte)'\r')); return ParseDigits(Encoding.ASCII.GetString(r), out az);
                case RotorProto.Spid:
                    l.Write(SpidAsk(0x1F)); r = Reply(l, 900, x => x.Count >= 12); return SpidDecode(r, out az);
                case RotorProto.Easycomm:
                    l.Write(Asc("AZ EL\r\n")); r = Reply(l, 700, x => x.Contains((byte)'\n') || x.Count >= 10);
                    { Match mt = Regex.Match(Encoding.ASCII.GetString(r), @"AZ\s*(-?\d+(\.\d+)?)"); if (!mt.Success) return false; az = Norm(double.Parse(mt.Groups[1].Value, CultureInfo.InvariantCulture)); return true; }
                default:
                    l.Write(Asc("p\n")); r = Reply(l, 700, x => { int n = 0; foreach (byte b in x) if (b == (byte)'\n') n++; return n >= 2; });
                    { string[] ln = Encoding.ASCII.GetString(r).Split('\n'); double v; if (ln.Length > 0 && double.TryParse(ln[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v)) { az = Norm(v); return true; } return false; }
            }
        }

        // Lance une action hors du fil de l'interface ; done(null) si réussi, sinon done(message)
        public static void Run(Action job, Action<string> done)
        {
            ThreadPool.QueueUserWorkItem(delegate { string err = null; try { job(); } catch (Exception ex) { err = ex.Message; Last = err; } if (done != null) { try { done(err); } catch { } } });
        }
        public static void GoAsync(double az, Action<string> done) { Run(() => GotoSync(az), done); }

        // ---------- suivi automatique (fenêtre Add) ----------
        static System.Threading.Timer auto; static double pending, lastSent = -999;
        public static void AutoFollow(double az)
        {
            if (App.Cfg.Get("rotor.auto", "0") != "1" || !Present) return;
            pending = az;
            if (auto == null) auto = new System.Threading.Timer(delegate
            {
                double a = pending, d = Math.Abs(Norm(a - lastSent + 180) - 180);
                if (lastSent < -900 || d >= 2) { lastSent = a; GoAsync(a, null); }
            }, null, 900, Timeout.Infinite);
            else auto.Change(900, Timeout.Infinite);
        }

        // Azimut d'un contact : locator, à défaut centre du pays de l'indicatif ; NaN si impossible
        public static double AzimuthOf(Qso q)
        {
            double la1, lo1, la2, lo2; if (q == null || !Geo.MyPos(out la1, out lo1)) return double.NaN;
            if (!Geo.GridToLatLon(q.S("COL_GRIDSQUARE"), out la2, out lo2))
            {
                Country c = Countries.Find(q.S("COL_CALL")); if (c == null || (c.Lat == 0 && c.Lon == 0)) return double.NaN; la2 = c.Lat; lo2 = c.Lon;
            }
            return Math.Round(Geo.Bearing(la1, lo1, la2, lo2)) % 360;
        }
    }

    // Menu « Rotor » : modèle, liaison, azimut actuel, aller vers le contact, chemin long, arrêt
    public class RotorForm : Form
    {
        readonly Func<Qso> selected; ComboBox cModel, cPort, cBaud; TextBox tHost, tTport, tCap; CheckBox kAuto; Label lNote, lAz, lSt, lPort, lBaud, lHost, lTport; Button bSel, bLong;
        CalibForm calWin;
        readonly System.Windows.Forms.Timer poll = new System.Windows.Forms.Timer { Interval = 2000 }; bool busy; double lastAz = double.NaN, selAz = double.NaN;

        public RotorForm(Func<Qso> selected)
        {
            this.selected = selected;
            Text = Tr.T("Rotor d'antenne"); ClientSize = new Size(560, 400); StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; Font = Ui.Normal; Icon = Ui.AppIcon;
            Ui.Lbl(this, Tr.T("Rotor :"), 12, 12);
            List<string> names = new List<string>(); foreach (RotorModel m in Rotor.Models) names.Add(m.ToString());
            cModel = Ui.Combo(this, 100, 10, 448, names); cModel.DropDownStyle = ComboBoxStyle.DropDownList;
            lNote = new Label { Left = 100, Top = 40, Width = 448, Height = 34, Font = Ui.Normal }; Controls.Add(lNote);
            lPort = Ui.Lbl(this, Tr.T("Port COM :"), 12, 82); cPort = Ui.Combo(this, 100, 80, 110, SerialPort.GetPortNames(), true);
            lBaud = Ui.Lbl(this, Tr.T("Vitesse :"), 230, 82); cBaud = Ui.Combo(this, 300, 80, 90, new[] { "600", "1200", "2400", "4800", "9600", "19200", "38400", "57600", "115200" }, true);
            lHost = Ui.Lbl(this, Tr.T("Adresse :"), 12, 82); tHost = Ui.Txt(this, 100, 80, 200); lTport = Ui.Lbl(this, Tr.T("Port TCP :"), 320, 82); tTport = Ui.Txt(this, 400, 80, 70);
            kAuto = Ui.Chk(this, Tr.T("Suivre automatiquement le correspondant (fenêtre Add)"), 12, 116, 540);
            lAz = new Label { Left = 12, Top = 150, Width = 536, Height = 40, Font = new Font(Font.FontFamily, 18, FontStyle.Bold), Text = Tr.T("Azimut actuel : ---") }; Controls.Add(lAz);
            Ui.Lbl(this, Tr.T("Cap voulu (°) :"), 12, 206); tCap = Ui.Txt(this, 130, 204, 60); Ui.Btn(this, Tr.T("Aller"), 200, 202, 90, 28, (s, e) => { double v; if (double.TryParse(tCap.Text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out v)) Go(v); else St(Tr.T("Cap illisible.")); });
            bSel = Ui.Btn(this, "", 12, 244, 360, 30, (s, e) => { if (!double.IsNaN(selAz)) Go(selAz); }); bLong = Ui.Btn(this, Tr.T("Chemin long"), 382, 244, 166, 30, (s, e) => { if (!double.IsNaN(selAz)) Go(selAz + 180); });
            Button stop = Ui.Btn(this, "STOP", 12, 290, 150, 44, (s, e) => { Do(() => Rotor.StopSync(), Tr.T("Arrêt demandé.")); }); stop.Font = new Font(Font, FontStyle.Bold);
            Button bCal = Ui.Btn(this, Tr.T("Étalonnage"), 458, 296, 90, 28, (s, e) => { if (MessageBox.Show(this, Tr.T("Attention : l'étalonnage peut dérégler le calage de l'antenne.\r\nContinuer ?"), Tr.T("Étalonnage du rotor"), MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.OK) return; if (calWin == null || calWin.IsDisposed) { calWin = new CalibForm(); calWin.Show(this); } else calWin.Activate(); }); bCal.Tag = "red";
            ClientSize = new Size(560, 400);
            Ui.Btn(this, Tr.T("Fermer"), 458, 352, 90, 28, (s, e) => Close());
            lSt = new Label { Left = 12, Top = 356, Width = 430, Height = 36, Font = Ui.Normal }; Controls.Add(lSt);
            RotorModel cur = Rotor.Current; cModel.SelectedIndex = cur != null ? Rotor.Models.IndexOf(cur) : 0;
            cPort.Text = App.Cfg.Get("rotor.port", ""); cBaud.Text = App.Cfg.Get("rotor.baud", ""); tHost.Text = App.Cfg.Get("rotor.host", "127.0.0.1"); tTport.Text = App.Cfg.Get("rotor.tport", "4533"); kAuto.Checked = App.Cfg.Get("rotor.auto", "0") == "1";
            cModel.SelectedIndexChanged += delegate { ModelChanged(true); }; ModelChanged(false);
            cPort.TextChanged += delegate { Save(); }; cBaud.TextChanged += delegate { Save(); }; tHost.TextChanged += delegate { Save(); }; tTport.TextChanged += delegate { Save(); }; kAuto.CheckedChanged += delegate { Save(); };
            poll.Tick += delegate { Poll(); }; poll.Start(); Disposed += delegate { poll.Stop(); poll.Dispose(); }; Shown += delegate { Poll(); }; RefreshSel();
        }

        RotorModel Cur() { return Rotor.Models[Math.Max(0, cModel.SelectedIndex)]; }
        void ModelChanged(bool user)
        {
            RotorModel m = Cur(); bool net = m.Proto == RotorProto.Net;
            lPort.Visible = cPort.Visible = lBaud.Visible = cBaud.Visible = !net; lHost.Visible = tHost.Visible = lTport.Visible = tTport.Visible = net;
            if (user && !net) cBaud.Text = m.Baud.ToString(); else if (!net && cBaud.Text.Length == 0) cBaud.Text = m.Baud.ToString();
            lNote.Text = Tr.T(m.Note); if (user) Save();
        }
        void Save()
        {
            if (cModel.SelectedIndex < 0) return;
            App.Cfg.Set("rotor.model", Cur().ToString()); App.Cfg.Set("rotor.port", cPort.Text.Trim()); App.Cfg.Set("rotor.baud", cBaud.Text.Trim()); App.Cfg.Set("rotor.host", tHost.Text.Trim()); App.Cfg.Set("rotor.tport", tTport.Text.Trim()); App.Cfg.Set("rotor.auto", kAuto.Checked ? "1" : "0"); try { App.Cfg.Save(); } catch { }
        }
        void St(string t) { lSt.Text = t; }
        public void RefreshSel()
        {
            Qso q = selected != null ? selected() : null; selAz = Rotor.AzimuthOf(q);
            bSel.Text = double.IsNaN(selAz) ? Tr.T("Aller vers le contact sélectionné (azimut inconnu)") : Tr.T("Aller vers le contact sélectionné : ") + selAz.ToString("0") + "° (" + q.S("COL_CALL") + ")";
            bSel.Enabled = bLong.Enabled = !double.IsNaN(selAz);
        }
        void Go(double az) { RefreshSel(); Do(() => Rotor.GotoSync(az), Tr.T("Rotation vers ") + Rotor.Norm(az).ToString("0") + "°"); }
        void Do(Action job, string okMsg)
        {
            Save(); St("..."); Rotor.Run(job, err => { if (IsDisposed) return; BeginInvoke(new Action(() => St(err == null ? okMsg : Tr.T("Erreur : ") + err))); });
        }
        void Poll()
        {
            if (busy || !Visible || Rotor.Current == null) return; busy = true; RefreshSel();
            Rotor.Run(() => { double a; if (Rotor.ReadSync(out a)) lastAz = a; else lastAz = double.NaN; }, err => { busy = false; if (IsDisposed) return; BeginInvoke(new Action(() => { lAz.Text = err != null ? Tr.T("Azimut actuel : pas de réponse") : double.IsNaN(lastAz) ? Tr.T("Azimut actuel : pas de réponse") : Tr.T("Azimut actuel : ") + lastAz.ToString("0") + "°"; if (err != null) St(Tr.T("Erreur : ") + err); })); });
        }
    }


    // Fenêtre d'étalonnage (non bloquante) : décalage entre le cap indiqué par le rotor et le cap réel de l'antenne.
    // 1) valeur directe : on donne le cap réel de l'antenne ; 2) pas à pas : on propose un cap, on corrige avec − / + jusqu'à ce que l'antenne soit dessus, puis on valide.
    public class CalibForm : Form
    {
        TextBox tReal, tRef; Label lCal, lCmd, lSt; double refAz, cmd; bool started;
        public CalibForm()
        {
            Text = Tr.T("Étalonnage du rotor"); ClientSize = new Size(480, 440); StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; Font = Ui.Normal; Icon = Ui.AppIcon;
            lCal = new Label { Left = 12, Top = 10, Width = 456, Height = 24, Font = new Font(Font, FontStyle.Bold) }; Controls.Add(lCal);
            Ui.Lbl(this, Tr.T("A. Vous connaissez le cap réel de l'antenne"), 12, 44).Font = new Font(Font, FontStyle.Bold);
            Ui.Lbl(this, Tr.T("Position réelle de l'antenne (°) :"), 12, 74); tReal = Ui.Txt(this, 250, 72, 60);
            Ui.Btn(this, Tr.T("Valider"), 320, 68, 100, 28, (s, e) => Direct());
            Ui.Lbl(this, Tr.T("B. Pas à pas"), 12, 116).Font = new Font(Font, FontStyle.Bold);
            Label h = new Label { Left = 12, Top = 142, Width = 456, Height = 48, Font = Ui.Normal, Text = Tr.T("Choisissez un cap que vous savez repérer (le nord, une balise, un repère visible), envoyez le rotor dessus, corrigez avec − / + jusqu'à ce que l'antenne pointe vraiment ce cap, puis validez.") }; Controls.Add(h);
            Ui.Lbl(this, Tr.T("Cap de référence (°) :"), 12, 198); tRef = Ui.Txt(this, 170, 196, 60); tRef.Text = App.Cfg.Get("rotor.calref", "0");
            Ui.Btn(this, Tr.T("Envoyer le rotor sur ce cap"), 244, 192, 224, 28, (s, e) => Start());
            lCmd = new Label { Left = 12, Top = 234, Width = 456, Height = 28, Font = new Font(Font.FontFamily, 13, FontStyle.Bold), Text = Tr.T("Rotor commandé : ---") }; Controls.Add(lCmd);
            int[] st = { -5, -1, 1, 5 };
            for (int i = 0; i < 4; i++) { int d = st[i]; Ui.Btn(this, (d > 0 ? "+" : "−") + Math.Abs(d) + "°", 12 + i * 114, 270, 108, 36, (s, e) => Nudge(d)); }
            Ui.Btn(this, Tr.T("Valider : l'antenne est bien sur ce cap"), 12, 318, 320, 32, (s, e) => Validate());
            Ui.Btn(this, Tr.T("Remettre l'étalonnage à zéro"), 12, 360, 240, 30, (s, e) => { Rotor.SetOffset(0); ShowCal(); lSt.Text = Tr.T("Étalonnage remis à zéro."); });
            Ui.Btn(this, Tr.T("Fermer"), 378, 400, 90, 28, (s, e) => Close());
            lSt = new Label { Left = 12, Top = 398, Width = 360, Height = 34, Font = Ui.Normal }; Controls.Add(lSt); ShowCal();
        }
        void ShowCal() { double o = Rotor.Offset; lCal.Text = Tr.T("Décalage actuel : ") + (o > 0 ? "+" : "") + o.ToString("0.#", CultureInfo.InvariantCulture) + "°"; }
        static bool Num(string s, out double v) { return double.TryParse((s ?? "").Replace(',', '.').Replace("°", "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v) && v >= 0 && v <= 360; }
        void Err(string e) { if (IsDisposed) return; BeginInvoke(new Action(() => { ShowCal(); lSt.Text = e == null ? Tr.T("Étalonnage enregistré.") : Tr.T("Erreur : ") + e; })); }
        // Valeur directe : décalage = valeur donnée − indication du rotor
        void Direct()
        {
            double t; if (!Num(tReal.Text, out t)) { lSt.Text = Tr.T("Cap illisible."); return; } lSt.Text = "...";
            Rotor.Run(() => { double r; if (!Rotor.ReadSync(out r, true)) throw new IOException(Tr.T("Lecture de la position du rotor impossible.")); Rotor.SetOffset(t - r); }, Err);
        }
        bool Ref() { if (!Num(tRef.Text, out refAz)) { lSt.Text = Tr.T("Cap illisible."); return false; } App.Cfg.Set("rotor.calref", tRef.Text.Trim()); try { App.Cfg.Save(); } catch { } return true; }
        void Send(double raw)
        {
            cmd = Rotor.Norm(raw); lCmd.Text = Tr.T("Rotor commandé : ") + cmd.ToString("0") + "°"; lSt.Text = "...";
            Rotor.Run(() => Rotor.GotoSync(cmd, true), e => { if (IsDisposed) return; BeginInvoke(new Action(() => lSt.Text = e == null ? "" : Tr.T("Erreur : ") + e)); });
        }
        void Start() { if (!Ref()) return; started = true; Send(refAz - Rotor.Offset); }   // le rotor va là où il enverrait l'antenne avec l'étalonnage actuel
        void Nudge(int d) { if (!started) { lSt.Text = Tr.T("Envoyez d'abord le rotor sur le cap de référence."); return; } Send(cmd + d); }
        void Validate()
        {
            if (!started) { lSt.Text = Tr.T("Envoyez d'abord le rotor sur le cap de référence."); return; }
            if (!Ref()) return; Rotor.SetOffset(refAz - cmd); ShowCal(); lSt.Text = Tr.T("Étalonnage enregistré.");
        }
    }
}
