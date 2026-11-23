using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace HamRadioLink
{
    // Menu du FTdx10 (même organisation que l'écran du poste : RADIO / CW / OPERATION / DISPLAY SETTING, plus une page de réglages rapides), d'après le « FTDX10 CAT Operation Reference Manual »,
    // tableau 2 (commande EX P1 P2 P3 P4 ; P1 groupe, P2 sous-groupe, P3 ligne). Les valeurs sont lues dans le poste et modifiables ; celles changées sur le poste sont relues toutes les 5 s.
    // Fenêtre non bloquante, ouverte par la touche MENU. Un réglage refusé par le poste est annoncé par un bandeau rouge.
    class MenuLine
    {
        public string P1, P2, P3, Name, Kind; public List<string[]> Ch = new List<string[]>(); public int Min, Max, Width, Step = 1, Div = 1; public string Unit = "";
        public string Cmd { get { return "EX" + P1 + P2 + P3; } }
        // texte de la valeur (code reçu du poste -> libellé)
        public string Label(string code)
        {
            if (Kind == "C") { foreach (string[] c in Ch) if (c[0] == code) return c[1]; return code; }
            int v; if (int.TryParse(code.Replace("+", ""), out v)) return Num(v); return code;
        }
        public string Num(int v) { return Div == 1 ? v.ToString(CultureInfo.InvariantCulture) : (v / (double)Div).ToString("0.0", CultureInfo.InvariantCulture); }
        public string Encode(int v) { return Kind == "S" ? (v < 0 ? "-" : "+") + Math.Abs(v).ToString("00") : v.ToString(new string('0', Width)); }
    }

    static class MenuData
    {
        public static readonly string[] Groups = { "RADIO SETTING", "CW SETTING", "OPERATION SETTING", "DISPLAY SETTING" };
        public static readonly string[][] Subs = {
            new[] { "MODE SSB", "MODE AM", "MODE FM", "MODE PSK/DATA", "MODE RTTY", "ENCDEC PSK", "ENCDEC RTTY" },
            new[] { "MODE CW", "KEYER", "DECODE CW" },
            new[] { "GENERAL", "RX DSP", "TX AUDIO", "TX GENERAL", "TUNING" },
            new[] { "DISPLAY", "SCOPE", "EXT MONITOR" } };

        const string AF = "AF TREBLE GAIN|S:-10,10;AF MIDDLE TONE GAIN|S:-10,10;AF BASS GAIN|S:-10,10;AGC FAST DELAY|N:20,4000,4,20,1,msec;AGC MID DELAY|N:20,4000,4,20,1,msec;AGC SLOW DELAY|N:20,4000,4,20,1,msec;";
        const string CUT = "LCUT FREQ|G:lcut;LCUT SLOPE|C:0=6 dB/oct,1=18 dB/oct;HCUT FREQ|G:hcut;HCUT SLOPE|C:0=6 dB/oct,1=18 dB/oct;";
        const string BPF = "TX BPF SEL|C:0=50 ~ 3050,1=100 ~ 2900,2=200 ~ 2800,3=300 ~ 2700,4=400 ~ 2600;";
        const string REAR = "REAR SELECT|C:0=DATA,1=USB;RPORT GAIN|N:0,100,3;RPTT SELECT|C:0=DAKY,1=RTS,2=DTR;";
        const string MS = "C:0=MIC,1=REAR";
        const string EN = "C:0=DISABLE,1=ENABLE";
        const string NR = "C:0=NOR,1=REV";
        const string RATE = "C:0=4800 bps,1=9600 bps,2=19200 bps,3=38400 bps";
        const string TMO = "C:0=10 msec,1=100 msec,2=1000 msec,3=3000 msec";

        // « P1 P2 : lignes » ; chaque ligne « nom|spec », séparées par « ; » (virgules à l'intérieur des listes de choix)
        static readonly string[][] Tab = {
            new[] { "0101", AF + CUT + "SSB OUT LEVEL|N:0,100,3;" + BPF + "SSB MOD SOURCE|" + MS + ";" + REAR },
            new[] { "0102", AF + CUT + "AM OUT LEVEL|N:0,100,3;" + BPF + "AM MOD SOURCE|" + MS + ";MIC GAIN|G:micg;" + REAR },
            new[] { "0103", AF + CUT + "FM OUT LEVEL|N:0,100,3;FM MOD SOURCE|" + MS + ";MIC GAIN|G:micg;" + REAR + "RPT SHIFT(28MHz)|N:0,1000,4,10,1,kHz;RPT SHIFT(50MHz)|N:0,4000,4,10,1,kHz;RPT|C:0=-,1=SIMPLEX,2=+;TONE FREQ|G:ctcss;ENC/DEC|C:0=OFF,1=ENC,2=TSQ;" },
            new[] { "0104", AF + "PSK TONE|C:0=1000 Hz,1=1500 Hz,2=2000 Hz;DATA SHIFT (SSB)|N:0,3000,4,10,1,Hz;" + CUT + "DATA OUT LEVEL|N:0,100,3;" + BPF + "DATA MOD SOURCE|" + MS + ";" + REAR },
            new[] { "0105", AF + "POLARITY-RX|" + NR + ";POLARITY-TX|" + NR + ";" + CUT + "RTTY OUT LEVEL|N:0,100,3;SHIFT PORT|C:0=SHIFT,1=DTR,2=RTS;MARK FREQUENCY|C:0=1275 Hz,1=2125 Hz;SHIFT FREQUENCY|C:0=170 Hz,1=200 Hz,2=425 Hz,3=850 Hz;" },
            new[] { "0106", "PSK MODE|C:0=BPSK,1=QPSK;DECODE AFC RANGE|C:0=8 Hz,1=15 Hz,2=30 Hz;QPSK POLARITY RX|" + NR + ";QPSK POLARITY TX|" + NR + ";PSK TX LEVEL|N:0,100,3;" },
            new[] { "0107", "RX USOS|" + EN + ";TX USOS|" + EN + ";RX NEW LINE CODE|C:0=CR or LF or CR+LF,1=CR / LF / CR+LF;TX AUTO CR+LF|" + EN + ";TX DIDDLE|C:0=OFF,1=BLANK,2=LTRS;BAUDOT CODE|C:0=CCIT,1=US;" },
            new[] { "0201", AF + CUT + "CW OUT LEVEL|N:0,100,3;CW AUTO MODE|C:0=OFF,1=50 MHz,2=ON;CW BK-IN TYPE|C:0=SEMI,1=FULL;CW WAVE SHAPE|C:0=1 msec,1=2 msec,2=4 msec,3=6 msec;CW FREQ DISPLAY|C:0=DIRECT FREQ,1=PITCH OFFSET;PC KEYING|C:0=OFF,1=DAKY,2=RTS,3=DTR;QSK DELAY TIME|C:0=15 msec,1=20 msec,2=25 msec,3=30 msec;CW INDICATOR|C:0=OFF,1=ON;" },
            new[] { "0202", "KEYER TYPE|C:0=OFF,1=BUG,2=ELEKEY-A,3=ELEKEY-B,4=ELEKEY-Y,5=ACS;KEYER DOT/DASH|" + NR + ";CW WEIGHT|N:25,45,2,1,10;NUMBER STYLE|C:0=1290,1=AUNO,2=AUNT,3=A2NO,4=A2NT,5=12NO,6=12NT;CONTEST NUMBER|N:1,9999,4;CW MEMORY 1|C:0=TEXT,1=MESSAGE;CW MEMORY 2|C:0=TEXT,1=MESSAGE;CW MEMORY 3|C:0=TEXT,1=MESSAGE;CW MEMORY 4|C:0=TEXT,1=MESSAGE;CW MEMORY 5|C:0=TEXT,1=MESSAGE;REPEAT INTERVAL|N:1,60,2,1,1,sec;" },
            new[] { "0203", "CW DECODE BW|C:0=25 Hz,1=50 Hz,2=100 Hz,3=250 Hz;" },
            new[] { "0301", "NB WIDTH|C:0=1 ms,1=3 ms,2=10 ms;NB REJECTION|C:0=10 dB,1=30 dB,2=50 dB;BEEP LEVEL|N:0,100,3;RF/SQL VR|C:0=RF,1=SQL;TUNER SELECT|C:0=INT,1=EXT1,2=EXT2,3=EXT3;232C RATE|" + RATE + ";232C TIME OUT TIMER|" + TMO + ";CAT RATE|" + RATE + ";CAT TIME OUT TIMER|" + TMO + ";CAT RTS|" + EN + ";QMB CH|C:0=5 ch,1=10 ch;MEM GROUP|" + EN + ";QUICK SPLIT INPUT|" + EN + ";QUICK SPLIT FREQ|S:-20,20,2,1,1,kHz;SPLIT FREQ DISPLAY|C:0=FREQ,1=DELTA;TX TIME OUT TIMER|N:0,30,2,1,1,min;MIC SCAN|" + EN + ";MIC SCAN RESUME|C:0=PAUSE,1=TIME;REF FREQ ADJ|S:-25,25,2;KEYBOARD LANGUAGE|C:00=JAPANESE,01=ENGLISH(US),02=ENGLISH(UK),03=FRENCH,04=FRENCH(CA),05=GERMAN,06=PORTUGUESE,07=PORTUGUESE(BR),08=SPANISH,09=SPANISH(LATAM),10=ITALIAN;" },
            new[] { "0302", "APF WIDTH|C:0=NARROW,1=MEDIUM,2=WIDE;CONTOUR LEVEL|S:-40,20;CONTOUR WIDTH|N:1,11,2;IF NOTCH WIDTH|C:0=NARROW,1=WIDE;" },
            new[] { "0303", "AMC RELEASE TIME|C:0=FAST,1=MID,2=SLOW;PRMTRC EQ1 FREQ|G:eq1;PRMTRC EQ1 LEVEL|S:-20,10;PRMTRC EQ1 BWTH|N:1,10,2;PRMTRC EQ2 FREQ|G:eq2;PRMTRC EQ2 LEVEL|S:-20,10;PRMTRC EQ2 BWTH|N:1,10,2;PRMTRC EQ3 FREQ|G:eq3;PRMTRC EQ3 LEVEL|S:-20,10;PRMTRC EQ3 BWTH|N:1,10,2;P PRMTRC EQ1 FREQ|G:eq1;P PRMTRC EQ1 LEVEL|S:-20,10;P PRMTRC EQ1 BWTH|N:1,10,2;P PRMTRC EQ2 FREQ|G:eq2;P PRMTRC EQ2 LEVEL|S:-20,10;P PRMTRC EQ2 BWTH|N:1,10,2;P PRMTRC EQ3 FREQ|G:eq3;P PRMTRC EQ3 LEVEL|S:-20,10;P PRMTRC EQ3 BWTH|N:1,10,2;" },
            new[] { "0304", "HF MAX POWER|N:5,100,3,1,1,W;50M MAX POWER|N:5,100,3,1,1,W;70M MAX POWER|N:5,50,3,1,1,W;AM MAX POWER|N:5,25,3,1,1,W;VOX SELECT|C:0=MIC,1=DATA;DATA VOX GAIN|N:0,100,3;EMERGENCY FREQ TX|" + EN + ";" },
            new[] { "0305", "SSB/CW DIAL STEP|C:0=5 Hz,1=10 Hz;RTTY/PSK DIAL STEP|C:0=5 Hz,1=10 Hz;CH STEP|C:0=1 kHz,1=2.5 kHz,2=5 kHz,3=10 kHz;AM CH STEP|C:0=2.5 kHz,1=5 kHz,2=9 kHz,3=10 kHz,4=12.5 kHz,5=25 kHz;FM CH STEP|C:0=5 kHz,1=6.25 kHz,2=10 kHz,3=12.5 kHz,4=20 kHz,5=25 kHz;MAIN STEPS PER REV.|C:1=250,2=500,3=1000;MPVD STEPS PER REV.|C:1=250,2=500;" },
            new[] { "0401", "MY CALL|T:12;MY CALL TIME|C:0=OFF,1=0.5 sec,2=1 sec,3=2 sec,4=3 sec,5=5 sec;SCREEN SAVER|C:0=OFF,1=15 min,2=30 min,3=60 min;DIMMER LED|N:0,20,2;MOUSE POINTER SPEED|N:0,20,2;" },
            new[] { "0402", "RBW|C:0=HIGH,1=MID,2=LOW;SCOPE CTR|C:0=FILTER,1=CAR POINT;2D DISP SENSITIVITY|C:0=NORMAL,1=HI;3DSS DISP SENSITIVITY|C:0=NORMAL,1=HI;" },
            new[] { "0403", "EXT DISPLAY|" + EN + ";PIXEL|C:0=800x480,1=800x600;" } };

        static void Gen(MenuLine l, string g)
        {
            l.Kind = "C";
            switch (g)
            {
                case "lcut": l.Ch.Add(new[] { "00", "OFF" }); for (int i = 1; i <= 19; i++) l.Ch.Add(new[] { i.ToString("00"), (100 + 50 * (i - 1)) + " Hz" }); break;
                case "hcut": l.Ch.Add(new[] { "00", "OFF" }); for (int i = 1; i <= 67; i++) l.Ch.Add(new[] { i.ToString("00"), (700 + 50 * (i - 1)) + " Hz" }); break;
                case "eq1": l.Ch.Add(new[] { "00", "OFF" }); for (int i = 1; i <= 7; i++) l.Ch.Add(new[] { i.ToString("00"), (100 * i) + " Hz" }); break;
                case "eq2": l.Ch.Add(new[] { "00", "OFF" }); for (int i = 1; i <= 9; i++) l.Ch.Add(new[] { i.ToString("00"), (700 + 100 * (i - 1)) + " Hz" }); break;
                case "eq3": l.Ch.Add(new[] { "00", "OFF" }); for (int i = 1; i <= 18; i++) l.Ch.Add(new[] { i.ToString("00"), (1500 + 100 * (i - 1)) + " Hz" }); break;
                case "ctcss":
                    { string[] t = "67.0,69.3,71.9,74.4,77.0,79.7,82.5,85.4,88.5,91.5,94.8,97.4,100.0,103.5,107.2,110.9,114.8,118.8,123.0,127.3,131.8,136.5,141.3,146.2,151.4,156.7,159.8,162.2,165.5,167.9,171.3,173.8,177.3,179.9,183.5,186.2,189.9,192.8,196.6,199.5,203.5,206.5,210.7,218.1,225.7,229.1,233.6,241.8,250.3,254.1".Split(',');
                      for (int i = 0; i < t.Length; i++) l.Ch.Add(new[] { i.ToString("00"), t[i] + " Hz" }); }
                    break;
                case "micg": l.Ch.Add(new[] { "1000", "MCVR" }); for (int i = 0; i <= 100; i++) l.Ch.Add(new[] { i.ToString("0000"), "FIX " + i }); break;
            }
        }

        public static List<MenuLine> Lines(int g, int s)
        {
            string key = "0" + (g + 1) + "0" + (s + 1); List<MenuLine> r = new List<MenuLine>();
            foreach (string[] t in Tab)
            {
                if (t[0] != key) continue; int n = 0;
                foreach (string it in t[1].Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    int b = it.IndexOf('|'); string name = it.Substring(0, b), spec = it.Substring(b + 1); n++;
                    MenuLine l = new MenuLine { P1 = key.Substring(0, 2), P2 = key.Substring(2, 2), P3 = n.ToString("00"), Name = name };
                    string k = spec.Substring(0, 1); string body = spec.Substring(2);
                    if (k == "G") Gen(l, body);
                    else if (k == "C") { l.Kind = "C"; foreach (string e in body.Split(',')) { int q = e.IndexOf('='); l.Ch.Add(new[] { e.Substring(0, q), e.Substring(q + 1) }); } }
                    else if (k == "N" || k == "S")
                    {
                        l.Kind = k; string[] p = body.Split(','); l.Min = int.Parse(p[0]); l.Max = int.Parse(p[1]); l.Width = k == "S" ? 3 : (p.Length > 2 ? int.Parse(p[2]) : l.Max.ToString().Length);
                        if (p.Length > 3) l.Step = int.Parse(p[3]); if (p.Length > 4) l.Div = int.Parse(p[4]); if (p.Length > 5) l.Unit = p[5];
                        if (k == "N" && l.Name == "CW WEIGHT") l.Div = 10;
                    }
                    else l.Kind = "T";
                    r.Add(l);
                }
            }
            return r;
        }
    }

    public class Ftdx10MenuForm : Form
    {
        static Ftdx10MenuForm inst;
        public static void Open(Rig rig, Form owner)
        {
            if (inst != null && !inst.IsDisposed) { inst.WindowState = FormWindowState.Normal; inst.BringToFront(); inst.Activate(); return; }
            inst = new Ftdx10MenuForm(rig); inst.Show(owner);
        }

        static readonly Color Back = Color.FromArgb(34, 36, 40), Pan = Color.FromArgb(52, 54, 60), Txt = Color.FromArgb(228, 228, 228), Acc = Color.FromArgb(255, 168, 40), Edit = Color.Black;
        readonly Rig rig; readonly Button[] tabs = new Button[5]; readonly ListBox subs; readonly Panel rows; readonly Label status, banner;
        int grp = -1, sub; readonly List<MenuLine> cur = new List<MenuLine>(); readonly Dictionary<MenuLine, Control> ed = new Dictionary<MenuLine, Control>();
        bool loading, busy; DateTime lastEdit = DateTime.MinValue; System.Windows.Forms.Timer poll, bannerTm;
        readonly Dictionary<Control, DateTime> pending = new Dictionary<Control, DateTime>(); System.Windows.Forms.Timer deb;

        Ftdx10MenuForm(Rig rig)
        {
            this.rig = rig; AutoScaleMode = AutoScaleMode.None; Text = "MENU — " + rig.Def.Name; FormBorderStyle = FormBorderStyle.Sizable; MinimumSize = new Size(820, 520);
            // hauteur : la plus grande possible dans la zone utile de l'écran (barre des tâches exclue), sans sortir de l'écran ; largeur 980
            Rectangle wa = (Application.OpenForms.Count > 0 ? Screen.FromControl(Application.OpenForms[0]) : Screen.PrimaryScreen).WorkingArea; int nonClient = SystemInformation.CaptionHeight + SystemInformation.FrameBorderSize.Height * 2 + 8;
            ClientSize = new Size(Math.Min(980, wa.Width - 24), Math.Max(480, wa.Height - nonClient)); StartPosition = FormStartPosition.Manual; Location = new Point(wa.Left + Math.Max(0, (wa.Width - Width) / 2), wa.Top);
            BackColor = Back; ForeColor = Txt; Font = new Font("Segoe UI", 9.5f); Icon = Application.OpenForms.Count > 0 ? Application.OpenForms[0].Icon : null;
            string[] tn = { "MENU RAPIDE", "RADIO SETTING", "CW SETTING", "OPERATION SETTING", "DISPLAY SETTING" };
            for (int i = 0; i < 5; i++)
            {
                int k = i - 1; Button b = new Button { Text = tn[i], Left = 10 + i * 190, Top = 10, Width = 184, Height = 38, FlatStyle = FlatStyle.Flat, BackColor = Pan, ForeColor = Color.FromArgb(110, 150, 255), Font = new Font(Font.FontFamily, 10f, FontStyle.Bold), UseVisualStyleBackColor = false };
                b.FlatAppearance.BorderColor = Color.FromArgb(90, 90, 100); b.Click += delegate { SelectGroup(k); }; Controls.Add(b); tabs[i] = b;
            }
            subs = new ListBox { Left = 10, Top = 58, Width = 200, Height = ClientSize.Height - 112, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left, BackColor = Pan, ForeColor = Txt, BorderStyle = BorderStyle.None, IntegralHeight = false, ItemHeight = 34, DrawMode = DrawMode.OwnerDrawFixed, Font = new Font(Font.FontFamily, 10.5f, FontStyle.Bold) };
            subs.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return; bool sel = (e.State & DrawItemState.Selected) != 0; using (SolidBrush bg = new SolidBrush(sel ? Color.FromArgb(80, 84, 96) : Pan)) e.Graphics.FillRectangle(bg, e.Bounds);
                TextRenderer.DrawText(e.Graphics, (string)subs.Items[e.Index], subs.Font, new Rectangle(e.Bounds.X + 10, e.Bounds.Y, e.Bounds.Width - 10, e.Bounds.Height), sel ? Color.White : Txt, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            };
            subs.SelectedIndexChanged += delegate { if (!loading && subs.SelectedIndex >= 0) { sub = subs.SelectedIndex; LoadRows(); } }; Controls.Add(subs);
            rows = new Panel { Left = 218, Top = 58, Width = ClientSize.Width - 228, Height = ClientSize.Height - 112, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, AutoScroll = true, BackColor = Back }; Controls.Add(rows);
            status = new Label { Left = 10, Top = ClientSize.Height - 46, Width = ClientSize.Width - 20, Height = 40, Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, ForeColor = Color.FromArgb(170, 170, 170) }; Controls.Add(status);
            banner = new Label { Left = 230, Top = 66, Width = 560, Height = 52, BackColor = Color.FromArgb(190, 20, 20), ForeColor = Color.White, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter, Visible = false, BorderStyle = BorderStyle.FixedSingle };
            Controls.Add(banner); banner.BringToFront(); banner.Click += delegate { banner.Visible = false; };
            bannerTm = new System.Windows.Forms.Timer { Interval = 15000 }; bannerTm.Tick += delegate { bannerTm.Stop(); ClearBanner(); }; status.Click += delegate { bannerTm.Stop(); ClearBanner(); };
            deb = new System.Windows.Forms.Timer { Interval = 600 }; deb.Tick += delegate { deb.Stop(); Flush(); };
            poll = new System.Windows.Forms.Timer { Interval = 5000 }; poll.Tick += delegate { if (grp >= 0) ReadVisible(false); else FillQuick(); }; poll.Start();
            FormClosed += delegate { poll.Stop(); poll.Dispose(); bannerTm.Stop(); bannerTm.Dispose(); deb.Stop(); inst = null; };
            Shown += delegate { SelectGroup(-1); };
        }

        // essai sur un vrai poste : modifie trois lignes par les contrôles de la fenêtre, relit le poste, remet les valeurs d'origine (--menuprobe=COM8:rapport.txt)
        public static string Probe(Rig r)
        {
            StringBuilder sb = new StringBuilder(); Ftdx10MenuForm f = new Ftdx10MenuForm(r); f.CreateControl();
            string[][] tests = { new[] { "3", "0", "EX040104" }, new[] { "3", "0", "EX040103" }, new[] { "2", "0", "EX030101" } };   // DIMMER LED, SCREEN SAVER, NB WIDTH
            foreach (string[] t in tests)
            {
                int g = int.Parse(t[0]) == 3 ? 3 : 2; f.grp = g; f.sub = int.Parse(t[1]); f.cur.Clear(); f.ed.Clear(); f.cur.AddRange(MenuData.Lines(g, f.sub));
                MenuLine l = f.cur.Find(x => x.Cmd == t[2]); string a0 = r.AskPrefix(l.Cmd + ";", l.Cmd, 1200), o = a0.Substring(a0.IndexOf(l.Cmd) + l.Cmd.Length).TrimEnd(';');
                string n; if (l.Kind == "C") n = l.Ch[(l.Ch.FindIndex(e => e[0] == o) + 1) % l.Ch.Count][0]; else { int v = int.Parse(o); n = l.Encode(v >= l.Max ? v - 1 : v + 1); }
                f.toSend[l] = n; f.Flush(); Thread.Sleep(1500); string a1 = r.AskPrefix(l.Cmd + ";", l.Cmd, 1200), now = a1.Substring(a1.IndexOf(l.Cmd) + l.Cmd.Length).TrimEnd(';');
                sb.AppendLine((now == n ? "OK    " : "ECHEC ") + l.Cmd + " " + l.Name + " : " + o + " -> " + n + " (relu " + now + ")");
                f.toSend[l] = o; f.Flush(); Thread.Sleep(1500); string a2 = r.AskPrefix(l.Cmd + ";", l.Cmd, 1200); sb.AppendLine("      remise : " + a2);
            }
            return sb.ToString();
        }

        // images (--menushot=dossier) : page rapide puis un groupe, avec quelques valeurs d'exemple
        public static void Shot(string dir)
        {
            Rig r = new Rig(); r.Def = Radios.ByKey("ftdx10"); r.V["mic"] = "30"; r.V["meq"] = "1"; r.V["procl"] = "61"; r.V["voxg"] = "0"; r.V["vd"] = "17"; r.V["pwr"] = "5"; r.V["monil"] = "0"; r.V["kr"] = "0"; r.V["bi"] = "0"; r.V["ks"] = "20"; r.V["kp"] = "35"; r.V["sd"] = "10"; r.V["dnf"] = "0";
            Ftdx10MenuForm f = new Ftdx10MenuForm(r); f.StartPosition = FormStartPosition.Manual; f.Location = new Point(-3000, 0); f.Show(); f.SelectGroup(-1); Application.DoEvents(); Thread.Sleep(400); Application.DoEvents();
            Action<string> snap = delegate(string n) { using (Bitmap b = new Bitmap(f.Width, f.Height)) { f.DrawToBitmap(b, new Rectangle(0, 0, f.Width, f.Height)); b.Save(System.IO.Path.Combine(dir, n), System.Drawing.Imaging.ImageFormat.Png); } };
            snap("menu-rapide.png");
            f.SelectGroup(2); f.subs.SelectedIndex = 2; Application.DoEvents(); Thread.Sleep(300); Application.DoEvents();
            foreach (KeyValuePair<MenuLine, Control> kv in f.ed) { f.loading = true; if (kv.Key.Kind == "C" && kv.Key.Ch.Count > 1) f.Show(kv.Key, kv.Key.Ch[1][0]); else if (kv.Key.Kind == "S") f.Show(kv.Key, "+05"); else if (kv.Key.Kind == "N") f.Show(kv.Key, kv.Key.Encode(5)); f.loading = false; }
            f.Banner("Commande non acceptée par le poste (PRMTRC EQ1 LEVEL = -15 dB) — problème de firmware connu.\r\nEntrez la valeur par le menu du poste : OPERATION SETTING > TX AUDIO."); Application.DoEvents(); snap("menu-groupe.png");
            f.Close();
        }

        void Say(string s) { try { if (IsHandleCreated) BeginInvoke(new Action(delegate { status.Text = s; })); } catch { } }
        // refus du poste : bandeau rouge en bas de la fenêtre (15 s, ou clic dessus)
        void Banner(string t) { status.BackColor = Color.FromArgb(190, 20, 20); status.ForeColor = Color.White; status.Font = new Font("Segoe UI", 10f, FontStyle.Bold); status.Text = t; bannerTm.Stop(); bannerTm.Start(); }
        void ClearBanner() { status.BackColor = Back; status.ForeColor = Color.FromArgb(170, 170, 170); status.Font = Font; }

        void SelectGroup(int g)
        {
            grp = g; for (int i = 0; i < 5; i++) { tabs[i].BackColor = i == g + 1 ? Color.FromArgb(40, 70, 150) : Pan; }
            loading = true; subs.Items.Clear(); if (g >= 0) foreach (string s in MenuData.Subs[g]) subs.Items.Add(s); else subs.Items.Add("FONCTIONS"); subs.SelectedIndex = 0; loading = false; sub = 0; LoadRows();
        }

        // ---- page « MENU RAPIDE » : les réglages de la page d'accueil du menu du poste (valeurs lues par la boucle de lecture du programme)
        static readonly string[][] Quick = { new[] { "MIC GAIN", "mic" }, new[] { "MIC EQ", "meq" }, new[] { "PROC LEVEL", "procl" }, new[] { "AMC LEVEL", "@AO" }, new[] { "VOX GAIN", "voxg" }, new[] { "VOX DELAY", "vd" }, new[] { "ANTI VOX", "@AV" },
            new[] { "RF POWER", "pwr" }, new[] { "MONI LEVEL", "monil" }, new[] { "KEYER", "kr" }, new[] { "BK-IN", "bi" }, new[] { "CW SPEED", "ks" }, new[] { "CW PITCH", "kp" }, new[] { "BK-DELAY", "sd" }, new[] { "DNF", "dnf" } };
        void LoadRows()
        {
            foreach (Control c in new List<Control>(rows.Controls.Cast<Control>())) c.Dispose(); rows.Controls.Clear(); ed.Clear(); cur.Clear(); pending.Clear();
            if (grp < 0) { BuildQuick(); return; }
            cur.AddRange(MenuData.Lines(grp, sub)); int y = 4;
            foreach (MenuLine l in cur)
            {
                MenuLine ml = l; Panel row = new Panel { Left = 0, Top = y, Width = Math.Max(600, rows.ClientSize.Width - 20), Height = 38, BackColor = Pan, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top }; rows.Controls.Add(row);
                row.Controls.Add(new Label { Text = l.Name, Left = 12, Top = 8, Width = 330, ForeColor = Color.White, Font = new Font(Font.FontFamily, 10f, FontStyle.Bold) });
                Control c;
                if (l.Kind == "C")
                { ComboBox cb = new ComboBox { Left = 350, Top = 6, Width = 260, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Edit, ForeColor = Acc, FlatStyle = FlatStyle.Flat, Font = new Font(Font.FontFamily, 10f, FontStyle.Bold) }; foreach (string[] e in l.Ch) cb.Items.Add(e[1]); cb.SelectedIndexChanged += delegate { if (!loading && cb.SelectedIndex >= 0) Edited(cb, ml, ml.Ch[cb.SelectedIndex][0]); }; c = cb; }
                else if (l.Kind == "T") { Label t = new Label { Left = 350, Top = 9, Width = 260, ForeColor = Color.FromArgb(150, 150, 150), Text = "(texte : à saisir sur le poste)" }; c = t; }
                else
                {
                    NumericUpDown n = new NumericUpDown { Left = 350, Top = 6, Width = 110, Minimum = l.Min, Maximum = l.Max, Increment = l.Step, BackColor = Edit, ForeColor = Acc, BorderStyle = BorderStyle.FixedSingle, TextAlign = HorizontalAlignment.Center, Font = new Font(Font.FontFamily, 10f, FontStyle.Bold) };
                    if (l.Div > 1) { n.DecimalPlaces = 1; n.Minimum = l.Min / (decimal)l.Div; n.Maximum = l.Max / (decimal)l.Div; n.Increment = l.Step / (decimal)l.Div; }
                    n.ValueChanged += delegate { if (!loading) Edited(n, ml, ml.Encode((int)Math.Round(n.Value * ml.Div))); }; c = n;
                    if (l.Unit.Length > 0) row.Controls.Add(new Label { Text = l.Unit, Left = 468, Top = 9, Width = 60, ForeColor = Color.FromArgb(150, 150, 150) });
                }
                row.Controls.Add(c); ed[ml] = c; y += 42;
            }
            ReadVisible(true);
        }

        // lecture des lignes affichées (une seule lecture à la fois) ; first = première lecture du groupe (message « lecture… »)
        void ReadVisible(bool first)
        {
            if (busy || rig.Loading || cur.Count == 0) return; if (!first && (DateTime.Now - lastEdit).TotalSeconds < 6) return; busy = true; if (first) Say("Lecture des réglages du poste…");
            List<MenuLine> ls = new List<MenuLine>(cur); int g0 = grp, s0 = sub;
            new Thread(delegate()
            {
                Dictionary<MenuLine, string> got = new Dictionary<MenuLine, string>(); int bad = 0;
                foreach (MenuLine l in ls)
                {
                    if (l.Kind == "T") continue; string a = null; for (int i = 0; i < 3 && a == null; i++) a = rig.AskPrefix(l.Cmd + ";", l.Cmd, 700);
                    if (a == null) { bad++; continue; } got[l] = a.Substring(a.IndexOf(l.Cmd) + l.Cmd.Length).TrimEnd(';').Trim();
                }
                try
                {
                    BeginInvoke(new Action(delegate
                    {
                        busy = false; if (g0 != grp || s0 != sub || IsDisposed) return; if (!first && (DateTime.Now - lastEdit).TotalSeconds < 6) return; loading = true;
                        foreach (KeyValuePair<MenuLine, string> kv in got) Show(kv.Key, kv.Value); loading = false;
                        if (first) Say(bad == 0 ? "Valeurs lues dans le poste (relues toutes les 5 s)." : "Certaines valeurs n'ont pas pu être lues (" + bad + ").");
                    }));
                }
                catch { busy = false; }
            }) { IsBackground = true }.Start();
        }
        void Show(MenuLine l, string code)
        {
            Control c; if (!ed.TryGetValue(l, out c)) return;
            if (l.Kind == "C") { ComboBox cb = (ComboBox)c; int i = l.Ch.FindIndex(e => e[0] == code); if (i >= 0) cb.SelectedIndex = i; else if (cb.Items.Count > 0 && code.Length > 0) { cb.SelectedIndex = -1; cb.Text = code; } }
            else if (l.Kind == "N" || l.Kind == "S") { int v; if (int.TryParse(code.Replace("+", ""), out v)) { NumericUpDown n = (NumericUpDown)c; decimal d = v / (decimal)l.Div; n.Value = Math.Max(n.Minimum, Math.Min(n.Maximum, d)); } }
        }

        // ---- modification : envoi différé (0,6 s sans nouvelle frappe), puis relecture de contrôle
        readonly Dictionary<MenuLine, string> toSend = new Dictionary<MenuLine, string>();
        void Edited(Control c, MenuLine l, string code) { lastEdit = DateTime.Now; toSend[l] = code; deb.Stop(); deb.Start(); }
        void Flush()
        {
            foreach (KeyValuePair<MenuLine, string> kv in new List<KeyValuePair<MenuLine, string>>(toSend))
            {
                MenuLine l = kv.Key; string code = kv.Value; lastEdit = DateTime.Now;
                if (rig.Loading) { Say("Le poste est encore en cours de lecture : réglage non envoyé."); continue; }
                rig.Raw(l.Cmd + code + ";"); CatLog.Event(rig.Def.Key, "menu " + l.Name + " = " + l.Label(code));
                new Thread(delegate()
                {
                    Thread.Sleep(700); string a = null; for (int i = 0; i < 3 && a == null; i++) a = rig.AskPrefix(l.Cmd + ";", l.Cmd, 800); if (a == null) return;
                    string v = a.Substring(a.IndexOf(l.Cmd) + l.Cmd.Length).TrimEnd(';').Trim(); int x, y2; bool same = v == code || (int.TryParse(v.Replace("+", ""), out x) && int.TryParse(code.Replace("+", ""), out y2) && x == y2);
                    if (!same) try { BeginInvoke(new Action(delegate { loading = true; Show(l, v); loading = false; Banner("Commande non acceptée par le poste (" + l.Name + " = " + l.Label(code) + ") — problème de firmware connu.\r\nEntrez la valeur par le menu du poste : OPERATION SETTING > TX AUDIO / menu correspondant."); })); } catch { }
                }) { IsBackground = true }.Start();
            }
            toSend.Clear();
        }

        // ---- page rapide : contrôles du programme (MG, PR, PL, VG, VD, PC, ML, KR, BI, KS, KP, SD, BC) et AO / AV
        readonly Dictionary<string, Control> qctl = new Dictionary<string, Control>();
        void BuildQuick()
        {
            qctl.Clear(); int y = 4; loading = true;
            foreach (string[] q in Quick)
            {
                string name = q[0], id = q[1]; Panel row = new Panel { Left = 0, Top = y, Width = Math.Max(600, rows.ClientSize.Width - 20), Height = 38, BackColor = Pan, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top }; rows.Controls.Add(row);
                row.Controls.Add(new Label { Text = name, Left = 12, Top = 8, Width = 330, ForeColor = Color.White, Font = new Font(Font.FontFamily, 10f, FontStyle.Bold) });
                Control c = null; Ctl ctl = id.StartsWith("@") ? null : rig.Def.Find(id);
                if (id.StartsWith("@")) { NumericUpDown n = new NumericUpDown { Left = 350, Top = 6, Width = 110, Minimum = 1, Maximum = 100, BackColor = Edit, ForeColor = Acc, BorderStyle = BorderStyle.FixedSingle, TextAlign = HorizontalAlignment.Center, Font = new Font(Font.FontFamily, 10f, FontStyle.Bold) }; string cmd = id.Substring(1); n.ValueChanged += delegate { if (!loading) { lastEdit = DateTime.Now; QueueQuick(cmd, (int)n.Value); } }; c = n; }
                else if (ctl == null) continue;
                else if (ctl.K == Kind.Knob) { NumericUpDown n = new NumericUpDown { Left = 350, Top = 6, Width = 110, Minimum = ctl.Min, Maximum = ctl.Max, Increment = Math.Max(1, ctl.Step), BackColor = Edit, ForeColor = Acc, BorderStyle = BorderStyle.FixedSingle, TextAlign = HorizontalAlignment.Center, Font = new Font(Font.FontFamily, 10f, FontStyle.Bold) }; n.ValueChanged += delegate { if (!loading) { lastEdit = DateTime.Now; rig.Set(ctl, ((int)n.Value).ToString()); } }; c = n; }
                else if (ctl.K == Kind.Toggle) { ComboBox cb = new ComboBox { Left = 350, Top = 6, Width = 110, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Edit, ForeColor = Acc, FlatStyle = FlatStyle.Flat, Font = new Font(Font.FontFamily, 10f, FontStyle.Bold) }; cb.Items.Add("OFF"); cb.Items.Add("ON"); cb.SelectedIndexChanged += delegate { if (!loading && cb.SelectedIndex >= 0) { lastEdit = DateTime.Now; rig.Set(ctl, cb.SelectedIndex == 1 ? ctl.OnSend : ctl.OffSend); } }; c = cb; }
                else continue;
                row.Controls.Add(c); qctl[id] = c; y += 42;
            }
            loading = false; FillQuick(); QuickReadExtra(); Say("Réglages rapides : valeurs lues par le programme.");
        }
        void FillQuick()
        {
            if (grp >= 0 || (DateTime.Now - lastEdit).TotalSeconds < 4) return; bool was = loading; loading = true;
            foreach (KeyValuePair<string, Control> kv in qctl)
            {
                if (kv.Key.StartsWith("@")) continue; Ctl ctl = rig.Def.Find(kv.Key); string v = rig.Get(kv.Key); if (ctl == null || v == null) continue;
                NumericUpDown n = kv.Value as NumericUpDown; ComboBox cb = kv.Value as ComboBox; int x;
                if (n != null && int.TryParse(v, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out x)) n.Value = Math.Max(n.Minimum, Math.Min(n.Maximum, x));
                else if (cb != null) cb.SelectedIndex = ctl.IsOn(v) ? 1 : 0;
            }
            loading = was; QuickReadExtra();
        }
        DateTime lastExtra = DateTime.MinValue;
        void QuickReadExtra()
        {
            if ((DateTime.Now - lastExtra).TotalSeconds < 4 || rig.Loading) return; lastExtra = DateTime.Now;
            new Thread(delegate()
            {
                foreach (string cmd in new[] { "AO", "AV" })
                {
                    string a = null; for (int i = 0; i < 2 && a == null; i++) a = rig.AskPrefix(cmd + ";", cmd, 700); int v; if (a == null || !int.TryParse(a.Substring(a.IndexOf(cmd) + 2).TrimEnd(';'), out v)) continue; string c2 = cmd;
                    try { BeginInvoke(new Action(delegate { Control c; if (grp < 0 && (DateTime.Now - lastEdit).TotalSeconds >= 4 && qctl.TryGetValue("@" + c2, out c)) { loading = true; NumericUpDown n = (NumericUpDown)c; n.Value = Math.Max(n.Minimum, Math.Min(n.Maximum, v)); loading = false; } })); } catch { }
                }
            }) { IsBackground = true }.Start();
        }
        void QueueQuick(string cmd, int v)
        {
            if (rig.Loading) return; if (qdeb == null) { qdeb = new System.Windows.Forms.Timer { Interval = 500 }; qdeb.Tick += delegate { qdeb.Stop(); foreach (KeyValuePair<string, int> kv in new List<KeyValuePair<string, int>>(qpend)) rig.Raw(kv.Key + kv.Value.ToString("000") + ";"); qpend.Clear(); }; }
            qpend[cmd] = v; qdeb.Stop(); qdeb.Start();
        }
        System.Windows.Forms.Timer qdeb; readonly Dictionary<string, int> qpend = new Dictionary<string, int>();
    }
}
