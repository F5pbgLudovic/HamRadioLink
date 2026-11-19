using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace HamRadioLink.Journal
{
    // Barres d'outils / menus qui réagissent dès le premier clic (sans clic d'activation de la fenêtre).
    public class ClickToolStrip : ToolStrip { protected override void WndProc(ref Message m) { base.WndProc(ref m); if (m.Msg == 0x21 && m.Result == (IntPtr)2) m.Result = (IntPtr)1; } }
    public class ClickMenuStrip : MenuStrip { protected override void WndProc(ref Message m) { base.WndProc(ref m); if (m.Msg == 0x21 && m.Result == (IntPtr)2) m.Result = (IntPtr)1; } }

    public class ClusterDef
    {
        public string Name = "", Host = "", Login = "", Pass = ""; public int Port = 7300;
        public override string ToString() { return Name + "  (" + Host + ":" + Port + ")"; }
    }

    public class Spot { public string Call, Spotter, Comment, Band, Time; public double Khz; }

    // Client DX Cluster (telnet) : liste des clusters enregistrés, connexion, spots, trames brutes.
    public static class DxCluster
    {
        public static readonly List<Spot> Spots = new List<Spot>();
        public static readonly List<string> Frames = new List<string>();
        public static string Status = "Déconnecté"; public static ClusterDef Current;
        public static event Action Changed; public static event Action<Spot> SpotClicked;   // clic sur un indicatif de la réglette : la fenêtre « Ajouter » ouverte le reprend
        public static void RaiseSpotClicked(Spot s) { Action<Spot> h = SpotClicked; if (h != null) h(s); }
        static SynchronizationContext ui; static volatile bool run; static TcpClient tcp; static int gen;

        public static bool ShowStrip { get { return App.Cfg.Get("cl.strip", "0") == "1"; } set { Set("cl.strip", value); } }
        public static bool ShowFrames { get { return App.Cfg.Get("cl.frames", "0") == "1"; } set { Set("cl.frames", value); } }
        public static bool AutoConnect { get { return App.Cfg.Get("cl.auto", "0") == "1"; } set { Set("cl.auto", value); } }
        public static bool AutoReconnect { get { return App.Cfg.Get("cl.reco", "0") == "1"; } set { Set("cl.reco", value); } }
        public static bool ShowVfoA { get { return App.Cfg.Get("cl.vfoa", "1") == "1"; } set { Set("cl.vfoa", value); } }
        public static bool ShowVfoB { get { return App.Cfg.Get("cl.vfob", "0") == "1"; } set { Set("cl.vfob", value); } }
        // Filtres : 0 numérique, 1 CW, 2 phonie, 3 FreeDV (tout coché par défaut)
        public static bool Shows(int n) { return App.Cfg.Get("cl.f" + n, "1") == "1"; }
        public static void SetShows(int n, bool v) { Set("cl.f" + n, v); Hub.ClusterChanged(); }
        static readonly double[] Ft = { 1840, 1842, 3573, 3575, 5357, 7047.5, 7074, 10136, 10140, 14074, 14080, 18100, 18104, 21074, 21140, 24915, 24919, 28074, 28180, 50313, 50318, 144174 };
        static readonly double[][] CwBand = { new[] { 1810.0, 1838 }, new[] { 3500.0, 3570 }, new[] { 7000.0, 7040 }, new[] { 10100.0, 10130 }, new[] { 14000.0, 14070 }, new[] { 18068.0, 18095 }, new[] { 21000.0, 21070 }, new[] { 24890.0, 24915 }, new[] { 28000.0, 28070 } };
        public static int KindOf(Spot s)
        {
            string c = (s.Comment ?? "").ToUpperInvariant();
            if (c.Contains("FREEDV") || c.Contains("RADE")) return 3;
            if (c.Contains("FT8") || c.Contains("FT4") || c.Contains("FT2") || c.Contains("RTTY") || c.Contains("PSK") || c.Contains("JS8") || c.Contains("JT65") || c.Contains("JT9") || c.Contains("MFSK")) return 0;
            if (Regex.IsMatch(c, @"\bCW\b")) return 1;
            if (c.Contains("SSB") || c.Contains("LSB") || c.Contains("USB") || Regex.IsMatch(c, @"\b(AM|FM)\b")) return 2;
            if (Math.Abs(s.Khz - 14236) < 2 || Math.Abs(s.Khz - 7177) < 2) return 3;
            foreach (double f in Ft) if (Math.Abs(s.Khz - f) < 3) return 0;
            foreach (double[] r in CwBand) if (s.Khz >= r[0] && s.Khz <= r[1]) return 1;
            return 2;
        }
        public static bool Allowed(Spot s) { return Shows(KindOf(s)); }
        // Modes à essayer sur le poste (le premier existant dans sa liste est retenu).
        public static string ModeCandidates(Spot s)
        {
            int k = KindOf(s); string c = (s.Comment ?? "").ToUpperInvariant();
            if (k == 1) return "CW";
            if (k == 0) return c.Contains("RTTY") ? "RTTY|RTTY-L|DATA-LSB|LSB" : "DATA-USB|USB-D|PKT-USB|USB";
            if (c.Contains("FM")) return "FM"; if (Regex.IsMatch(c, @"\bAM\b")) return "AM";
            if (c.Contains("LSB")) return "LSB"; if (c.Contains("USB")) return "USB";
            return s.Khz < 10000 ? "LSB" : "USB";
        }
        // Alertes : lignes « ou|texte » / « et|texte » (la 1re ligne n'a pas d'opérateur). Évaluées de haut en bas ; bip quand un spot en direct correspond.
        public class AlertLine { public string Op = "ou", Text = ""; }
        public static bool AlertsOn { get { return App.Cfg.Get("cl.alert", "1") == "1"; } set { Set("cl.alert", value); } }
        public static List<AlertLine> LoadAlerts()
        {
            List<AlertLine> l = new List<AlertLine>();
            foreach (string e in App.Cfg.Get("cl.alerts", "").Split(new[] { ";;" }, StringSplitOptions.RemoveEmptyEntries)) { int i = e.IndexOf('|'); if (i < 0) continue; l.Add(new AlertLine { Op = e.Substring(0, i) == "et" ? "et" : "ou", Text = e.Substring(i + 1) }); }
            return l;
        }
        public static void SaveAlerts(List<AlertLine> l)
        {
            List<string> s = new List<string>(); foreach (AlertLine a in l) if (a.Text.Trim().Length > 0) s.Add(a.Op + "|" + a.Text.Replace(";;", " ").Replace("\r", " ").Replace("\n", " "));
            App.Cfg.Set("cl.alerts", string.Join(";;", s.ToArray())); HamRadioLink.Cfg.Save();
        }
        public static bool Matches(Spot sp, List<AlertLine> l)
        {
            string hay = (sp.Call + " " + sp.Comment + " " + sp.Khz.ToString("0.0", CultureInfo.InvariantCulture)).ToUpperInvariant(); bool? r = null;
            foreach (AlertLine a in l)
            {
                string t = a.Text.Trim().ToUpperInvariant(); if (t.Length == 0) continue;
                bool m = hay.Contains(t); if (r == null) r = m; else r = a.Op == "et" ? (r.Value && m) : (r.Value || m);
            }
            return r == true;
        }
        public static void Beep() { ThreadPool.QueueUserWorkItem(delegate { try { Console.Beep(1100, 180); Console.Beep(1400, 180); } catch { } }); }
        static void Set(string k, bool v) { App.Cfg.Set(k, v ? "1" : "0"); HamRadioLink.Cfg.Save(); }

        // Liste : « nom|hôte|port|login|mot de passe » séparés par ;;
        public static List<ClusterDef> Load()
        {
            List<ClusterDef> l = new List<ClusterDef>();
            foreach (string e in App.Cfg.Get("cl.list", "").Split(new[] { ";;" }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] f = e.Split('|'); if (f.Length < 3) continue; int p; int.TryParse(f[2], out p);
                l.Add(new ClusterDef { Name = f[0], Host = f[1], Port = p > 0 ? p : 7300, Login = f.Length > 3 ? f[3] : "", Pass = f.Length > 4 ? f[4] : "" });
            }
            return l;
        }
        public static void Save(List<ClusterDef> l)
        {
            List<string> s = new List<string>(); foreach (ClusterDef c in l) s.Add(c.Name.Replace("|", " ") + "|" + c.Host + "|" + c.Port + "|" + c.Login + "|" + c.Pass);
            App.Cfg.Set("cl.list", string.Join(";;", s.ToArray())); HamRadioLink.Cfg.Save();
        }

        static void Raise() { Action a = Changed; if (a == null) return; if (ui != null) ui.Post(delegate { a(); }, null); else a(); }

        public static void AutoStart()
        {
            ui = SynchronizationContext.Current ?? ui;
            if (!AutoConnect || run) return;
            List<ClusterDef> l = Load(); string sel = App.Cfg.Get("cl.sel", ""); ClusterDef d = l.Find(x => x.Name == sel) ?? (l.Count > 0 ? l[0] : null);
            if (d != null) Connect(d);
        }

        public static bool Connected { get { return run; } }

        public static void Connect(ClusterDef d)
        {
            Disconnect(); ui = SynchronizationContext.Current ?? ui; Current = d; run = true; int my = ++gen; App.Cfg.Set("cl.sel", d.Name);
            Status = "Connexion à " + d.Host + ":" + d.Port + "…"; Raise();
            Thread th = new Thread(delegate() { Loop(d, my); }) { IsBackground = true, Name = "dx-cluster" }; th.Start();
        }

        public static void Disconnect() { run = false; gen++; try { if (tcp != null) tcp.Close(); } catch { } tcp = null; Status = "Déconnecté"; Raise(); }

        static void Loop(ClusterDef d, int my)
        {
            while (run && my == gen)
            {
                try
                {
                    TcpClient c = new TcpClient(); c.Connect(d.Host, d.Port); tcp = c; Status = "Connecté à " + d.Name; Raise();
                    Stream s = c.GetStream(); byte[] buf = new byte[4096]; StringBuilder line = new StringBuilder(); bool sentL = false, sentP = false;
                    string login = d.Login.Length > 0 ? d.Login : App.Cfg.Get("me.call", "");
                    ThreadPool.QueueUserWorkItem(delegate { try { Thread.Sleep(4000); if (run && my == gen) Send(s, "sh/dx 30"); } catch { } });   // derniers spots, puis le flux continu
                    while (run && my == gen)
                    {
                        int n = s.Read(buf, 0, buf.Length); if (n <= 0) break;
                        for (int i = 0; i < n; i++)
                        {
                            byte b = buf[i];
                            if (b == 255) { i += 2; continue; }       // négociation telnet ignorée
                            if (b == '\n') { Line(line.ToString().TrimEnd('\r')); line.Length = 0; } else if (b != '\r') line.Append((char)b);
                        }
                        string p = line.ToString().ToLowerInvariant();
                        if (!sentL && login.Length > 0 && (p.TrimEnd().EndsWith("login:") || p.TrimEnd().EndsWith("call:") || p.TrimEnd().EndsWith("callsign:") || p.TrimEnd().EndsWith("call :"))) { Send(s, login); sentL = true; Line(line.ToString()); line.Length = 0; }
                        else if (!sentP && d.Pass.Length > 0 && p.EndsWith("password:")) { Send(s, d.Pass); sentP = true; Line(line.ToString()); line.Length = 0; }
                    }
                }
                catch (Exception ex) { Status = "Erreur : " + ex.Message; Raise(); }
                try { if (tcp != null) tcp.Close(); } catch { }
                if (!run || my != gen) return;
                if (!AutoReconnect) { run = false; Status = "Déconnecté"; Raise(); return; }
                Status = "Reconnexion dans 15 s…"; Raise();
                for (int i = 0; i < 15 && run && my == gen; i++) Thread.Sleep(1000);
            }
        }

        static void Send(Stream s, string t) { byte[] b = Encoding.ASCII.GetBytes(t + "\r\n"); s.Write(b, 0, b.Length); }

        static readonly Regex Rx = new Regex(@"^DX de ([^:\s]+):\s+([\d.]+)\s+(\S+)\s+(.*?)\s*(\d{4})Z", RegexOptions.IgnoreCase);

        // Réponse à sh/dx :   14074.0  DL1XX  19-Nov-2026 0830Z  commentaire <F5ABC>
        static readonly Regex Rx2 = new Regex(@"^\s*([\d.]+)\s+(\S+)\s+\d+-\w+-\d+\s+(\d{4})Z\s*(.*?)\s*<([^>]+)>", RegexOptions.IgnoreCase);

        static void Line(string t)
        {
            if (t.Length == 0) return;
            lock (Frames) { Frames.Add(t); if (Frames.Count > 300) Frames.RemoveAt(0); }
            Match m = Rx.Match(t); Spot sp = null; double k;
            bool live = false;
            if (m.Success && double.TryParse(m.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out k))
            { sp = new Spot { Spotter = m.Groups[1].Value, Khz = k, Call = m.Groups[3].Value, Comment = m.Groups[4].Value, Time = m.Groups[5].Value }; live = true; }
            else { m = Rx2.Match(t); if (m.Success && double.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out k)) sp = new Spot { Khz = k, Call = m.Groups[2].Value, Time = m.Groups[3].Value, Comment = m.Groups[4].Value, Spotter = m.Groups[5].Value }; }
            if (sp != null)
            {
                sp.Band = WsjtListener.BandOf((long)(sp.Khz * 1000));
                if (live && AlertsOn) { try { if (Matches(sp, LoadAlerts())) Beep(); } catch { } }
                lock (Spots) { Spots.RemoveAll(x => x.Call == sp.Call && Math.Abs(x.Khz - sp.Khz) < 0.5); Spots.Insert(0, sp); if (Spots.Count > 400) Spots.RemoveAt(Spots.Count - 1); }
            }
            Raise();
        }
    }

    // Réglette verticale à droite sur toute la hauteur : échelle de la bande (haut = bas de bande), indicatifs du cluster,
    // curseurs du VFO A (rouge) et du VFO B (bleu). Automatique = bande du VFO A, ou bande choisie à la main.
    public class ClusterStrip : Panel
    {
        readonly ComboBox band; readonly Ruler ruler; readonly Label state;
        public ClusterStrip()
        {
            Dock = DockStyle.Right; Width = 200; Font = Ui.Normal;
            band = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList }; band.Items.Add("Auto (bande du poste)"); foreach (string s in WsjtListener.BandNames) band.Items.Add(s); band.SelectedIndex = 0;
            band.SelectedIndexChanged += delegate { Fill(); };
            state = new Label { Dock = DockStyle.Bottom, Height = 34, ForeColor = SystemColors.GrayText };
            ruler = new Ruler { Dock = DockStyle.Fill };
            Controls.Add(ruler); Controls.Add(band); Controls.Add(state);
            DxCluster.Changed += Fill; Disposed += delegate { DxCluster.Changed -= Fill; };
            System.Windows.Forms.Timer t = new System.Windows.Forms.Timer { Interval = 700 }; t.Tick += delegate { Fill(); }; t.Start(); Disposed += delegate { t.Dispose(); };
            Fill();
        }
        // Bande affichée : choisie, sinon celle du VFO A, sinon (poste non connecté) la dernière connue ou 20m.
        static string last = "20m";
        string CurBand() { if (band.SelectedIndex > 0) return (string)band.SelectedItem; string b = RigState.Hz > 0 ? WsjtListener.BandOf(RigState.Hz) : ""; if (b.Length > 0) last = b; return last; }
        void Fill()
        {
            string b = CurBand(); int bi = Array.IndexOf(WsjtListener.BandNames, b); if (bi < 0) return;
            state.Text = DxCluster.Status + (band.SelectedIndex == 0 ? "\r\nBande : " + b + (RigState.Hz > 0 ? "" : " (poste non connecté)") : "");
            List<Spot> l = new List<Spot>(); lock (DxCluster.Spots) foreach (Spot s in DxCluster.Spots) if (s.Band == b && DxCluster.Allowed(s)) l.Add(s);
            ruler.Set(WsjtListener.Bands[bi][0] * 1000, WsjtListener.Bands[bi][1] * 1000, l, DxCluster.ShowVfoA ? RigState.Hz / 1000.0 : 0, DxCluster.ShowVfoB ? RigState.HzB / 1000.0 : 0);
        }
    }

    // Échelle verticale : molette = zoom (le texte garde sa taille, seule l'échelle s'étire), glisser = déplacer, clic sur un indicatif = régler le poste,
    // clic droit = filtres de modes.
    class Ruler : Control
    {
        double lo, hi, vlo, vhi, a, bb; List<Spot> spots = new List<Spot>(); string key = ""; readonly List<KeyValuePair<RectangleF, Spot>> hits = new List<KeyValuePair<RectangleF, Spot>>();
        bool drag; int dragY; double dragLo; bool moved;
        public Ruler() { DoubleBuffered = true; Font = Ui.Mono; ResizeRedraw = true; Theme.Changed += Redo; Disposed += delegate { Theme.Changed -= Redo; }; }
        void Redo() { Invalidate(); }
        public void Set(double lo, double hi, List<Spot> s, double a, double b)
        {
            if (lo != this.lo || hi != this.hi) { this.lo = lo; this.hi = hi; vlo = lo; vhi = hi; }
            string k = a + "|" + b + "|" + s.Count + (s.Count > 0 ? s[0].Call + s[0].Khz : "") + "|" + vlo + "|" + vhi; if (k == key) return; key = k;
            this.a = a; this.bb = b; spots = s; Invalidate();
        }
        float Y(double khz) { return (float)((khz - vlo) / (vhi - vlo) * (Height - 20)) + 10; }
        double F(float y) { return vlo + (y - 10) / (Height - 20) * (vhi - vlo); }
        void View(double nlo, double span) { span = Math.Max((hi - lo) / 40, Math.Min(hi - lo, span)); nlo = Math.Max(lo, Math.Min(hi - span, nlo)); vlo = nlo; vhi = nlo + span; key = ""; Invalidate(); }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (hi <= lo) return; double f = F(e.Y), span = vhi - vlo, ns = span * (e.Delta > 0 ? 0.8 : 1.25), frac = (f - vlo) / span; View(f - frac * ns, ns);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right) { Menu(e.Location); return; }
            drag = true; moved = false; dragY = e.Y; dragLo = vlo;
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (drag && Math.Abs(e.Y - dragY) > 3) moved = true;
            if (drag && moved) { double span = vhi - vlo; View(dragLo - (e.Y - dragY) / (double)(Height - 20) * span, span); return; }
            Cursor = HitAt(e.Location) != null ? Cursors.Hand : Cursors.Default;
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            bool was = drag && !moved; drag = false; if (!was || e.Button != MouseButtons.Left) return;
            Spot s = HitAt(e.Location); if (s == null) return; if (RigState.TuneFn != null) RigState.TuneFn((long)Math.Round(s.Khz * 1000), DxCluster.ModeCandidates(s)); DxCluster.RaiseSpotClicked(s);
        }
        Spot HitAt(Point p) { foreach (KeyValuePair<RectangleF, Spot> h in hits) if (h.Key.Contains(p)) return h.Value; return null; }
        void Menu(Point p)
        {
            ContextMenuStrip m = new ContextMenuStrip();
            string[] t = { "Afficher les contacts numériques (FT8, FT4, FT2, RTTY)", "Afficher les contacts CW (morse)", "Afficher les contacts phonie (LSB, USB, SSB, AM, FM)", "Afficher les contacts phonie FreeDV" };
            for (int i = 0; i < 4; i++) { int n = i; ToolStripMenuItem mi = new ToolStripMenuItem(t[i]) { Checked = DxCluster.Shows(n), CheckOnClick = true }; mi.Click += delegate { DxCluster.SetShows(n, mi.Checked); }; m.Items.Add(mi); }
            m.Items.Add(new ToolStripSeparator()); m.Items.Add("Zoom : tout voir", null, delegate { View(lo, hi - lo); });
            m.Show(this, p);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics; g.Clear(Theme.Get("lb.grid.back")); hits.Clear(); if (hi <= lo) return;
            using (Pen axis = new Pen(Theme.Mix(Theme.Get("lb.tool.text"), Theme.Get("lb.grid.back"), 0.5))) using (Brush txt = new SolidBrush(Theme.Get("lb.tool.text"))) using (Brush spot = new SolidBrush(Theme.Get("lb.spot")))
            {
                int x0 = 52; g.DrawLine(axis, x0, 10, x0, Height - 10);
                double span = vhi - vlo, step = span > 1000 ? 100 : span > 300 ? 50 : span > 100 ? 10 : span > 40 ? 5 : span > 15 ? 1 : 0.5;
                for (double f = Math.Ceiling(vlo / step) * step; f <= vhi; f += step) { float y = Y(f); g.DrawLine(axis, x0 - 5, y, x0, y); g.DrawString(f.ToString(step < 1 ? "0.0" : "0", CultureInfo.InvariantCulture), Font, txt, 0, y - 7); }
                Cursor1(g, a, Color.Red, "A"); Cursor1(g, bb, Color.DeepSkyBlue, "B");
                List<Spot> o = new List<Spot>(spots); o.Sort((p, q) => p.Khz.CompareTo(q.Khz)); float prev = -100;
                foreach (Spot s in o)
                {
                    if (s.Khz < vlo || s.Khz > vhi) continue; float y = Y(s.Khz), ty = Math.Max(y - 7, prev + 13); prev = ty;
                    g.DrawLine(axis, x0, y, x0 + 8, y); g.DrawLine(axis, x0 + 8, y, x0 + 14, ty + 7); g.DrawString(s.Call, Font, spot, x0 + 15, ty);
                    hits.Add(new KeyValuePair<RectangleF, Spot>(new RectangleF(x0 + 8, ty, Width - x0 - 8, 13), s));
                }

            }
        }
        void Cursor1(Graphics g, double khz, Color c, string n)
        {
            if (khz < vlo || khz > vhi) return; float y = Y(khz);
            using (Pen p = new Pen(c, 2)) using (Brush b = new SolidBrush(c)) using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Far })
            { g.DrawLine(p, 36, y, Width, y); g.FillPolygon(b, new[] { new PointF(30, y - 5), new PointF(30, y + 5), new PointF(42, y) }); g.DrawString("VFO " + n + " " + khz.ToString("0.00", CultureInfo.InvariantCulture), Font, b, new RectangleF(40, y - 15, Width - 44, 14), sf); }
        }
    }

    // Trames brutes du cluster, partie basse du journal.
    // Bouton de même forme que les onglets du journal (F5PBG, +).
    class TabButton : Control
    {
        bool hot; public Action Clicked;
        public TabButton(string text, int w) { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true); Text = text; Size = new Size(w, 28); Cursor = Cursors.Hand; Theme.Changed += Reshape; Disposed += delegate { Theme.Changed -= Reshape; }; }
        protected override void OnMouseEnter(EventArgs e) { hot = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { hot = false; Invalidate(); }
        protected override void OnTextChanged(EventArgs e) { Invalidate(); }
        // Forme du thème : coins arrondis (la forme de la fenêtre est un rectangle arrondi) ou angles droits
        void Reshape()
        {
            if (Width < 12 || Height < 12) return;
            if (Theme.Cur.Look != LookKind.Round) { Region = null; Invalidate(); return; }
            using (System.Drawing.Drawing2D.GraphicsPath p = new System.Drawing.Drawing2D.GraphicsPath())
            { int k = Math.Max(4, Theme.Cur.Radius * 2), w = Width, h = Height; p.AddArc(0, 0, k, k, 180, 90); p.AddArc(w - k, 0, k, k, 270, 90); p.AddArc(w - k, h - k, k, k, 0, 90); p.AddArc(0, h - k, k, k, 90, 90); p.CloseFigure(); Region = new Region(p); }
            Invalidate();
        }
        protected override void OnResize(EventArgs e) { base.OnResize(e); Reshape(); }
        protected override void OnClick(EventArgs e) { Action a = Clicked; if (a != null) a(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics; Color bg = Theme.Get("lb.btn.back"); g.Clear(hot ? Theme.Lift(bg, 0.25f) : bg); Rectangle r = new Rectangle(0, 0, Width + 1, Height + 1);
            using (Font f = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            { Color tc = Looks.DrawButton(g, r, null, Theme.Get("lb.btn.text"), hot, false, true, bg); TextRenderer.DrawText(g, Text, f, r, tc, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis); }
        }
    }
    // Fenêtre des trames du cluster (partie basse du journal) ; les boutons Cluster et Alerte sont en haut à droite, par-dessus le texte.
    public class ClusterFrames : Panel
    {
        readonly TextBox tb; readonly TabButton bCl, bAl;
        public ClusterFrames()
        {
            Dock = DockStyle.Bottom; Height = 120;
            tb = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Font = Ui.Mono, WordWrap = false };
            bAl = new TabButton("Alerte", 80) { Anchor = AnchorStyles.Top | AnchorStyles.Right }; bAl.Clicked = delegate { using (AlertForm f = new AlertForm()) f.ShowDialog(FindForm()); };
            bCl = new TabButton("Aucun cluster", 150) { Anchor = AnchorStyles.Top | AnchorStyles.Right }; bCl.Clicked = ClusterMenu;
            Controls.Add(tb); Controls.Add(bAl); Controls.Add(bCl); bAl.BringToFront(); bCl.BringToFront();
            Layout += delegate { bAl.Location = new Point(Width - bAl.Width - 24, 2); bCl.Location = new Point(bAl.Left - bCl.Width - 4, 2); };
            DxCluster.Changed += Fill; Disposed += delegate { DxCluster.Changed -= Fill; }; Fill();
        }
        // Bouton « cluster actif » : son nom ; un clic liste les clusters enregistrés (se connecter / changer / déconnecter).
        void ClusterMenu()
        {
            ContextMenuStrip m = new ContextMenuStrip();
            foreach (ClusterDef d in DxCluster.Load())
            {
                ClusterDef c = d; ToolStripMenuItem mi = new ToolStripMenuItem(d.Name + "  (" + d.Host + ":" + d.Port + ")") { Checked = DxCluster.Connected && DxCluster.Current != null && DxCluster.Current.Name == d.Name };
                mi.Click += delegate { DxCluster.Connect(c); }; m.Items.Add(mi);
            }
            if (m.Items.Count == 0) m.Items.Add(new ToolStripMenuItem("Aucun cluster enregistré") { Enabled = false });
            m.Items.Add(new ToolStripSeparator());
            if (DxCluster.Connected) m.Items.Add("Déconnecter", null, delegate { DxCluster.Disconnect(); });
            m.Items.Add("Configurer les clusters...", null, delegate { using (ClusterForm f = new ClusterForm()) f.ShowDialog(FindForm()); });
            m.Show(bCl, new Point(0, bCl.Height));
        }
        void Fill()
        {
            string nm = DxCluster.Connected && DxCluster.Current != null ? DxCluster.Current.Name : "Aucun cluster"; if (bCl.Text != nm) bCl.Text = nm;
            string[] a; lock (DxCluster.Frames) a = DxCluster.Frames.ToArray(); int from = Math.Max(0, a.Length - 100); tb.Text = string.Join("\r\n", a, from, a.Length - from); tb.SelectionStart = tb.TextLength; tb.ScrollToCaret();
        }
    }

    // Alertes du cluster : plusieurs lignes de texte (indicatif, mot, série de lettres espaces compris) reliées par « ou » / « et ».
    public class AlertForm : Form
    {
        const int N = 8; readonly TextBox[] tb = new TextBox[N]; readonly ComboBox[] op = new ComboBox[N]; CheckBox on;
        public AlertForm()
        {
            Text = "Alertes du DX Cluster"; ClientSize = new Size(560, 370); StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; Font = Ui.Normal; Icon = Ui.AppIcon;
            on = Ui.Chk(this, "Alertes actives : un bip retentit quand un spot en direct correspond", 12, 12, 530); on.Checked = DxCluster.AlertsOn;
            Label l = Ui.Lbl(this, "Chaque ligne cherche son texte dans l'indicatif, le commentaire et la fréquence du spot (majuscules ou minuscules, espaces compris). Les lignes sont combinées de haut en bas.", 12, 40, 536); l.Height = 38; l.ForeColor = SystemColors.GrayText;
            List<DxCluster.AlertLine> a = DxCluster.LoadAlerts(); int y = 90;
            for (int i = 0; i < N; i++)
            {
                op[i] = Ui.Combo(this, 12, y - 2, 60, new[] { "ou", "et" }); op[i].DropDownStyle = ComboBoxStyle.DropDownList; op[i].SelectedIndex = i < a.Count && a[i].Op == "et" ? 1 : 0; if (i == 0) { op[i].Visible = false; Ui.Lbl(this, "Alerte :", 12, y); }
                tb[i] = Ui.Txt(this, 80, y - 2, 460); tb[i].Text = i < a.Count ? a[i].Text : ""; y += 30;
            }
            Ui.Btn(this, "Tester le bip", 12, 330, 110, 28, (s, e) => DxCluster.Beep());
            Button ok = Ui.Btn(this, "OK", 370, 330, 80, 28, (s, e) => Save()); Ui.Btn(this, "Annuler", 458, 330, 80, 28, (s, e) => Close()); AcceptButton = ok;
        }
        void Save()
        {
            List<DxCluster.AlertLine> l = new List<DxCluster.AlertLine>(); for (int i = 0; i < N; i++) l.Add(new DxCluster.AlertLine { Op = op[i].SelectedIndex == 1 ? "et" : "ou", Text = tb[i].Text });
            DxCluster.SaveAlerts(l); DxCluster.AlertsOn = on.Checked; Close();
        }
    }

    // Configuration > Station
    public class StationForm : Form
    {
        TextBox call, name, grid;
        public StationForm()
        {
            Text = "Configuration : Station"; ClientSize = new Size(740, 230); StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; Font = Ui.Normal; Icon = Ui.AppIcon;
            int y = 16;
            Ui.Lbl(this, "Indicatif station :", 12, y); call = Ui.Txt(this, 170, y - 2, 140); call.Text = App.Cfg.Get("me.call", ""); Label ex = Ui.Lbl(this, "Radioamateur ou non, exemple : F5PBG  14VL73", 320, y, 400); ex.AutoSize = false; ex.Width = 400; y += 38;
            Ui.Lbl(this, "Prénom :", 12, y); name = Ui.Txt(this, 170, y - 2, 200); name.Text = App.Cfg.Get("me.name", ""); y += 38;
            Ui.Lbl(this, "QTH Locator (Grid) :", 12, y); grid = Ui.Txt(this, 170, y - 2, 100); grid.Text = App.Cfg.Get("me.grid", ""); Ui.Lbl(this, "ex. IN78TI", 280, y); y += 30;
            Label l = Ui.Lbl(this, "Ce locator sert à calculer la distance de chaque contact (colonne Distance) dès sa réception.", 12, y, 716); l.Height = 36; l.ForeColor = SystemColors.GrayText;
            Button ok = Ui.Btn(this, "OK", 550, 190, 80, 28, (s, e) => Save()); Ui.Btn(this, "Annuler", 638, 190, 80, 28, (s, e) => Close()); AcceptButton = ok;
        }
        void Save()
        {
            string g = grid.Text.Trim().ToUpperInvariant();
            if (g.Length > 0 && !Geo.Valid(g)) { MessageBox.Show(this, "Locator invalide (ex. IN78TI)."); return; }
            App.Cfg.Set("me.call", call.Text.Trim().ToUpperInvariant()); App.Cfg.Set("me.name", name.Text.Trim()); App.Cfg.Set("me.grid", g); HamRadioLink.Cfg.Save(); Close();
        }
    }

    // Configuration > DX Cluster
    public class ClusterForm : Form
    {
        ListBox list; List<ClusterDef> defs; Label status; CheckBox cAuto, cReco, cStrip, cFrames, cVa, cVb; Button bConn;
        public ClusterForm()
        {
            Text = "Configuration : DX Cluster"; ClientSize = new Size(640, 400); StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; Font = Ui.Normal; Icon = Ui.AppIcon;
            defs = DxCluster.Load();
            Ui.Lbl(this, "DX Cluster enregistrés :", 12, 12);
            list = new ListBox { Left = 12, Top = 36, Width = 470, Height = 130 }; Controls.Add(list);
            Ui.Btn(this, "Ajouter...", 494, 36, 134, 28, (s, e) => Edit(null)); Ui.Btn(this, "Modifier...", 494, 70, 134, 28, (s, e) => { if (list.SelectedIndex >= 0) Edit(defs[list.SelectedIndex]); });
            Ui.Btn(this, "Supprimer", 494, 104, 134, 28, (s, e) => { if (list.SelectedIndex >= 0) { defs.RemoveAt(list.SelectedIndex); DxCluster.Save(defs); Fill(); } });
            bConn = Ui.Btn(this, "Connecter", 494, 138, 134, 28, (s, e) => Conn());
            cAuto = Ui.Chk(this, "Connexion automatique au démarrage du journal", 12, 180, 500); cAuto.Checked = DxCluster.AutoConnect;
            cReco = Ui.Chk(this, "Reconnexion automatique (toutes les 15 s)", 12, 208, 500); cReco.Checked = DxCluster.AutoReconnect;
            cStrip = Ui.Chk(this, "Afficher la réglette à droite (indicatifs de la bande ; Auto = bande du poste, ou bande choisie)", 12, 236, 620); cStrip.Checked = DxCluster.ShowStrip;
            cFrames = Ui.Chk(this, "Afficher les trames du cluster dans la partie basse du journal", 12, 264, 620); cFrames.Checked = DxCluster.ShowFrames;
            cVa = Ui.Chk(this, "Afficher le curseur du VFO A", 12, 292, 300); cVa.Checked = DxCluster.ShowVfoA;
            cVb = Ui.Chk(this, "Afficher le curseur du VFO B", 320, 292, 300); cVb.Checked = DxCluster.ShowVfoB;
            status = Ui.Lbl(this, "", 12, 324, 616); status.Height = 30;
            Ui.Btn(this, "OK", 460, 360, 80, 28, (s, e) => { Apply(); Close(); }); Ui.Btn(this, "Annuler", 548, 360, 80, 28, (s, e) => Close());
            Fill(); DxCluster.Changed += Stat; Disposed += delegate { DxCluster.Changed -= Stat; }; Stat();
        }
        void Fill() { list.Items.Clear(); foreach (ClusterDef c in defs) list.Items.Add(c.ToString()); if (list.Items.Count > 0) { int i = defs.FindIndex(x => x.Name == App.Cfg.Get("cl.sel", "")); list.SelectedIndex = i >= 0 ? i : 0; } }
        void Stat() { status.Text = "État : " + DxCluster.Status; bConn.Text = DxCluster.Connected ? "Déconnecter" : "Connecter"; }
        void Conn() { if (DxCluster.Connected) { DxCluster.Disconnect(); return; } if (list.SelectedIndex < 0) { MessageBox.Show(this, "Choisissez ou ajoutez un cluster."); return; } Apply(); DxCluster.Connect(defs[list.SelectedIndex]); }
        void Apply() { DxCluster.AutoConnect = cAuto.Checked; DxCluster.AutoReconnect = cReco.Checked; DxCluster.ShowStrip = cStrip.Checked; DxCluster.ShowFrames = cFrames.Checked; DxCluster.ShowVfoA = cVa.Checked; DxCluster.ShowVfoB = cVb.Checked; if (list.SelectedIndex >= 0) App.Cfg.Set("cl.sel", defs[list.SelectedIndex].Name); HamRadioLink.Cfg.Save(); Hub.ClusterChanged(); }
        void Edit(ClusterDef d)
        {
            bool isNew = d == null; if (isNew) d = new ClusterDef();
            using (Form f = new Form { Text = isNew ? "Ajout de DX Cluster" : "Modifier le DX Cluster", ClientSize = new Size(440, 250), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, Font = Ui.Normal })
            {
                Ui.Lbl(f, "Nom affiché :", 12, 16); TextBox n = Ui.Txt(f, 150, 14, 270); n.Text = d.Name;
                Ui.Lbl(f, "Hôte :", 12, 52); TextBox h = Ui.Txt(f, 150, 50, 270); h.Text = d.Host;
                Ui.Lbl(f, "Port :", 12, 88); TextBox p = Ui.Txt(f, 150, 86, 80); p.Text = d.Port.ToString();
                Ui.Lbl(f, "Login (facultatif) :", 12, 124); TextBox lg = Ui.Txt(f, 150, 122, 160); lg.Text = d.Login;
                Ui.Lbl(f, "Mot de passe (facultatif) :", 12, 160); TextBox pw = Ui.Txt(f, 190, 158, 160); pw.Text = d.Pass; pw.UseSystemPasswordChar = true;
                Button ok = Ui.Btn(f, "OK", 250, 205, 80, 28, (s, e) => { int pt; if (n.Text.Trim().Length == 0 || h.Text.Trim().Length == 0 || !int.TryParse(p.Text.Trim(), out pt) || pt < 1 || pt > 65535) { MessageBox.Show(f, "Nom, hôte et port (1-65535) obligatoires."); return; } f.DialogResult = DialogResult.OK; });
                Ui.Btn(f, "Annuler", 338, 205, 80, 28, (s, e) => f.DialogResult = DialogResult.Cancel); f.AcceptButton = ok;
                if (f.ShowDialog(this) != DialogResult.OK) return;
                d.Name = n.Text.Trim(); d.Host = h.Text.Trim(); d.Port = int.Parse(p.Text.Trim()); d.Login = lg.Text.Trim(); d.Pass = pw.Text;
                if (isNew) defs.Add(d); DxCluster.Save(defs); Fill();
            }
        }
    }
}
