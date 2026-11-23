using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

namespace HamRadioLink.Journal
{
    // Un port d'écoute des QSO : clé du réglage, texte, valeur par défaut (mêmes ports que le logiciel remplacé), exemple.
    public class PortDef
    {
        public string Key, Label, Example; public int Default;
        public PortDef(string k, string l, int d, string e) { Key = k; Label = l; Default = d; Example = e; }
        public int Value { get { return HamRadioLink.Cfg.GetInt("jn.port." + Key, Default); } set { HamRadioLink.Cfg.Set("jn.port." + Key, value.ToString()); } }
    }

    // Reçoit les QSO enregistrés par WSJT-X et les range dans le journal. Reconnaît seul chaque format :
    //  - message UDP WSJT-X « Logged ADIF » (type 12) ;  - message XML « contactinfo » (diffusion N1MM de WSJT-X) ;  - texte ADIF brut.
    public static class WsjtListener
    {
        public static readonly PortDef[] Ports = {
            new PortDef("wsjt", Tr.T("Serveur UDP de WSJT-X (Réglages > Rapports)"), 2237, Tr.T("réglage Serveur UDP de WSJT-X ; ex. 2237")),
            new PortDef("n1mm", Tr.T("Diffusion de WSJT-X (option « N1MM Logger+ »)"), 2333, Tr.T("à saisir aussi dans WSJT-X, Réglages > Rapports, case N1MM")),
            new PortDef("adif", Tr.T("ADIF brut en UDP"), 7825, "ex. 7825, 12060"),
            new PortDef("x1", Tr.T("Port libre n° 1 (0 = inutilisé)"), 0, Tr.T("ex. 2238 pour une seconde instance de WSJT-X")),
            new PortDef("x2", Tr.T("Port libre n° 2 (0 = inutilisé)"), 0, Tr.T("ex. 2239 pour une troisième instance de WSJT-X")) };

        static readonly List<UdpClient> socks = new List<UdpClient>(); static SynchronizationContext ui; static volatile bool run;
        public static bool Disabled; public static int Received = 0; public static string Error = ""; public static List<int> Open = new List<int>();

        public static void Start()
        {
            if (run || Disabled) return;
            ui = SynchronizationContext.Current; Error = ""; Open.Clear(); run = true;
            foreach (PortDef p in Ports)
            {
                int port = p.Value; if (port <= 0 || Open.Contains(port)) continue;
                try
                {
                    UdpClient u = new UdpClient(); u.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    u.Client.Bind(new IPEndPoint(IPAddress.Any, port)); socks.Add(u); Open.Add(port);
                    Thread th = new Thread(delegate() { Loop(u); }) { IsBackground = true, Name = "qso-udp-" + port }; th.Start();
                }
                catch (Exception ex) { Error += Tr.T("port UDP ") + port + " : " + ex.Message + "\r\n"; }
            }
        }

        public static void Stop() { run = false; foreach (UdpClient u in socks) { try { u.Close(); } catch { } } socks.Clear(); Open.Clear(); }
        public static void Restart() { Stop(); Start(); }

        static void Loop(UdpClient u)
        {
            while (run)
            {
                try
                {
                    IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0); byte[] d = u.Receive(ref ep);
                    string adif = Decode(d); if (adif == null) continue;
                    Received++;
                    if (ui != null) ui.Post(delegate { Store(adif); }, null); else Store(adif);
                }
                catch { if (!run) return; Thread.Sleep(200); }
            }
        }

        static uint U32(byte[] d, int o) { return (uint)(d[o] << 24 | d[o + 1] << 16 | d[o + 2] << 8 | d[o + 3]); }

        // Datagramme -> texte ADIF (ou null si ce n'est pas un QSO).
        public static string Decode(byte[] d)
        {
            try
            {
                if (d.Length >= 16 && U32(d, 0) == 0xadbccbda)
                {
                    if (U32(d, 8) != 12) return null;      // schéma WSJT-X (Qt, grand-boutiste) : magic, schéma, type, id, puis le texte ADIF
                    int o = 12; uint n = U32(d, o); o += 4; if (n != 0xFFFFFFFF) o += (int)n;
                    uint m = U32(d, o); o += 4; if (m == 0xFFFFFFFF || o + m > d.Length) return null;
                    return Encoding.UTF8.GetString(d, o, (int)m);
                }
                string t = Encoding.UTF8.GetString(d);
                if (t.IndexOf("<contactinfo", StringComparison.OrdinalIgnoreCase) >= 0) return FromN1mm(t);
                if (t.IndexOf("<eor>", StringComparison.OrdinalIgnoreCase) >= 0 && t.IndexOf("<call:", StringComparison.OrdinalIgnoreCase) >= 0) return t;
            }
            catch { }
            return null;
        }

        static void Tag(StringBuilder sb, string name, string v) { if (!string.IsNullOrEmpty(v)) sb.Append("<" + name + ":" + Encoding.UTF8.GetByteCount(v) + ">" + v + " "); }

        // XML N1MM « contactinfo » -> ADIF. rxfreq est en dizaines de Hz (1407400 = 14,074 MHz).
        static string FromN1mm(string xml)
        {
            XmlDocument doc = new XmlDocument(); doc.LoadXml(xml);
            Func<string, string> g = delegate(string n) { XmlNode x = doc.SelectSingleNode("//" + n); return x == null ? "" : x.InnerText.Trim(); };
            string call = g("call"); if (call.Length == 0) return null;
            DateTime ts; if (!DateTime.TryParse(g("timestamp"), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out ts)) ts = DateTime.UtcNow;
            double hz; long f = 0; if (double.TryParse(g("rxfreq"), NumberStyles.Float, CultureInfo.InvariantCulture, out hz)) f = (long)Math.Round(hz) * 10;
            StringBuilder sb = new StringBuilder("<ADIF_VER:5>3.1.0 <EOH>\n");
            Tag(sb, "CALL", call); Tag(sb, "QSO_DATE", ts.ToString("yyyyMMdd")); Tag(sb, "TIME_ON", ts.ToString("HHmmss"));
            if (f > 0) { Tag(sb, "FREQ", (f / 1e6).ToString("0.000000", CultureInfo.InvariantCulture)); Tag(sb, "BAND", BandOf(f)); }
            Tag(sb, "MODE", g("mode")); Tag(sb, "RST_SENT", g("snt")); Tag(sb, "RST_RCVD", g("rcv")); Tag(sb, "GRIDSQUARE", g("gridsquare"));
            Tag(sb, "NAME", g("name")); Tag(sb, "COMMENT", g("comment")); Tag(sb, "STATION_CALLSIGN", g("mycall"));
            sb.Append("<EOR>\n"); return sb.ToString();
        }

        public static readonly double[][] Bands = { new[] { 1.8, 2.0 }, new[] { 3.5, 4.0 }, new[] { 5.3, 5.5 }, new[] { 7.0, 7.3 }, new[] { 10.1, 10.15 }, new[] { 14.0, 14.35 }, new[] { 18.068, 18.168 }, new[] { 21.0, 21.45 }, new[] { 24.89, 24.99 }, new[] { 28.0, 29.7 }, new[] { 50.0, 54.0 }, new[] { 144.0, 148.0 }, new[] { 430.0, 440.0 } };
        public static readonly string[] BandNames = { "160m", "80m", "60m", "40m", "30m", "20m", "17m", "15m", "12m", "10m", "6m", "2m", "70cm" };
        public static string BandOf(long hz) { double m = hz / 1e6; for (int i = 0; i < Bands.Length; i++) if (m >= Bands[i][0] && m <= Bands[i][1]) return BandNames[i]; return ""; }

        // Dans le fil de la fenêtre principale (la connexion à la base n'est utilisée que là).
        static void Store(string adif)
        {
            try
            {
                if (App.Log == null && !Hub.Connect(null)) return;
                string want = HamRadioLink.Cfg.Get("wsjt.log", ""); LogRef dest = null;   // journal de destination choisi (vide = journal actif)
                if (want.Length > 0 && App.Logs != null) { dest = App.Logs.Find(l => l.Name == want); if (dest == App.CurrentLog) dest = null; }
                if (dest != null)
                {   // autre journal que celui affiché : connexion séparée, le journal affiché ne change pas
                    string h, u, p; int pt; App.ServerOf(dest, out h, out pt, out u, out p);
                    using (MySqlClient db2 = new MySqlClient(h, pt, u, p, dest.Schema)) { db2.Open(); int a2, d2; AdifImport.Text(new PeerLog(db2, dest.Schema), adif, out a2, out d2); }
                    return;
                }
                int a, du; AdifImport.Text(App.Log, adif, out a, out du);
                Hub.Refresh();
            }
            catch (Exception ex) { Error = ex.Message; }
        }
    }

    // « Réglages des ports » : tous les ports de réception, leurs valeurs par défaut et un exemple.
    public class PortsForm : Form
    {
        TextBox[] tb = new TextBox[WsjtListener.Ports.Length]; Label status; ComboBox destCb;
        public PortsForm()
        {
            Text = Tr.T("Réglages des ports"); ClientSize = new Size(880, 470); StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; Font = Ui.Normal; Icon = Ui.AppIcon;
            int y = 12; Ui.Lbl(this, Tr.T("Ports UDP écoutés par HamRadioLink pour recevoir les QSO de WSJT-X (0 = inutilisé)."), 12, y); Controls[Controls.Count - 1].Width = 856; Controls[Controls.Count - 1].Height = 34; y += 40;
            for (int i = 0; i < WsjtListener.Ports.Length; i++)
            {
                PortDef p = WsjtListener.Ports[i]; Ui.Lbl(this, p.Label, 12, y); tb[i] = Ui.Txt(this, 330, y - 2, 70); tb[i].Text = p.Value.ToString();
                Label ex = Ui.Lbl(this, Tr.T("Défaut ") + p.Default + " — " + p.Example, 410, y); ex.Width = 460; ex.ForeColor = SystemColors.GrayText; y += 32;
            }
            y += 8; Ui.Lbl(this, Tr.T("Autres ports (réglés ailleurs) :"), 12, y); y += 24;
            string[] info = { Tr.T("Serveur de commande IP (WSJT-X, SDRLink…) : 7809 — bouton Serveur de la fenêtre principale"), Tr.T("Base MariaDB installée : 3306 — bouton Base… du journal"), Tr.T("MariaDB intégré à HamRadioLink : 3307 (ou le premier libre au-dessus)") };
            foreach (string s in info) { Label l = Ui.Lbl(this, s, 28, y); l.Width = 840; y += 22; }
            y += 6; Ui.Lbl(this, Tr.T("Journal de destination des QSO de WSJT-X :"), 12, y + 3, 310);
            List<string> dn = new List<string>(); dn.Add(Tr.T("Journal actif (celui affiché)")); if (App.Logs != null) foreach (LogRef l in App.Logs) dn.Add(l.Name);
            destCb = Ui.Combo(this, 330, y, 330, dn); destCb.DropDownStyle = ComboBoxStyle.DropDownList; { string w = HamRadioLink.Cfg.Get("wsjt.log", ""); int ix = w.Length == 0 ? 0 : dn.IndexOf(w); destCb.SelectedIndex = ix < 0 ? 0 : ix; }
            y += 32;
            status = Ui.Lbl(this, "", 12, y + 6); status.Width = 856; status.Height = 40;
            Ui.Btn(this, Tr.T("Appliquer"), 660, 470 - 40, 100, 28, (s, e) => Apply()); Ui.Btn(this, Tr.T("Fermer"), 768, 470 - 40, 100, 28, (s, e) => Close());
            Ui.Btn(this, Tr.T("Valeurs par défaut"), 12, 470 - 40, 140, 28, (s, e) => { for (int i = 0; i < tb.Length; i++) tb[i].Text = WsjtListener.Ports[i].Default.ToString(); });
            ShowState();
        }
        void ShowState() { status.Text = WsjtListener.Open.Count == 0 ? Tr.T("Aucun port ouvert.") + (WsjtListener.Error.Length > 0 ? "\r\n" + WsjtListener.Error : "") : Tr.T("Ports ouverts : ") + string.Join(", ", WsjtListener.Open.ConvertAll(x => x.ToString()).ToArray()) + (WsjtListener.Error.Length > 0 ? "\r\nOccupés : " + WsjtListener.Error.Replace("\r\n", " ; ") : ""); }
        void Apply()
        {
            for (int i = 0; i < tb.Length; i++) { int v; if (!int.TryParse(tb[i].Text.Trim(), out v) || v < 0 || v > 65535) { MessageBox.Show(this, Tr.T("Port invalide : ") + WsjtListener.Ports[i].Label); return; } WsjtListener.Ports[i].Value = v; }
            HamRadioLink.Cfg.Set("wsjt.log", destCb.SelectedIndex <= 0 ? "" : (string)destCb.SelectedItem);
            HamRadioLink.Cfg.Save(); WsjtListener.Restart(); ShowState();
        }
    }
}
