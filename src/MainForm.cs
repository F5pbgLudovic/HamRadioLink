using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO.Ports;
using System.Text;
using System.Windows.Forms;

namespace HamRadioLink
{
    // petit bouton de l'en-tête (même style que les boutons de la face avant)
    public class Btn : Control
    {
        public bool Checked, LockStyle; public Color Accent = T.Amber; bool hot, down; public int Icon; public string Badge; public Color Fill = Color.Empty; public MouseButtons LastButton; public event EventHandler RightClick;
        public Btn() { SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true); Cursor = Cursors.Hand; }
        protected override void OnMouseEnter(EventArgs e) { hot = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hot = false; down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { down = true; LastButton = e.Button; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); if (e.Button == MouseButtons.Right && RightClick != null && ClientRectangle.Contains(e.Location)) RightClick(this, EventArgs.Empty); }
        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit; g.Clear(Parent != null ? Parent.BackColor : T.Panel);
            RectangleF r = new RectangleF(0.5f, 0.5f, Width - 2, Height - 2);
            Color top = Checked ? Color.FromArgb(58, 66, 82) : Color.FromArgb(46, 55, 70), bot = Checked ? Color.FromArgb(34, 40, 52) : Color.FromArgb(28, 34, 45);
            bool lk = LockStyle && Checked; if (lk) { top = T.LockBtn; bot = ControlPaint.Dark(T.LockBtn, 0.2f); }
            if (Fill != Color.Empty) { top = ControlPaint.Light(Fill, 0.12f); bot = ControlPaint.Dark(Fill, 0.12f); }
            if (hot) { top = ControlPaint.Light(top, 0.15f); bot = ControlPaint.Light(bot, 0.1f); } if (down) { Color t = top; top = bot; bot = t; }
            using (GraphicsPath p = T.Round(r, 5 * T.Scale)) { using (LinearGradientBrush b = new LinearGradientBrush(r, top, bot, 90f)) g.FillPath(b, p); using (Pen pen = new Pen(lk ? ControlPaint.Light(T.LockBtn, 0.6f) : (Checked ? Accent : T.Edge), Checked ? 1.4f : 1f)) g.DrawPath(pen, p); }
            if (Icon != 0)
            {
                float k = T.Scale; RectangleF ir = new RectangleF(r.X + r.Width / 2 - 10 * k, r.Y + r.Height / 2 - 8 * k, 20 * k, 16 * k);
                using (Pen ip = new Pen(Checked ? Accent : T.Dim, Math.Max(1.5f, 1.6f * k))) { g.DrawRectangle(ip, ir.X, ir.Y, ir.Width, ir.Height); if (Icon == 1) g.DrawLine(ip, ir.X + ir.Width / 2, ir.Y, ir.X + ir.Width / 2, ir.Bottom); else g.DrawLine(ip, ir.X, ir.Y + ir.Height / 2, ir.Right, ir.Y + ir.Height / 2); }
            }
            else using (StringFormat sf = T.Center()) using (Brush b = new SolidBrush(lk ? Color.White : (Checked ? T.Text : Color.FromArgb(196, 208, 222)))) g.DrawString(Text, T.Fn("Segoe UI Semibold", 9f), b, r, sf);
            if (Badge != null) using (Brush bb = new SolidBrush(T.Amber)) g.DrawString(Badge, T.Fn("Segoe UI", 7f, FontStyle.Bold), bb, r.X + 8 * T.Scale, r.Y + 2 * T.Scale);
                    }
    }

    public class MainForm : Form
    {
        public const string Version = "1.1";
        const int DesignW = 1860, DesignH = 944; const double Ratio = DesignW / (double)DesignH;
        readonly List<RadioDef> radios = Radios.All(); RadioDef cur; Rig rig = new Rig();      // cur / rig / widgets = poste ACTIF (plusieurs postes peuvent être connectés en même temps)
        readonly Panel stage = new Panel(); readonly ToolTip tip = new ToolTip();
        List<HW> widgets = new List<HW>(); readonly Dictionary<Control, Rectangle> hdr = new Dictionary<Control, Rectangle>(); readonly Dictionary<Control, float> hdrPt = new Dictionary<Control, float>();
        readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        DarkCombo cbRadio, cbView, cbIface, cbPort, cbBaud, cbStop; TextBox tbHost, tbPort, tbCiv; Label lbCiv; CheckBox chkRts, chkDtr;
        Btn btnOpt, btnExp, btnConnect, btnMon, btnHelp, btnDefLayout, btnOk, btnUndo, btnReset, btnAdd; List<string> undo = new List<string>(); string lastSnap = "", baseSnap = ""; Label lbStatus, lbClock, lbHint, lbLink, lbRadio, lbView;
        MonitorForm monitor; int lastVer = -1, lastFull; bool loading = true;
        IC7760.MainForm ic; Panel icBar; DarkCombo icRadio;
        // un poste ouvert = une session : son moteur (Rig), sa face avant (panneau) et ses éléments
        class Sess { public RadioDef Def; public Rig Rig = new Rig(); public List<HW> W = new List<HW>(); public Panel Pane = new Panel(); public float Scale = 1f; public int LastVer = -1; public bool PendingDone, Fresh = true, Shares; public string Port; }
        readonly List<Sess> sess = new List<Sess>(); Sess act; int split; Btn btnSplitV, btnSplitH; bool quiet, updSess; string notice; int noticeUntil;
        // postes « épinglés » : après une connexion réussie le poste garde son bouton en haut (même après fermeture du programme) ; clic droit = le retirer
        List<string> Pins() { List<string> l = new List<string>(); foreach (string k in Cfg.Get("pins", "").Split(',')) if (k.Length > 0 && !l.Contains(k)) l.Add(k); return l; }
        void SetPins(List<string> l) { Cfg.Set("pins", string.Join(",", l.ToArray())); Cfg.Save(); }
        void Pin(string key) { List<string> l = Pins(); if (!l.Contains(key)) { l.Add(key); SetPins(l); } }
        void UnpinTick(RadioDef d) { List<string> l = Pins(); if (l.Remove(d.Key)) SetPins(l); UpdateSessUi(); }
        // paramètres de liaison du poste affiché (port, vitesse, arrêt, RTS/DTR, adresse réseau, CI-V) : mémorisés POSTE PAR POSTE
        void SaveLink()
        {
            if (cur == null || cbIface == null || cbIface.SelectedIndex < 0) return; string k = cur.Key, lk = "link." + k + "."; int i = cbIface.SelectedIndex;
            Cfg.SetInt(lk + "iface", i); Cfg.Set(lk + "port", cbPort.Text); Cfg.Set(lk + "host", tbHost.Text); Cfg.Set(lk + "tcpport", tbPort.Text); Cfg.SetBool(lk + "rts", chkRts.Checked); Cfg.SetBool(lk + "dtr", chkDtr.Checked);
            if (cbBaud.Text.Length > 0) Cfg.Set("baud." + k, cbBaud.Text); Cfg.SetInt("stop." + k, cbStop.SelectedIndex); if (cur.Civ >= 0 && tbCiv.Text.Trim().Length > 0) Cfg.Set("civ." + k, tbCiv.Text.Trim());
            Cfg.Save();
        } readonly List<Btn> ticks = new List<Btn>();

        public MainForm()
        {
            AutoScaleMode = AutoScaleMode.None; try { using (Graphics gd = CreateGraphics()) T.Dpi = gd.DpiX; } catch { }
            BackColor = T.Bg; ForeColor = T.Text; Font = new Font("Segoe UI", 9f); ClientSize = new Size(DesignW, DesignH); StartPosition = FormStartPosition.CenterScreen; DoubleBuffered = true; KeyPreview = true;
            Text = "HamRadioLink " + Version + " @F5PBG 2026"; try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            stage.BackColor = T.Bg; stage.Bounds = new Rectangle(0, 0, DesignW, DesignH); Controls.Add(stage);
            BuildHeader();
            string key = Cfg.Get("radio", radios[0].Key); cur = radios[0]; foreach (RadioDef r in radios) if (r.Key == key) cur = r;
            act = NewSess(cur); Activate(act);
            tip.InitialDelay = 500; tip.ReshowDelay = 200; tip.AutoPopDelay = 12000;
            Resize += delegate { Relayout(); };
            timer.Interval = 30; timer.Tick += OnTick; timer.Start();
            Load += delegate { Restore(); try { MicRoute.Update(); } catch { } try { Journal.WsjtListener.Start(); } catch { } }; FormClosed += delegate { TuneMic.Stop(null); MicRoute.Stop(); if (cmdSrv != null) { try { cmdSrv.Dispose(); } catch { } } if (cmdSrv2 != null) { try { cmdSrv2.Dispose(); } catch { } } }; FormClosing += OnClosing; loading = false;
        }

        // ============================================================ en-tête
        T0 Add<T0>(T0 c, int x, int y, int w, int h, float pt) where T0 : Control { stage.Controls.Add(c); hdr[c] = new Rectangle(x, y, w, h); hdrPt[c] = pt; return c; }
        Label Lab(string t, int x, int y, int w, int h, float pt, Color col, bool bold)
        {
            Label l = new Label(); l.Text = t; l.ForeColor = col; l.BackColor = Color.Transparent; l.AutoSize = false; l.TextAlign = ContentAlignment.MiddleLeft; l.Font = new Font("Segoe UI", pt, bold ? FontStyle.Bold : FontStyle.Regular);
            return Add(l, x, y, w, h, pt);
        }
        DarkCombo Cmb(int x, int y, int w, params string[] items) { DarkCombo c = new DarkCombo(); foreach (string s in items) c.Items.Add(s); return Add(c, x, y, w, 26, 9f); }
        Btn Bt(string t, int x, int y, int w, int h, Action a) { Btn b = new Btn(); b.Text = t; b.Click += delegate { if (b.LastButton != MouseButtons.Right) a(); }; return Add(b, x, y, w, h, 9f); }
        TextBox Tb(int x, int y, int w) { TextBox t = new TextBox(); t.BackColor = T.Panel2; t.ForeColor = T.Text; t.BorderStyle = BorderStyle.FixedSingle; return Add(t, x, y, w, 24, 9f); }
        CheckBox Chk(string t, int x, int y, int w) { CheckBox c = new CheckBox(); c.Text = t; c.ForeColor = T.Text; c.BackColor = Color.Transparent; c.FlatStyle = FlatStyle.Flat; return Add(c, x, y, w, 24, 9f); }

        void BuildHeader()
        {
            lbRadio = Lab("Poste", 14, 8, 52, 26, 9f, T.Dim, false); cbRadio = Cmb(70, 8, 300);
            foreach (RadioDef r in radios) cbRadio.Items.Add(r.Name); cbRadio.MaxDropDownItems = 32;
            cbRadio.SelectedIndexChanged += delegate { if (!loading) SwitchRadio(); };
            lbView = Lab("Vue", 14, 44, 52, 26, 9f, T.Dim, false); cbView = Cmb(70, 44, 170, "normal", "config vue"); cbView.SelectedIndex = 0;
            cbView.SelectedIndexChanged += delegate { if (!loading) SetView(cbView.SelectedIndex == 1); };
            btnSplitV = Bt("", 508, 44, 36, 26, delegate { SetSplit(split == 1 ? 0 : 1); }); btnSplitH = Bt("", 550, 44, 36, 26, delegate { SetSplit(split == 2 ? 0 : 2); }); btnSplitV.Icon = 1; btnSplitH.Icon = 2; btnSplitV.Visible = btnSplitH.Visible = false;
            btnAdd = Bt("Ajouter", 250, 44, 90, 26, AddElement); btnAdd.Visible = false; btnOk = Bt("Valider", 346, 44, 90, 26, ValidateView); btnOk.Accent = T.Green; btnUndo = Bt("Retour arrière", 442, 44, 130, 26, UndoMove); btnReset = Bt("Réinit", 578, 44, 80, 26, ResetAll); btnReset.Accent = T.Red; btnDefLayout = btnReset; btnOk.Visible = btnUndo.Visible = btnReset.Visible = false;
            lbHint = Lab("Glisser = déplacer · coin = taille · clic droit = commande CAT", 664, 44, 440, 26, 9f, T.Amber, false); lbHint.Visible = false;
            btnMon = Bt("Moniteur CAT", 384, 8, 130, 26, ShowMonitor); btnHelp = Bt("?", 522, 8, 34, 26, ShowHelp); btnOpt = Bt("\u2699", 562, 8, 34, 26, ShowOptions); btnExp = Bt("\u21C5", 602, 8, 34, 26, ShowExport);
            lbClock = Lab("", 646, 8, 130, 26, 10f, T.Cyan, true);
            lbLink = Lab("LIAISON", 1110, 0, 120, 16, 8.5f, T.Amber, true);
            cbIface = Cmb(1110, 16, 250, "Port COM (CAT direct)", "CAT sur TCP (brut)", "Poste virtuel (simulateur)", "TCI (ExpertSDR3, Thetis…)");
            cbIface.SelectedIndexChanged += delegate { UpdateIface(); };
            btnConnect = Bt("CONNECTER", 1372, 14, 170, 30, ToggleConnect); btnConnect.Accent = T.Green;
            btnConnect.RightClick += delegate { string ak = "auto." + cur.Key; bool v = !Cfg.GetBool(ak, false); Cfg.SetBool(ak, v); Cfg.Save(); btnConnect.Badge = v ? "AUTO" : null; btnConnect.Invalidate(); tip.SetToolTip(btnConnect, "Clic droit : connexion automatique de ce poste au lancement (" + (v ? "activée" : "désactivée") + ")"); };
            btnSrv = Bt("Serveur", 1672, 14, 110, 30, ShowServer); btnSrv.Accent = T.Cyan;
            btnLog = Bt("LOG", 1672, 50, 110, 30, ShowLog); btnLog.Accent = T.Cyan;
            btnLock = Bt("Lock", 1790, 14, 60, 30, ToggleAppLock); btnLock.LockStyle = true;
            btnShare = Bt("Partage", 1552, 14, 110, 30, ShowShare); btnShare.Accent = T.Cyan;
            cbPort = Cmb(1110, 50, 138); cbBaud = Cmb(1256, 50, 100, "4800", "9600", "19200", "38400", "57600", "115200"); cbStop = Cmb(1364, 50, 56, "1", "2"); cbStop.SelectedIndex = 0;
            chkRts = Chk("RTS", 1430, 50, 62); chkDtr = Chk("DTR", 1494, 50, 62); chkRts.Checked = true;
            tbHost = Tb(1110, 50, 250); tbPort = Tb(1372, 50, 90); tbHost.Text = "127.0.0.1"; tbPort.Text = "4532";
            lbCiv = Lab("CI-V hex", 1560, 52, 62, 22, 9f, T.Dim, false); tbCiv = Tb(1624, 50, 44); lbCiv.Visible = tbCiv.Visible = false;
            lbStatus = Lab("", 784, 8, 316, 26, 9f, T.Dim, false);
            RefreshPorts(); cbIface.SelectedIndex = 0; cbBaud.Text = "38400";
        }
        void RefreshPorts() { string p = cbPort.Text; cbPort.Items.Clear(); try { foreach (string s in SerialPort.GetPortNames()) cbPort.Items.Add(s); } catch { } if (p.Length > 0) cbPort.Text = p; else if (cbPort.Items.Count > 0) cbPort.SelectedIndex = 0; }
        void FixIface() { int i = cbIface.SelectedIndex; if (cur != null && cur.Tci && (i == 0 || i == 1)) cbIface.SelectedIndex = 3; else if (cur != null && !cur.Tci && i == 3) cbIface.SelectedIndex = 0; }
        void UpdateCiv() { if (cur == null || tbCiv == null) return; bool v = cur.Civ >= 0 && cbIface.SelectedIndex >= 0 && cbIface.SelectedIndex <= 1; lbCiv.Visible = tbCiv.Visible = v; if (cur.Civ >= 0) tbCiv.Text = Cfg.Get("civ." + cur.Key, cur.Civ.ToString("X2")); }
        void UpdateIface()
        {
            int i = cbIface.SelectedIndex; bool com = i == 0, net = i == 1 || i == 3;
            cbPort.Visible = cbBaud.Visible = cbStop.Visible = chkRts.Visible = chkDtr.Visible = com; tbHost.Visible = tbPort.Visible = net; UpdateCiv();
            if (i == 3 && tbPort.Text == "4533") tbPort.Text = "40001"; if (i == 1 && tbPort.Text == "40001") tbPort.Text = "4533";
        }
        void SwitchRadio()
        {
            if (cbRadio.SelectedIndex < 0 || radios[cbRadio.SelectedIndex] == cur) return;
            if (!OpenRadio(radios[cbRadio.SelectedIndex])) { loading = true; cbRadio.SelectedItem = cur.Name; loading = false; }
        }
        // ouvre (ou réactive) un poste : un poste connecté reste ouvert à côté du nouveau ; un poste non connecté est remplacé
        bool OpenRadio(RadioDef want)
        {
            if (want == cur) return true; if (!Settle()) return false; bool ed = HW.Edit; if (ed) cur.SaveCat(); SaveLink();
            Sess old = act; Sess ns = sess.Find(delegate(Sess x) { return x.Def == want; }); if (ns == null) ns = NewSess(want);
            Activate(ns); if (old != ns && old.Rig.Link == null && !ServerBound(old.Def.Key)) DropSess(old);
            if (ed) { undo.Clear(); baseSnap = lastSnap = Snap(); ViewButtons(); }
            return true;
        }
        Sess NewSess(RadioDef d) { CatLog.Event(d.Key, "poste ouvert"); Sess s = new Sess(); s.Def = d; s.Rig.Def = d; d.LoadUser(); s.Pane.BackColor = Color.Transparent; stage.Controls.Add(s.Pane); sess.Add(s); return s; }
        void DropSess(Sess s)
        {
            if (s == act) return; CatLog.Event(s.Def.Key, "poste fermé");
            foreach (HW w in s.W) { tip.SetToolTip(w, null); w.Dispose(); }
            s.W.Clear(); stage.Controls.Remove(s.Pane); s.Pane.Dispose(); sess.Remove(s); try { s.Rig.Disconnect(); } catch { }
            if (sess.Count < 2 && split != 0) split = 0; UpdateSessUi(); Relayout();
        }
        void Activate(Sess s)
        {
            if (act != s) { if (monitor != null && !monitor.IsDisposed) monitor.Close(); if (optDlg != null && !optDlg.IsDisposed) optDlg.Close(); }
            act = s; cur = s.Def; rig = s.Rig; widgets = s.W; Cfg.Set("radio", cur.Key);
            bool ol = loading; loading = true; cbRadio.SelectedIndex = radios.IndexOf(cur); loading = ol;
            if (s.Fresh) { s.Fresh = false; rig.V.Clear(); LocalDefaults(); BuildRadio(); }
            LoadLink(); UpdateSessUi(); UpdateIc(); Relayout(); lastVer = -1; UpdateConnColor();
        }
        void NextSess() { if (sess.Count < 2) return; GoSess(sess[(sess.IndexOf(act) + 1) % sess.Count]); }
        void SetSplit(int mode) { split = sess.Count < 2 ? 0 : mode; UpdateSessUi(); Relayout(); }
        // une coche (☑ actif / ☐ autre) portant le nom de chaque poste ouvert, à droite de « Vue », dans toutes les vues ; un clic active le poste
        void UpdateSessUi()
        {
            if (btnSplitV == null || updSess) return; updSess = true;
            try
            {
                // une coche par poste mémorisé (connecté un jour) ou ouvert ; ☑ = poste actif ; verte = connecté ; clic = ouvrir / activer ; clic droit = retirer
                List<RadioDef> items = new List<RadioDef>();
                foreach (string pk in Pins()) { RadioDef d0 = radios.Find(delegate(RadioDef r) { return r.Key == pk; }); if (d0 != null && !items.Contains(d0)) items.Add(d0); }
                foreach (Sess z in sess) if (!items.Contains(z.Def)) items.Add(z.Def);
                int x = 250;
                while (ticks.Count < items.Count)
                {
                    Btn b = new Btn();
                    b.Click += delegate { RadioDef d1 = b.Tag as RadioDef; if (b.LastButton != MouseButtons.Right && d1 != null) OpenRadio(d1); };
                    b.RightClick += delegate { RadioDef d2 = b.Tag as RadioDef; if (d2 != null) UnpinTick(d2); };
                    Add(b, x, 44, 150, 26, 9f); ticks.Add(b);
                }
                for (int i = 0; i < ticks.Count; i++)
                {
                    Btn b = ticks[i]; bool show = i < items.Count; b.Visible = show; if (!show) { b.Tag = null; continue; }
                    RadioDef d = items[i]; Sess s = sess.Find(delegate(Sess q) { return q.Def == d; }); b.Tag = d;
                    b.Text = (s != null && s == act ? "\u2611  " : "\u2610  ") + CmdServer.ModelName(d); b.Checked = s != null && s == act; b.Accent = T.Amber;
                    b.Fill = s == null || s.Rig.Link == null ? Color.Empty
                        : (s == act ? Color.FromArgb(T.Green.R * 45 / 100, T.Green.G * 88 / 100, T.Green.B * 52 / 100)          // poste sélectionné : vert LUMINEUX
                                    : Color.FromArgb(T.Green.R * 20 / 100, T.Green.G * 38 / 100, T.Green.B * 24 / 100));          // autre poste connecté : vert SOMBRE ; ☑ et bord ambre = poste actif
                    tip.SetToolTip(b, "Clic : ouvrir ce poste (ses réglages de liaison sont rappelés)\nClic droit : retirer ce bouton");
                    int w; using (Graphics g = CreateGraphics()) w = (int)(g.MeasureString(b.Text, Font).Width * 96f / T.Dpi) + 34; w = Math.Max(110, w);
                    hdr[b] = new Rectangle(x, 44, w, 26); x += w + 6; b.Invalidate();
                }
                bool multi = sess.Count >= 2; btnSplitV.Visible = btnSplitH.Visible = multi;
                if (multi) { hdr[btnSplitV] = new Rectangle(x + 4, 44, 36, 26); hdr[btnSplitH] = new Rectangle(x + 46, 44, 36, 26); x += 90; btnSplitV.Checked = split == 1; btnSplitH.Checked = split == 2; btnSplitV.Invalidate(); btnSplitH.Invalidate(); }
                int ex = x + 10; hdr[btnAdd] = new Rectangle(ex, 44, 90, 26); ex += 96; hdr[btnOk] = new Rectangle(ex, 44, 90, 26); ex += 96; hdr[btnUndo] = new Rectangle(ex, 44, 130, 26); ex += 136; hdr[btnReset] = new Rectangle(ex, 44, 80, 26); ex += 86;
                hdr[lbHint] = new Rectangle(ex, 44, Math.Max(160, 1100 - ex), 26);
                Relayout();
            }
            finally { updSess = false; }
        }
        // change de poste actif (en config vue : propose d'abord d'enregistrer, puis repart de zéro)
        void GoSess(Sess s)
        {
            if (s == act || !sess.Contains(s)) return; if (!Settle()) return; bool ed = HW.Edit; if (ed) cur.SaveCat(); SaveLink();
            Activate(s); if (ed) { undo.Clear(); baseSnap = lastSnap = Snap(); ViewButtons(); }
        }
        // réglages de liaison propres à chaque poste (anciennes clés globales en repli)
        void LoadLink()
        {
            string k = cur.Key, lk = "link." + k + "."; bool ol = loading; loading = true;
            int i = Cfg.GetInt(lk + "iface", Cfg.GetInt("iface", cur.Tci ? 3 : 0)); if (Cfg.GetInt("ifacev", 1) < 2) { i = i == 3 ? 2 : (i == 2 ? 0 : i); Cfg.SetInt("ifacev", 2); }
            if (i < 0 || i >= cbIface.Items.Count) i = 0; cbIface.SelectedIndex = i; UpdateIface();
            string p = Cfg.Get(lk + "port", Cfg.Get("port", "")); if (p.Length > 0) cbPort.Text = p;
            cbBaud.Text = Cfg.Get("baud." + k, cur.DefBaud.ToString()); cbStop.SelectedIndex = Math.Max(0, Math.Min(1, Cfg.GetInt("stop." + k, cur.Stop2 ? 1 : 0)));
            chkRts.Checked = Cfg.GetBool(lk + "rts", Cfg.GetBool("rts", true)); chkDtr.Checked = Cfg.GetBool(lk + "dtr", Cfg.GetBool("dtr", false)); tbHost.Text = Cfg.Get(lk + "host", Cfg.Get("host", "127.0.0.1"));
            tbPort.Text = Cfg.Get(lk + "tcpport", Cfg.Get("tcpport." + (i == 3 ? "3" : "1"), i == 3 ? "40001" : "4533"));
            FixIface(); UpdateCiv(); loading = ol;
            btnShare.Checked = ShareOpts.IsOn(k); btnShare.Invalidate(); btnConnect.Badge = Cfg.GetBool("auto." + k, false) ? "AUTO" : null; btnConnect.Checked = rig.Link != null; btnConnect.Text = rig.Link != null ? "DÉCONNECTER" : "CONNECTER"; btnConnect.Invalidate();
        }
        // lancement : les postes marqués « connexion automatique » (clic droit sur CONNECTER) se connectent, chacun avec sa liaison
        void AutoConnect()
        {
            List<RadioDef> list = radios.FindAll(delegate(RadioDef r) { return r.Key != "ic7760" && Cfg.GetBool("auto." + r.Key, false); });
            if (list.Count == 0) return; Sess keep = act;
            foreach (RadioDef r in list)
            {
                Sess s = sess.Find(delegate(Sess x) { return x.Def == r; }) ?? NewSess(r); Activate(s);
                if (rig.Link == null) { quiet = true; try { ToggleConnect(); } finally { quiet = false; } }
            }
            Activate(keep); if (keep.Rig.Link == null && list.Count > 0 && !list.Contains(keep.Def)) { /* le poste mémorisé reste affiché, non connecté */ }
        }
        // bouton CONNECTER / DÉCONNECTER : fond VERT quand le poste répond, ROUGE quand la connexion est absente ou perdue (couleurs réglables dans Paramètres)
        void UpdateConnColor()
        {
            if (btnConnect == null || rig == null) return; Color fc = rig.Connected ? T.ConnOk : T.ConnKo;
            if (btnConnect.Fill != fc) { btnConnect.Fill = fc; btnConnect.Invalidate(); }
        }
        void Warn(string text, string caption) { if (quiet) { lbStatus.Text = caption + " : " + text; lbStatus.ForeColor = T.Amber; } else MessageBox.Show(this, text, caption); }
        void Warn(string text, string caption, MessageBoxButtons b, MessageBoxIcon ic0) { if (quiet) { lbStatus.Text = caption + " : " + text; lbStatus.ForeColor = T.Amber; } else MessageBox.Show(this, text, caption, b, ic0); }

        void LocalDefaults() { foreach (Ctl c in cur.C) if (c.Local && c.K == Kind.Choice) { string d = Cfg.Get("local." + c.Id, ""); rig.V[c.Id] = d.Length > 0 ? d : (c.ItemList.Length > 2 ? c.ItemList[2].Code : (c.ItemList.Length > 0 ? c.ItemList[0].Code : "")); } }

        // ============================================================ IC-7760 : interface complète reprise d'IC7760Link (cascade, vumètres, export/import, options)
        void UpdateIc()
        {
            bool want = cur != null && cur.Key == "ic7760";
            if (!want)
            {
                if (ic != null) { try { ic.Shutdown(); } catch { } Controls.Remove(ic); ic.Dispose(); ic = null; }
                if (icBar != null) icBar.Visible = false;
                stage.Visible = true; Text = "HamRadioLink " + Version + " @F5PBG 2026 - " + cur.Name; Relayout(); return;
            }
            if (rig.Link != null) { rig.Disconnect(); btnConnect.Checked = false; btnConnect.Text = "CONNECTER"; }
            stage.Visible = false;
            if (icBar == null)
            {
                icBar = new Panel(); icBar.Dock = DockStyle.Top; icBar.Height = 32; icBar.BackColor = T.Panel;

                Label p = new Label(); p.Text = "Poste"; p.ForeColor = T.Dim; p.AutoSize = false; p.SetBounds(10, 3, 44, 26); p.TextAlign = ContentAlignment.MiddleLeft; icBar.Controls.Add(p);
                icRadio = new DarkCombo(); icRadio.SetBounds(56, 3, 300, 26); icRadio.MaxDropDownItems = 32; foreach (RadioDef r in radios) icRadio.Items.Add(r.Name); icBar.Controls.Add(icRadio);
                icRadio.SelectedIndexChanged += delegate { if (!loading && icRadio.SelectedIndex >= 0 && icRadio.SelectedIndex != cbRadio.SelectedIndex) cbRadio.SelectedIndex = icRadio.SelectedIndex; };
                icLock = new Btn(); icLock.Text = "Lock"; icLock.LockStyle = true; icLock.SetBounds(372, 1, 60, 30); icLock.Checked = AppLocked; icLock.Click += delegate { ToggleAppLock(); }; icBar.Controls.Add(icLock);
                icLog = new Btn(); icLog.Text = "LOG"; icLog.Accent = T.Cyan; icLog.SetBounds(440, 1, 80, 30); icLog.Click += delegate { ShowLog(); }; icBar.Controls.Add(icLog);   // le bouton LOG existe aussi dans la fenêtre de l'IC-7760
                Controls.Add(icBar);
            }
            bool ol = loading; loading = true; icRadio.SelectedIndex = radios.IndexOf(cur); loading = ol; icBar.Visible = true;
            if (ic == null)
            {
                IC7760.Cfg.Load();
                ic = new IC7760.MainForm(); ic.TopLevel = false; ic.FormBorderStyle = FormBorderStyle.None; ic.Dock = DockStyle.Fill; Controls.Add(ic); ic.Show(); ic.BringToFront(); icBar.SendToBack();
            }
            Text = "HamRadioLink " + Version + " @F5PBG 2026 - " + cur.Name;
        }

        // ============================================================ face avant
        void BuildRadio()
        {
            foreach (HW w in widgets) { tip.SetToolTip(w, null); act.Pane.Controls.Remove(w); w.Dispose(); }
            widgets.Clear(); act.Pane.SuspendLayout();
            foreach (Ctl c in cur.C) if (c.K == Kind.Frame) AddWidget(c);
            foreach (Ctl c in cur.C) if (c.K != Kind.Frame) AddWidget(c);
            foreach (HW w in widgets) if (w.Def.K == Kind.Frame) w.SendToBack();
            act.Pane.ResumeLayout(false); UpdateTips(); Text = "HamRadioLink " + Version + " @F5PBG 2026 - " + cur.Name;
        }
        void AddWidget(Ctl c)
        {
            Sess owner = act; HW w = Make.For(c); w.Enabled = !AppLocked; w.Rig = rig; w.Scale = owner.Scale; w.LoadLock(); w.RequestEdit += EditCmd; w.Changed += delegate(HW x) { Staged(); };
            w.Touched += delegate { if (act != owner) GoSess(owner); };      // toucher l'élément d'un poste visible le rend actif
            owner.Pane.Controls.Add(w); widgets.Add(w); w.ApplyBounds(); if (c.Hidden) w.Visible = HW.Edit;
        }
        string TipFor(Ctl c)
        {
            if (!HW.Edit) return string.IsNullOrEmpty(c.Tip) ? null : c.Label + "\n" + c.Tip;
            StringBuilder sb = new StringBuilder(c.Label + "  [" + c.Id + "]");
            if (c.HasCat) { if (c.Set.Length > 0) sb.Append("\nSET : " + c.Set); if (c.Read.Length > 0) sb.Append("\nREAD : " + c.Read); if (c.Dirty) sb.Append("\n(modifié — clic droit puis Défaut pour revenir à l'origine)"); }
            sb.Append("\nClic droit : modifier la commande"); return sb.ToString();
        }
        void UpdateTips() { foreach (HW w in widgets) tip.SetToolTip(w, w.Def.K == Kind.Frame ? null : TipFor(w.Def)); }
        void EditCmd(HW w)
        {
            if (w.Def.K == Kind.Frame && false) return;
            using (CmdEditor ed = new CmdEditor(w.Def, rig))
            {
                ed.ShowDialog(this);
                if (ed.Deleted) { cur.RemoveCtl(w.Def); tip.SetToolTip(w, null); act.Pane.Controls.Remove(w); widgets.Remove(w); w.Dispose(); Staged(); rig.Version++; return; }
                if (ed.Changed) { cur.SaveCat(); w.Invalidate(); UpdateTips(); rig.V.Clear(); LocalDefaults(); rig.Version++; }
            }
        }
        // ---- config vue : déplacements en attente (Valider / Retour arrière / Réinit)
        string Snap()
        {
            StringBuilder sb = new StringBuilder(); foreach (Ctl c in cur.C) sb.Append(c.Id).Append(':').Append(c.X).Append(',').Append(c.Y).Append(',').Append(c.W).Append(',').Append(c.H).Append(';');
            sb.Append('\u0002').Append(string.Join("\u0003", cur.CustomLines())); return sb.ToString();
        }
        string CustIds() { StringBuilder sb = new StringBuilder(); foreach (Ctl c in cur.C) if (c.Custom) sb.Append(c.Id).Append(';'); return sb.ToString(); }
        void Restore(string s)
        {
            string[] parts = s.Split('\u0002'); string[] lines = parts.Length > 1 && parts[1].Length > 0 ? parts[1].Split('\u0003') : new string[0];
            StringBuilder ids = new StringBuilder(); foreach (string ln in lines) ids.Append(ln.Split('\t')[0]).Append(';');
            bool rebuild = ids.ToString() != CustIds(); if (rebuild) cur.SetCustomLines(lines);
            foreach (string it in parts[0].Split(';'))
            {
                int k = it.IndexOf(':'); if (k < 0) continue; Ctl c = cur.Find(it.Substring(0, k)); if (c == null) continue; string[] p = it.Substring(k + 1).Split(',');
                int x, y, w, h; if (p.Length == 4 && int.TryParse(p[0], out x) && int.TryParse(p[1], out y) && int.TryParse(p[2], out w) && int.TryParse(p[3], out h)) { c.X = x; c.Y = y; c.W = w; c.H = h; }
            }
            if (rebuild) { BuildRadio(); rig.Version++; } else foreach (HW w in widgets) w.ApplyBounds();
        }
        void AddElement()
        {
            using (AddDialog d = new AddDialog(cur))
            {
                if (d.ShowDialog(this) != DialogResult.OK || d.Result == null) return;
                Ctl c = d.Result; c.Id = cur.NewId(); c.X = 30; c.Y = HW.HeaderH + 10;
                c.DSet = c.Set; c.DRead = c.Read; c.DParse = c.Parse; c.DItems = c.Items; c.DOnVal = c.OnVal; c.DOffVal = c.OffVal; c.DMin = c.Min; c.DMax = c.Max; c.DLabel = c.Label; c.DX = c.X; c.DY = c.Y; c.DW = c.W; c.DH = c.H;
                cur.AddCtl(c); AddWidget(c); Staged(); foreach (HW w in widgets) if (w.Def == c) { w.BringToFront(); EditCmd(w); break; }
            }
        }
        void Staged() { undo.Add(lastSnap); lastSnap = Snap(); ViewButtons(); }
        void ViewButtons() { btnUndo.Enabled = undo.Count > 0; btnOk.Enabled = lastSnap != baseSnap; btnUndo.Text = undo.Count > 0 ? "Retour arrière (" + undo.Count + ")" : "Retour arrière"; btnUndo.Invalidate(); btnOk.Invalidate(); }
        void UndoMove() { if (undo.Count == 0) return; string s = undo[undo.Count - 1]; undo.RemoveAt(undo.Count - 1); Restore(s); lastSnap = s; ViewButtons(); }
        void ValidateView() { cur.SaveCustom(); cur.SaveLayout(); cur.SaveCat(); undo.Clear(); baseSnap = lastSnap = Snap(); ViewButtons(); }
        // false = l'utilisateur annule l'action en cours
        bool Settle()
        {
            if (!HW.Edit || lastSnap == baseSnap) return true;
            DialogResult r = MessageBox.Show(this, "Des modifications de la vue ne sont pas enregistrées.\n\nOui = enregistrer    Non = abandonner les modifications    Annuler = rester en config vue", "Config vue", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (r == DialogResult.Cancel) return false;
            if (r == DialogResult.Yes) ValidateView(); else { Restore(baseSnap); undo.Clear(); lastSnap = baseSnap; ViewButtons(); }
            return true;
        }
        void SetView(bool edit)
        {
            if (HW.Edit && !edit && !Settle()) { loading = true; cbView.SelectedIndex = 1; loading = false; return; }
            if (HW.Edit && !edit) cur.SaveCat();
            if (edit && split != 0) { split = 0; Relayout(); } HW.Edit = edit; btnAdd.Visible = btnOk.Visible = btnUndo.Visible = btnReset.Visible = lbHint.Visible = edit; UpdateTips();
            if (edit) { undo.Clear(); baseSnap = lastSnap = Snap(); ViewButtons(); }
            foreach (HW w in widgets) { if (w.Def.Hidden) w.Visible = edit; w.Invalidate(); } Cursor = Cursors.Default;
        }
        void ResetAll()
        {
            if (MessageBox.Show(this, "Tout remettre par défaut, comme à l'installation ?\n\n• toutes les positions et tailles\n• toutes les commandes CAT modifiées\n\nCette action est définitive pour " + cur.Name + ".", "Réinit", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            foreach (Ctl c in cur.C) { c.ResetPos(); c.ResetCat(); }
            cur.RemoveCustoms(); cur.SaveCustom(); cur.SaveLayout(); cur.SaveCat();
            foreach (Ctl c in cur.C) if (c.Local) Cfg.Set("local." + c.Id, "");
            undo.Clear(); BuildRadio(); rig.V.Clear(); LocalDefaults(); rig.Version++; baseSnap = lastSnap = Snap(); ViewButtons();
        }
        // ============================================================ connexion
        void ToggleConnect()
        {
            if (rig.Link != null) { bool lostRig = !rig.Connected && rig.Status.StartsWith("Plus de réponse"); rig.Disconnect(); act.Port = null; act.Shares = false; btnConnect.Checked = false; btnConnect.Text = "CONNECTER"; UpdateSessUi(); if (!lostRig) return; }
            ILink link; int i = cbIface.SelectedIndex;
            if (cur.Tci && (i == 0 || i == 1)) { Warn("Ce poste se commande en TCI : choisissez « TCI » (serveur d'ExpertSDR3 ou de Thetis) ou « Poste virtuel » dans Liaison.", "Liaison"); return; }
            if (!cur.Tci && i == 3) { Warn("La liaison TCI ne sert qu'au « Poste commandé en TCI » (liste Poste). Pour un autre poste, choisissez Port COM, CAT sur TCP ou Poste virtuel.", "Liaison"); return; }
            try
            {
                if (i == 0)
                {
                    if (cbPort.Text.Length == 0) { Warn("Choisissez un port COM.", "Liaison"); return; }
                    int baud; if (!int.TryParse(cbBaud.Text, out baud)) { Warn("Vitesse invalide.", "Liaison"); return; }
                    link = new SerialLink(cbPort.Text, baud, cbStop.SelectedIndex == 1 ? 2 : 1, chkRts.Checked, chkDtr.Checked);
                }
                else if (i == 1)
                {
                    int p; if (!int.TryParse(tbPort.Text, out p) || p < 1 || p > 65535) { Warn("Port TCP invalide.", "Liaison"); return; }
                    link = new TcpLink(tbHost.Text.Trim(), p);
                }
                else if (i == 3)
                {
                    int p; if (!int.TryParse(tbPort.Text, out p) || p < 1 || p > 65535) { Warn("Port TCI invalide.", "Liaison"); return; }
                    link = new TciLink(tbHost.Text.Trim(), p);
                }
                else link = cur.Tci ? (ILink)new TciVirtualLink() : new VirtualLink(cur);
                Framing fr = cur.Civ >= 0 ? Framing.Civ : (cur.Bin ? Framing.Fixed5 : Framing.Text); ShareOpts so = ShareOpts.Load(fr, cur.Key); so.LearnReads(cur); so.Owner = CmdServer.ModelName(cur);
                if (so.Any && (cur.Leg != null || cur.Tci)) { Warn("Le partage n'est pas disponible pour ce poste (" + (cur.Tci ? "poste commandé en TCI : le serveur TCI se partage déjà tout seul" : "ancien protocole à blocs d'état") + ") : décochez-le dans « Partage ».", "Partage"); return; }
                if (so.Com.Length > 0) { bool held; lock (ShareLink.Held) held = ShareLink.Held.ContainsKey(so.Com); if (held) { Warn("Le port " + so.Com + " est déjà tenu par le partage d'un autre poste : choisissez une autre paire pour ce poste (bouton Partage).", "Partage"); return; } }
                if (so.TcpPort > 0) { bool held; lock (ShareLink.HeldTcp) held = ShareLink.HeldTcp.Contains(so.TcpPort); if (held) { Warn("Le port TCP " + so.TcpPort + " est déjà tenu par le partage d'un autre poste : choisissez un autre port pour ce poste (bouton Partage).", "Partage"); return; } }
                act.Shares = so.Any;
                if (so.Any)
                {
                    if (i == 2 && fr != Framing.Text) { Warn("Le partage sur le poste virtuel n'est disponible que pour les postes à commandes texte (Yaesu, Kenwood, Elecraft). Décochez le partage ou choisissez un port COM / TCP.", "Partage"); return; }
                    link = new ShareLink(link, so);
                }
                if (cur.Civ >= 0 && (i == 0 || i == 1))
                {
                    int ad; if (!int.TryParse(tbCiv.Text.Trim(), System.Globalization.NumberStyles.HexNumber, null, out ad) || ad < 1 || ad > 0xDF) { Warn("Adresse CI-V invalide (hexadécimal, 01 à DF).", "Liaison"); return; }
                    Cfg.Set("civ." + cur.Key, ad.ToString("X2")); link = new CivLink(link, ad);
                }
                if (cur.Bin && (i == 0 || i == 1)) link = new Y5Link(link);
                if (cur.Leg != null && (i == 0 || i == 1)) link = new YLegLink(link, cur.Leg);
            }
            catch (Exception ex) { Warn(ex.Message, "Liaison"); return; }
            string err; Cursor = Cursors.WaitCursor; bool ok = rig.Connect(cur, link, out err); Cursor = Cursors.Default;
            if (!ok) { Warn("Connexion impossible : " + err, "Liaison", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            act.Port = i == 0 ? cbPort.Text : null; btnConnect.Checked = true; btnConnect.Text = "DÉCONNECTER"; Pin(cur.Key); UpdateSessUi();
            Cfg.SetInt("iface", i); Cfg.Set("port", cbPort.Text); Cfg.Set("baud." + cur.Key, cbBaud.Text); Cfg.Set("host", tbHost.Text); Cfg.Set("tcpport." + i, tbPort.Text); Cfg.SetInt("stop." + cur.Key, cbStop.SelectedIndex); Cfg.SetBool("rts", chkRts.Checked); Cfg.SetBool("dtr", chkDtr.Checked);
            string lk = "link." + cur.Key + "."; Cfg.SetInt(lk + "iface", i); Cfg.Set(lk + "port", cbPort.Text); Cfg.Set(lk + "host", tbHost.Text); Cfg.Set(lk + "tcpport", tbPort.Text); Cfg.SetBool(lk + "rts", chkRts.Checked); Cfg.SetBool(lk + "dtr", chkDtr.Checked); Cfg.Save();
        }
        Btn btnShare;
        void ShowShare()
        {
            List<string> rp = new List<string>(); foreach (Sess z in sess) if (z.Port != null) rp.Add(z.Port); if (cbIface.SelectedIndex == 0 && cbPort.Text.Length > 0) rp.Add(cbPort.Text);
            using (ShareDlg d = new ShareDlg(cur.Key, CmdServer.ModelName(cur), rp.ToArray())) { if (d.ShowDialog(this) == DialogResult.OK) { btnShare.Checked = ShareOpts.IsOn(cur.Key); btnShare.Invalidate(); if (rig.Link != null) MessageBox.Show(this, "Le partage sera appliqué à la prochaine connexion (déconnectez puis reconnectez).", "Partage"); } }
        }
        OptionsDlg optDlg; bool importing, pendingDone;
        int LockCount() { int n = 0; foreach (HW w in widgets) n += w.LockCount; return n; }
        void ShowOptions()
        {
            if (optDlg == null || optDlg.IsDisposed)
            {
                optDlg = new OptionsDlg(rig, LockCount);
                optDlg.ThemeChanged += delegate(Dictionary<int, Color> map) { Theme.Recolor(this, map); foreach (HW w in widgets) w.Invalidate(); Invalidate(true); };
                optDlg.AllMeters += delegate(bool on) { Cfg.SetBool("analog.all", on); foreach (HW w in widgets) { HMeter m = w as HMeter; if (m != null) { m.Analog = on; Cfg.SetBool("analog." + cur.Key + "." + m.Def.Id, on); m.Invalidate(); } } Cfg.Save(); };
                optDlg.UnlockAll += delegate { foreach (HW w in widgets) w.UnlockAll(); Cfg.Save(); };
                optDlg.OpenMonitor += ShowMonitor;
                optDlg.VeilChanged += delegate { foreach (Sess z in sess) foreach (HW w in z.W) w.Invalidate(); };
                optDlg.ResetAllLocks += delegate { foreach (Sess z in sess) foreach (HW w in z.W) w.LoadLock(); };
            }
            optDlg.Show(this); optDlg.BringToFront();
        }
        void ShowExport()
        {
            using (ExportDlg d = new ExportDlg(cur, rig))
            {
                d.ShowDialog(this);
                if (d.Imported) { importing = true; Application.Restart(); }
            }
        }
        void ShowMonitor() { if (monitor == null || monitor.IsDisposed) { monitor = new MonitorForm(rig); monitor.Location = new Point(Left + 60, Top + 140); } monitor.Show(); monitor.BringToFront(); }
        void ShowHelp()
        {
            MessageBox.Show(this,
                "UTILISATION NORMALE\n• Chaque élément reproduit une touche, un bouton ou un potentiomètre de la face avant et envoie la commande CAT correspondante.\n" +
                "• Potentiomètres : glisser verticalement, molette, double-clic = zéro (Maj = réglage fin).\n• Fréquence : clic en haut/bas d'un chiffre, molette, double-clic = saisie (14074 = kHz).\n" +
                "• Molette d'accord : glisser en rond ou molette ; le pas se choisit dessous.\n• Flèches ← → : pas, ↑ ↓ : 1 kHz. Échap coupe l'émission.\n\n" +
                "VUE « config vue »\n• Tous les éléments se déplacent à la souris (coin bas-droit = taille). Valider enregistre, Retour arrière annule le dernier déplacement, Réinit remet tout par défaut comme à l'installation.\n• Clic droit : voir et changer la commande CAT (envoi, lecture, réponse). Le bouton Défaut rétablit la valeur d'origine.\n• Une pastille verte marque un élément dont la commande a été modifiée.\n\n" +
                "LIAISON\n• FT-2000 : câble CAT RS-232 ou USB, vitesse réglée dans le menu 027 CAT RATE, RTS actif (menu 029).\n• FTdx5000 : prise CAT RS-232 (ou interface USB-série), vitesse réglée dans le menu CAT RATE, RTS actif.\n• FTdx10 : câble USB, port COM « Enhanced » pour le CAT, 38400 bauds (menu CAT RATE).\n• Poste virtuel : simulateur strict des manuels CAT pour essayer sans matériel.\n\nFichiers de personnalisation : dossier data (…cat.ini, …layout.ini).\n\n" +
                "PLUSIEURS POSTES EN MÊME TEMPS\n• Choisissez un autre poste dans la liste Poste alors que le premier est connecté : il reste connecté. À droite de « Vue », dans toutes les vues : une coche par poste ouvert (☑ actif, ☐ autre ; clic = activer ce poste) et deux icônes de scission (côte à côte / l'un sous l'autre) ; chaque face avant garde les proportions de l'ensemble. Toucher un élément d'un poste le rend actif. Clic droit sur CONNECTER = connexion automatique de ce poste au lancement (AUTO). Le bouton CONNECTER est vert quand le poste répond, rouge si la connexion est absente ou perdue (couleurs dans Paramètres). Paramètres → Verrous : niveau du voile. Clic droit sur PTT = délai de sécurité TX (jusqu'à 30 min, compte à rebours, coupure automatique). Après une connexion réussie, le bouton du poste reste en haut (clic = le rouvrir avec ses réglages de liaison, clic droit = le retirer). Le partage se règle poste par poste (une paire COM différente par poste).\n\n" +
                "TOUCHE LOCK ET PARAMÈTRES\n• Paramètres → Réinitialiser les verrous : tous les verrous sautent après confirmation. Paramètres → Journal des commandes : un fichier par jour (dossier logs). Les touches actives ont un fond bleu marine (réglable aussi). La touche Lock (en haut à droite) bloque TOUT le programme sauf elle-même (clic, molette, clavier ; le logiciel connecté au serveur garde la main) et passe en rouge ; un second clic déverrouille. Sa couleur se règle aussi dans Paramètres → Couleurs.\n\nVERROUS\n• Clic droit sur un organe (bouton, potentiomètre, choix, fréquence, molette) = verrouillé : voile rouge foncé (couleur réglable) et cadre rouge, il ne répond plus. Sur une rangée (bandes, modes) seule la touche cliquée est verrouillée. Ctrl + clic droit ouvre la liste d'un choix. Échap reste actif.\n\n" +
                "SERVEUR DE COMMANDE (bouton « Serveur »)\n• HamRadioLink ouvre un serveur de commande IP (port 7809) : WSJT-X, JTDX, JS8Call, SDRLink et les autres logiciels qui savent piloter un poste par un serveur IP pilotent le poste choisi ici (fréquence A/B, mode, PTT, split, volume), quel que soit son modèle. Dans ce logiciel : type de poste « serveur IP » (son nom exact dépend du logiciel), serveur 127.0.0.1, port 7809. Si un autre programme occupe déjà ce port, fermez-le.\n\n" +
                "PARTAGE DU POSTE (bouton « Partage »)\n• Un port COM virtuel (paire com0com, installée et créée pour vous si besoin) et / ou un serveur TCP laissent OmniRig, un logiciel de log, WSJT-X… piloter le même poste en même temps. La fenêtre indique le port à choisir dans l'autre logiciel.\n\n" +
                "POSTE COMMANDÉ EN TCI\n• Choisissez « Poste commandé en TCI » (ExpertSDR3, Thetis…) puis la liaison TCI (port 40001).\n\n" +
                "JOURNAL DE TRAFIC (bouton « LOG »)\n• Ouvre le journal : liste des QSO, recherche, filtres, Ajouter (Ctrl+A), onglets pour plusieurs journaux, import / export ADIF. « Base… » choisit la base MariaDB / MySQL et la teste (installée ? port ? base ? table ?) ; sans MariaDB, HamRadioLink utilise son MariaDB intégré. « Importer / partager… » reprend le journal MariaDB d'un autre logiciel (copie ou partage direct) ou un fichier ADIF.",
                "Aide — HamRadioLink " + Version, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ============================================================ boucle d'affichage
        void OnTick(object s, EventArgs e)
        {
            int tick = Environment.TickCount; bool full = tick - lastFull > 90; if (full) lastFull = tick; bool anyTx = false;
            foreach (Sess x in sess.ToArray())
            {
                if (x != act && x.Rig.Link == null && !ServerBound(x.Def.Key)) { DropSess(x); continue; }      // poste en arrière-plan déconnecté : fermé
                if (!x.Rig.Connected) x.PendingDone = false; else if (!x.PendingDone) { x.PendingDone = true; try { Backup.ApplyPending(x.Def, x.Rig); } catch { } }
                int v = x.Rig.Version; if (x.Pane.Visible && (v != x.LastVer || full)) { x.LastVer = v; foreach (HW w in x.W) w.Sync(); }
                if (x.Rig.Tx) anyTx = true;
                if (x.Rig.PttCheck()) { notice = "Émission coupée automatiquement après " + HW.TimeoutLabel(x.Rig.PttLimit) + " (délai de sécurité, clic droit sur PTT pour le régler) — " + CmdServer.ModelName(x.Def); noticeUntil = Environment.TickCount + 15000; }
            }
            BackColor = anyTx ? Color.FromArgb(40, 8, 8) : T.Bg; stage.BackColor = BackColor; UpdateConnColor();
            int ver = rig.Version; if (ver == lastVer && !full) return; lastVer = ver;
            lbClock.Text = DateTime.UtcNow.ToString("HH:mm:ss") + " UTC";
            string st = rig.Status; Color col = rig.Connected ? T.Green : (rig.Link != null ? T.Amber : T.Dim);
            string idn = rig.SeenId.Length > 0 ? Rig.IdName(rig.SeenId) : null;
            if (rig.Connected && rig.SeenId.Length > 0 && cur.IdValue.Length > 0 && rig.SeenId != cur.IdValue) { st = "Attention : le poste répond " + (cur.Civ >= 0 ? "à l'identifiant CI-V " : "ID") + rig.SeenId + (idn != null ? " (" + idn + ")" : "") + " mais « " + cur.Name + " » est choisi. Choisissez le bon poste."; col = T.Red; }
            else if (rig.Connected) { st = "Connecté — " + (rig.Link != null ? rig.Link.Name : "") + (rig.SeenId.Length > 0 ? "  (" + (cur.Civ >= 0 ? "identifiant " : "ID") + rig.SeenId + ")" : "") + (rig.Errors > 0 ? "   ?; reçus : " + rig.Errors + " (dernier : " + rig.LastError + ")" : ""); }
            if (notice != null && Environment.TickCount - noticeUntil < 0) { st = notice; col = T.Amber; } lbStatus.Text = st; lbStatus.ForeColor = col;
            if (rig.Link == null && btnConnect.Checked) { btnConnect.Checked = false; btnConnect.Text = "CONNECTER"; }
            // poste qui ne répond plus : bouton rouge « CONNECTER » (un clic relance la connexion) ; s'il répond de nouveau : « DÉCONNECTER »
            else if (rig.Link != null && !rig.Connected && rig.Status.StartsWith("Plus de réponse")) { if (btnConnect.Text != "CONNECTER") { btnConnect.Checked = false; btnConnect.Text = "CONNECTER"; btnConnect.Invalidate(); } }
            else if (rig.Link != null && rig.Connected && btnConnect.Text == "CONNECTER") { btnConnect.Checked = true; btnConnect.Text = "DÉCONNECTER"; btnConnect.Invalidate(); }
        }

        bool IsLocked(string id) { foreach (HW w in widgets) if (w.Def.Id == id) return w.Locked; return false; }
        protected override bool ProcessCmdKey(ref Message msg, Keys key)
        {
            if (AppLocked) return true;      // programme verrouillé : aucune touche du clavier n'agit
            if (ActiveControl is TextBox || ActiveControl is ComboBox || HW.Edit) return base.ProcessCmdKey(ref msg, key);
            Ctl fa = cur.Find("fa"), fb = cur.Find("fb"); bool b = rig.Get("vs") == "1"; Ctl f = b ? fb : fa; long cf = rig.GetL(f.Id);
            long step = 100; string sv = rig.Get("step"); long sx; if (sv != null && long.TryParse(sv, out sx)) step = sx;
            bool fl = IsLocked(f.Id);      // VFO verrouillé : les flèches ne changent plus la fréquence (Échap reste actif)
            if (fl && (key == Keys.Left || key == Keys.Right || key == Keys.Up || key == Keys.Down)) return true;
            switch (key)
            {
                case Keys.Left: if (cf > 0) rig.Set(f, (cf - step).ToString()); return true;
                case Keys.Right: if (cf > 0) rig.Set(f, (cf + step).ToString()); return true;
                case Keys.Down: if (cf > 0) rig.Set(f, (cf - 1000).ToString()); return true;
                case Keys.Up: if (cf > 0) rig.Set(f, (cf + 1000).ToString()); return true;
                case Keys.Escape: { Ctl p = cur.Find("ptt"); if (p != null && rig.Tx) rig.Set(p, p.OffSend); return true; }
            }
            return base.ProcessCmdKey(ref msg, key);
        }

        // ============================================================ échelle de la fenêtre (rapport conservé, aucun ascenseur)
        void Relayout()
        {
            int cw = ClientSize.Width, ch = ClientSize.Height; if (cw < 50 || ch < 50 || WindowState == FormWindowState.Minimized) return;
            float s = Math.Min(cw / (float)DesignW, ch / (float)DesignH); int sw = (int)Math.Round(DesignW * s), sh = (int)Math.Round(DesignH * s); if (sw > cw) sw = cw; if (sh > ch) sh = ch;
            stage.SuspendLayout(); T.Scale = s;
            foreach (KeyValuePair<Control, Rectangle> kv in hdr)
            {
                Control c = kv.Key; Rectangle r = kv.Value; c.Bounds = new Rectangle((int)Math.Round(r.X * s), (int)Math.Round(r.Y * s), (int)Math.Round(r.Width * s), (int)Math.Round(r.Height * s));
                c.Font = T.Fn(c is Label && ((Label)c).Font.Bold ? "Segoe UI Semibold" : "Segoe UI", hdrPt[c], FontStyle.Regular);
                DarkCombo dc = c as DarkCombo; if (dc != null) dc.ItemHeight = Math.Max(10, (int)Math.Round(20 * s));
            }
            // face avant : sous l'en-tête, un panneau par poste affiché ; en mode scindé chacun garde les proportions de l'ensemble
            int hdrH = HW.HeaderH - HW.Shift, bodyH = DesignH - hdrH; int top = (int)Math.Round(hdrH * s); Rectangle area = new Rectangle(0, top, sw, Math.Max(10, sh - top));
            List<Sess> vis = new List<Sess>(); vis.Add(act);
            if (split != 0 && sess.Count >= 2 && !HW.Edit) { Sess other = sess[(sess.IndexOf(act) + 1) % sess.Count]; vis.Add(other); if (sess.IndexOf(vis[0]) > sess.IndexOf(vis[1])) vis.Reverse(); }
            foreach (Sess x in sess) x.Pane.Visible = vis.Contains(x);
            for (int k = 0; k < vis.Count; k++)
            {
                Sess x = vis[k]; Rectangle slot = area; float ps = s;
                if (vis.Count == 2)
                {
                    slot = split == 1 ? new Rectangle(area.X + k * (area.Width / 2), area.Y, area.Width / 2, area.Height) : new Rectangle(area.X, area.Y + k * (area.Height / 2), area.Width, area.Height / 2);
                    ps = Math.Min(slot.Width / (float)DesignW, slot.Height / (float)bodyH);
                }
                int pw = (int)Math.Round(DesignW * ps), ph = (int)Math.Round(bodyH * ps); x.Scale = ps;
                x.Pane.Bounds = new Rectangle(slot.X + (slot.Width - pw) / 2, slot.Y + (slot.Height - ph) / 2, pw, ph);
                foreach (HW w in x.W) { w.Scale = ps; w.ApplyBounds(); }
            }
            stage.Bounds = new Rectangle((cw - sw) / 2, (ch - sh) / 2, sw, sh); stage.ResumeLayout(true); Invalidate(true);
        }
        Size FitClient(int w)
        {
            Rectangle wa = Screen.FromControl(this).WorkingArea; Size nc = new Size(Width - ClientSize.Width, Height - ClientSize.Height);
            int maxW = wa.Width - nc.Width, maxH = wa.Height - nc.Height; if (w > maxW) w = maxW; if ((int)(w / Ratio) > maxH) w = (int)(maxH * Ratio); if (w < 900) w = 900;
            return new Size(w, (int)Math.Round(w / Ratio));
        }
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        struct RECT { public int Left, Top, Right, Bottom; }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0214)   // WM_SIZING : conserve le rapport de la fenêtre
            {
                RECT r = (RECT)System.Runtime.InteropServices.Marshal.PtrToStructure(m.LParam, typeof(RECT)); int edge = m.WParam.ToInt32();
                int ncw = Width - ClientSize.Width, nch = Height - ClientSize.Height; int cw = r.Right - r.Left - ncw, ch = r.Bottom - r.Top - nch;
                if (edge == 3 || edge == 6) cw = (int)Math.Round(ch * Ratio); else ch = (int)Math.Round(cw / Ratio);
                if (cw < 900) { cw = 900; ch = (int)Math.Round(cw / Ratio); }
                if (edge == 1 || edge == 4 || edge == 7) r.Left = r.Right - (cw + ncw); else r.Right = r.Left + cw + ncw;
                if (edge == 3 || edge == 4 || edge == 5) r.Top = r.Bottom - (ch + nch); else r.Bottom = r.Top + ch + nch;
                System.Runtime.InteropServices.Marshal.StructureToPtr(r, m.LParam, false); m.Result = (IntPtr)1; return;
            }
            base.WndProc(ref m);
        }

        // ============================================================ réglages mémorisés
        void Restore()
        {
            ShareOpts.MigrateGlobal(Cfg.Get("radio", ""));
            LoadLink();
            int w = Cfg.GetInt("w", 0); if (w < 900) w = (int)Math.Round(DesignW * T.Dpi / 96f * 0.9);
            Size nc = new Size(Width - ClientSize.Width, Height - ClientSize.Height); MinimumSize = new Size(nc.Width + 900, nc.Height + (int)Math.Round(900 / Ratio));
            ClientSize = FitClient(w); CenterToScreen(); UpdateCiv(); Relayout(); StartServer(); AutoConnect();
        }
        // verrou général : tout le programme est bloqué sauf la touche Lock (rouge quand verrouillé)
        public static bool AppLocked; Btn btnLock, icLock, icLog;
        void ToggleAppLock() { AppLocked = !AppLocked; ApplyAppLock(); }
        void ApplyAppLock()
        {
            bool l = AppLocked; if (btnLock != null) { btnLock.Checked = l; btnLock.Invalidate(); } if (icLock != null) { icLock.Checked = l; icLock.Invalidate(); }
            foreach (Control c in stage.Controls) { bool en = !l || c == btnLock || c == btnLog; if (c.Enabled != en) { c.Enabled = en; c.Invalidate(); } }
            if (icBar != null) foreach (Control c in icBar.Controls) { bool en = !l || c == icLock || c == icLog; if (c.Enabled != en) c.Enabled = en; }
            if (ic != null) ic.Enabled = !l;
            foreach (Form f in Application.OpenForms) if (f != this && !(f.GetType().Namespace ?? "").StartsWith("HamRadioLink.Journal")) f.Enabled = !l;      // le journal (LOG) n'est jamais verrouillé
            Text = Text.Replace(" — VERROUILLÉ", "") + (l ? " — VERROUILLÉ" : "");
        }
        // serveur de commande IP (WSJT-X, JTDX, SDRLink…, port 7809)
        CmdServer cmdSrv, cmdSrv2; Btn btnSrv, btnLog;
        // Bouton LOG : le journal de trafic (base MariaDB), dans une fenêtre à part ; la fréquence et le mode du poste actuel sont proposés pour un nouveau QSO.
        void ShowLog()
        {
            Journal.RigState.NameFn = delegate { if (ic != null && cur != null && cur.Key == "ic7760") return ic.MainHz > 0 ? "IC-7760" : ""; return act == null ? "" : CmdServer.ModelName(act.Def); };
            Journal.RigState.HzFn = delegate { if (ic != null && cur != null && cur.Key == "ic7760") return ic.MainHz; return act == null ? 0 : act.Rig.GetL("fa"); };
            Journal.RigState.TuneFn = delegate(long hz, string modes)
            {
                try
                {
                    if (!Journal.RigState.Enabled) return; if (ic != null && cur != null && cur.Key == "ic7760") { ic.TuneTo(hz); return; } if (act == null) return; Ctl f = act.Def.Find("fa"); if (f != null) act.Rig.Set(f, hz.ToString());
                    Ctl m = act.Def.Find("md0"); if (m == null || m.ItemList == null || string.IsNullOrEmpty(modes)) return;
                    foreach (string want in modes.Split('|')) foreach (Item it in m.ItemList) if (string.Equals(it.Label, want, StringComparison.OrdinalIgnoreCase)) { act.Rig.Set(m, it.Code); return; }
                }
                catch { }
            };
            Journal.Hub.ShowMain = BringBack;
            Journal.Hub.LogClosed = delegate { if (!Visible && !quitting) { quitting = true; Close(); } };
            Journal.RigState.HzBFn = delegate { if (ic != null && cur != null && cur.Key == "ic7760") return ic.SubHz; return act == null ? 0 : act.Rig.GetL("fb"); };
            Journal.RigState.ModeFn = delegate { try { if (act == null) return ""; Ctl m = act.Def.Find("md0"); string v = act.Rig.Get("md0"); if (m == null || v == null || m.ItemList == null) return ""; foreach (Item it in m.ItemList) if (it.Match(v)) return it.Label; } catch { } return ""; };
            Journal.Hub.Open(this);
        }
        // un poste lié à un serveur de commande n'est jamais fermé automatiquement (le logiciel client verrait le poste changer)
        bool ServerBound(string key) { return (Cfg.GetBool("srv.on", false) && Cfg.Get("srv.radio", "") == key) || (Cfg.GetBool("srv2.on", false) && Cfg.Get("srv2.radio", "") == key); }
        void StartServer()
        {
            if (cmdSrv != null) { cmdSrv.Dispose(); cmdSrv = null; } if (cmdSrv2 != null) { cmdSrv2.Dispose(); cmdSrv2 = null; }
            if (Cfg.GetBool("srv.on", false) && Cfg.Get("srv.radio", "").Length == 0 && sess.Count > 0) { Cfg.Set("srv.radio", sess[0].Def.Key); Cfg.Save(); }      // le premier serveur est lié à UN poste, mémorisé
            if (btnSrv != null) { btnSrv.Checked = false; btnSrv.Invalidate(); }
            try
            {
                // le premier serveur reste sur le PREMIER poste ouvert ; le second serveur (autre port) pilote le poste choisi dans la fenêtre Serveur
                if (Cfg.GetBool("srv.on", false)) cmdSrv = new CmdServer(delegate { string k1 = Cfg.Get("srv.radio", ""); Sess x = sess.Find(delegate(Sess y) { return y.Def.Key == k1; }); return x != null ? x.Rig : null; }, Cfg.GetInt("srv.port", 7809), Cfg.GetBool("srv.net", false));
                if (Cfg.GetBool("srv2.on", false)) { string key = Cfg.Get("srv2.radio", ""); cmdSrv2 = new CmdServer(delegate { Sess x = sess.Find(delegate(Sess y) { return y.Def.Key == key; }); return x != null ? x.Rig : null; }, Cfg.GetInt("srv2.port", 7810), Cfg.GetBool("srv.net", false)); }
                if (btnSrv != null) { btnSrv.Checked = cmdSrv != null || cmdSrv2 != null; btnSrv.Invalidate(); }
            }
            catch (Exception ex) { MessageBox.Show(this, "Serveur de commande impossible : " + ex.Message + "\n\n(un autre programme occupe-t-il déjà ce port ?)", "Serveur de commande", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
        void ShowServer()
        {
            ServerDlg.State = delegate
            {
                string a = cmdSrv == null ? "Serveur 1 arrêté." : "Serveur 1 (" + (sess.Count > 0 ? CmdServer.ModelName(sess[0].Def) : "") + ") : port " + cmdSrv.Port + " — " + cmdSrv.Clients + " logiciel(s) connecté(s), " + cmdSrv.Commands + " commande(s).";
                if (cmdSrv2 != null) a += "\nServeur 2 (" + Cfg.Get("srv2.radio", "?") + ") : port " + cmdSrv2.Port + " — " + cmdSrv2.Clients + " logiciel(s) connecté(s), " + cmdSrv2.Commands + " commande(s).";
                return a;
            };
            ServerDlg.First = sess.Count > 0 ? sess[0].Def.Key : ""; ServerDlg.Others = new List<string[]>(); ServerDlg.AllRadios = new List<string[]>(); foreach (RadioDef rd in radios) ServerDlg.AllRadios.Add(new string[] { rd.Key, CmdServer.ModelName(rd) });
            foreach (Sess z in sess) ServerDlg.Others.Add(new string[] { z.Def.Key, CmdServer.ModelName(z.Def) });
            using (ServerDlg d = new ServerDlg()) { if (d.ShowDialog(this) == DialogResult.OK) StartServer(); }
        }
        bool quitting;
        // Rappelle la télécommande masquée (clic sur le journal, ou second lancement du programme).
        readonly List<RadioDef> wasConnected = new List<RadioDef>();
        // Télécommande fermée (journal resté ouvert) : les postes sont déconnectés ; ils se reconnectent quand la télécommande revient.
        void DisconnectForHide()
        {
            wasConnected.Clear(); Sess keep = act;
            foreach (Sess s in new List<Sess>(sess)) { if (s.Rig.Link == null) continue; wasConnected.Add(s.Def); try { Activate(s); quiet = true; try { ToggleConnect(); } finally { quiet = false; } } catch { } }
            try { if (keep != null) Activate(keep); } catch { }
        }
        void ReconnectAfterHide()
        {
            if (wasConnected.Count == 0) return; Sess keep = act; List<RadioDef> l = new List<RadioDef>(wasConnected); wasConnected.Clear();
            foreach (RadioDef r in l) { Sess s = sess.Find(delegate(Sess x) { return x.Def == r; }); if (s == null) continue; try { Activate(s); if (rig.Link == null) { quiet = true; try { ToggleConnect(); } finally { quiet = false; } } } catch { } }
            try { if (keep != null) Activate(keep); } catch { }
        }
        public void BringBack() { Journal.RigState.Enabled = true; Show(); wasConnected.Clear();   /* postes laissés déconnectés */ if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal; Activate(); }
        void OnClosing(object s, FormClosingEventArgs e)
        {
            // Journal ouvert : fermer la télécommande la masque seulement ; le programme s'arrête à la fermeture du journal.
            if (!quitting && e.CloseReason == CloseReason.UserClosing && Journal.Hub.IsOpen)
            {
                e.Cancel = true; Journal.RigState.Enabled = false;
                try { foreach (Sess x in sess) { Ctl p = x.Def.Find("ptt"); if (p != null && x.Rig.Link != null && x.Rig.Tx) x.Rig.Set(p, p.OffSend); } System.Threading.Thread.Sleep(80); } catch { }
                DisconnectForHide(); Hide();
                Journal.Hub.ShowMain = BringBack;
                Journal.Hub.LogClosed = delegate { if (!Visible && !quitting) { quitting = true; Close(); } };
                return;
            }
            quitting = true;
            if (AppLocked && e.CloseReason == CloseReason.UserClosing && MessageBox.Show(this, "Le programme est verrouillé (touche Lock). Quitter quand même ?", "Verrouillé", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) { e.Cancel = true; return; }
            try { if (!Settle()) { e.Cancel = true; return; } } catch { }
            try { SaveLink(); } catch { } try { if (ic != null) ic.Shutdown(); } catch { } try { Journal.Hub.Shutdown(); } catch { }
            try
            {
                foreach (Sess x in sess) { Ctl p = x.Def.Find("ptt"); if (p != null && x.Rig.Link != null && x.Rig.Tx) x.Rig.Set(p, p.OffSend); }
                if (HW.Edit) cur.SaveCat();
                if (!importing) { Cfg.SetInt("w", ClientSize.Width); Cfg.Set("radio", cur.Key); Cfg.Save(); } System.Threading.Thread.Sleep(80); foreach (Sess x in sess) x.Rig.Disconnect();
            }
            catch { }
        }

        // ============================================================ essais automatiques (captures d'écran)
        public void AutoStart(string how, string radio)
        {
            if (radio != null) for (int i = 0; i < radios.Count; i++) if (radios[i].Key == radio) cbRadio.SelectedIndex = i;
            if (ic != null) { if (how == "virtual") ic.AutoStart("virtual"); return; }
            if (how == "virtual") { cbIface.SelectedIndex = 2; ToggleConnect(); }
        }
        public void ShotAndExit(string path, int ms, string actions)
        {
            System.Windows.Forms.Timer t = new System.Windows.Forms.Timer(); t.Interval = Math.Max(800, ms);
            if (actions != null && actions.Length > 0) { System.Windows.Forms.Timer a = new System.Windows.Forms.Timer(); a.Interval = Math.Max(400, ms / 2); a.Tick += delegate { a.Stop(); TestActions(actions); }; a.Start(); }
            t.Tick += delegate
            {
                t.Stop();
                try { using (Bitmap b = new Bitmap(Width, Height)) { if (Environment.GetEnvironmentVariable("HRL_SCREEN") == "1") { Activate(); Application.DoEvents(); using (Graphics sg = Graphics.FromImage(b)) sg.CopyFromScreen(Left, Top, 0, 0, b.Size); } else DrawToBitmap(b, new Rectangle(0, 0, b.Width, b.Height)); b.Save(path, System.Drawing.Imaging.ImageFormat.Png); } ShotEditor(path); } catch (Exception ex) { System.IO.File.WriteAllText(path + ".err", ex.ToString()); }
                Close();
            };
            t.Start();
        }
        Form shotEd;
        void TestActions(string actions)
        {
            foreach (string a in actions.Split('+'))
            {
                switch (a)
                {
                    case "icopt": if (ic != null) ic.Test("optshot"); break;
                    case "analog": foreach (HW w in widgets) { HMeter m = w as HMeter; if (m != null) m.Analog = true; } break;
                    case "lock": { int n = 0; foreach (HW w in widgets) if ((w.Def.K == Kind.Knob || w.Def.K == Kind.Toggle) && n++ < 3) w.Locked = true; } break;
                    case "opts": ShowOptions(); break;
                    case "share": { ShareDlg sd = new ShareDlg(cur.Key, CmdServer.ModelName(cur), new string[0]); sd.StartPosition = FormStartPosition.CenterScreen; sd.Show(this); } break;
                    case "server": { ServerDlg sv = new ServerDlg(); sv.StartPosition = FormStartPosition.CenterScreen; sv.Show(this); } break;
                    case "applock": ToggleAppLock(); break;
                    case "txbtn": { Ctl p = cur.Find("ptt"); if (p != null) { Cfg.SetInt("ptt.timeout." + cur.Key, 300); rig.Set(p, p.OnSend); rig.PttButtonStart(); } } break;
                    case "two": { int ix = radios.FindIndex(delegate(RadioDef r) { return r.Key == "ft991a"; }); cbRadio.SelectedIndex = ix; cbIface.SelectedIndex = 2; ToggleConnect(); } break;
                    case "splitv": SetSplit(1); break;
                    case "splith": SetSplit(2); break;
                    case "next": NextSess(); break;
                    case "pair": { PairDlg pd = new PairDlg("COM22", "COM23"); pd.StartPosition = FormStartPosition.CenterScreen; pd.Show(this); } break;
                    case "mon": ShowMonitor(); System.Windows.Forms.Timer mt = new System.Windows.Forms.Timer(); mt.Interval = 2500; mt.Tick += delegate { mt.Stop(); System.IO.File.WriteAllText("mon.txt", "vis=" + monitor.Visible + " " + monitor.Bounds + " lines=" + monitor.Controls.Count); monitor.Close(); System.IO.File.AppendAllText("mon.txt", " closed=" + monitor.IsDisposed); }; mt.Start(); break;
                    case "backup": { string s = Backup.Dump(cur, rig); System.IO.File.WriteAllText("bk.txt", s); int n = Backup.Import(s); System.IO.File.AppendAllText("bk.txt", "# import=" + n + " pending=" + Cfg.Get("pending." + cur.Key, "").Length); } break;
                    case "noframes": foreach (HW w in widgets) if (w.Def.K == Kind.Frame) w.Visible = false; break;
                    case "dump": { StringBuilder sb = new StringBuilder(); sb.AppendLine("cur=" + cur.Key + " stage=" + stage.Visible + " " + stage.Bounds + " n=" + widgets.Count); int q = 0; foreach (HW w in widgets) if (q++ < 12) sb.AppendLine(w.Def.Id + " " + w.Def.K + " vis=" + w.Visible + " " + w.Bounds + " par=" + (w.Parent == stage)); foreach (HW w in widgets) if (w.Def.Id == "vox") { try { using (Bitmap bb = new Bitmap(w.Width, w.Height)) { w.DrawToBitmap(bb, new Rectangle(0, 0, w.Width, w.Height)); sb.AppendLine("paint ok idx=" + stage.Controls.GetChildIndex(w) + " frameidx=" + stage.Controls.GetChildIndex(widgets[0]) + " ctl=" + stage.Controls.Count); bb.Save("vox.png"); } } catch (Exception ex) { sb.AppendLine(ex.ToString()); } } System.IO.File.WriteAllText("dump.txt", sb.ToString()); } break;
                    case "config": cbView.SelectedIndex = 1; break;
                    case "tx": { Ctl p = cur.Find("ptt"); if (p != null) rig.Set(p, p.OnSend); } break;
                    case "small": ClientSize = new Size(900, 474); break;
                    case "editor": { Ctl c = cur.Find("agc") ?? cur.Find("af"); shotEd = new CmdEditor(c, rig); shotEd.StartPosition = FormStartPosition.Manual; shotEd.Location = new Point(Left + 100, Top + 60); shotEd.Show(this); } break;
                    case "add": { shotEd = new AddDialog(cur); shotEd.StartPosition = FormStartPosition.Manual; shotEd.Location = new Point(Left + 100, Top + 60); shotEd.Show(this); } break;
                }
            }
        }
        public void ShotEditor(string path) { if (shotEd != null) using (Bitmap b = new Bitmap(shotEd.Width, shotEd.Height)) { shotEd.DrawToBitmap(b, new Rectangle(0, 0, b.Width, b.Height)); b.Save(path + ".editor.png", System.Drawing.Imaging.ImageFormat.Png); } }
    }
}
